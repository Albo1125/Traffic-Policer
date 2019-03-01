using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage.Native;
using Traffic_Policer.Extensions;
using Albo1125.Common.CommonLibrary;
using static Albo1125.Common.CommonLibrary.ExtensionMethods;

namespace Traffic_Policer.Callouts
{
    [CalloutInfo("DrugsRunners", CalloutProbability.Medium)]
    internal class DrugsRunners : Callout
    {
        private Vehicle car;
        private Ped driver;
        private Ped passenger;
        private bool isPlayerCheatingTrafficStop = false;
        private bool taseredOfficer = false;

        private string[] drugsModels = new string[] { "PROP_WEED_BLOCK_01","PROP_DRUG_PACKAGE_02", "PROP_DRUG_PACKAGE", "PROP_MP_DRUG_PACK_RED", "PROP_MP_DRUG_PACK_BLUE", "PROP_COKE_BLOCK_01", "PROP_COKE_BLOCK_HALF_B", "PROP_COKE_BLOCK_HALF_A" };
        private Rage.Object drugs1; 
        private Rage.Object drugs2;
        private Rage.Object drugs3;
        private Rage.Object drugs4;
        private Rage.Object drugs5;
        private Rage.Object money; //MODEL: "PROP_CASH_PILE_01"
        private SpawnPoint spawnPoint;
        private Blip driverBlip;
        private LHandle pursuit;
        private bool calloutStarted = false;
        private bool calloutFinished;
        private string[] sportsCars = { "SULTAN", "EXEMPLAR", "F620", "FELON2", "FELON", "SENTINEL", "WINDSOR", "DOMINATOR", "DUKES", "GAUNTLET", "VIRGO", "ADDER", "BUFFALO", "ZENTORNO", "MASSACRO",
        "DOMINATOR", "ASTEROPE", "PRAIRIE", "NINEF", "WASHINGTON", "CHINO", "CASCO", "INFERNUS", "ZTYPE", "DILETTANTE", "VIRGO"};
        private string[] vowels = new string[] { "a", "e", "o", "i", "u" };
        private string[] numbers = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        private Model carModel;
        private Vehicle boat1;
        private Ped invisBoatPed;
        private List<Blip> remindLocations = new List<Blip>();

        private Camera cam;
        private string driverName;
        private string passengerName;
        bool driverDead;
        bool passengerDead;
        bool driverArrested;
        bool passengerArrested;

        private bool drugs1Found = false;
        private bool drugs2Found = false;
        private bool drugs1Created = false;
        private bool drugs2Created = false;
        private bool drugs3Created = false;
        private bool drugs3Found = false;
        private bool drugs4Found = false;
        private bool drugs4Created = false;
        private bool drugs5Found = false;
        private bool drugs5Created = false;
        private bool moneyCreated = false;
        private bool moneyFound = false;
        private int difference;

        private int upperSituationNumber;
        private bool displayedModelName = false;
        private string scannerMessage;
        private string descriptionOwnerWanted;
        private bool displayCode4Message = true;
        private int cheatCount = 0;


        //private Vector3 ElysianIsland = new Vector3(-458.361f,-2224.029f,-0.06659801f);
        //private float ElysianIslandHeading = 170.348f;
        //private Vector3 ElysianIslandFleePoint = new Vector3(-620.7472f, -3323.048f, 2.193986f);
        //private Vector3 ElysianIslandDrivePoint = new Vector3(-472.6364f, -2203.34f, 7.740581f);
        

        private void determineWhyOwnerIsWanted()
        {
            int reasonOwnerWanted = TrafficPolicerHandler.rnd.Next(2);
            if (reasonOwnerWanted == 0)
            {
                descriptionOwnerWanted = "international drug dealing";
                scannerMessage = "CRIME_PWITS";

            }
            else if (reasonOwnerWanted == 1)
            {
                descriptionOwnerWanted = "drugs being transported internationally";
                scannerMessage = "CRIME_NARCOTICS_IN_TRANSIT";
            }
        }

        public override bool OnBeforeCalloutDisplayed()
        {
            Game.LogTrivial("TrafficPolicer.DrugsRunners");
            int WaitCount = 0;
            while (!World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(220f, 400f)).GetClosestVehicleSpawnPoint(out spawnPoint))
            {
                GameFiber.Yield();
                WaitCount++;
                if (WaitCount > 10) { return false; }
            }


            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 70f);
            determineWhyOwnerIsWanted();
            carModel = new Model(sportsCars[TrafficPolicerHandler.rnd.Next(sportsCars.Length)]);
            carModel.LoadAndWait();
            CalloutMessage = "~o~ANPR Hit: ~b~Known ~r~drugs runners ~b~in vehicle.";
            CalloutPosition = spawnPoint;
            Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + TrafficPolicerHandler.DivisionUnitBeatAudioString + " WE_HAVE_01 CRIME_TRAFFIC_ALERT IN_OR_ON_POSITION FOR " + scannerMessage, spawnPoint);
            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted()
        {
            TrafficPolicerHandler.isOwnerWantedCalloutRunning = true;
            car = new Vehicle(carModel, spawnPoint.Position, spawnPoint.Heading);
            car.RandomiseLicencePlate();
            driver = car.CreateRandomDriver();
            driver.MakeMissionPed();
            while (!driver.Exists())
            {
                GameFiber.Yield();
            }
            TrafficPolicerHandler.driversConsidered.Add(driver);

            
            car.IsPersistent = true;
            //driver.WarpIntoVehicle(car, -1);
            driverBlip = driver.AttachBlip();
            driverBlip.Scale = 0.7f;


            Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~o~ANPR Hit: ~r~Drugs Runners", "Dispatch to ~b~" + TrafficPolicerHandler.DivisionUnitBeat, "The ~o~ANPR Hit ~s~is for ~r~" + descriptionOwnerWanted + ". ~b~Use appropriate caution.");



            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            if (driver.Exists()) { driver.Delete(); }
            if (passenger.Exists()) { passenger.Delete(); }
            if (car.Exists()) { car.Delete(); }
            if (TrafficPolicerHandler.OtherUnitRespondingAudio)
            {
                Functions.PlayScannerAudio("OTHER_UNIT_TAKING_CALL");
            }
        }

        public override void Process()
        {
            base.Process();
            if (!calloutStarted)
            {
                //Situations
                int situationNumber = TrafficPolicerHandler.rnd.Next(4);
                
                if (situationNumber == 0)
                {
                    situationTwo();
                }
                else if (situationNumber == 1)
                {
                    situationThree();
                }
                else if (situationNumber == 2)
                {
                    situationFour();
                }
                else if (situationNumber == 3)
                {
                    situationFive();
                }
            }

            if (!displayedModelName)
            {
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 35f)
                {
                    string modelName = carModel.Name.ToLower();
                    string article;
                    if (vowels.Contains<string>(modelName[0].ToString()))
                    {
                        article = "an";
                    }
                    else { article = "a"; }

                    if (numbers.Contains<string>(modelName.Last().ToString()))
                    {
                        modelName = modelName.Substring(0, modelName.Length - 1);
                    }
                    modelName = char.ToUpper(modelName[0]) + modelName.Substring(1);
                    //Game.DisplayNotification("The vehicle is reported to be " + article + " ~r~" + modelName + ".");
                    Game.DisplayHelp("Perform a ~g~traffic stop ~s~on the target ~r~" + modelName + ".");
                    if (TrafficPolicerHandler.dispatchCautionMessages)
                    {
                        
                        Functions.PlayScannerAudio("DISP_ATTENTION_UNIT " + TrafficPolicerHandler.DivisionUnitBeatAudioString + " APPROACH_WITH_CAUTION");
                    }
                    displayedModelName = true;
                }
            }
            if (TrafficPolicerHandler.driverChangedDueToKeys)
            {
                if (car.Exists())
                {
                    if (car.HasDriver)
                    {
                        driver = car.Driver;
                    }
                    TrafficPolicerHandler.driverChangedDueToKeys = false;
                }
            }
            if (calloutFinished)
            {
                End();
            }
            if (Game.LocalPlayer.Character.IsDead)
            {
                Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                End();
            }



        }
        /// <summary>
        /// Final search for drugs by player
        /// </summary>
        private void searchForDrugs()
        {
            if (!TrafficPolicerHandler.isOwnerWantedCalloutRunning) { return; }
            uint noti = Game.DisplayNotification("Did you see any ~r~drug dealing evidence ~s~being disposed of? ~h~~b~Y/N");
            int waitCount = 0;
            while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
            {
                GameFiber.Yield();
                waitCount++;
                
                if (waitCount >= 800)
                {
                    Game.LogTrivial("Waitcount reached, yes!");
                    break;
                }
                if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(System.Windows.Forms.Keys.Y))
                {
                    try
                    {
                        Game.RemoveNotification(noti);
                    }
                    catch (Exception e) { }
                    break;
                }
                else if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(System.Windows.Forms.Keys.N)) 
                {
                    try
                    {
                        Game.RemoveNotification(noti);
                    }
                    catch (Exception e) { }
                    return;
                }
            }
            foreach (Blip blip in remindLocations)
            {
                if (blip.Exists())
                {
                    blip.Scale = 1f;
                    blip.Flash(100, 3000);
                }
            }
            Game.DisplayNotification("Collect the ~r~drug dealing evidence. ~s~When you're done, ~b~hold 0~s~ to present it to ~b~court.");
            while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
            {
                GameFiber.Yield();
                if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(TrafficPolicerHandler.courtKey))
                {
                    GameFiber.Sleep(50);
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(TrafficPolicerHandler.courtKey))
                    {
                        GameFiber.Sleep(600);
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(TrafficPolicerHandler.courtKey))
                        {
                            
                            break;
                        }
                        else
                        {
                            Game.DisplayNotification("Hold down ~b~"+ TrafficPolicerHandler.kc.ConvertToString(TrafficPolicerHandler.courtKey)+"~s~ to present evidence to court.");
                        }
                    }
                }
                if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                {
                    if (drugs1.Exists())
                    {
                        if (Vector3.Distance(drugs1.Position, Game.LocalPlayer.Character.Position) < 1.5f)
                        {
                            Rage.Native.NativeFunction.Natives.PLAY_SOUND_FRONTEND(-1, "PURCHASE", "HUD_LIQUOR_STORE_SOUNDSET", 1);
                            drugs1.Delete();
                            drugs1Found = true;
                            Game.DisplayNotification("You found a quantity of ~r~drugs.");
                            Game.DisplayNotification("When you're done, ~b~hold down " + TrafficPolicerHandler.courtKey + "~s~ to present evidence to ~b~court.");
                            if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                            {
                                API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "Drugs Runners - Pieces of evidence found");
                            }
                        }
                    }

                    if (drugs2.Exists())
                    {
                        if (Vector3.Distance(drugs2.Position, Game.LocalPlayer.Character.Position) < 1.5f)
                        {
                            Rage.Native.NativeFunction.Natives.PLAY_SOUND_FRONTEND(-1, "PURCHASE", "HUD_LIQUOR_STORE_SOUNDSET", 1);
                            drugs2.Delete();
                            drugs2Found = true;
                            Game.DisplayNotification("You found a quantity of ~r~drugs.");
                            Game.DisplayNotification("When you're done, ~b~hold down " + TrafficPolicerHandler.courtKey + "~s~ to present evidence to ~b~court.");
                            if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                            {
                                API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "Drugs Runners - Pieces of evidence found");
                            }
                        }
                    }
                    if (drugs3.Exists())
                    {
                        if (Vector3.Distance(drugs3.Position, Game.LocalPlayer.Character.Position) < 1.5f)
                        {
                            Rage.Native.NativeFunction.Natives.PLAY_SOUND_FRONTEND(-1, "PURCHASE", "HUD_LIQUOR_STORE_SOUNDSET", 1);
                            drugs3.Delete();
                            drugs3Found = true;
                            Game.DisplayNotification("You found a quantity of ~r~drugs.");
                            Game.DisplayNotification("When you're done, ~b~hold down " + TrafficPolicerHandler.courtKey + "~s~ to present evidence to ~b~court.");
                            if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                            {
                                API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "Drugs Runners - Pieces of evidence found");
                            }
                        }
                    }
                    if (drugs4.Exists())
                    {
                        if (Vector3.Distance(drugs4.Position, Game.LocalPlayer.Character.Position) < 1.5f)
                        {
                            Rage.Native.NativeFunction.Natives.PLAY_SOUND_FRONTEND(-1, "PURCHASE", "HUD_LIQUOR_STORE_SOUNDSET", 1);
                            drugs4.Delete();
                            drugs4Found = true;
                            Game.DisplayNotification("You found a quantity of ~r~drugs.");
                            Game.DisplayNotification("When you're done, ~b~hold down " + TrafficPolicerHandler.courtKey + "~s~ to present evidence to ~b~court.");
                            if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                            {
                                API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "Drugs Runners - Pieces of evidence found");
                            }
                        }
                    }
                    if (drugs5.Exists())
                    {
                        if (Vector3.Distance(drugs5.Position, Game.LocalPlayer.Character.Position) < 1.5f)
                        {
                            Rage.Native.NativeFunction.Natives.PLAY_SOUND_FRONTEND(-1, "PURCHASE", "HUD_LIQUOR_STORE_SOUNDSET", 1);
                            drugs5.Delete();
                            drugs5Found = true;
                            Game.DisplayNotification("You found a quantity of ~r~drugs.");
                            Game.DisplayNotification("When you're done, ~b~hold down " + TrafficPolicerHandler.courtKey + "~s~ to present evidence to ~b~court.");
                            if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                            {
                                API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "Drugs Runners - Pieces of evidence found");
                            }
                        }
                    }
                    if (money.Exists())
                    {
                        if (Vector3.Distance(money.Position, Game.LocalPlayer.Character.Position) < 1.5f)
                        {
                            Rage.Native.NativeFunction.Natives.PLAY_SOUND_FRONTEND(-1, "PURCHASE", "HUD_LIQUOR_STORE_SOUNDSET", 1);
                            money.Delete();
                            moneyFound = true;
                            Game.DisplayNotification("You found a large amount of ~r~cash.");
                            Game.DisplayNotification("When you're done, ~b~hold down " + TrafficPolicerHandler.courtKey + "~s~ to present evidence to ~b~court.");
                            if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                            {
                                API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "Drugs Runners - Pieces of evidence found");
                            }
                        }
                    }
                }
            }
            
        }


        /// <summary>
        /// Common method for the final pursuit in which drugs are dropped
        /// </summary>
        private void drugsPursuit()
        {

            //driver = ClonePed(driver);
            if (!TrafficPolicerHandler.isOwnerWantedCalloutRunning) { return; }
            driverName = Functions.GetPersonaForPed(driver).FullName;

            if (passenger.Exists())
            {
                //passenger = ClonePed(passenger);
                passengerName = Functions.GetPersonaForPed(passenger).FullName;
                if (!passenger.IsInVehicle(car, false))
                {
                    passenger.WarpIntoVehicle(car, 0);
                }
            }
            if (!driver.IsInVehicle(car, false))
            {
                driver.WarpIntoVehicle(car, -1);

            }
            driver.PlayAmbientSpeech("GENERIC_CURSE_HIGH", true);
            driver.Tasks.PerformDrivingManeuver(VehicleManeuver.BurnOut).WaitForCompletion(500);
            if (!driver.IsInVehicle(car, false))
            {
                driver.WarpIntoVehicle(car, -1);
            }
            if (passenger.Exists())
            {
                if (!passenger.IsInVehicle(car, false))
                {
                    passenger.WarpIntoVehicle(car, -1);
                }
            }
            if (Functions.IsPlayerPerformingPullover()) { Functions.ForceEndCurrentPullover(); }
            
            pursuit = Functions.CreatePursuit();
            Functions.AddPedToPursuit(pursuit, driver);
            if (passenger.Exists())
            {
                Functions.AddPedToPursuit(pursuit, passenger);
            }
            Functions.SetPursuitIsActiveForPlayer(pursuit, true);
            Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
            driverArrested = false;
            passengerArrested = false;
            driverDead = false;
            passengerDead = false;
            bool doneWithDriver = false;
            bool doneWithPassenger = false;
            bool outofloop = false;
            GameFiber.StartNew(delegate
            {
                Game.DisplayNotification("You can press ~b~"+ TrafficPolicerHandler.kc.ConvertToString(TrafficPolicerHandler.markMapKey)+" ~s~to mark any ~r~evidence ~s~thrown and collect it later.");
                while (!outofloop)
                {
                    GameFiber.Yield();
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(TrafficPolicerHandler.markMapKey))
                    {
                        Blip blip = new Blip(Game.LocalPlayer.Character.Position);
                        blip.Color = System.Drawing.Color.Green;
                        blip.Scale = 0.8f;
                        remindLocations.Add(blip);
                    }
                    if (!TrafficPolicerHandler.isOwnerWantedCalloutRunning) { break; }
                }
            });
            int timeSinceLastThrow = 0;
            while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
            {
                GameFiber.Sleep(500);
                timeSinceLastThrow++;
                if (timeSinceLastThrow > 33) {
                    if (car.Exists())
                    {
                        if (car.HasDriver)
                        {
                            if (car.Driver.IsAlive)
                            {
                                int randomNumber = 65 - (int)Math.Round(Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position));
                                if (randomNumber < 20) { randomNumber = 20; }
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 10f)
                                {
                                    randomNumber = 80;
                                }

                                if (!drugs1.Exists())
                                {
                                    if (TrafficPolicerHandler.rnd.Next(randomNumber) == 1)
                                    {
                                        string drugsmodelstring = drugsModels[TrafficPolicerHandler.rnd.Next(drugsModels.Length)];
                                        Game.LogTrivial(drugsmodelstring);
                                        Model drugsmodel = new Model(drugsmodelstring);
                                        drugsmodel.LoadAndWait();



                                        if (TrafficPolicerHandler.rnd.Next(2) == 1)
                                        {
                                            Vector3 pos = car.GetOffsetPosition(Vector3.RelativeLeft * 1.7f);
                                            pos.Z += 1f;
                                            drugs1 = new Rage.Object(drugsmodel, pos);
                                        }
                                        else
                                        {
                                            Vector3 pos = car.GetOffsetPosition(Vector3.RelativeRight * 1.7f);
                                            pos.Z += 1f;
                                            drugs1 = new Rage.Object(drugsmodel, pos);
                                        }

                                        drugs1.IsPersistent = true;

                                        drugs1Created = true;
                                        Game.LogTrivial("New Drugs 1 created");
                                        timeSinceLastThrow = 0;
                                        continue;

                                    }
                                }
                                GameFiber.Yield();
                                if (!drugs2.Exists())
                                {
                                    if (TrafficPolicerHandler.rnd.Next(randomNumber) == 1)
                                    {
                                        string drugsmodelstring = drugsModels[TrafficPolicerHandler.rnd.Next(drugsModels.Length)];
                                        Game.LogTrivial(drugsmodelstring);
                                        Model drugsmodel = new Model(drugsmodelstring);
                                        drugsmodel.LoadAndWait();



                                        if (TrafficPolicerHandler.rnd.Next(2) == 1)
                                        {
                                            Vector3 pos = car.GetOffsetPosition(Vector3.RelativeLeft * 1.7f);
                                            pos.Z += 1f;
                                            drugs2 = new Rage.Object(drugsmodel, pos);
                                        }
                                        else
                                        {
                                            Vector3 pos = car.GetOffsetPosition(Vector3.RelativeRight * 1.7f);
                                            pos.Z += 1f;
                                            drugs2 = new Rage.Object(drugsmodel, pos);
                                        }
                                        drugs2.IsPersistent = true;

                                        drugs2Created = true;
                                        Game.LogTrivial("New Drugs 2 created");
                                        timeSinceLastThrow = 0;
                                        continue;

                                    }
                                }
                                if (!drugs3.Exists())
                                {
                                    if (TrafficPolicerHandler.rnd.Next(randomNumber) == 1)
                                    {
                                        string drugsmodelstring = drugsModels[TrafficPolicerHandler.rnd.Next(drugsModels.Length)];
                                        Game.LogTrivial(drugsmodelstring);
                                        Model drugsmodel = new Model(drugsmodelstring);
                                        drugsmodel.LoadAndWait();



                                        if (TrafficPolicerHandler.rnd.Next(2) == 1)
                                        {
                                            Vector3 pos = car.GetOffsetPosition(Vector3.RelativeLeft * 1.7f);
                                            pos.Z += 1f;
                                            drugs3 = new Rage.Object(drugsmodel, pos);
                                        }
                                        else
                                        {
                                            Vector3 pos = car.GetOffsetPosition(Vector3.RelativeRight * 1.7f);
                                            pos.Z += 1f;
                                            drugs3 = new Rage.Object(drugsmodel, pos);
                                        }
                                        drugs3.IsPersistent = true;

                                        drugs3Created = true;
                                        Game.LogTrivial("New Drugs 3 created");
                                        timeSinceLastThrow = 0;
                                        continue;

                                    }
                                }
                                if (!drugs4.Exists())
                                {
                                    if (TrafficPolicerHandler.rnd.Next(randomNumber) == 1)
                                    {
                                        string drugsmodelstring = drugsModels[TrafficPolicerHandler.rnd.Next(drugsModels.Length)];
                                        Game.LogTrivial(drugsmodelstring);
                                        Model drugsmodel = new Model(drugsmodelstring);
                                        drugsmodel.LoadAndWait();



                                        if (TrafficPolicerHandler.rnd.Next(2) == 1)
                                        {
                                            Vector3 pos = car.GetOffsetPosition(Vector3.RelativeLeft * 1.7f);
                                            pos.Z += 1f;
                                            drugs4 = new Rage.Object(drugsmodel, pos);
                                        }
                                        else
                                        {
                                            Vector3 pos = car.GetOffsetPosition(Vector3.RelativeRight * 1.7f);
                                            pos.Z += 1f;
                                            drugs4 = new Rage.Object(drugsmodel, pos);
                                        }
                                        drugs4.IsPersistent = true;

                                        drugs4Created = true;
                                        Game.LogTrivial("New Drugs 4 created");
                                        timeSinceLastThrow = 0;
                                        continue;

                                    }
                                }
                                if (!drugs5.Exists())
                                {
                                    if (TrafficPolicerHandler.rnd.Next(randomNumber) == 1)
                                    {
                                        string drugsmodelstring = drugsModels[TrafficPolicerHandler.rnd.Next(drugsModels.Length)];
                                        Game.LogTrivial(drugsmodelstring);
                                        Model drugsmodel = new Model(drugsmodelstring);
                                        drugsmodel.LoadAndWait();



                                        if (TrafficPolicerHandler.rnd.Next(2) == 1)
                                        {
                                            Vector3 pos = car.GetOffsetPosition(Vector3.RelativeLeft * 1.7f);
                                            pos.Z += 1f;
                                            drugs5 = new Rage.Object(drugsmodel, pos);
                                        }
                                        else
                                        {
                                            Vector3 pos = car.GetOffsetPosition(Vector3.RelativeRight * 1.7f);
                                            pos.Z += 1f;
                                            drugs5 = new Rage.Object(drugsmodel, pos);
                                        }
                                        drugs5.IsPersistent = true;

                                        drugs5Created = true;
                                        Game.LogTrivial("New Drugs 5 created");
                                        timeSinceLastThrow = 0;
                                        continue;

                                    }
                                }
                                if (!money.Exists())
                                {
                                    if (TrafficPolicerHandler.rnd.Next(randomNumber) == 1)
                                    {
                                        string moneystring = "PROP_CASH_PILE_01";
                                        Game.LogTrivial(moneystring);
                                        Model moneymodel = new Model(moneystring);
                                        moneymodel.LoadAndWait();



                                        if (TrafficPolicerHandler.rnd.Next(2) == 1)
                                        {
                                            Vector3 pos = car.GetOffsetPosition(Vector3.RelativeLeft * 1.7f);
                                            pos.Z += 1f;
                                            money = new Rage.Object(moneymodel, pos);
                                        }
                                        else
                                        {
                                            Vector3 pos = car.GetOffsetPosition(Vector3.RelativeRight * 1.7f);
                                            pos.Z += 1f;
                                            money = new Rage.Object(moneymodel, pos);
                                        }
                                        money.IsPersistent = true;

                                        moneyCreated = true;
                                        Game.LogTrivial("New money created");
                                        timeSinceLastThrow = 0;
                                        continue;

                                    }
                                }
                            }
                        }
                    }
                }


                GameFiber.Yield();
                if (driver.Exists())
                {
                    if (driver.IsDead)
                    {
                        driverDead = true;
                        doneWithDriver = true;
                    }
                    if (Functions.IsPedArrested(driver))
                    {
                        driverArrested = true;
                        doneWithDriver = true;
                        Game.LogTrivial("Driver arrested");
                        
                        
                    }

                }
                else
                {
                    doneWithDriver = true;
                    Game.LogTrivial("Driver doesn't exist");
                }
                if (passengerName != null)
                {
                    if (passenger.Exists())
                    {
                        if (passenger.IsDead)
                        {
                            passengerDead = true;
                            doneWithPassenger = true;
                        }
                        if (Functions.IsPedArrested(passenger))
                        {
                            passengerArrested = true;
                            doneWithPassenger = true;
                        }
                    }
                    else
                    {
                        doneWithPassenger = true;
                        Game.LogTrivial("Passenger doesn't exist");
                    }
                }

                if (Game.LocalPlayer.Character.IsDead)
                {
                    Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                    break;
                }
                if (doneWithDriver && doneWithPassenger)
                {
                    break;
                }
                
                if (!Functions.IsPursuitStillRunning(pursuit))
                {
                    Game.LogTrivial("PURSUIT ENDED");
                    break;
                }


                


            }


            if (driver.Exists())
            {
                if (driver.IsDead)
                {
                    driverDead = true;

                }
                if (Functions.IsPedArrested(driver))
                {
                    driverArrested = true;
                    if (!drugs1Created && !drugs2Created && !drugs3Created && !drugs4Created && !drugs5Created && !moneyCreated)
                    {
                        string drugsmodelstring = drugsModels[TrafficPolicerHandler.rnd.Next(drugsModels.Length)];
                        Game.LogTrivial(drugsmodelstring);
                        Model drugsmodel = new Model(drugsmodelstring);
                        drugsmodel.LoadAndWait();
                        Vector3 pos = driver.GetOffsetPosition(Vector3.RelativeFront * 1f);
                        pos.Z += 1f;
                        drugs1 = new Rage.Object(drugsmodel, pos);
                        drugs1.IsPersistent = true;

                        drugs1Created = true;
                        Game.LogTrivial("New Drugs 1 created");
                        GameFiber.Sleep(4000);
                    }

                }
            }
            if (passenger.Exists())
            {
                if (passenger.IsDead)
                {
                    passengerDead = true;

                }
                if (Functions.IsPedArrested(passenger))
                {
                    passengerArrested = true;
                    if (!drugs1Created && !drugs2Created && !drugs3Created && !drugs4Created && !drugs5Created && !moneyCreated)
                    {
                        string drugsmodelstring = drugsModels[TrafficPolicerHandler.rnd.Next(drugsModels.Length)];
                        Game.LogTrivial(drugsmodelstring);
                        Model drugsmodel = new Model(drugsmodelstring);
                        drugsmodel.LoadAndWait();
                        Vector3 pos = passenger.GetOffsetPosition(Vector3.RelativeFront * 1f);
                        pos.Z += 1f;
                        drugs1 = new Rage.Object(drugsmodel, pos);
                        drugs1.IsPersistent = true;

                        drugs1Created = true;
                        Game.LogTrivial("New Drugs 1 created");
                        GameFiber.Sleep(4000);
                    }


                }
            }
            outofloop = true;
            Game.LogTrivial("Out of pursuit method.");
            GameFiber.Yield();
        }



        private void situationTwo()
        {
            calloutStarted = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);
                    driver.Armor += 55;
                    driver.Health += 120;
                    
                    try
                    {
                        car.Mods.InstallModKit();

                        car.Mods.EngineModIndex = car.Mods.EngineModCount - 1;


                        car.Mods.ExhaustModIndex = car.Mods.ExhaustModCount - 1;

                        car.Mods.TransmissionModIndex = car.Mods.TransmissionModCount - 1;

                        VehicleWheelType wheelType = MathHelper.Choose(VehicleWheelType.Sport, VehicleWheelType.SUV, VehicleWheelType.HighEnd);
                        int wheelModIndex = MathHelper.GetRandomInteger(car.Mods.GetWheelModCount(wheelType));
                        car.Mods.SetWheelMod(wheelType, wheelModIndex, true);

                        car.Mods.HasTurbo = true;

                        car.Mods.HasXenonHeadlights = true;
                    }
                    catch (Exception e)
                    {

                    }
                    Rage.Native.NativeFunction.Natives.SET_DRIVER_ABILITY(driver, 1.0f);
                    driverBlip.EnableRoute(System.Drawing.Color.Red);
                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();
                    if (driverBlip.Exists())
                    {
                        driverBlip.DisableRoute();
                        driverBlip.Delete();
                    }

                    int waitCount = 0;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        waitCount++;
                        GameFiber.Sleep(10);
                        if (!Functions.IsPlayerPerformingPullover())
                        {
                            break;
                        }
                        if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) < 5f)
                            {
                                break;
                            }
                        }
                        if (!driver.IsInVehicle(car, false))
                        {
                            driver.WarpIntoVehicle(car, -1);
                            break;
                        }
                        if (waitCount >= 2400)
                        {
                            break;
                        }
                        if (isPlayerCheatingTrafficStop)
                        {
                            break;
                        }
                    }
                    drugsPursuit();
                    searchForDrugs();
                    createDrugsEndMessage();
                    if (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        calloutFinished = true;
                    }
                    End();
                }
                catch (System.Threading.ThreadAbortException e)
                {
                    End();
                }

                catch (Exception e)
                {
                    if (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        Game.LogTrivial(e.ToString());
                        Game.LogTrivial("Traffic Policer handled the exception successfully.");
                        Game.DisplayNotification("~O~ANPR Hit ~s~callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }

                }
            });
        }
        private void situationThree()
        {
            calloutStarted = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    passenger = new Ped(spawnPoint);
                    passenger.BlockPermanentEvents = true;
                    passenger.WarpIntoVehicle(car, 0);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);
                    driver.Armor += 55;
                    driver.Health += 120;
                    try
                    {
                        car.Mods.InstallModKit();

                        car.Mods.EngineModIndex = car.Mods.EngineModCount - 1;


                        car.Mods.ExhaustModIndex = car.Mods.ExhaustModCount - 1;

                        car.Mods.TransmissionModIndex = car.Mods.TransmissionModCount - 1;

                        VehicleWheelType wheelType = MathHelper.Choose(VehicleWheelType.Sport, VehicleWheelType.SUV, VehicleWheelType.HighEnd);
                        int wheelModIndex = MathHelper.GetRandomInteger(car.Mods.GetWheelModCount(wheelType));
                        car.Mods.SetWheelMod(wheelType, wheelModIndex, true);

                        car.Mods.HasTurbo = true;

                        car.Mods.HasXenonHeadlights = true;
                    }
                    catch (Exception e)
                    {

                    }
                    Rage.Native.NativeFunction.Natives.SET_DRIVER_ABILITY(driver, 1.0f);
                    driverBlip.EnableRoute(System.Drawing.Color.Red);
                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();
                    if (driverBlip.Exists())
                    {
                        driverBlip.DisableRoute();
                        driverBlip.Delete();
                    }

                    int waitCount = 0;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        waitCount++;
                        GameFiber.Sleep(10);
                        if (!Functions.IsPlayerPerformingPullover())
                        {
                            break;
                        }
                        if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) < 5f)
                            {
                                break;
                            }
                        }
                        if (!driver.IsInVehicle(car, false))
                        {
                            driver.WarpIntoVehicle(car, -1);
                            break;
                        }
                        if (waitCount >= 2400)
                        {
                            break;
                        }
                        if (isPlayerCheatingTrafficStop)
                        {
                            break;
                        }
                    }
                    drugsPursuit();
                    searchForDrugs();
                    createDrugsEndMessage();
                    if (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        calloutFinished = true;
                    }
                    End();
                }
                catch (System.Threading.ThreadAbortException e)
                {
                    End();
                }
                catch (Exception e)
                {
                    if (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        Game.LogTrivial(e.ToString());
                        Game.LogTrivial("Traffic Policer handled the exception successfully.");
                        Game.DisplayNotification("~O~ANPR Hit ~s~callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }

                }
            });
        }
        private void situationFour()
        {

            calloutStarted = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    //Add Passenger

                    passenger = new Ped(spawnPoint);
                    passenger.BlockPermanentEvents = true;
                    passenger.WarpIntoVehicle(car, 0);


                    driverBlip.EnableRoute(System.Drawing.Color.Red);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(passenger, 3);

                    driver.Armor += 50;
                    passenger.Armor += 50;
                    driver.Health += 130;
                    passenger.Health += 130;
                    try
                    {
                        car.Mods.InstallModKit();

                        car.Mods.EngineModIndex = car.Mods.EngineModCount - 1;


                        car.Mods.ExhaustModIndex = car.Mods.ExhaustModCount - 1;

                        car.Mods.TransmissionModIndex = car.Mods.TransmissionModCount - 1;

                        VehicleWheelType wheelType = MathHelper.Choose(VehicleWheelType.Sport, VehicleWheelType.SUV, VehicleWheelType.HighEnd);
                        int wheelModIndex = MathHelper.GetRandomInteger(car.Mods.GetWheelModCount(wheelType));
                        car.Mods.SetWheelMod(wheelType, wheelModIndex, true);

                        car.Mods.HasTurbo = true;

                        car.Mods.HasXenonHeadlights = true;
                    }
                    catch (Exception e)
                    {

                    }



                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();

                    //When player pulls the vehicle over.....
                    if (driverBlip.Exists())
                    {
                        driverBlip.DisableRoute();
                        driverBlip.Delete();
                    }
                    int waitingCount = 0;
                    int closeCount = 0;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        try
                        {
                            GameFiber.Sleep(10);
                            waitingCount += 1;

                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 3.5f)
                                {
                                    closeCount++;
                                    if (Game.IsControllerButtonDown(ControllerButtons.DPadRight) || Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(System.Windows.Forms.Keys.E) || closeCount >= 500)
                                    {
                                        GameFiber.Sleep(3500);
                                        driver = driver.ClonePed(true);
                                        driver.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_STUNGUN"), 500, true);

                                        Game.LocalPlayer.Character.Tasks.ClearSecondary();
                                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.ReverseStraight).WaitForCompletion(900);
                                        driver.Tasks.ClearImmediately();
                                        driver.WarpIntoVehicle(car, -1);
                                        Rage.Native.NativeFunction.Natives.TASK_DRIVE_BY(driver, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 100, 1, Game.GetHashKey("firing_pattern_burst_fire_driveby"));
                                        //Rage.Native.NativeFunction.Natives.TASK_DRIVE_BY(passenger, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 100, 1, Game.GetHashKey("firing_pattern_full_auto"));
                                        GameFiber.Sleep(1300);
                                        taseredOfficer = true;
                                        break;
                                    }

                                }
                            }
                            if (!driver.IsInVehicle(car, false) || !passenger.IsInVehicle(car, false))
                            {
                                driver.WarpIntoVehicle(car, -1);
                                passenger.WarpIntoVehicle(car, 0);
                                isPlayerCheatingTrafficStop = true;
                                
                            }

                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) > 60f)
                            {
                                break;
                            }
                            if (waitingCount >= 2000)
                            {
                                break;
                            }
                            if (isPlayerCheatingTrafficStop)
                            {
                                driver = driver.ClonePed(true);
                                driver.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_STUNGUN"), 500, true);

                                Game.LocalPlayer.Character.Tasks.ClearSecondary();
                                driver.Tasks.PerformDrivingManeuver(VehicleManeuver.ReverseStraight).WaitForCompletion(900);
                                driver.Tasks.ClearImmediately();
                                driver.WarpIntoVehicle(car, -1);
                                Rage.Native.NativeFunction.Natives.TASK_DRIVE_BY(driver, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 100, 1, Game.GetHashKey("firing_pattern_burst_fire_driveby"));
                                //Rage.Native.NativeFunction.Natives.TASK_DRIVE_BY(passenger, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 100, 1, Game.GetHashKey("firing_pattern_full_auto"));
                                GameFiber.Sleep(1300);
                                taseredOfficer = true;
                                break;

                            }
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }
                    
                    drugsPursuit();
                    searchForDrugs();
                    createDrugsEndMessage();
                    if (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        calloutFinished = true;
                    }
                    End();
                }
                catch (System.Threading.ThreadAbortException e)
                {
                    End();
                }
                catch (Exception e)
                {
                    if (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        Game.LogTrivial(e.ToString());
                        Game.LogTrivial("Traffic Policer handled the exception successfully.");
                        Game.DisplayNotification("~O~ANPR Hit ~s~callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }

                }
            });
        }
        private void situationFive()
        {

            calloutStarted = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    


                    driverBlip.EnableRoute(System.Drawing.Color.Red);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);


                    driver.Armor += 50;
                 
                    driver.Health += 130;
                   
                    try
                    {
                        car.Mods.InstallModKit();

                        car.Mods.EngineModIndex = car.Mods.EngineModCount - 1;


                        car.Mods.ExhaustModIndex = car.Mods.ExhaustModCount - 1;

                        car.Mods.TransmissionModIndex = car.Mods.TransmissionModCount - 1;

                        VehicleWheelType wheelType = MathHelper.Choose(VehicleWheelType.Sport, VehicleWheelType.SUV, VehicleWheelType.HighEnd);
                        int wheelModIndex = MathHelper.GetRandomInteger(car.Mods.GetWheelModCount(wheelType));
                        car.Mods.SetWheelMod(wheelType, wheelModIndex, true);

                        car.Mods.HasTurbo = true;

                        car.Mods.HasXenonHeadlights = true;
                    }
                    catch (Exception e)
                    {

                    }



                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();

                    //When player pulls the vehicle over.....
                    if (driverBlip.Exists())
                    {
                        driverBlip.DisableRoute();
                        driverBlip.Delete();
                    }
                    int waitingCount = 0;
                    int closeCount = 0;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        try
                        {
                            GameFiber.Sleep(10);
                            waitingCount += 1;

                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 3.5f)
                                {
                                    closeCount++;
                                    if (Game.IsControllerButtonDown(ControllerButtons.DPadRight) || Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(System.Windows.Forms.Keys.E) || closeCount >= 500)
                                    {
                                        GameFiber.Sleep(3500);
                                        driver = driver.ClonePed(true);
                                        driver.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_STUNGUN"), 500, true);

                                        Game.LocalPlayer.Character.Tasks.ClearSecondary();
                                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.ReverseStraight).WaitForCompletion(900);
                                        driver.Tasks.ClearImmediately();
                                        driver.WarpIntoVehicle(car, -1);
                                        Rage.Native.NativeFunction.Natives.TASK_DRIVE_BY(driver, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 100, 1, Game.GetHashKey("firing_pattern_burst_fire_driveby"));
                                        //Rage.Native.NativeFunction.Natives.TASK_DRIVE_BY(passenger, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 100, 1, Game.GetHashKey("firing_pattern_full_auto"));
                                        GameFiber.Sleep(1300);
                                        taseredOfficer = true;
                                        break;
                                    }

                                }
                            }
                            if (!driver.IsInVehicle(car, false))
                            {
                                driver.WarpIntoVehicle(car, -1);
                                
                                isPlayerCheatingTrafficStop = true;
                                
                            }

                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) > 60f)
                            {
                                break;
                            }
                            if (waitingCount >= 2000)
                            {
                                break;
                            }
                            if (isPlayerCheatingTrafficStop)
                            {
                                driver = driver.ClonePed(true);
                                driver.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_STUNGUN"), 500, true);

                                Game.LocalPlayer.Character.Tasks.ClearSecondary();
                                driver.Tasks.PerformDrivingManeuver(VehicleManeuver.ReverseStraight).WaitForCompletion(900);
                                driver.Tasks.ClearImmediately();
                                driver.WarpIntoVehicle(car, -1);
                                Rage.Native.NativeFunction.Natives.TASK_DRIVE_BY(driver, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 100, 1, Game.GetHashKey("firing_pattern_burst_fire_driveby"));
                                //Rage.Native.NativeFunction.Natives.TASK_DRIVE_BY(passenger, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 100, 1, Game.GetHashKey("firing_pattern_full_auto"));
                                GameFiber.Sleep(1300);
                                taseredOfficer = true;
                                break;

                            }
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }

                    drugsPursuit();
                    searchForDrugs();
                    createDrugsEndMessage();
                    calloutFinished = true;
                }
                catch (System.Threading.ThreadAbortException e)
                {
                    End();
                }
                catch (Exception e)
                {
                    if (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        Game.LogTrivial(e.ToString());
                        Game.LogTrivial("Traffic Policer handled the exception successfully.");
                        Game.DisplayNotification("~O~ANPR Hit ~s~callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }

                }
            });
        }











































        private void convictionsNotification(string suspectName, float percentage, string driverorpassenger)
        {
            string convictionMessage;
            int jailTerm;
            Game.LogTrivial("Percentage: " + percentage.ToString());
            Game.DisplayNotification("The " + driverorpassenger + ", ~r~" + suspectName + ", ~s~was ~g~found guilty~s~ of:");

            if (percentage == 1.1f)
            {
                if (driverorpassenger == "driver")
                {
                    convictionMessage = "~r~Dangerous driving, resisting arrest ~s~and ~r~extremely high scale drug dealing.";
                }
                else
                {
                    convictionMessage = "~r~Resisting arrest ~s~and ~r~extremely high scale drug dealing.";
                }
                jailTerm = TrafficPolicerHandler.rnd.Next(24, 30);

            }
            else if (percentage == 1f)
            {
                if (driverorpassenger == "driver")
                {
                    convictionMessage = "~r~Dangerous driving, resisting arrest ~s~and ~r~very high scale drug dealing.";
                }
                else
                {
                    convictionMessage = "~r~Resisting arrest ~s~and ~r~very high scale drug dealing.";
                }
                jailTerm = TrafficPolicerHandler.rnd.Next(18, 24);

            }
            else if (percentage > 0.79f)
            {
                if (driverorpassenger == "driver")
                {
                    convictionMessage = "~r~Dangerous driving, resisting arrest ~s~and ~r~high scale drug dealing.";
                }
                else
                {
                    convictionMessage = "~r~Resisting arrest ~s~and ~r~high scale drug dealing.";
                }
                jailTerm = TrafficPolicerHandler.rnd.Next(14, 18);
            }
            else if (percentage > 0.49f)
            {
                if (driverorpassenger == "driver")
                {
                    convictionMessage = "~r~Dangerous driving, resisting arrest ~s~and ~r~medium scale drug dealing.";
                }
                else
                {
                    convictionMessage = "~r~Resisting arrest ~s~and ~r~medium scale drug dealing.";
                }
                jailTerm = TrafficPolicerHandler.rnd.Next(9, 14);
            }
            else if (percentage > 0.32f)
            {
                if (driverorpassenger == "driver")
                {
                    convictionMessage = "~r~Dangerous driving, resisting arrest ~s~and ~r~low scale drug dealing.";
                }
                else
                {
                    convictionMessage = "~r~Resisting arrest ~s~and ~r~low scale drug dealing.";
                }
                jailTerm = TrafficPolicerHandler.rnd.Next(5, 9);
            }
            else if (percentage > 0.19f)
            {
                if (driverorpassenger == "driver")
                {
                    convictionMessage = "~r~Dangerous driving, resisting arrest ~s~and ~r~possession of drugs.";
                }
                else
                {
                    convictionMessage = "~r~Resisting arrest ~s~and ~r~possession of drugs.";
                }
                jailTerm = TrafficPolicerHandler.rnd.Next(2, 5);
            }
            else
            {
                if (driverorpassenger == "driver")
                {
                    convictionMessage = "~r~Dangerous driving ~s~and~r~ resisting arrest.";
                }
                else
                {
                    convictionMessage = "~r~Resisting arrest.";
                }
                jailTerm = TrafficPolicerHandler.rnd.Next(1, 3);
            }
            GameFiber.Sleep(4000);
            Game.DisplayNotification(convictionMessage);
            if (moneyFound)
            {
                GameFiber.Sleep(3500);
                Game.DisplayNotification("~r~" + suspectName + " ~s~was further convicted of ~r~money laundering.");
                jailTerm += 4;
            }
            if (taseredOfficer && (driverorpassenger == "driver"))
            {
                GameFiber.Sleep(3500);
                Game.DisplayNotification("~r~" + suspectName + " ~s~was also found guilty of ~r~tasering a police officer.");
                jailTerm += 4;
            }
            GameFiber.Sleep(6000);
            if (driverorpassenger == "passenger")
            {
                jailTerm -= 2;
            }
            if (jailTerm < 1) { jailTerm = 1; }
            Game.DisplayNotification("~r~" + suspectName + " ~s~was jailed for a total of ~r~" + jailTerm.ToString() + " years ~s~as a result.");
            GameFiber.Sleep(5000);
        }

        private void createDrugsEndMessage()
        {
            if (!TrafficPolicerHandler.isOwnerWantedCalloutRunning)
            {
                return;
            }
            float totalEvidence=0;
            float foundEvidence=0;

            if (drugs1Created)
            {
                totalEvidence++;
                if (drugs1Found)
                {
                    foundEvidence++;
                }
                
            }
            if (drugs2Created)
            {
                totalEvidence++;
                if (drugs2Found)
                {
                    foundEvidence++;
                }
            }
            if (drugs3Created)
            {
                totalEvidence++;
                if (drugs3Found)
                {
                    foundEvidence++;
                }
            }
            if (drugs4Created)
            {
                totalEvidence++;
                if (drugs4Found)
                {
                    foundEvidence++;
                }
            }
            if (drugs5Created)
            {
                totalEvidence++;
                if (drugs5Found)
                {
                    foundEvidence++;
                }
            }
            
            float percentage = foundEvidence / totalEvidence;
            if (foundEvidence == 5)
            {
                percentage = 1.1f;
            }
            if (moneyCreated)
            {
                totalEvidence++;
                if (moneyFound)
                {
                    foundEvidence++;
                }
                Game.LogTrivial("Money was created");
            }
            
            Game.FadeScreenOut(1500, true);
            Ped oldPlayer = Game.LocalPlayer.Character;
            oldPlayer.IsPersistent = true;
            oldPlayer.BlockPermanentEvents = true;
            try
            {
                if (oldPlayer.CurrentVehicle.Exists())
                {
                    oldPlayer.CurrentVehicle.IsPersistent = true;
                }
                else if (oldPlayer.LastVehicle.Exists())
                {
                    oldPlayer.LastVehicle.IsPersistent = true;
                }
            }
            catch (Exception e) { }
            cam = new Camera(true);
            cam.Position = new Vector3(285.6085f, -1637.78f, 40.53216f);

            cam.Rotation = new Rotator(0f, 0f, -101.9942f);
            Ped camPed = new Ped(new Vector3(285.6085f, -1637.78f, 32.53216f));
            Game.LocalPlayer.Character = camPed;
            Game.LocalPlayer.HasControl = false;
            GameFiber.Sleep(2000);
            Game.FadeScreenIn(1500, true);
            Game.LocalPlayer.HasControl = false;
            if (foundEvidence == 1)
            {
                Game.DisplayNotification("~g~Traffic Officer ~b~" + TrafficPolicerHandler.DivisionUnitBeat + " ~s~presented ~b~" + foundEvidence.ToString() + " ~s~piece of ~g~evidence ~s~to ~b~court.");
            }
            else
            {
                Game.DisplayNotification("~g~Traffic Officer ~b~" + TrafficPolicerHandler.DivisionUnitBeat + " ~s~presented ~b~" + foundEvidence.ToString() + " ~s~pieces of ~g~evidence ~s~to ~b~court.");
            }
            GameFiber.Sleep(3000);
            if (totalEvidence != 0)
            {
                if (driverArrested && !driverDead)
                {
                    convictionsNotification(driverName, percentage, "driver");
                }
                if (passengerArrested && !passengerDead)
                {
                    convictionsNotification(passengerName, percentage, "passenger");
                }
            }
            
            if (driverDead)
            {
                Game.DisplayNotification("The driver, ~b~" + driverName + ", ~s~died due to their reckless actions.");
                GameFiber.Sleep(2500);
            }
            if (passengerDead)
            {
                Game.DisplayNotification("The passenger, ~b~" + passengerName + ", ~s~died due to their reckless actions.");
                GameFiber.Sleep(2500);
            }
            if (!driverDead && !driverArrested)
            {
                Game.DisplayNotification("The driver, ~b~" + driverName + ", ~s~managed to escape from police.");
                Game.DisplayNotification("A ~b~warrant ~s~has been issued for their arrest.");
                GameFiber.Sleep(3000);
            }
            if (passengerName != null)
            {
                if (!passengerDead && !passengerArrested)
                {
                    Game.DisplayNotification("The passenger, ~b~" + passengerName + ", ~s~managed to escape from police.");
                    Game.DisplayNotification("A ~b~warrant ~s~has been issued for their arrest.");
                    GameFiber.Sleep(3000);
                }
            }
            
            
            difference = (int)Math.Round(totalEvidence - foundEvidence);
            if (difference == 0)
            {
                Game.DisplayNotification("You collected ~g~all the available evidence!");
                Game.DisplayNotification("~b~Great job, ~b~" + TrafficPolicerHandler.DivisionUnitBeat + "!");
            }
            else if (difference == 1)
            {

                Game.DisplayNotification("You unfortunately missed ~r~" + difference.ToString() + " piece of evidence.");
            }
            else
            {
                Game.DisplayNotification("You unfortunately missed ~r~" + difference.ToString() + " pieces of evidence.");
            }
            GameFiber.Sleep(5000);
            Game.FadeScreenOut(1500, true);
            cam.Delete();
            Game.LocalPlayer.Character = oldPlayer;
            Game.LocalPlayer.HasControl = true;
            GameFiber.Sleep(2000);
            Game.FadeScreenIn(1500, true);
        }

        private void deleteAllEntities()
        {
            if (driver.Exists()) { driver.Delete(); }
            if (car.Exists()) { car.Delete(); }
            if (passenger.Exists()) { passenger.Delete(); }
            if (driverBlip.Exists()) { driverBlip.Delete(); }
            if (boat1.Exists()) { boat1.Delete(); }
            if (drugs1.Exists()) { drugs1.Delete(); }
            if (drugs2.Exists()) { drugs2.Delete(); }
            if (drugs3.Exists()) { drugs3.Delete(); }
            if (drugs4.Exists()) { drugs4.Delete(); }
            if (drugs5.Exists()) { drugs5.Delete(); }
            if (money.Exists()) { money.Delete(); }
            foreach(Blip blip in remindLocations)
            {
                if (blip.Exists())
                {
                    blip.Delete();
                }
            }

            Game.LogTrivial("All Drugsrunners entities deleted");
        }
        public override void End()
        {
            base.End();
            TrafficPolicerHandler.isOwnerWantedCalloutRunning = false;
            try
            {
                Game.LogTrivial("ANPR Hit (drugsrunners) callout has ended.");

                if (car.Exists()) { car.IsDriveable = true; }
                if (!calloutFinished)
                {

                    deleteAllEntities();

                }
                else
                {
                    if (displayCode4Message)
                    {

                        GameFiber.Sleep(500);
                        if (Game.LocalPlayer.Character.Exists())
                        {
                            if (Game.LocalPlayer.Character.IsDead)
                            {
                                while (true)
                                {
                                    if (Game.LocalPlayer.Character.Exists())
                                    {
                                        if (Game.LocalPlayer.Character.IsAlive)
                                        {
                                            break;
                                        }
                                    }
                                    GameFiber.Yield();
                                }

                                Game.DisplayNotification("~g~Traffic Officer ~b~" + TrafficPolicerHandler.DivisionUnitBeat + " ~s~has ~r~died~s~ in the line of duty.");
                                Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~o~ANPR Hit: ~r~Drugs Runners", "Dispatch to ~b~" + TrafficPolicerHandler.DivisionUnitBeat, "~o~ANPR Hit ~s~callout is ~r~CODE 4, suspects escaped.");

                                deleteAllEntities();
                                return;
                            }
                            else
                            {
                                
                                
                                
                                Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH WE_ARE_CODE FOUR NO_FURTHER_UNITS_REQUIRED");
                                Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~o~ANPR Hit: ~r~Drugs Runners", "Dispatch to ~b~" + TrafficPolicerHandler.DivisionUnitBeat, "~o~ANPR Hit ~s~callout is ~g~CODE 4.");
                            }

                        }
                        else
                        {
                            GameFiber.Sleep(2000);
                            Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~o~ANPR Hit: ~r~Drugs Runners", "Dispatch to ~b~" + TrafficPolicerHandler.DivisionUnitBeat, "~o~ANPR Hit ~s~callout is ~r~CODE 4, suspects escaped.");
                            GameFiber.Sleep(3500);
                            deleteAllEntities();
                            return;
                        }

                    }
                    if (driverBlip.Exists()) { driverBlip.Delete(); }
                    if (driver.Exists() && !Functions.IsPedArrested(driver))
                    {
                        driver.Dismiss();
                    }
                    if (car.Exists()) { car.Dismiss(); }
                    if (passenger.Exists() && !passenger.IsInVehicle(car, false) && !Functions.IsPedArrested(passenger))
                    {

                        passenger.Dismiss();
                    }
                    if (drugs1.Exists())
                    {
                        drugs1.Dismiss();
                    }
                    if (drugs2.Exists())
                    {
                        drugs2.Dismiss();
                    }
                    if (drugs3.Exists())
                    {
                        drugs3.Dismiss();
                    }
                    if (drugs4.Exists())
                    {
                        drugs4.Dismiss();
                    }
                    if (drugs5.Exists())
                    {
                        drugs5.Dismiss();
                    }
                    if (money.Exists())
                    {
                        money.Dismiss();
                    }
                    foreach (Blip blip in remindLocations)
                    {
                        if (blip.Exists())
                        {
                            blip.Delete();
                        }
                    }

                }
            }
            catch (Exception e)
            {
                deleteAllEntities();
                Game.LogTrivial("Forced all entity delete");
            }
        }
 
        private void beforeTrafficStopDrive()
        {
            while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
            {
                try
                {
                    GameFiber.Yield();
                    Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(driver, 786603);
                    if (Functions.IsPlayerPerformingPullover() && Functions.GetPulloverSuspect(Functions.GetCurrentPullover()) == driver)
                    {


                        break;

                    }
                    if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 15f)
                    {
                        if (!driver.IsInVehicle(car, true))
                        {
                            driver = driver.ClonePed(true);
                            driver.WarpIntoVehicle(car, -1);
                            Game.DisplayNotification("Performing a ~b~Traffic Stop ~s~is necessary due to coding restrictions.");
                            cheatCount++;
                        }
                        if (passenger.Exists())
                        {
                            if (!passenger.IsInVehicle(car, true))
                            {
                                passenger = passenger.ClonePed(true);
                                passenger.WarpIntoVehicle(car, 0);
                                Game.DisplayNotification("Performing a ~b~Traffic Stop ~s~is necessary due to coding restrictions.");
                                cheatCount++;
                            }
                        }
                        if (Game.LocalPlayer.Character.IsInVehicle(car, true) || Game.LocalPlayer.Character.IsJacking)
                        {
                            Game.LocalPlayer.Character.Tasks.ClearImmediately();
                        }
                    }
                    GameFiber.Yield();
                    if (driver.IsDead)
                    {       
                        calloutFinished = true;
                        Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                        break;
                    }
                    if (passenger.Exists())
                    {
                        if (passenger.IsDead)
                        {
                            calloutFinished = true;
                            break;
                        }
                    }
                    if (cheatCount > 3)
                    {
                        car.Explode(true);

                    }
                    GameFiber.Yield();
                    if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 12f)
                    {
                        if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            isPlayerCheatingTrafficStop = true;
                            break;
                        }
                    }
                }
                catch { continue; }



            }

        }
    }
}
