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

namespace Traffic_Policer.Callouts
{
    //public enum reasonOwnerWanted { Murder, Robbery, Shoplifting }
    [CalloutInfo("OwnerWanted", CalloutProbability.Medium)]
    internal class OwnerWanted : Callout
    {

        private Vehicle car; // a rage vehicle
        private Ped driver; // a rage ped
        private Ped passenger;
        private SpawnPoint spawnPoint; // a Vector3
        private Blip driverBlip; // a rage blip
        private LHandle pursuit; // an API pursuit handle
        private bool calloutStarted;
        private bool calloutFinished;
        private Group group;
        private bool isPlayerCheatingTrafficStop = false;
        private bool processedKeyTaking = true;
        private bool endLikeNormal = true;
        private Ped reinforcementCarDriver;
        private Ped passenger1;
        private Ped passenger2;
        private Blip reinforcementCarBlip;
        private Vehicle reinforcementCar;
        private string descriptionOwnerWanted;
        private bool reinforcementDriverDead;
        private bool passenger1Dead;
        private bool passenger2Dead;
        private bool inReinforcementLoop = false;
        private Vehicle bike1;
        private Vehicle bike2;
        private Vehicle pursueCar;
        private Ped bikeRider1;
        private Ped bikeRider2;
        private Ped pursueCarDriver;
        private Ped firePed;
        private Vehicle playerVehicle;
        private int cheatCount = 0;
        private Model carModel;

        private Blip bikeBlip1;
        private Blip bikeBlip2;
        private Blip pursueCarBlip;
        private bool switchedPursuit;
        private string[] vehiclesToSelectFrom = new string[] {"DUKES", "BALLER", "BALLER2", "BISON", "BISON2", "BJXL", "CAVALCADE", "CHEETAH", "COGCABRIO", "ASEA", "ADDER", "FELON", "FELON2", "ZENTORNO",
        "WARRENER", "RAPIDGT", "INTRUDER", "FELTZER2", "FQ2", "RANCHERXL", "REBEL", "SCHWARZER", "COQUETTE", "CARBONIZZARE", "EMPEROR", "SULTAN", "EXEMPLAR", "MASSACRO",
        "DOMINATOR", "ASTEROPE", "PRAIRIE", "NINEF", "WASHINGTON", "CHINO", "CASCO", "INFERNUS", "ZTYPE", "DILETTANTE", "VIRGO", "F620", "PRIMO" };
        private string[] meleeWeaponsToSelectFrom = new string[] { "WEAPON_KNIFE", "WEAPON_KNIFE", "WEAPON_UNARMED", "WEAPON_NIGHTSTICK", "WEAPON_GOLFCLUB", "WEAPON_BAT", "WEAPON_CROWBAR", "WEAPON_KNIFE" };
        private string[] firearmsToSelectFrom = new string[] { "WEAPON_PISTOL", "WEAPON_APPISTOL", "WEAPON_PISTOL50", "WEAPON_PISTOL50", "WEAPON_MICROSMG", "WEAPON_SMG", "WEAPON_PISTOL50", "WEAPON_ASSAULTRIFLE"
                                                                , "WEAPON_ADVANCEDRIFLE", "WEAPON_PISTOL50" };
        private string[] bikesToSelectFrom = new string[] { "BATI", "BATI2", "AKUMA", "BAGGER", "DOUBLE", "NEMESIS", "HEXER" };
        private string[] sportsCars = { "SULTAN", "EXEMPLAR", "F620", "FELON2", "FELON", "SENTINEL", "WINDSOR", "DOMINATOR", "DUKES", "GAUNTLET", "VIRGO", "ADDER", "BUFFALO", "ZENTORNO", "MASSACRO" };
        private List<Vehicle> bikesSpawned = new List<Vehicle>();

        private int upperSituationNumber;
        private bool displayedModelName = false;
        private string scannerMessage;
        private bool canCallForReinforcements;
        private string[] vowels = new string[] { "a", "e", "o", "i", "u" };
        private string[] numbers = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        private bool displayCode4Message = true;
        private bool driverDead;
        private bool biker1Dead;
        private bool biker2Dead;
        private bool pursueDriverDead;
        private Vector3 bikeSpawnPoint;
        private bool passengerDead;
        private string CourtSentence;

        private void determineWhyOwnerIsWanted()
        {
            int reasonOwnerWanted = TrafficPolicerHandler.rnd.Next(20);
            if (reasonOwnerWanted == 0)
            {
                descriptionOwnerWanted = "murdering a police officer";
                CourtSentence = "Jailed for life without the possibility of parole.";
                upperSituationNumber = 31;
                scannerMessage = "CRIME_OFFICER_HOMICIDE";
            }
            else if (reasonOwnerWanted == 1)
            {
                descriptionOwnerWanted = "a recent armed robbery";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(72, 180, 0);
                }
                scannerMessage = "CRIME_ARMED_ROBBERY";
                upperSituationNumber = 31;
            }
            else if (reasonOwnerWanted == 2)
            {
                descriptionOwnerWanted = "assault with a deadly weapon";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(72, 180, 0);
                }
                scannerMessage = "CRIME_ASSAULT_DEADLY_WEAPON";
                upperSituationNumber = 31;
            }
            else if (reasonOwnerWanted == 3)
            {

                descriptionOwnerWanted = "aggravated burglary";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(36, 96, 0);
                }
                scannerMessage = "CRIME_BURGLARY";
                upperSituationNumber = 32;
            }
            else if (reasonOwnerWanted == 4)
            {
                descriptionOwnerWanted = "attempted murder";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(150, 240, 0);
                }
                scannerMessage = "CRIME_ATTEMPTED_HOMICIDE";
                upperSituationNumber = 31;
            }
            else if (reasonOwnerWanted == 5)
            {
                descriptionOwnerWanted = "home invasion";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(24, 72, 0);
                }
                scannerMessage = "CRIME_BREAKING_AND_ENTERING";
                upperSituationNumber = 33;
            }
            else if (reasonOwnerWanted == 6)
            {
                descriptionOwnerWanted = "a kidnapping";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(60, 120, 0);
                }
                scannerMessage = "CRIME_KIDNAPPING";
                upperSituationNumber = 31;
            }
            else if (reasonOwnerWanted == 7)
            {
                descriptionOwnerWanted = "an armed carjacking";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(120, 180, 0);
                }
                scannerMessage = "CRIME_CARJACKING";
                upperSituationNumber = 31;
            }
            else if (reasonOwnerWanted == 8)
            {
                descriptionOwnerWanted = "a recent shootout";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(120, 212, 0);
                }
                scannerMessage = "CRIME_INCIDENT_INVOLVING_SHOTS_FIRED";
                upperSituationNumber = 31;
            }
            else if (reasonOwnerWanted == 9)
            {
                descriptionOwnerWanted = "violent gang activity";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(84, 180, 0);
                }
                scannerMessage = "CRIME_HIGH_RANKING_GANG_MEMBER_TRANSIT";
                upperSituationNumber = 31;
            }
            else if (reasonOwnerWanted == 10)
            {
                descriptionOwnerWanted = "links to ISIS";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(84, 180, 0);
                }
                scannerMessage = "CRIME_POSSIBLE_TERRORIST_ACTIVITY";
                upperSituationNumber = 29;
            }
            else if (reasonOwnerWanted == 11)
            {
                descriptionOwnerWanted = "various serious felonies";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(84, 180, 0);
                }
                scannerMessage = "CRIME_WANTED_FELON_ON_THE_LOOSE";
                upperSituationNumber = 32;
            }
            else if (reasonOwnerWanted == 12)
            {
                descriptionOwnerWanted = "a violent road rage incident";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(6, 24, 40);
                }
                scannerMessage = "CRIME_ATTACK_ON_VEHICLE";
                upperSituationNumber = 33;
            }
            else if (reasonOwnerWanted == 13)
            {
                descriptionOwnerWanted = "organised shoplifting";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(6, 24, 30);
                }
                scannerMessage = "CRIME_GRANDTHEFT";
                upperSituationNumber = 32;
            }
            else if (reasonOwnerWanted == 14)
            {
                descriptionOwnerWanted = "an armoured car robbery";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(120, 180, 0);
                }
                scannerMessage = "CRIME_ARMOURED_CAR_ROBBERY";
                upperSituationNumber = 31;
            }
            else if (reasonOwnerWanted == 15)
            {
                descriptionOwnerWanted = "a recent shootout";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(120, 212, 0);
                }
                scannerMessage = "CRIME_SHOOTOUT";
                upperSituationNumber = 31;
            }
            else if (reasonOwnerWanted == 16)
            {
                descriptionOwnerWanted = "trespassing";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(2, 12, 60);
                }
                scannerMessage = "CRIME_TRESPASSING_GOVERNMENT";
                upperSituationNumber = 31;
            }
            else if (reasonOwnerWanted == 17)
            {
                descriptionOwnerWanted = "a driveby attack";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(100, 180, 0);
                }
                scannerMessage = "CRIME_DRIVEBYATTACK";
                upperSituationNumber = 31;
            }
            else if (reasonOwnerWanted == 18)
            {
                descriptionOwnerWanted = "a hit and run";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(3, 24, 20);
                }
                scannerMessage = "CRIME_HITRUN";
                upperSituationNumber = 32;
            }
            else if (reasonOwnerWanted == 19)
            {
                descriptionOwnerWanted = "organised shoplifting";
                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                {
                    CourtSentence = API.LSPDFRPlusFunctions.DeterminePrisonSentence(6, 24, 30);
                }
                scannerMessage = "CRIME_GRANDTHEFT";
                upperSituationNumber = 32;
            }

        }



        public override bool OnBeforeCalloutDisplayed()
        {

            Game.LogTrivial("TrafficPolicer.OwnerWanted");
            int WaitCount = 0;
            while (!World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(220f, 400f)).GetClosestVehicleSpawnPoint(out spawnPoint))
            {
                GameFiber.Yield();
                WaitCount++;
                if (WaitCount > 10) { return false; }
            }


            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 70f);
            determineWhyOwnerIsWanted();
            carModel = new Model(vehiclesToSelectFrom[TrafficPolicerHandler.rnd.Next(vehiclesToSelectFrom.Length)]);
            carModel.LoadAndWait();
            CalloutMessage = "~o~ANPR Hit: ~b~Vehicle's usual occupants ~r~wanted.";
            CalloutPosition = spawnPoint;
            Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + TrafficPolicerHandler.DivisionUnitBeatAudioString + " WE_HAVE_01 CRIME_TRAFFIC_ALERT FOR " + scannerMessage, spawnPoint);
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
            
            driverBlip = driver.AttachBlip();
            driverBlip.Scale = 0.7f;
            LSPD_First_Response.Engine.Scripting.Entities.Persona oldpersona = Functions.GetPersonaForPed(driver);
            oldpersona.Wanted = true;
            Functions.SetPersonaForPed(driver, oldpersona);                   
            Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~o~ANPR Hit: ~r~Owner Wanted", "Dispatch to ~b~" + TrafficPolicerHandler.DivisionUnitBeat, "The ~o~ANPR Hit ~s~is for ~r~" + descriptionOwnerWanted + ". ~b~Use appropriate caution.");
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

                int situationNumber = TrafficPolicerHandler.rnd.Next(1, upperSituationNumber);
                
                
                int createPassenger = TrafficPolicerHandler.rnd.Next(2);
                //situationNumber = 35;
                


                Game.LogTrivial("SituationNumber: " + situationNumber.ToString());
                Game.LogTrivial("CreatePassenger: " + createPassenger.ToString());
                if ((situationNumber == 15) || (situationNumber == 9) || (situationNumber == 1))
                {
                    if (createPassenger == 0)
                    {
                        situationOneWithPassenger();
                    }
                    else { situationOneWithoutPassenger(); }
                }

                else if ((situationNumber == 6) || (situationNumber == 17) || (situationNumber == 21))
                {
                    if (createPassenger == 0)
                    {
                        situationTwoWithPassenger();

                    }
                    else { situationTwoWithoutPassenger(); }
                }

                else if ((situationNumber == 27) || (situationNumber == 19) || (situationNumber == 12) || (situationNumber == 11))
                {
                    situationFourWithPassenger();
                }
                else if ((situationNumber == 4) || (situationNumber == 23) || (situationNumber == 13))
                {
                    situationFiveWithoutPassenger();
                }

                else if ((situationNumber == 22) || (situationNumber == 5) || (situationNumber == 18))
                {
                    situationSix();
                }

                else if ((situationNumber == 2) || (situationNumber == 16))
                {
                    situationSeven();
                }
                else if ((situationNumber == 7) || (situationNumber == 26) || (situationNumber == 14))
                {
                    situationEight();
                }
                else if ((situationNumber == 10) || (situationNumber == 8) || (situationNumber == 24))
                {
                    situationNine();
                }
                else if ((situationNumber == 25) || (situationNumber == 20) || (situationNumber == 3))
                {
                    situationTen();
                }
                else if ((situationNumber == 28) || (situationNumber == 29))
                {
                    if (createPassenger == 0)
                    {
                        situationThreeWithPassenger();
                    }
                    else { situationThreeWithoutPassenger(); }
                }

                
                else
                {
                    if (createPassenger == 0)
                    {
                        situationLSPDFR(true);
                    }
                    else
                    {
                        situationLSPDFR(false);
                    }
                }


                //else
                //{
                //    if (createPassenger == 0)
                //    {
                //        situationLSPDFRWithPassenger();
                //    }
                //    else { situationLSPDFRWithoutPassenger(); }
                //}
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
                if (car.HasDriver)
                {
                    driver = car.Driver;
                }
                TrafficPolicerHandler.driverChangedDueToKeys = false;
                processedKeyTaking = false;
            }
            if (calloutFinished)
            {
                End();
            }

        }
        private void deleteAllEntities()
        {
            if (driver.Exists()) { driver.Delete(); }
            if (car.Exists()) { car.Delete(); }
            if (passenger.Exists()) { passenger.Delete(); }
            if (driverBlip.Exists()) { driverBlip.Delete(); }
            if (reinforcementCarDriver.Exists())
            {
                reinforcementCarDriver.Delete();
            }
            if (passenger1.Exists()) { passenger1.Delete(); }
            if (passenger2.Exists()) { passenger2.Delete(); }
            if (reinforcementCar.Exists()) { reinforcementCar.Delete(); }
            if (reinforcementCarBlip.Exists()) { reinforcementCarBlip.Delete(); }
            if (bikeBlip1.Exists())
            {
                bikeBlip1.Delete();
            }
            if (bikeBlip2.Exists())
            {
                bikeBlip2.Delete();
            }
            if (pursueCarBlip.Exists())
            {
                pursueCarBlip.Delete();
            }
            if (bike1.Exists()) { bike1.Delete(); }
            if (bike2.Exists()) { bike2.Delete(); }
            if (pursueCar.Exists()) { pursueCar.Delete(); }
            if (bikeRider1.Exists()) { bikeRider1.Delete(); }
            if (bikeRider2.Exists()) { bikeRider2.Delete(); }
            if (pursueCarDriver.Exists()) { pursueCarDriver.Delete(); }
            if (firePed.Exists()) { firePed.Delete(); }
            Game.LogTrivial("All Owner Wanted entities deleted");
        }
        public override void End()
        {
            base.End();
            TrafficPolicerHandler.isOwnerWantedCalloutRunning = false;
            try {
                Game.LogTrivial("ANPR Hit callout has ended.");

                if (car.Exists()) { car.IsDriveable = true; }
                if (!endLikeNormal) { return; }
                if (!calloutFinished)
                {

                    deleteAllEntities();

                }
                else
                {
                    if (displayCode4Message)
                    {
                        if (driver.Exists())
                        {
                            if (TrafficPolicerHandler.IsLSPDFRPlusRunning && !driver.IsDead && Functions.IsPedArrested(driver))
                            {
                                API.LSPDFRPlusFunctions.CreateCourtCase(Functions.GetPersonaForPed(driver), descriptionOwnerWanted, 100, CourtSentence);
                            }
                        }
                        GameFiber.Sleep(3500);
                        Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH WE_ARE_CODE FOUR NO_FURTHER_UNITS_REQUIRED");
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
                                Functions.PlayScannerAudio("TARGETS_EVADED_CAPTURE");
                                Game.DisplayNotification("~g~Traffic Officer ~b~" + TrafficPolicerHandler.DivisionUnitBeat + " ~s~has ~r~died~s~ in the line of duty.");
                                Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~o~ANPR Hit: ~r~Owner Wanted", "Dispatch to ~b~" + TrafficPolicerHandler.DivisionUnitBeat, "~o~ANPR Hit ~s~callout is ~r~CODE 4, suspects escaped.");

                                deleteAllEntities();
                                return;
                            }
                            else
                            {


                                Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~o~ANPR Hit: ~r~Owner Wanted", "Dispatch to ~b~" + TrafficPolicerHandler.DivisionUnitBeat, "~o~ANPR Hit ~s~callout is ~g~CODE 4.");
                                
                            }

                        }
                        else
                        {
                            GameFiber.Sleep(2000);
                            Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~o~ANPR Hit: ~r~Owner Wanted", "Dispatch to ~b~" + TrafficPolicerHandler.DivisionUnitBeat, "~o~ANPR Hit ~s~callout is ~r~CODE 4, suspects escaped.");
                            GameFiber.Sleep(3500);
                            deleteAllEntities();
                            return;
                        }

                    }
                    if (firePed.Exists()) { firePed.Delete(); }
                    if (driverBlip.Exists()) { driverBlip.Delete(); }
                    if (driver.Exists() && !Functions.IsPedArrested(driver))
                    {
                        driver.Dismiss();
                    }
                    if (car.Exists()) { car.Dismiss(); }
                    if (passenger.Exists() && !passenger.IsInVehicle(car, false) && !Functions.IsPedArrested(passenger))
                    {

                        passenger.Dismiss(); }
                    if (reinforcementCarDriver.Exists())
                    {
                        reinforcementCarDriver.Dismiss();
                    }
                    if (passenger1.Exists())
                    {
                        passenger1.Dismiss();
                    }
                    if (passenger2.Exists())
                    {
                        passenger2.Dismiss();
                    }
                    if (reinforcementCar.Exists()) { reinforcementCar.Dismiss(); }
                    if (reinforcementCarBlip.Exists()) { reinforcementCarBlip.Delete(); }

                    if (bikeBlip1.Exists())
                    {
                        bikeBlip1.Delete();
                    }
                    if (bikeBlip2.Exists())
                    {
                        bikeBlip2.Delete();
                    }
                    if (pursueCarBlip.Exists())
                    {
                        pursueCarBlip.Delete();
                    }
                    if (bike1.Exists())
                    {
                        bike1.Dismiss();
                    }
                    if (bike2.Exists())
                    {
                        bike2.Dismiss();
                    }
                    if (pursueCar.Exists())
                    {
                        pursueCar.Dismiss();
                    }
                    if (bikeRider1.Exists())
                    {
                        bikeRider1.Dismiss();
                    }
                    if (bikeRider2.Exists())
                    {
                        bikeRider2.Dismiss();
                    }
                    if (pursueCarDriver.Exists())
                    {
                        pursueCarDriver.Dismiss();
                    }

                }
            }
            catch (Exception e)
            {
                deleteAllEntities();
                Game.LogTrivial("Forced all entity delete");
            }
        }
        /// <summary>
        /// Instantly become aggressive
        /// </summary>
        private void situationOneWithPassenger()
        {
            calloutStarted = true;
            GameFiber.StartNew(delegate
            {
                try {
                    //Add Passenger
                    passenger = new Ped(spawnPoint);
                    passenger.BlockPermanentEvents = true;
                    passenger.WarpIntoVehicle(car, 0);

                    //Group definition, weapons and combat stats
                    group = new Group(driver);
                    driverBlip.EnableRoute(System.Drawing.Color.Red);

                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(passenger, 3);

                    driver.Armor += 55;
                    passenger.Armor += 55;
                    driver.Health += 120;
                    passenger.Health += 120;

                    group.AddMember(passenger);

                    //Drive
                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();




                    //When player pulls the vehicle over.....
                    if (driverBlip.Exists())
                    {
                        driverBlip.DisableRoute();
                        driverBlip.Delete();
                    }
                    int waitingCount = 0;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        try {
                            GameFiber.Sleep(10);
                            waitingCount += 1;

                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 3f)
                                {
                                    break;
                                }
                            }
                            if (!driver.IsInVehicle(car, false) || !passenger.IsInVehicle(car, false))
                            {
                                break;
                            }

                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) > 60f)
                            {
                                break;
                            }
                            if (waitingCount >= 3000)
                            {
                                break;
                            }
                            if (isPlayerCheatingTrafficStop)
                            {
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }

                    //Fight!




                    car.IsDriveable = false;
                    driver.BlockPermanentEvents = true;
                    if (driver.IsInVehicle(car, false))
                    {
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
                        driver = driver.ClonePed(true);
                        driver.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(500);


                    }
                    else { driver.Tasks.ClearImmediately(); }
                    if (passenger.IsInVehicle(car, false))
                    {
                        passenger.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(4000);
                    }

                    passenger.Tasks.ClearImmediately();




                    driver.Inventory.GiveNewWeapon(new WeaponAsset(meleeWeaponsToSelectFrom[TrafficPolicerHandler.rnd.Next(meleeWeaponsToSelectFrom.Length)]), 1, true);
                    passenger.Inventory.GiveNewWeapon(new WeaponAsset(firearmsToSelectFrom[TrafficPolicerHandler.rnd.Next(firearmsToSelectFrom.Length)]), 500, true);
                    driver.BlockPermanentEvents = true;
                    passenger.BlockPermanentEvents = true;
                    driver.KeepTasks = true;

                    Rage.Native.NativeFunction.Natives.TaskCombatPed(driver, Game.LocalPlayer.Character, 0, 16);
                    Rage.Native.NativeFunction.Natives.TaskCombatPed(passenger, Game.LocalPlayer.Character, 0, 16);

                    bool driverDead = false;
                    bool passengerDead = false;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        if (driver.Exists())
                        {
                            if (driver.IsDead || Functions.IsPedArrested(driver))
                            {
                                driverDead = true;
                            }
                            else
                            {
                                if (!Functions.IsPedGettingArrested(driver))
                                {
                                    if (!Rage.Native.NativeFunction.Natives.IS_PED_IN_COMBAT<bool>(driver))
                                    {
                                        driver.Tasks.ClearImmediately();

                                    }
                                    driver.BlockPermanentEvents = true;



                                    Rage.Native.NativeFunction.Natives.TaskCombatPed(driver, Game.LocalPlayer.Character, 0, 16);
                                }
                            }

                        }
                        if (passenger.Exists())
                        {
                            if (passenger.IsDead || Functions.IsPedArrested(passenger))
                            {
                                passengerDead = true;
                            }
                            else
                            {
                                if (!Functions.IsPedGettingArrested(passenger))
                                {
                                    if (!Rage.Native.NativeFunction.Natives.IS_PED_IN_COMBAT<bool>(passenger))
                                    {
                                        passenger.Tasks.ClearImmediately();
                                    }
                                    passenger.BlockPermanentEvents = true;
                                    Rage.Native.NativeFunction.Natives.TaskCombatPed(passenger, Game.LocalPlayer.Character, 0, 16);
                                }
                            }
                        }
                        if (Game.LocalPlayer.Character.IsDead)
                        {
                            Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                            break;
                        }
                        if (driverDead && passengerDead)
                        {
                            break;
                        }
                        if (!driver.Exists() && !passenger.Exists())
                        {
                            break;
                        }




                        GameFiber.Sleep(5000);
                    }
                    if (group.Exists())
                    {
                        group.Delete();
                    }


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
        /// <summary>
        /// Instantly become aggressive
        /// </summary>
        private void situationOneWithoutPassenger()
        {
            calloutStarted = true;
            GameFiber.StartNew(delegate
            {

                try {
                    //Group definition, weapons and combat stats

                    driverBlip.EnableRoute(System.Drawing.Color.Red);


                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);


                    driver.Armor += 60;

                    driver.Health += 140;



                    //Drive
                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();




                    //When player pulls the vehicle over.....
                    if (driverBlip.Exists())
                    {
                        driverBlip.DisableRoute();
                        driverBlip.Delete();
                    }
                    int waitingCount = 0;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        try
                        {
                            GameFiber.Sleep(10);
                            waitingCount++;
                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 3f)
                                {
                                    break;
                                }
                            }
                            if (!driver.IsInVehicle(car, false))
                            {
                                break;
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
                                break;
                            }


                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }

                    //Fight!




                    car.IsDriveable = false;

                    if (driver.IsInVehicle(car, false))
                    {


                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);

                        driver.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(4000);
                    }
                    driver.Inventory.GiveNewWeapon(new WeaponAsset(firearmsToSelectFrom[TrafficPolicerHandler.rnd.Next(firearmsToSelectFrom.Length)]), 1, true);

                    driver.BlockPermanentEvents = true;

                    driver.KeepTasks = true;




                    bool driverDead = false;

                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        if (driver.Exists())
                        {
                            if (driver.IsDead || Functions.IsPedArrested(driver))
                            {
                                driverDead = true;
                            }
                            else
                            {
                                if (!Functions.IsPedGettingArrested(driver))
                                {
                                    if (!Rage.Native.NativeFunction.Natives.IS_PED_IN_COMBAT<bool>(driver))
                                    {
                                        driver.Tasks.ClearImmediately();
                                    }
                                    driver.BlockPermanentEvents = true;



                                    Rage.Native.NativeFunction.Natives.TaskCombatPed(driver, Game.LocalPlayer.Character, 0, 16);
                                }
                            }
                        }

                        if (Game.LocalPlayer.Character.IsDead)
                        {
                            Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                            break;
                        }
                        if (driverDead)
                        {
                            break;
                        }
                        if (!driver.Exists())
                        {
                            break;
                        }



                        GameFiber.Sleep(1000);
                    }


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



        /// <summary>
        /// Fake hands up and become aggressive
        /// </summary>
        private void situationTwoWithPassenger()
        {
            calloutStarted = true;
            GameFiber.StartNew(delegate
            {
                try {
                    //Add Passenger
                    passenger = new Ped(spawnPoint);
                    passenger.BlockPermanentEvents = true;
                    passenger.WarpIntoVehicle(car, 0);

                    group = new Group(driver);
                    driverBlip.EnableRoute(System.Drawing.Color.Red);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(passenger, 3);

                    driver.Armor += 55;
                    passenger.Armor += 55;
                    driver.Health += 140;
                    passenger.Health += 140;

                    group.AddMember(passenger);


                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();

                    //When player pulls the vehicle over.....
                    if (driverBlip.Exists())
                    {
                        driverBlip.DisableRoute();
                        driverBlip.Delete();
                    }
                    int waitingCount = 0;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        try
                        {
                            GameFiber.Sleep(10);
                            waitingCount += 1;

                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 3f)
                                {
                                    break;
                                }
                            }
                            if (!driver.IsInVehicle(car, false) || !passenger.IsInVehicle(car, false))
                            {
                                break;
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
                                break;
                            }

                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }

                    //Driver puts hands up
                    car.IsDriveable = false;

                    car.IsDriveable = false;
                    driver.BlockPermanentEvents = true;
                    passenger.BlockPermanentEvents = true;
                    driver.Inventory.GiveNewWeapon(new WeaponAsset(firearmsToSelectFrom[TrafficPolicerHandler.rnd.Next(firearmsToSelectFrom.Length)]), 500, false);

                    if (driver.IsInVehicle(car, false))
                    {
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);

                        driver.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(1000);
                    }
                    if (passenger.IsInVehicle(car, false))
                    {
                        passenger.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(3000);
                    }

                    driver.Tasks.ClearImmediately();
                    passenger.Tasks.ClearImmediately();
                    driver.Tasks.PutHandsUp(9000, Game.LocalPlayer.Character);

                    passenger.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.LastVehicle.GetOffsetPosition(Vector3.RelativeRight * 2f), Game.LocalPlayer.Character.Heading + 180f, 3f).WaitForCompletion(2000);
                    passenger.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.LastVehicle.GetOffsetPosition(Vector3.RelativeBack * 4f), Game.LocalPlayer.Character.Heading + 180f, 3f).WaitForCompletion(2500);
                    passenger.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeBack * 1f), Game.LocalPlayer.Character.Heading, 3f);

                    while (Vector3.Distance(Game.LocalPlayer.Character.Position, passenger.Position) > 5f)
                    {
                        GameFiber.Yield();
                        if (driver.IsInVehicle(car, true))
                        {
                            driver.Tasks.ClearImmediately();
                            driver.Tasks.PutHandsUp(3000, Game.LocalPlayer.Character);
                        }
                    }

                    passenger.Tasks.ClearImmediately();
                    passenger.KeepTasks = true;
                    passenger.Inventory.GiveNewWeapon(new WeaponAsset(meleeWeaponsToSelectFrom[TrafficPolicerHandler.rnd.Next(meleeWeaponsToSelectFrom.Length)]), 1, true);
                    Rage.Native.NativeFunction.Natives.TaskCombatPed(passenger, Game.LocalPlayer.Character, 0, 16);
                    GameFiber.Sleep(1000);

                    if (!Functions.IsPedGettingArrested(driver))
                    {
                        driver.Tasks.ClearImmediately();
                        driver.Inventory.GiveNewWeapon(new WeaponAsset(firearmsToSelectFrom[TrafficPolicerHandler.rnd.Next(firearmsToSelectFrom.Length)]), 500, true);
                        driver.KeepTasks = true;
                    }

                    bool driverDead = false;
                    bool passengerDead = false;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        if (driver.Exists())
                        {
                            if (driver.IsDead || Functions.IsPedArrested(driver))
                            {
                                driverDead = true;
                            }
                            else
                            {
                                if (!Functions.IsPedGettingArrested(driver))
                                {
                                    if (!Rage.Native.NativeFunction.Natives.IS_PED_IN_COMBAT<bool>(driver))
                                    {
                                        driver.Tasks.ClearImmediately();
                                    }
                                    driver.BlockPermanentEvents = true;



                                    Rage.Native.NativeFunction.Natives.TaskCombatPed(driver, Game.LocalPlayer.Character, 0, 16);
                                }
                            }

                        }
                        if (passenger.Exists())
                        {
                            if (passenger.IsDead || Functions.IsPedArrested(passenger))
                            {
                                passengerDead = true;
                            }
                            else
                            {
                                if (!Functions.IsPedGettingArrested(passenger))
                                {
                                    if (!Rage.Native.NativeFunction.Natives.IS_PED_IN_COMBAT<bool>(passenger))
                                    {
                                        passenger.Tasks.ClearImmediately();
                                    }
                                    passenger.BlockPermanentEvents = true;
                                    Rage.Native.NativeFunction.Natives.TaskCombatPed(passenger, Game.LocalPlayer.Character, 0, 16);
                                }


                            }
                        }
                        if (Game.LocalPlayer.Character.IsDead)
                        {
                            Game.DisplaySubtitle("~r~Driver: ~b~That's why you don't mess with us!", 4000);
                            Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                            GameFiber.Sleep(4000);
                            if (driver.Exists()) { driver.Delete(); }
                            if (passenger.Exists()) { passenger.Delete(); }
                            if (car.Exists()) { car.Delete(); }
                            break;
                        }
                        if (driverDead && passengerDead)
                        {

                            break;
                        }
                        if (!driver.Exists() && !passenger.Exists())
                        {
                            break;
                        }




                        GameFiber.Sleep(1000);
                    }
                    group.Delete();

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
        /// <summary>
        /// Fake hands up and become aggressive
        /// </summary>
        private void situationTwoWithoutPassenger()
        {
            calloutStarted = true;
            GameFiber.StartNew(delegate
            {
                try {


                    driverBlip.EnableRoute(System.Drawing.Color.Red);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);


                    driver.Armor += 60;

                    driver.Health += 140;




                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();

                    //When player pulls the vehicle over.....
                    if (driverBlip.Exists())
                    {
                        driverBlip.DisableRoute();
                        driverBlip.Delete();
                    }
                    int waitingCount = 0;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        try
                        {
                            GameFiber.Sleep(10);
                            waitingCount++;
                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 3f)
                                {
                                    break;
                                }
                            }
                            if (!driver.IsInVehicle(car, false))
                            {
                                break;
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
                                break;
                            }

                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }

                    //Driver puts hands up
                    car.IsDriveable = false;

                    car.IsDriveable = false;
                    driver.BlockPermanentEvents = true;



                    if (driver.IsInVehicle(car, false))
                    {
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);

                        driver.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(5000);
                    }

                    driver.Tasks.ClearImmediately();

                    driver.Tasks.PutHandsUp(2000, Game.LocalPlayer.Character);

                    GameFiber.Sleep(2000);



                    driver.Tasks.ClearImmediately();
                    driver.KeepTasks = true;
                    driver.Inventory.GiveNewWeapon(new WeaponAsset(firearmsToSelectFrom[TrafficPolicerHandler.rnd.Next(firearmsToSelectFrom.Length)]), 500, true);
                    Rage.Native.NativeFunction.Natives.TaskCombatPed(driver, Game.LocalPlayer.Character, 0, 16);




                    bool driverDead = false;

                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        if (driver.Exists())
                        {
                            if (driver.IsDead || Functions.IsPedArrested(driver))
                            {
                                driverDead = true;
                            }
                            else
                            {
                                if (!Functions.IsPedGettingArrested(driver))
                                {
                                    if (!Rage.Native.NativeFunction.Natives.IS_PED_IN_COMBAT<bool>(driver))
                                    {
                                        driver.Tasks.ClearImmediately();
                                    }
                                    driver.BlockPermanentEvents = true;



                                    Rage.Native.NativeFunction.Natives.TaskCombatPed(driver, Game.LocalPlayer.Character, 0, 16);
                                }
                            }

                        }

                        if (Game.LocalPlayer.Character.IsDead)
                        {
                            Game.DisplaySubtitle("~r~Driver: ~b~That's why you don't mess with me!", 4000);
                            Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                            GameFiber.Sleep(4000);
                            if (driver.Exists()) { driver.Delete(); }

                            if (car.Exists()) { car.Delete(); }
                            break;
                        }
                        if (driverDead)
                        {

                            break;
                        }
                        if (!driver.Exists())
                        {
                            break;
                        }



                        GameFiber.Sleep(1000);
                    }


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

        /// <summary>
        /// Hands up and surrender
        /// </summary>
        private void situationThreeWithPassenger()
        {
            calloutStarted = true;
            GameFiber.StartNew(delegate
            {
                try {
                    //Add Passenger
                    passenger = new Ped(spawnPoint);
                    passenger.BlockPermanentEvents = true;
                    passenger.WarpIntoVehicle(car, 0);

                    group = new Group(driver);
                    driverBlip.EnableRoute(System.Drawing.Color.Red);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(passenger, 3);

                    driver.Armor += 55;
                    passenger.Armor += 55;
                    driver.Health += 120;
                    passenger.Health += 120;

                    group.AddMember(passenger);


                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();

                    //When player pulls the vehicle over.....
                    if (driverBlip.Exists())
                    {
                        driverBlip.DisableRoute();
                        driverBlip.Delete();
                    }
                    int waitingCount = 0;
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
                                    break;
                                }
                            }
                            if (!driver.IsInVehicle(car, false) || !passenger.IsInVehicle(car, false))
                            {
                                break;
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
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }

                    //Driver puts hands up
                    car.IsDriveable = false;


                    driver.BlockPermanentEvents = true;
                    passenger.BlockPermanentEvents = true;

                    if (driver.IsInVehicle(car, false))
                    {
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);

                        driver.Tasks.LeaveVehicle(LeaveVehicleFlags.None);
                    }
                    if (passenger.IsInVehicle(car, false))
                    {
                        passenger.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(4000);
                    }


                    driver.Tasks.ClearImmediately();
                    passenger.Tasks.ClearImmediately();
                    driver.Tasks.PutHandsUp(18000, Game.LocalPlayer.Character);

                    passenger.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.LastVehicle.GetOffsetPosition(Vector3.RelativeRight * 2f), Game.LocalPlayer.Character.Heading + 180f, 3f).WaitForCompletion(2000);
                    passenger.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.LastVehicle.GetOffsetPosition(Vector3.RelativeBack * 4f), Game.LocalPlayer.Character.Heading + 180f, 3f).WaitForCompletion(2500);
                    passenger.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeBack * 1f), Game.LocalPlayer.Character.Heading, 3f);

                    while (Vector3.Distance(Game.LocalPlayer.Character.Position, passenger.Position) > 2f)
                    {
                        GameFiber.Yield();
                    }

                    passenger.Tasks.ClearImmediately();

                    passenger.Tasks.PutHandsUp(18000, Game.LocalPlayer.Character);

                    GameFiber.Sleep(2000);



                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {

                        GameFiber.Yield();
                        if (driver.Exists() && passenger.Exists())
                        {
                            if (Functions.IsPedArrested(driver) && Functions.IsPedArrested(passenger))
                            {
                                break;
                            }
                            if (driver.IsDead && passenger.IsDead)
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }

                        if (Game.LocalPlayer.Character.IsDead)
                        {
                            Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                            break;
                        }


                    }



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
        /// <summary>
        /// Hands up and sureEntryPoint.rnder
        /// </summary>
        private void situationThreeWithoutPassenger()
        {
            calloutStarted = true;
            GameFiber.StartNew(delegate
            {
                try
                {


                    driverBlip.EnableRoute(System.Drawing.Color.Red);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);


                    driver.Armor += 60;

                    driver.Health += 140;





                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();

                    //When player pulls the vehicle over.....
                    if (driverBlip.Exists())
                    {
                        driverBlip.DisableRoute();
                        driverBlip.Delete();
                    }
                    int waitingCount = 0;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {

                        GameFiber.Sleep(10);
                        waitingCount++;
                        if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 3.2f)
                            {
                                break;
                            }
                        }
                        if (!driver.IsInVehicle(car, false))
                        {
                            break;
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
                            break;
                        }
                    }


                

                    //Driver puts hands up
                    car.IsDriveable = false;


                    driver.BlockPermanentEvents = true;



                    if (driver.IsInVehicle(car, false))
                    {
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);

                        driver.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(4000);
                        driver.Tasks.ClearImmediately();
                    }




                    while (!Functions.IsPedGettingArrested(driver))
                    {
                        try {
                            driver.Tasks.PutHandsUp(6000, Game.LocalPlayer.Character).WaitForCompletion();

                        }
                        catch (Exception e)
                        {
                            break;
                        }
                    }





                    while (!Game.LocalPlayer.Character.IsDead)
                    {

                        GameFiber.Yield();
                        if (driver.Exists())
                        {
                            if (Functions.IsPedArrested(driver))
                            {
                                break;
                            }
                            if (driver.IsDead)
                            {
                                break;
                            }

                        }
                        else
                        {
                            break;
                        }
                        if (Game.LocalPlayer.Character.IsDead)
                        {
                            Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                            break;
                        }

                    }



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
        private void callForReinforcements(Ped caller)
        {
            Game.DisplayNotification("~r~Reinforcements ~s~have been alerted!");
            Vector3 spawnPoint = spawnPoint = World.GetNextPositionOnStreet(caller.Position.Around2D(50f));
            Vector3 callerPosition = caller.Position;
            while (Vector3.Distance(Game.LocalPlayer.Character.Position, spawnPoint) < 40f)
            {
                spawnPoint = World.GetNextPositionOnStreet(caller.Position.Around2D(50f));
            }
            reinforcementCar = new Vehicle("GRANGER", spawnPoint);
            reinforcementCar.RandomiseLicencePlate();
            reinforcementCarDriver = reinforcementCar.CreateRandomDriver();
            passenger1 = new Ped(spawnPoint);
            passenger2 = new Ped(spawnPoint);
            passenger1.WarpIntoVehicle(reinforcementCar, 0);
            passenger2.WarpIntoVehicle(reinforcementCar, 1);
            reinforcementCarDriver.BlockPermanentEvents = true;
            reinforcementCarDriver.IsPersistent = true;
            passenger1.BlockPermanentEvents = true;
            passenger2.BlockPermanentEvents = true;
            passenger1.IsPersistent = true;
            passenger2.IsPersistent = true;


            Group reinforcementGroup = new Group(reinforcementCarDriver);
            reinforcementGroup.AddMember(passenger1);
            reinforcementGroup.AddMember(passenger2);
            reinforcementGroup.AddMember(driver);
            reinforcementGroup.AddMember(passenger);

            Vector3 directionFromVehicleToPed = (Game.LocalPlayer.Character.Position - reinforcementCar.Position);
            directionFromVehicleToPed.Normalize();

            float heading = MathHelper.ConvertDirectionToHeading(directionFromVehicleToPed);
            reinforcementCar.Heading = heading;
            int drivingCount = 0;
            inReinforcementLoop = true;

            GameFiber.StartNew(delegate {
                try {
                    Game.LogTrivial("Reinforcements created");
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        reinforcementCarDriver.Tasks.DriveToPosition(callerPosition, 40f, VehicleDrivingFlags.Emergency);
                        if (Vector3.Distance(reinforcementCar.Position, callerPosition) < 15f)
                        {
                            break;
                        }
                        if (Vector3.Distance(reinforcementCar.Position, Game.LocalPlayer.Character.Position) < 15f)
                        {
                            break;
                        }
                        if (!reinforcementCar.Exists())
                        {
                            break;
                        }
                        GameFiber.Sleep(50);
                        drivingCount++;
                        if (drivingCount == 300)
                        {
                            Vector3 newspawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(30f));
                            while ((Vector3.Distance(newspawnPoint, Game.LocalPlayer.Character.Position) < 20f) || (newspawnPoint.Z - Game.LocalPlayer.Character.Position.Z < -4f) || (newspawnPoint.Z - Game.LocalPlayer.Character.Position.Z > 4f))
                            {
                                newspawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(30f));
                            }
                            reinforcementCar.Position = newspawnPoint;
                            directionFromVehicleToPed = (Game.LocalPlayer.Character.Position - reinforcementCar.Position);
                            directionFromVehicleToPed.Normalize();

                            heading = MathHelper.ConvertDirectionToHeading(directionFromVehicleToPed);
                            reinforcementCar.Heading = heading;
                            drivingCount = 0;
                        }

                    }
                    if (reinforcementCarBlip.Exists()) { reinforcementCarBlip.Delete(); }
                    //give weapons and fight etc
                    reinforcementCarDriver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
                    reinforcementCarDriver.Tasks.LeaveVehicle(LeaveVehicleFlags.None);
                    passenger1.Tasks.LeaveVehicle(LeaveVehicleFlags.None);
                    passenger2.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(2000);
                    reinforcementCarDriver.Tasks.ClearImmediately();
                    passenger1.Tasks.ClearImmediately();
                    passenger2.Tasks.ClearImmediately();
                    reinforcementCarDriver.Armor += 50;
                    passenger1.Armor += 50;
                    passenger2.Armor += 50;
                    reinforcementCarDriver.Health += 140;
                    passenger1.Health += 140;
                    passenger2.Health += 140;

                    reinforcementCarDriver.Inventory.GiveNewWeapon(new WeaponAsset(meleeWeaponsToSelectFrom[TrafficPolicerHandler.rnd.Next(meleeWeaponsToSelectFrom.Length)]), 500, true);
                    passenger2.Inventory.GiveNewWeapon(new WeaponAsset(firearmsToSelectFrom[TrafficPolicerHandler.rnd.Next(firearmsToSelectFrom.Length)]), 500, true);
                    passenger1.Inventory.GiveNewWeapon(new WeaponAsset(firearmsToSelectFrom[TrafficPolicerHandler.rnd.Next(firearmsToSelectFrom.Length)]), 500, true);
                    Rage.Native.NativeFunction.Natives.TaskCombatPed(reinforcementCarDriver, Game.LocalPlayer.Character, 0, 16);
                    Rage.Native.NativeFunction.Natives.TaskCombatPed(passenger1, Game.LocalPlayer.Character, 0, 16);
                    Rage.Native.NativeFunction.Natives.TaskCombatPed(passenger2, Game.LocalPlayer.Character, 0, 16);
                    reinforcementDriverDead = false;
                    passenger1.BlockPermanentEvents = true;
                    passenger2.BlockPermanentEvents = true;
                    reinforcementCarDriver.BlockPermanentEvents = true;
                    passenger1Dead = false;
                    passenger2Dead = false;

                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        if (reinforcementCarDriver.Exists())
                        {
                            if (reinforcementCarDriver.IsDead || Functions.IsPedArrested(reinforcementCarDriver))
                            {
                                reinforcementDriverDead = true;
                            }
                            else
                            {
                                if (!Functions.IsPedGettingArrested(reinforcementCarDriver))
                                {
                                    if (!Rage.Native.NativeFunction.Natives.IS_PED_IN_COMBAT<bool>(reinforcementCarDriver))
                                    {
                                        reinforcementCarDriver.Tasks.ClearImmediately();

                                    }
                                    reinforcementCarDriver.BlockPermanentEvents = true;



                                    Rage.Native.NativeFunction.Natives.TaskCombatPed(reinforcementCarDriver, Game.LocalPlayer.Character, 0, 16);
                                }
                            }
                        }
                        if (passenger1.Exists())
                        {
                            if (passenger1.IsDead || Functions.IsPedArrested(passenger1))
                            {
                                passenger1Dead = true;
                            }
                            else
                            {
                                if (!Functions.IsPedGettingArrested(passenger1))
                                {
                                    if (!Rage.Native.NativeFunction.Natives.IS_PED_IN_COMBAT<bool>(passenger1))
                                    {
                                        passenger1.Tasks.ClearImmediately();
                                    }
                                    passenger1.BlockPermanentEvents = true;


                                    Rage.Native.NativeFunction.Natives.TaskCombatPed(passenger1, Game.LocalPlayer.Character, 0, 16);
                                }
                            }
                        }
                        if (passenger2.Exists())
                        {
                            if (passenger2.IsDead || Functions.IsPedArrested(passenger2))
                            {
                                passenger2Dead = true;
                            }
                            else
                            {
                                if (!Functions.IsPedGettingArrested(passenger2))
                                {
                                    if (!Rage.Native.NativeFunction.Natives.IS_PED_IN_COMBAT<bool>(passenger2))
                                    {
                                        passenger2.Tasks.ClearImmediately();
                                    }
                                    passenger2.BlockPermanentEvents = true;


                                    Rage.Native.NativeFunction.Natives.TaskCombatPed(passenger2, Game.LocalPlayer.Character, 0, 16);
                                }
                            }
                        }
                        if (Game.LocalPlayer.Character.IsDead)
                        {

                            break;
                        }
                        if (reinforcementDriverDead && passenger1Dead && passenger2Dead)
                        {
                            break;
                        }
                        if (!reinforcementCarDriver.Exists() && !passenger1.Exists() && !passenger2.Exists())
                        {
                            break;
                        }
                        GameFiber.Sleep(3000);

                    }
                    inReinforcementLoop = false;
                } catch (Exception e) {
                    Game.LogTrivial(e.ToString());
                    Game.LogTrivial("Traffic Policer handled the exception successfully.");
                    if (reinforcementCarBlip.Exists()) { reinforcementCarBlip.Delete(); }
                    inReinforcementLoop = false;
                }

            });
        }
        /// <summary>
        /// Situation where passenger sends in reinforcements when given the chance to call for them
        /// </summary>
        private void situationFourWithPassenger()
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
                                        driver.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_PISTOL"), 500, true);

                                        Game.LocalPlayer.Character.Tasks.ClearSecondary();
                                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.ReverseStraight).WaitForCompletion(900);
                                        driver.Tasks.ClearImmediately();
                                        driver.WarpIntoVehicle(car, -1);
                                        Rage.Native.NativeFunction.Natives.TASK_DRIVE_BY(driver, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 100, 1, Game.GetHashKey("firing_pattern_burst_fire_driveby"));
                                        //Rage.Native.NativeFunction.Natives.TASK_DRIVE_BY(passenger, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 100, 1, Game.GetHashKey("firing_pattern_full_auto"));
                                        GameFiber.Sleep(3000);
                                        break;
                                    }

                                }
                            }
                            if (!driver.IsInVehicle(car, false) || !passenger.IsInVehicle(car, false))
                            {
                                break;
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
                                driver.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_PISTOL"), 500, true);

                                Game.LocalPlayer.Character.Tasks.ClearSecondary();
                                driver.Tasks.PerformDrivingManeuver(VehicleManeuver.ReverseStraight).WaitForCompletion(900);
                                driver.Tasks.ClearImmediately();
                                driver.WarpIntoVehicle(car, -1);
                                Rage.Native.NativeFunction.Natives.TASK_DRIVE_BY(driver, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 100, 1, Game.GetHashKey("firing_pattern_burst_fire_driveby"));
                                //Rage.Native.NativeFunction.Natives.TASK_DRIVE_BY(passenger, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 100, 1, Game.GetHashKey("firing_pattern_full_auto"));
                                GameFiber.Sleep(3000);
                                break;

                            }
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }
                    car.IsDriveable = false;
                    driver.BlockPermanentEvents = true;
                    driver.BlockPermanentEvents = true;
                    passenger.BlockPermanentEvents = true;



                    if (driver.IsInVehicle(car, false))
                    {
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
                        driver.Tasks.LeaveVehicle(LeaveVehicleFlags.None);
                    }
                    if (passenger.IsInVehicle(car, false))
                    {
                        passenger.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(4000);
                    }
                    driver = driver.ClonePed(true);

                    driver.Tasks.ClearImmediately();
                    passenger.Tasks.ClearImmediately();




                    driver.Inventory.GiveNewWeapon(new WeaponAsset(meleeWeaponsToSelectFrom[TrafficPolicerHandler.rnd.Next(meleeWeaponsToSelectFrom.Length)]), 500, true);
                    passenger.Inventory.GiveNewWeapon(new WeaponAsset(firearmsToSelectFrom[TrafficPolicerHandler.rnd.Next(firearmsToSelectFrom.Length)]), 500, true);

                    Rage.Native.NativeFunction.Natives.TaskCombatPed(driver, Game.LocalPlayer.Character, 0, 16);
                    Rage.Native.NativeFunction.Natives.TaskCombatPed(passenger, Game.LocalPlayer.Character, 0, 16);
                    GameFiber.Sleep(3000);
                    bool i = true;
                    bool driverDead = false;
                    bool passengerDead = false;
                    bool callingForReinforcements = false;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        if (driver.Exists())
                        {
                            if (driver.IsDead || Functions.IsPedArrested(driver))
                            {
                                driverDead = true;
                            }
                            else
                            {
                                if (!Functions.IsPedGettingArrested(driver))
                                {
                                    if (!Rage.Native.NativeFunction.Natives.IS_PED_IN_COMBAT<bool>(driver))
                                    {
                                        driver.Tasks.ClearImmediately();

                                    }
                                    driver.BlockPermanentEvents = true;



                                    Rage.Native.NativeFunction.Natives.TaskCombatPed(driver, Game.LocalPlayer.Character, 0, 16);
                                }
                            }

                        }
                        if (passenger.Exists() && !callingForReinforcements)
                        {
                            if (passenger.IsDead || Functions.IsPedArrested(passenger))
                            {
                                passengerDead = true;
                            }
                            else
                            {
                                if (!Functions.IsPedGettingArrested(passenger))
                                {
                                    if (!Rage.Native.NativeFunction.Natives.IS_PED_IN_COMBAT<bool>(passenger))
                                    {
                                        passenger.Tasks.ClearImmediately();
                                    }
                                    passenger.BlockPermanentEvents = true;


                                    Rage.Native.NativeFunction.Natives.TaskCombatPed(passenger, Game.LocalPlayer.Character, 0, 16);
                                }
                            }
                        }
                        if (passenger.Exists())
                        {
                            if (canCallForReinforcements && !Game.LocalPlayer.Character.IsDead && !passenger.IsDead)
                            {
                                callingForReinforcements = true;
                                GameFiber.StartNew(delegate
                                {

                                    canCallForReinforcements = false;
                                    int mobileCount = 0;

                                    passenger.Tasks.ClearImmediately();
                                    Rage.Native.NativeFunction.Natives.TASK_USE_MOBILE_PHONE(passenger, 1);
                                    Game.DisplayNotification("~r~Passenger is calling for reinforcements! ~b~Quick, shoot the phone out of their hands!");
                                    while (Rage.Native.NativeFunction.Natives.IS_PED_RUNNING_MOBILE_PHONE_TASK<bool>(passenger))
                                    {
                                        GameFiber.Sleep(100);
                                        mobileCount++;
                                        if (Game.LocalPlayer.Character.IsDead)
                                        {
                                            break;
                                        }
                                        if (mobileCount >= 40)
                                        {

                                            callForReinforcements(passenger);
                                            passenger.Tasks.ClearImmediately();
                                            break;
                                        }

                                    }
                                    if (mobileCount < 40 && !Game.LocalPlayer.Character.IsDead)
                                    {
                                        Game.DisplayNotification("You ~g~successfully ~s~prevented ~r~reinforcements ~s~from being alerted.");
                                    }
                                    callingForReinforcements = false;

                                });





                            }
                        }
                        if (Game.LocalPlayer.Character.IsDead)
                        {
                            Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                            break;
                        }
                        if (driverDead && passengerDead)
                        {
                            break;
                        }
                        if (!driver.Exists() && !passenger.Exists())
                        {
                            break;
                        }




                        GameFiber.Sleep(3000);
                        if (i)
                        {
                            canCallForReinforcements = true;
                            i = false;
                        }
                    }
                    if (reinforcementCarDriver.Exists() || passenger1.Exists() || passenger2.Exists())
                    {
                        while (!reinforcementCarDriver.IsDead || !passenger1.IsDead || !passenger2.IsDead)
                        {
                            GameFiber.Yield();
                            if (reinforcementDriverDead && passenger1Dead && passenger2Dead)
                            {
                                break;
                            }
                            if (!inReinforcementLoop)
                            {
                                break;
                            }
                            if (Game.LocalPlayer.Character.IsDead)
                            {
                                Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                                break;
                            }
                        }
                    }
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

        private void situationFiveWithoutPassenger()
        {
            calloutStarted = true;
            GameFiber.StartNew(delegate
            {
                try
                {


                    driverBlip.EnableRoute(System.Drawing.Color.Red);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);


                    driver.Armor += 60;

                    driver.Health += 140;





                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();

                    //When player pulls the vehicle over.....

                    if (driverBlip.Exists())
                    {
                        driverBlip.DisableRoute();
                        driverBlip.Delete();
                    }
                    int waitingCount = 0;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        try
                        {
                            GameFiber.Sleep(10);
                            waitingCount++;
                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 3.5f)
                                {
                                    break;
                                }
                            }
                            if (!driver.IsInVehicle(car, false))
                            {
                                break;
                            }
                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) > 60f)
                            {
                                driver = driver.ClonePed(true);
                                driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
                                break;
                            }
                            if (waitingCount >= 2000)
                            {
                                break;
                            }
                            if (isPlayerCheatingTrafficStop)
                            {
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }
                    int closeCount = 0;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 3.5f)
                        {

                            closeCount++;
                            if (Game.IsControllerButtonDownRightNow(ControllerButtons.DPadRight) || Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(System.Windows.Forms.Keys.E) || (closeCount >= 500))
                            {
                                GameFiber.Sleep(3500);
                                driver = driver.ClonePed(true);
                                driver.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_PISTOL"), 500, true);
                                break;
                            }

                        }
                        else if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) > 30f)
                        {
                            break;
                        }
                        else if (!driver.IsAlive)
                        {
                            calloutFinished = true;
                            break;
                        }
                        if (!driver.IsInVehicle(car, false))
                        {
                            break;
                        }
                        if (isPlayerCheatingTrafficStop)
                        {
                            driver = driver.ClonePed(true);
                            driver.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_PISTOL"), 500, true);
                            break;

                        }
                        GameFiber.Sleep(10);
                    }
                    if (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        Game.LocalPlayer.Character.Tasks.ClearSecondary();
                        if (driver.IsInVehicle(car, false))
                        {
                            driver.Tasks.PerformDrivingManeuver(VehicleManeuver.ReverseStraight).WaitForCompletion(900);
                            driver.Tasks.ClearImmediately();
                            driver.WarpIntoVehicle(car, -1);
                            Rage.Native.NativeFunction.Natives.TASK_DRIVE_BY(driver, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 100, 1, Game.GetHashKey("firing_pattern_full_auto"));
                            GameFiber.Sleep(3000);


                        }
                        else
                        {
                            driver.Tasks.FightAgainst(Game.LocalPlayer.Character);
                        }
                        GameFiber.Sleep(3000);
                        if (!Game.LocalPlayer.Character.IsDead && !driver.IsDead)
                        {
                            if (driver.IsInVehicle(car, false))
                            {
                                driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraight).WaitForCompletion(500);
                            }
                            //driver = driver.ClonePed(true);
                            if (Functions.IsPlayerPerformingPullover()) { Functions.ForceEndCurrentPullover(); }
                            pursuit = Functions.CreatePursuit();
                            Functions.AddPedToPursuit(pursuit, driver);
                            Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                            Functions.PlayScannerAudioUsingPosition("WE_HAVE SUSPECT_FLEEING_CRIMESCENE IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                        }
                    }
                    bool driverDead = false;

                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        if (driver.Exists())
                        {
                            if (driver.IsDead || Functions.IsPedArrested(driver))
                            {
                                driverDead = true;
                            }

                        }

                        if (Game.LocalPlayer.Character.IsDead)
                        {
                            Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                            break;
                        }
                        if (driverDead)
                        {
                            break;
                        }
                        if (!driver.Exists())
                        {
                            break;
                        }



                        GameFiber.Sleep(1000);
                    }


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

        private void situationSix()
        {
            calloutStarted = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    //Add Passenger

                    //passenger = new Ped(spawnPoint);
                    //passenger.BlockPermanentEvents = true;
                    //passenger.WarpIntoVehicle(car, 0);

                    if (!RelationshipGroup.DoesRelationshipGroupExist("CRIMINALS"))
                    {
                        RelationshipGroup criminal = new RelationshipGroup("CRIMINALS");
                        Game.LogTrivial("Criminals created");
                    }
                    driverBlip.EnableRoute(System.Drawing.Color.Red);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);
                    Game.LocalPlayer.Character.Armor += 10;

                    driver.Armor += 40;
                    //passenger.Armor += 50;
                    driver.Health += 170;


                    //passenger.Health += 130;




                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();

                    //When player pulls the vehicle over.....
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
                    if (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        driver = driver.ClonePed(true);

                        if (!driver.IsInVehicle(car, false))
                        {
                            driver.WarpIntoVehicle(car, -1);

                        }
                        driver.PlayAmbientSpeech("GENERIC_CURSE_HIGH", true);
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.BurnOut).WaitForCompletion(1200);




                        if (Functions.IsPlayerPerformingPullover()) { Functions.ForceEndCurrentPullover(); }
                        pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(pursuit, driver);
                        Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                        Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                        //Create the bikes
                        bikeSpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(160f));
                        while (Vector3.Distance(Game.LocalPlayer.Character.Position, bikeSpawnPoint) < 140f)
                        {
                            bikeSpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(160f));
                        }
                        GameFiber.Sleep(2000);


                        bike1 = new Vehicle(bikesToSelectFrom[TrafficPolicerHandler.rnd.Next(bikesToSelectFrom.Length)], bikeSpawnPoint);
                        bike1.RandomiseLicencePlate();
                        bike1.IsPersistent = true;
                        bikeRider1 = bike1.CreateRandomDriver();

                        bikeRider1.MakeMissionPed();

                        bikeRider1.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_PISTOL50"), 1500, true);

                        Rage.Native.NativeFunction.Natives.TASK_VEHICLE_CHASE(bikeRider1, Game.LocalPlayer.Character);

                        bikeBlip1 = bikeRider1.AttachBlip();

                        bikeRider1.Health += 160;
                        bikeRider1.Armor += 20;
                        
                        Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(bikeRider1, 3);
                        bikeRider1.BlockPermanentEvents = true;

                        bikeSpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(180f));
                        while (Vector3.Distance(Game.LocalPlayer.Character.Position, bikeSpawnPoint) < 150f)
                        {
                            bikeSpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(180f));
                        }

                        bike2 = new Vehicle(bikesToSelectFrom[TrafficPolicerHandler.rnd.Next(bikesToSelectFrom.Length)], bikeSpawnPoint);
                        bike2.RandomiseLicencePlate();
                        bike2.IsPersistent = true;
                        bikeRider2 = bike2.CreateRandomDriver();
                        bikeRider2.MakeMissionPed();

                        bikeRider2.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_PISTOL50"), 1500, true);
                        Rage.Native.NativeFunction.Natives.TASK_VEHICLE_CHASE(bikeRider2, Game.LocalPlayer.Character);
                        bikeBlip2 = bikeRider2.AttachBlip();
                        bikeRider2.Health += 160;
                        bikeRider2.Armor += 20;
                        bikeRider2.BlockPermanentEvents = true;
                        Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(bikeRider2, 3);
                        bikeSpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(140f));
                        while (Vector3.Distance(Game.LocalPlayer.Character.Position, bikeSpawnPoint) < 100f)
                        {
                            bikeSpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(140f));
                        }

                        pursueCar = new Vehicle(sportsCars[TrafficPolicerHandler.rnd.Next(sportsCars.Length)], bikeSpawnPoint);
                        pursueCar.RandomiseLicencePlate();
                        pursueCar.IsPersistent = true;
                        pursueCarDriver = pursueCar.CreateRandomDriver();
                        pursueCarDriver.MakeMissionPed();
                        pursueCarDriver.Health += 160;
                        pursueCarDriver.Armor += 20;
                        pursueCarDriver.BlockPermanentEvents = true;


                        pursueCar.Mods.InstallModKit();
                        pursueCar.Mods.ApplyAllMods();
                        

                        pursueCarDriver.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_PISTOL"), 1500, true);
                        Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(pursueCarDriver, 3);
                        Rage.Native.NativeFunction.Natives.TASK_VEHICLE_CHASE(pursueCarDriver, Game.LocalPlayer.Character);

                        pursueCarBlip = pursueCar.AttachBlip();

                        pursueCarBlip.Color = System.Drawing.Color.Black;
                        bikeBlip1.Color = System.Drawing.Color.Black;
                        bikeBlip2.Color = System.Drawing.Color.Black;


                        bikeRider1.GiveHelmet(false, HelmetTypes.RegularMotorcycleHelmet, 3);
                        bikeRider2.GiveHelmet(false, HelmetTypes.RegularMotorcycleHelmet, 2);

                        GameFiber.Sleep(2000);
                        driver.PlayAmbientSpeech("GENERIC_INSULT_HIGH", true);
                        driverDead = false;
                        biker1Dead = false;
                        biker2Dead = false;
                        pursueDriverDead = false;
                        switchedPursuit = false;
                        bikeRider1.RelationshipGroup = "CRIMINALS";
                        bikeRider2.RelationshipGroup = "CRIMINALS";
                        pursueCarDriver.RelationshipGroup = "CRIMINALS";
                        Game.SetRelationshipBetweenRelationshipGroups(Game.LocalPlayer.Character.RelationshipGroup.Name, "CRIMINALS", Relationship.Hate);
                        Game.SetRelationshipBetweenRelationshipGroups("CRIMINALS", Game.LocalPlayer.Character.RelationshipGroup, Relationship.Hate);
                    }

                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        try {
                            if (driver.Exists())
                            {
                                if (driver.IsDead)
                                {
                                    if (!switchedPursuit)
                                    {
                                        driverDead = true;
                                        if (!Functions.IsPursuitStillRunning(pursuit))
                                        {
                                            if (Functions.IsPlayerPerformingPullover()) { Functions.ForceEndCurrentPullover(); }
                                            pursuit = Functions.CreatePursuit();

                                        }
                                        if (!biker1Dead)
                                        {
                                            bikeRider1 = bikeRider1.ClonePed(true);
                                            Functions.AddPedToPursuit(pursuit, bikeRider1);
                                            switchedPursuit = true;
                                        }
                                        if (!biker2Dead)
                                        {
                                            bikeRider2 = bikeRider2.ClonePed(true);
                                            Functions.AddPedToPursuit(pursuit, bikeRider2);
                                            switchedPursuit = true;
                                        }
                                        if (!pursueDriverDead)
                                        {
                                            pursueCarDriver = pursueCarDriver.ClonePed(true);
                                            Functions.AddPedToPursuit(pursuit, pursueCarDriver);
                                            switchedPursuit = true;
                                        }
                                        if (switchedPursuit) { Functions.SetPursuitIsActiveForPlayer(pursuit, true); }

                                        Game.DisplayNotification("The driver of the original vehicle was killed.");
                                        break;
                                    }
                                }
                                if (Functions.IsPedArrested(driver))
                                {
                                    if (!Functions.IsPursuitStillRunning(pursuit))
                                    {
                                        if (Functions.IsPlayerPerformingPullover()) { Functions.ForceEndCurrentPullover(); }
                                        pursuit = Functions.CreatePursuit();

                                    }
                                    if (!biker1Dead)
                                    {
                                        bikeRider1 = bikeRider1.ClonePed(true);
                                        Functions.AddPedToPursuit(pursuit, bikeRider1);
                                        switchedPursuit = true;
                                    }
                                    if (!biker2Dead)
                                    {
                                        bikeRider2 = bikeRider2.ClonePed(true);
                                        Functions.AddPedToPursuit(pursuit, bikeRider2);
                                        switchedPursuit = true;
                                    }
                                    if (!pursueDriverDead)
                                    {
                                        pursueCarDriver = pursueCarDriver.ClonePed(true);
                                        Functions.AddPedToPursuit(pursuit, pursueCarDriver);
                                        switchedPursuit = true;
                                    }
                                    if (switchedPursuit) { Functions.SetPursuitIsActiveForPlayer(pursuit, true); }
                                    break;
                                }

                            }
                            else
                            {
                                if (!switchedPursuit)
                                {
                                    if (!Functions.IsPursuitStillRunning(pursuit))
                                    {
                                        if (Functions.IsPlayerPerformingPullover()) { Functions.ForceEndCurrentPullover(); }
                                        pursuit = Functions.CreatePursuit();

                                    }
                                    if (!biker1Dead)
                                    {
                                        bikeRider1 = bikeRider1.ClonePed(true);
                                        Functions.AddPedToPursuit(pursuit, bikeRider1);
                                        switchedPursuit = true;
                                    }
                                    if (!biker2Dead)
                                    {
                                        bikeRider2 = bikeRider2.ClonePed(true);
                                        Functions.AddPedToPursuit(pursuit, bikeRider2);
                                        switchedPursuit = true;
                                    }
                                    if (!pursueDriverDead)
                                    {
                                        pursueCarDriver = pursueCarDriver.ClonePed(true);
                                        Functions.AddPedToPursuit(pursuit, pursueCarDriver);
                                        switchedPursuit = true;
                                    }
                                    if (switchedPursuit) { Functions.SetPursuitIsActiveForPlayer(pursuit, true); }
                                    break;
                                }
                            }
                            GameFiber.StartNew(delegate
                            {
                                Ped[] pedsNearPlayer = Game.LocalPlayer.Character.GetNearbyPeds(10);
                                foreach (Ped pedNear in pedsNearPlayer)
                                {
                                    if (pedNear.Exists())
                                    {
                                        int pedNearType = Rage.Native.NativeFunction.Natives.GET_PED_TYPE<int>(pedNear);
                                        if ((pedNearType == 6) || (pedNearType == 27))
                                        {
                                            pedNear.RelationshipGroup = Game.LocalPlayer.Character.RelationshipGroup;
                                            Game.SetRelationshipBetweenRelationshipGroups(Game.LocalPlayer.Character.RelationshipGroup.Name, "CRIMINALS", Relationship.Hate);
                                            Game.SetRelationshipBetweenRelationshipGroups("CRIMINALS", Game.LocalPlayer.Character.RelationshipGroup, Relationship.Hate);
                                            Game.LogTrivial("Added cop to relationshipgroup");
                                        }
                                    }
                                    GameFiber.Yield();
                                }
                            });

                            if (Game.LocalPlayer.Character.IsDead)
                            {
                                Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                                break;
                            }
                            if (driverDead && biker1Dead && biker2Dead && pursueDriverDead)
                            {
                                break;
                            }
                            if (!driver.Exists() && !bikeRider1.Exists() && !bikeRider2.Exists() && !pursueCarDriver.Exists())
                            {
                                break;
                            }

                            if (bikeRider1.Exists())
                            {
                                if (!bikeRider1.IsDead)
                                {
                                    if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                                    {
                                        if (Vector3.Distance(bikeRider1.Position, Game.LocalPlayer.Character.Position) < 45f)
                                        {
                                            bikeRider1.Tasks.FightAgainstClosestHatedTarget(55f, 3000);
                                        }
                                        else
                                        {
                                            Ped pedToChase = Game.LocalPlayer.Character;
                                            Ped[] nearbyPeds = bikeRider1.GetNearbyPeds(4);
                                            foreach (Ped nearPed in nearbyPeds)
                                            {
                                                GameFiber.Yield();
                                                if (nearPed.Exists())
                                                {
                                                    if (nearPed.RelationshipGroup == Game.LocalPlayer.Character.RelationshipGroup)
                                                    {
                                                        pedToChase = nearPed;

                                                        break;
                                                    }
                                                }
                                            }
                                            Rage.Native.NativeFunction.Natives.TASK_VEHICLE_CHASE(bikeRider1, pedToChase);
                                        }
                                    }
                                    if (!bikeRider1.IsInVehicle(bike1, false))
                                    {
                                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, bikeRider1.Position) > 110f)
                                        {


                                            bikeRider1.WarpIntoVehicle(bike1, -1);

                                            Rage.Native.NativeFunction.Natives.TASK_VEHICLE_CHASE(bikeRider1, Game.LocalPlayer.Character);
                                        }
                                        else
                                        {
                                            bikeRider1.Tasks.FightAgainstClosestHatedTarget(55f, 4000);
                                        }
                                    }
                                    else
                                    {
                                        Rage.Native.NativeFunction.Natives.TASK_VEHICLE_CHASE(bikeRider1, Game.LocalPlayer.Character);
                                    }
                                }
                                else
                                {
                                    if (bikeBlip1.Exists())
                                    {
                                        bikeBlip1.Delete();
                                    }
                                    bikeRider1.Dismiss();
                                    biker1Dead = true;
                                }

                            }
                            GameFiber.Yield();
                            if (bikeRider2.Exists())
                            {
                                if (!bikeRider2.IsDead)
                                {
                                    if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                                    {
                                        if (Vector3.Distance(bikeRider2.Position, Game.LocalPlayer.Character.Position) < 45f)
                                        {
                                            bikeRider2.Tasks.FightAgainstClosestHatedTarget(55f);
                                        }
                                        else
                                        {
                                            Rage.Native.NativeFunction.Natives.TASK_VEHICLE_CHASE(bikeRider2, Game.LocalPlayer.Character);
                                        }
                                    }
                                    if (!bikeRider2.IsInVehicle(bike2, false))
                                    {
                                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, bikeRider2.Position) > 110f)
                                        {
                                            bikeRider2.WarpIntoVehicle(bike2, -1);

                                            Rage.Native.NativeFunction.Natives.TASK_VEHICLE_CHASE(bikeRider2, Game.LocalPlayer.Character);
                                        }
                                        else
                                        {
                                            bikeRider2.Tasks.FightAgainstClosestHatedTarget(55f);
                                        }
                                    }
                                    else
                                    {
                                        Ped pedToChase = Game.LocalPlayer.Character;
                                        Ped[] nearbyPeds = bikeRider2.GetNearbyPeds(4);
                                        foreach (Ped nearPed in nearbyPeds)
                                        {
                                            GameFiber.Yield();
                                            if (nearPed.Exists())
                                            {
                                                if (nearPed.RelationshipGroup == Game.LocalPlayer.Character.RelationshipGroup)
                                                {
                                                    pedToChase = nearPed;
                                                    break;
                                                }
                                            }
                                        }

                                        Rage.Native.NativeFunction.Natives.TASK_VEHICLE_CHASE(bikeRider2, pedToChase);
                                    }

                                }
                                else
                                {
                                    if (bikeBlip2.Exists())
                                    {
                                        bikeBlip2.Delete();
                                    }
                                    bikeRider2.Dismiss();
                                    biker2Dead = true;
                                }

                            }
                            if (pursueCarDriver.Exists())
                            {
                                if (pursueCarDriver.IsDead)
                                {
                                    if (pursueCarBlip.Exists())
                                    {
                                        pursueCarBlip.Delete();

                                    }
                                    pursueCarDriver.Dismiss();
                                    pursueDriverDead = true;
                                }
                                else
                                {
                                    bool checkNearPeds = true;
                                    if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                                    {
                                        if (Vector3.Distance(pursueCarDriver.Position, Game.LocalPlayer.Character.Position) < 55f)
                                        {

                                            pursueCarDriver.Tasks.FightAgainstClosestHatedTarget(70f);
                                            GameFiber.Sleep(2000);
                                        }
                                        else
                                        {
                                            Rage.Native.NativeFunction.Natives.TASK_VEHICLE_CHASE(pursueCarDriver, Game.LocalPlayer.Character);
                                            checkNearPeds = false;
                                        }
                                    }
                                    if (Vector3.Distance(pursueCarDriver.Position, Game.LocalPlayer.Character.Position) > 300f)
                                    {
                                        bikeSpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(180f));
                                        while (Vector3.Distance(Game.LocalPlayer.Character.Position, bikeSpawnPoint) < 150f)
                                        {
                                            bikeSpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(180f));
                                        }
                                        pursueCar.Position = bikeSpawnPoint;
                                        pursueCarDriver.WarpIntoVehicle(pursueCar, -1);
                                        Rage.Native.NativeFunction.Natives.TASK_VEHICLE_CHASE(pursueCarDriver, Game.LocalPlayer.Character);
                                    }
                                    if (pursueCarDriver.IsInVehicle(pursueCar, false) && checkNearPeds)
                                    {
                                        Ped pedToChase = Game.LocalPlayer.Character;
                                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, pursueCarDriver.Position) < 110f)
                                        {
                                            Ped[] nearbyPeds = pursueCarDriver.GetNearbyPeds(4);
                                            foreach (Ped nearPed in nearbyPeds)
                                            {
                                                GameFiber.Yield();
                                                if (nearPed.Exists())
                                                {
                                                    if (nearPed.RelationshipGroup == Game.LocalPlayer.Character.RelationshipGroup)
                                                    {
                                                        pedToChase = nearPed;
                                                        break;
                                                    }
                                                }
                                            }
                                        }


                                        Rage.Native.NativeFunction.Natives.TASK_VEHICLE_CHASE(pursueCarDriver, pedToChase);
                                    }
                                    else
                                    {
                                        pursueCarDriver.Tasks.FightAgainstClosestHatedTarget(70f);
                                    }

                                }
                            }


                            GameFiber.Sleep(3200);
                        }
                        catch (Exception e)
                        {
                            Game.LogTrivial(e.ToString());
                            Game.LogTrivial("Exception has been handled, not crashing.");
                            if (!TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                            {
                                break;
                            }
                            continue;
                        }
                    }

                    if (switchedPursuit)
                    {
                        Game.DisplayNotification("The ~r~driveby shooters~s~ are fleeing!");
                        if (bikeBlip1.Exists()) { bikeBlip1.Delete(); }
                        if (bikeBlip2.Exists()) { bikeBlip2.Delete(); }
                        if (pursueCarBlip.Exists()) { pursueCarBlip.Delete(); }

                        while (Functions.IsPursuitStillRunning(pursuit))
                        {
                            GameFiber.Yield();
                            if (Game.LocalPlayer.Character.IsDead)
                            {
                                Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                                break;
                            }
                        }
                    }


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

        //FIRE
        private void situationSeven()
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
                    
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 2);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(passenger, 2);

                    driver.Armor += 50;
                    passenger.Armor += 50;
                    driver.Health += 130;
                    passenger.Health += 130;




                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();
                    if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    {
                        playerVehicle = Game.LocalPlayer.Character.CurrentVehicle;
                    }
                    else
                    {
                        playerVehicle = Game.LocalPlayer.Character.LastVehicle;
                    }
                    //When player pulls the vehicle over.....
                    if (driverBlip.Exists())
                    {
                        driverBlip.DisableRoute();
                        driverBlip.Delete();
                    }
                    int waitingCount = 0;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        try
                        {
                            GameFiber.Sleep(10);
                            waitingCount++;
                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 3.5f)
                                {
                                    break;
                                }
                            }
                            if (!driver.IsInVehicle(car, false))
                            {
                                break;
                            }
                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) > 55f)
                            {
                                driver = driver.ClonePed(true);
                                driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
                                break;
                            }
                            if (waitingCount >= 2000)
                            {
                                break;
                            }
                            if (isPlayerCheatingTrafficStop)
                            {
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }
                    int closeCount = 0;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 3.5f)
                        {

                            closeCount++;
                            if (Game.IsControllerButtonDown(ControllerButtons.DPadRight) || Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(System.Windows.Forms.Keys.E) || (closeCount >= 500))
                            {
                                GameFiber.Sleep(3500);

                                break;
                            }

                        }
                        else if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) > 30f)
                        {
                            driver = driver.ClonePed(true);
                            driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
                            while (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) > 10f)
                            {
                                GameFiber.Yield();
                            }
                            while (Game.LocalPlayer.Character.IsInVehicle(playerVehicle, false))
                            {
                                GameFiber.Yield();
                            }
                            GameFiber.Sleep(2000);
                            break;
                        }
                        if (!driver.Exists())
                        {
                            calloutFinished = true;
                            break;
                        }
                        if (!driver.IsInVehicle(car, false))
                        {
                            break;
                        }
                        if (isPlayerCheatingTrafficStop)
                        {
                            GameFiber.Sleep(1000);
                            break;
                        }


                        GameFiber.Sleep(10);
                    }

                    if (passenger.IsInVehicle(car, false))
                    {
                        passenger.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(1000);

                    }

                    driver = driver.ClonePed(true);
                    driver.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_STUNGUN"), 500, true);
                    if (driver.IsInVehicle(car, false))
                    {
                        GameFiber.StartNew(delegate
                        {
                            driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardLeft).WaitForCompletion(1200);
                            driver.Tasks.ClearImmediately();
                            driver.WarpIntoVehicle(car, -1);
                            GameFiber.Sleep(1000);
                            Rage.Native.NativeFunction.Natives.TASK_DRIVE_BY(driver, Game.LocalPlayer.Character, 0, 0, 0, 0, 50.0f, 100, 1, Game.GetHashKey("firing_pattern_full_auto"));
                        });
                    }
                    else
                    {
                        driver.Tasks.FightAgainst(Game.LocalPlayer.Character, 12000);
                    }
                    driver.PlayAmbientSpeech("GENERIC_INSULT_HIGH");
                    if (passenger.IsAlive)
                    {
                        if (Functions.IsPedGettingArrested(passenger))
                        {

                            passenger = passenger.ClonePed(true);

                        }

                        passenger.Tasks.FollowNavigationMeshToPosition(playerVehicle.GetOffsetPosition(Vector3.RelativeRight * 2f), playerVehicle.Heading + 90f, 6f).WaitForCompletion(9000);
                        passenger.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_PETROLCAN"), 20, true);
                    }
                    if (passenger.IsAlive)
                    {
                        if (Vector3.Distance(passenger.Position, playerVehicle.Position) < 3f)
                        {




                            Vector3 directionFromPedToCar = (playerVehicle.Position - passenger.Position);
                            directionFromPedToCar.Normalize();
                            passenger.Tasks.AchieveHeading(MathHelper.ConvertDirectionToHeading(directionFromPedToCar)).WaitForCompletion(1800);
                            passenger.Tasks.PlayAnimation("missexile3", "ex03_dingy_search_case_base_michael", 1.5f, AnimationFlags.None).WaitForCompletion(3000);
                            if (passenger.IsAlive)
                            {
                                GameFiber.StartNew(delegate
                                {
                                    firePed = Game.LocalPlayer.Character.LastVehicle.CreateRandomDriver();
                                    firePed.IsVisible = false;
                                    firePed.BlockPermanentEvents = true;
                                    firePed.Voice = null;
                                    GameFiber.Sleep(2000);
                                    Rage.Native.NativeFunction.Natives.START_ENTITY_FIRE(firePed);
                                    GameFiber.Sleep(500);
                                    int fireCount = 0;
                                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                                    {
                                        fireCount++;

                                        if (firePed.Exists())
                                        {
                                            firePed.WarpIntoVehicle(playerVehicle, -1);
                                            firePed.Health = 200;
                                        }
                                        if (fireCount >= 850)
                                        {
                                            firePed.Delete();
                                            playerVehicle.Explode();
                                            Game.DisplayNotification("You'll have to commandeer someone's vehicle in the name of the ~g~law...");
                                            GameFiber.Yield();

                                            break;
                                        }
                                        if (Game.LocalPlayer.Character.IsDead)
                                        {
                                            Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                                            calloutFinished = true;
                                            break;
                                        }
                                        GameFiber.Sleep(10);
                                    }
                                });
                            }
                        }
                    }
                    if (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        if (passenger.IsAlive)
                        {

                            passenger.Tasks.FollowNavigationMeshToPosition(car.GetOffsetPosition(Vector3.RelativeRight * 1.5f), car.Heading, 2.5f).WaitForCompletion(4000);
                            passenger.PlayAmbientSpeech("GENERIC_INSULT_HIGH");
                            passenger.Tasks.EnterVehicle(car, 3000, 0).WaitForCompletion(3500);
                        }
                        if (passenger.IsAlive || driver.IsAlive)
                        {
                            Game.LogTrivial("Pursuit created");
                            if (Functions.IsPlayerPerformingPullover()) { Functions.ForceEndCurrentPullover(); }
                            pursuit = Functions.CreatePursuit();
                        }
                        else
                        {
                            calloutFinished = true;
                            return;
                        }
                        driverDead = true;
                        passengerDead = true;
                        if (driver.IsAlive)
                        {
                            driver =driver.ClonePed(true);

                            Functions.AddPedToPursuit(pursuit, driver);
                            driver.PlayAmbientSpeech("GENERIC_FUCK_YOU", true);
                            driverDead = false;

                        }
                        if (passenger.IsAlive)
                        {
                            Functions.AddPedToPursuit(pursuit, passenger);
                            passengerDead = false;
                        }
                        Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                        GameFiber.Sleep(1000);
                        Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                    }


                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        if (driver.Exists())
                        {
                            if (driver.IsDead || Functions.IsPedArrested(driver))
                            {
                                driverDead = true;
                            }

                        }
                        if (passenger.Exists())
                        {
                            if (passenger.IsDead || Functions.IsPedArrested(passenger))
                            {
                                passengerDead = true;
                            }
                        }

                        if (Game.LocalPlayer.Character.IsDead)
                        {
                            Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                            break;
                        }
                        if (driverDead && passengerDead)
                        {
                            break;
                        }
                        if (!driver.Exists() && !passenger.Exists())
                        {
                            break;
                        }



                        GameFiber.Sleep(1000);
                    }


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

        private void situationEight()
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
                    driver.RelationshipGroup = "ANRYDRIVER";
                    passenger.RelationshipGroup = "ANGRYDRIVER";
                    Game.SetRelationshipBetweenRelationshipGroups("ANGRYDRIVERS", "COP", Relationship.Hate);

                    group = new Group(driver);
                    driverBlip.EnableRoute(System.Drawing.Color.Red);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(passenger, 3);

                    driver.Armor += 55;
                    passenger.Armor += 55;
                    driver.Health += 140;
                    passenger.Health += 140;

                    


                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();

                    //When player pulls the vehicle over.....
                    if (driverBlip.Exists())
                    {
                        driverBlip.DisableRoute();
                        driverBlip.Delete();
                    }
                    
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        try
                        {
                            GameFiber.Yield();
                            

                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 3.2f)
                                {
                                    if (Functions.IsPlayerPerformingPullover())
                                    {
                                        break;
                                    }
                                }
                            }
                            if (!driver.IsInVehicle(car, false) || !passenger.IsInVehicle(car, false))
                            {
                                break;
                            }

                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) > 60f)
                            {
                                endLikeNormal = false;
                                End();
                                return;
                            }
                            
                            if (isPlayerCheatingTrafficStop)
                            {
                                break;
                            }

                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }
                    Game.LogTrivial("Listening for occupants out of car.");
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        GameFiber.Yield();
                        if (!driver.IsInVehicle(car, false) || !passenger.IsInVehicle(car, false))
                        {
                            driver = driver.ClonePed(true);
                            break;
                        }

                        else if (!Functions.IsPlayerPerformingPullover())
                        {
                            GameFiber.Sleep(500);
                            if (!driver.IsInVehicle(car, false) || !passenger.IsInVehicle(car, false))
                            {
                                driver = driver.ClonePed(true);
                                break;
                            }
                            Game.LogTrivial("No pullover");
                            if (!processedKeyTaking)
                            {
                                processedKeyTaking = true;
                                break;
                            }
                            else
                            {

                                calloutFinished = true;
                                End();
                                return;
                            }


                        }
                        
                        if (isPlayerCheatingTrafficStop)
                        {
                            break;
                        }

                    }

                    //Driver puts hands up


                    Game.LogTrivial("Starting fight");
                    driver.BlockPermanentEvents = true;
                    passenger.BlockPermanentEvents = true;
                    

                    if (driver.IsInVehicle(car, false))
                    {
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);

                        driver.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(1000);
                    }
                    if (passenger.IsInVehicle(car, false))
                    {
                        passenger.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(1000);
                    }






                    passenger.KeepTasks = true;
                    passenger.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_PISTOL"), 1500, true);
                    passenger.Tasks.FightAgainst(Game.LocalPlayer.Character);



                    driver.Tasks.ClearImmediately();
                    driver.Inventory.GiveNewWeapon(new WeaponAsset(meleeWeaponsToSelectFrom[TrafficPolicerHandler.rnd.Next(meleeWeaponsToSelectFrom.Length)]), 500, true);
                    driver.KeepTasks = true;
                    


                    bool driverDead = false;
                    bool passengerDead = false;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        GameFiber.Yield();
                        if (driver.Exists())
                        {
                            
                            
                            if (driver.IsDead || Functions.IsPedArrested(driver))
                            {
                                driverDead = true;
                            }
                            else
                            {
                                driver.RelationshipGroup = "ANRYDRIVER";
                                if (!Functions.IsPedGettingArrested(driver))
                                {
                                    if (!Rage.Native.NativeFunction.Natives.IS_PED_IN_COMBAT<bool>(driver))
                                    {
                                        driver.Tasks.ClearImmediately();
                                        Rage.Native.NativeFunction.Natives.TaskCombatPed(driver, Game.LocalPlayer.Character, 0, 16);

                                    }
                                    driver.BlockPermanentEvents = true;

                                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);
                                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(driver, 46, true);

                                    driver.Tasks.FightAgainst(Game.LocalPlayer.Character);
                                }
                            }

                        }
                        if (passenger.Exists())
                        {
                            passenger.RelationshipGroup = "ANGRYDRIVER";
                            if (passenger.IsDead || Functions.IsPedArrested(passenger))
                            {
                                passengerDead = true;
                            }
                            else
                            {
                                if (!Functions.IsPedGettingArrested(passenger))
                                {
                                    if (!Rage.Native.NativeFunction.Natives.IS_PED_IN_COMBAT<bool>(passenger))
                                    {
                                        passenger.Tasks.ClearImmediately();
                                        Rage.Native.NativeFunction.Natives.TaskCombatPed(passenger, Game.LocalPlayer.Character, 0, 16);
                                    }
                                    passenger.BlockPermanentEvents = true;
                                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(passenger, 3);
                                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ATTRIBUTES(passenger, 46, true);
                                    passenger.Tasks.FightAgainst(Game.LocalPlayer.Character);
                                }


                            }
                        }
                        if (Game.LocalPlayer.Character.IsDead)
                        {

                            Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");

                            break;
                        }
                        if (driverDead && passengerDead)
                        {

                            break;
                        }
                        if (!driver.Exists() && !passenger.Exists())
                        {
                            break;
                        }




                        GameFiber.Sleep(1100);
                    }


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
    
        private void situationNine()
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

                    driver.Armor += 55;
                    passenger.Armor += 55;
                    driver.Health += 120;
                    passenger.Health += 120;

                    


                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();

                    //When player pulls the vehicle over.....
                    if (driverBlip.Exists())
                    {
                        driverBlip.DisableRoute();
                        driverBlip.Delete();
                    }
                    int waitingCount = 0;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        try
                        {
                            GameFiber.Sleep(10);
                            waitingCount += 1;

                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 4.5f)
                                {
                                    break;
                                }
                            }
                            if (!driver.IsInVehicle(car, false) || !passenger.IsInVehicle(car, false))
                            {
                                break;
                            }

                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) > 60f)
                            {
                                driver = driver.ClonePed(true);
                                passenger = passenger.ClonePed(true);
                                break;
                            }
                            if (waitingCount >= 2000)
                            {
                                break;
                            }
                            if (isPlayerCheatingTrafficStop)
                            {
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }

                    //Driver puts hands up
                    car.IsDriveable = false;


                    driver.BlockPermanentEvents = true;
                    passenger.BlockPermanentEvents = true;

                    if (driver.IsInVehicle(car, false))
                    {
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);

                        driver.Tasks.LeaveVehicle(LeaveVehicleFlags.None);
                    }
                    if (passenger.IsInVehicle(car, false))
                    {
                        passenger.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(4000);
                    }


                    driver.Tasks.ClearImmediately();
                    passenger.Tasks.ClearImmediately();
                    driver.Tasks.PutHandsUp(18000, Game.LocalPlayer.Character);

                    passenger.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.LastVehicle.GetOffsetPosition(Vector3.RelativeRight * 2f), Game.LocalPlayer.Character.Heading + 180f, 3f).WaitForCompletion(2000);
                    passenger.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.LastVehicle.GetOffsetPosition(Vector3.RelativeBack * 4f), Game.LocalPlayer.Character.Heading + 180f, 3f).WaitForCompletion(2500);
                    passenger.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeBack * 1f), Game.LocalPlayer.Character.Heading, 3f);
                    
                    while (Vector3.Distance(Game.LocalPlayer.Character.Position, passenger.Position) > 5f)
                    {
                        GameFiber.Yield();
                        passenger.Tasks.FollowNavigationMeshToPosition(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeBack * 1f), Game.LocalPlayer.Character.Heading, 3f).WaitForCompletion(700);
                    }

                    passenger.Tasks.ClearImmediately();

                    passenger.Tasks.PutHandsUp(18000, Game.LocalPlayer.Character);




                    bool playerHasBeenSurprised = false;
                    int waitCount = 0;
                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {

                        GameFiber.Sleep(10);
                        waitCount++;
                        if (!playerHasBeenSurprised)
                        {
                            if (waitCount >= 1000)
                            {
                                Game.LogTrivial("Timeout");
                                playerHasBeenSurprised = true;
                                
                                driver.Tasks.ClearImmediately();
                                driver.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_PISTOL"), 500, true);
                                driver.Tasks.FightAgainst(Game.LocalPlayer.Character);
                                
                                passenger.Tasks.ClearImmediately();
                                passenger.Inventory.GiveNewWeapon(new WeaponAsset(meleeWeaponsToSelectFrom[TrafficPolicerHandler.rnd.Next(meleeWeaponsToSelectFrom.Length)]), 500, true);
                                passenger.Tasks.FightAgainst(Game.LocalPlayer.Character);
                            }
                        }
                        if (driver.Exists())
                        {
                            Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);

                            if (Functions.IsPedGettingArrested(driver) && !playerHasBeenSurprised)
                            {
                                Game.LogTrivial("Driver");
                                playerHasBeenSurprised = true;
                                GameFiber.Sleep(1500);
                                passenger.Tasks.ClearImmediately();
                                passenger.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_KNIFE"), 500, true);
                                passenger.Tasks.FightAgainst(Game.LocalPlayer.Character);
                                GameFiber.Sleep(4000);
                                driver = driver.ClonePed(true);
                                GameFiber.Yield();

                                driver.Tasks.FightAgainst(Game.LocalPlayer.Character);
                            }
                        }
                        else {
                            Game.LogTrivial("Driver doesn't exist");
                        }
                        
                        if (passenger.Exists())
                        {
                            Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(passenger, 3);
                            if (Functions.IsPedGettingArrested(passenger) && !playerHasBeenSurprised)
                            {
                                Game.LogTrivial("Passenger");
                                playerHasBeenSurprised = true;
                                GameFiber.Sleep(1500);
                                driver.Tasks.ClearImmediately();
                                driver.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_KNIFE"), 500, true);
                                driver.Tasks.FightAgainst(Game.LocalPlayer.Character);
                                GameFiber.Sleep(4000);
                                passenger = passenger.ClonePed(true);
                                GameFiber.Yield();
                                
                                passenger.Tasks.FightAgainst(Game.LocalPlayer.Character);
                            }
                        }
                        else
                        {
                            Game.LogTrivial("Passenger doesn't exist.");
                        }
                        if (driver.Exists() && passenger.Exists())
                        {
                            if (Functions.IsPedArrested(driver) && Functions.IsPedArrested(passenger))
                            {
                                break;
                            }
                            if (driver.IsDead && passenger.IsDead)
                            {
                                break;
                            }
                        }
                        else
                        {
                            Game.LogTrivial("Driver&Passenger don't exist");
                            break;
                        }

                        if (Game.LocalPlayer.Character.IsDead)
                        {
                            Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                            break;
                        }


                    }



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
        private void situationTen()
        {
            calloutStarted = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    //Add Passenger

                    


                    driverBlip.EnableRoute(System.Drawing.Color.Red);
                    Rage.Native.NativeFunction.Natives.SET_PED_COMBAT_ABILITY(driver, 3);
                    

                    driver.Armor += 50;
                    
                    driver.Health += 130;
                    
                    



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
                                        GameFiber.Sleep(1000);
                                        
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
                                driver = driver.ClonePed(true);
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

                                break;

                            }
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }
                    if (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        if (Functions.IsPlayerPerformingPullover()) { Functions.ForceEndCurrentPullover(); }
                        pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(pursuit, driver);
                        Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                        Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                    }
                    bool driverDead = false;

                    while (TrafficPolicerHandler.isOwnerWantedCalloutRunning)
                    {
                        GameFiber.Sleep(1000);
                        if (driver.Exists())
                        {
                            if (driver.IsDead || Functions.IsPedArrested(driver))
                            {
                                driverDead = true;
                            }

                        }

                        if (Game.LocalPlayer.Character.IsDead)
                        {
                            Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                            break;
                        }
                        if (driverDead)
                        {
                            break;
                        }
                        if (!driver.Exists())
                        {
                            break;
                        }



                        
                    }

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




    /// <summary>
    /// Leave it to LSPDFR.
    /// </summary>
    private void situationLSPDFR(bool createPassenger)
        {
            //Add Passenger
            calloutStarted = true;
            GameFiber.StartNew(delegate
            {
                try {
                    if (createPassenger)
                    {
                        passenger = new Ped(spawnPoint);
                        passenger.BlockPermanentEvents = true;
                        passenger.WarpIntoVehicle(car, 0);
                    }


                    driverBlip.EnableRoute(System.Drawing.Color.Red);

                    driver.Tasks.CruiseWithVehicle(car, 18f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                    beforeTrafficStopDrive();






                    
                    displayCode4Message = false;
                    calloutFinished = true;
                }
                catch (System.Threading.ThreadAbortException e)
                {
                    End();
                }
                catch (Exception e)
                {
                    Game.LogTrivial(e.ToString());
                    Game.DisplayNotification("~O~ANPR Hit ~s~callout crashed, sorry. Please send me your log file.");
                    Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                    End();
                }
            });

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
                    if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) < 15f)
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
                    if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 13f)
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
