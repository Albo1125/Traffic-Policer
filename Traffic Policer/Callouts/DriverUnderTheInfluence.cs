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
using System.IO;
using Albo1125.Common.CommonLibrary;
using Traffic_Policer.Impairment_Tests;

namespace Traffic_Policer.Callouts
{
    [CalloutInfo("Driver Under The Influence", CalloutProbability.Medium)]
    internal class DriverUnderTheInfluence : Callout
    {
        private SpawnPoint spawnPoint;
        private Ped driver;
        private Vehicle car;
        private Blip driverBlip;
        private bool DriverStopped = false;
        private bool CalloutRunning;
        private bool CalloutFinished = false;
        private string msg = "";
        private bool PursuitCreated;
        private LHandle Pursuit;
        private string[] vehiclesToSelectFrom = new string[] {"DUKES", "BALLER", "BALLER2", "BISON", "BISON2", "BJXL", "CAVALCADE", "CHEETAH", "COGCABRIO", "ASEA", "ADDER", "FELON", "FELON2", "ZENTORNO",
        "WARRENER", "RAPIDGT", "INTRUDER", "FELTZER2", "FQ2", "RANCHERXL", "REBEL", "SCHWARZER", "COQUETTE", "CARBONIZZARE", "EMPEROR", "SULTAN", "EXEMPLAR", "MASSACRO",
        "DOMINATOR", "ASTEROPE", "PRAIRIE", "NINEF", "WASHINGTON", "CHINO", "CASCO", "INFERNUS", "ZTYPE", "DILETTANTE", "VIRGO", "F620", "PRIMO" };
        private Model carModel;
        public override bool OnBeforeCalloutDisplayed()
        {

            Game.LogTrivial("TrafficPolicer.DriverUnderTheInfluence");
            int WaitCount = 0;
            while (!World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(250f, 400f)).GetClosestVehicleSpawnPoint(out spawnPoint))
            {
                GameFiber.Yield();
                WaitCount++;
                if (WaitCount > 10) { return false; }
            }


            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 70f);

            carModel = new Model(vehiclesToSelectFrom[TrafficPolicerHandler.rnd.Next(vehiclesToSelectFrom.Length)]);
            carModel.LoadAndWait();
            CalloutMessage = "Driver Under The Influence";
            CalloutPosition = spawnPoint;
            Functions.PlayScannerAudioUsingPosition("DISP_ATTENTION_UNIT " + TrafficPolicerHandler.DivisionUnitBeatAudioString + " CITIZENS_REPORT CRIME_DUI IN_OR_ON_POSITION", spawnPoint);
            return base.OnBeforeCalloutDisplayed();
        }
        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            if (driver.Exists()) { driver.Delete(); }

            if (car.Exists()) { car.Delete(); }
            if (TrafficPolicerHandler.OtherUnitRespondingAudio)
            {
                Functions.PlayScannerAudio("OTHER_UNIT_TAKING_CALL");
            }
        }

        public override bool OnCalloutAccepted()
        {
            

            car = new Vehicle(carModel, spawnPoint.Position, spawnPoint.Heading);
            car.RandomiseLicencePlate();
            driver = car.CreateRandomDriver();
            driver.MakeMissionPed();
            while (!driver.Exists())
            {
                GameFiber.Yield();
            }
            TrafficPolicerHandler.driversConsidered.Add(driver);
            Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "Driver Under The Influence", "Dispatch to ~b~" + TrafficPolicerHandler.DivisionUnitBeat, "Citizens are reporting a ~r~possibly impaired driver. ~b~Check~s~ for ~r~impairing substances.");
            Game.DisplaySubtitle("~h~Impairing substances to test for include alcohol and drugs.", 7000);



            car.IsPersistent = true;

            driverBlip = driver.AttachBlip();
            driverBlip.Scale = 0.7f;
            CalloutHandler();
            return base.OnCalloutAccepted();
        }
        public override void Process()
        {
            base.Process();
            if (Game.LocalPlayer.Character.Exists())
            {
                if (Game.LocalPlayer.Character.IsDead)
                {

                    GameFiber.StartNew(End);
                }
            }
            else
            {
                GameFiber.StartNew(End);
            }

        }
        private void CalloutHandler()
        {
            CalloutRunning = true;
            int maxnumber = 1;
            //if (!File.Exists("Plugins/BreathalyzerRAGE.dll")) { maxnumber = 1; }
            int roll = TrafficPolicerHandler.rnd.Next(maxnumber);
            Game.LogTrivial("Roll: " + roll.ToString());
            if (roll == 0)
            {
                SituationDrunk();
                
            }
            else if (roll == 1)
            {
                SituationDrugs();
            }
        }

        private void SituationDrugs()
        {
            GameFiber.StartNew(delegate
            {
                try
                {
                    int randomNumber = TrafficPolicerHandler.rnd.Next(3);

                    switch (randomNumber)
                    {
                        case 0:
                            DrugTestKit.SetPedDrugsLevels(driver, DrugsLevels.POSITIVE, DrugsLevels.POSITIVE);
                            break;
                        case 1:
                            DrugTestKit.SetPedDrugsLevels(driver, DrugsLevels.NEGATIVE, DrugsLevels.POSITIVE);
                            break;
                        case 2:
                            DrugTestKit.SetPedDrugsLevels(driver, DrugsLevels.POSITIVE, DrugsLevels.NEGATIVE);
                            break;
                    }
                    beforeTrafficStopDrive();
                    //Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "Traffic Policer", "DUI", "Test the person for illegal substances. If);
                    
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 4.5f && !Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            if (TrafficPolicerHandler.rnd.Next(5) == 0 && Functions.GetActivePursuit() == null)
                            {
                                Pursuit = Functions.CreatePursuit();
                                Functions.AddPedToPursuit(Pursuit, driver);
                                Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                                Functions.ForceEndCurrentPullover();
                                PursuitCreated = true;
                            }
                            
                            break;
                        }
                        if (Functions.GetActivePursuit() != null)
                        {
                            if (Functions.GetPursuitPeds(Functions.GetActivePursuit()).Contains(driver))
                            {
                                Pursuit = Functions.GetActivePursuit();
                                PursuitCreated = true;
                                break;
                            }
                            

                        }
                        

                    }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (!driver.Exists())
                        {
                            break;
                        }
                        else if (Functions.IsPedArrested(driver)) { break; }
                        else if (driver.IsDead) { break; }
                        else if (PursuitCreated)
                        {
                            if (Functions.GetActivePursuit() == null)
                            {
                                break;
                            }
                        }
                    }
                    DisplayCodeFourMessage();





                }
                catch (System.Threading.ThreadAbortException e)
                {
                    End();
                }

                catch (Exception e)
                {
                    if (CalloutRunning)
                    {
                        Game.LogTrivial(e.ToString());
                        Game.LogTrivial("Traffic Policer handled the exception successfully.");
                        Game.DisplayNotification("~O~DUI ~s~callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }

                }
            });

        }

        private void SituationDrunk()
        {
            GameFiber.StartNew(delegate
            {
                try
                {
                    Impairment_Tests.Breathalyzer.SetPedAlcoholLevels(driver, Breathalyzer.GetRandomOverTheLimitAlcoholLevel());
                    beforeTrafficStopDrive();
                    //Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "Traffic Policer", "DUI", "Test the person for illegal substances. If);
                    
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 4.5f && !Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            if (TrafficPolicerHandler.rnd.Next(5) == 0 && Functions.GetActivePursuit() == null)
                            {
                                //Game.DisplayNotification("There's a " + (driver.IsMale ? "man" : "woman") + " on the deck.");
                                Pursuit = Functions.CreatePursuit();

                                Functions.AddPedToPursuit(Pursuit, driver);
                                Functions.SetPursuitIsActiveForPlayer(Pursuit, true);
                                Functions.ForceEndCurrentPullover();
                            }
                            break;
                        }
                        if (Functions.GetActivePursuit() != null)
                        {
                            if (Functions.GetPursuitPeds(Functions.GetActivePursuit()).Contains(driver))
                            {
                                Pursuit = Functions.GetActivePursuit();
                                PursuitCreated = true;
                                break;
                            }


                        }


                    }
                    while (CalloutRunning)
                    {
                        GameFiber.Yield();
                        if (!driver.Exists())
                        {
                            Game.LogTrivial("Driver doesn't exist.");
                            break;
                        }
                        else if (Functions.IsPedArrested(driver)) { break; }
                        else if (driver.IsDead) { break; }
                        else if (PursuitCreated)
                        {
                            if (Functions.GetActivePursuit() == null)
                            {
                                Game.LogTrivial("Active pursuit end..");
                                break;
                            }
                        }
                    }
                    DisplayCodeFourMessage();





                }
                catch (System.Threading.ThreadAbortException e)
                {
                    End();
                }

                catch (Exception e)
                {
                    if (CalloutRunning)
                    {
                        Game.LogTrivial(e.ToString());
                        Game.LogTrivial("Traffic Policer handled the exception successfully.");
                        Game.DisplayNotification("~O~DUI ~s~callout crashed, sorry. Please send me your log file.");
                        Game.DisplayNotification("Full LSPDFR crash prevented ~g~successfully.");
                        End();
                    }

                }
            });

        }





        private void DisplayCodeFourMessage()
        {
            if (CalloutRunning)
            {
                if (!driver.Exists())
                {
                    msg = "The driver ceased to exist.";
                }
                else if (Functions.IsPedArrested(driver))
                {
                    msg = "The driver is ~g~in custody.~s~";
                }

                else if (driver.IsDead)
                {
                    msg = "The driver is dead.";
                }
                else if (PursuitCreated)
                {
                    if (Functions.GetActivePursuit() == null)
                    {
                        msg = "The driver has ~r~escaped.";
                    }
                }
                msg += " The DUI call is ~g~CODE 4~s~, over.";
                GameFiber.Sleep(4000);
                Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "Driver Under The Influence", "Dispatch to ~b~" + TrafficPolicerHandler.DivisionUnitBeat, msg);



                Functions.PlayScannerAudio("ATTENTION_THIS_IS_DISPATCH_HIGH WE_ARE_CODE FOUR NO_FURTHER_UNITS_REQUIRED");
                CalloutFinished = true;
                End();
            }
        }

        private void beforeTrafficStopDrive()
        {
            driver.Tasks.CruiseWithVehicle(car, 18f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.YieldToCrossingPedestrians);
            MakeDriverSwerve();
            while (CalloutRunning)
            {

                GameFiber.Yield();
                
                if (Functions.IsPlayerPerformingPullover() && Functions.GetPulloverSuspect(Functions.GetCurrentPullover()) == driver)
                {


                    break;

                }

                
                if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 13f)
                {
                    if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    {

                        break;
                    }
                }





            }
            DriverStopped = true;
        }
        private void MakeDriverSwerve()
        {
            GameFiber.StartNew(delegate
            {
                try
                {
                    while (CalloutRunning && !DriverStopped)
                    {
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.SwerveRight);
                        GameFiber.Sleep(250);
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.SwerveLeft);
                        GameFiber.Sleep(500);
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.SwerveRight);
                        GameFiber.Sleep(600);
                        driver.Tasks.CruiseWithVehicle(car, 18f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.YieldToCrossingPedestrians);
                        Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(driver, 786603);
                        GameFiber.Sleep(5500);
                    }
                }
                catch (Exception e) { End(); }
            });
        }

        public override void End()
        {
            CalloutRunning = false;
            if (Game.LocalPlayer.Character.Exists())
            {
                if (Game.LocalPlayer.Character.IsDead)
                {
                    GameFiber.Wait(1500);
                    Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                    GameFiber.Wait(3000);


                }
            }
            else
            {
                GameFiber.Wait(1500);
                Functions.PlayScannerAudio("OFFICER HAS_BEEN_FATALLY_SHOT NOISE_SHORT OFFICER_NEEDS_IMMEDIATE_ASSISTANCE");
                GameFiber.Wait(3000);


            }
            base.End();
            if (driverBlip.Exists()) { driverBlip.Delete(); }
            //SpeechHandler.HandlingSpeech = false;
            if (!CalloutFinished)
            {
                if (driver.Exists()) { driver.Delete(); }
                if (car.Exists()) { car.Delete(); }
            }
            else
            {

                if (driver.Exists()) { driver.Dismiss(); }
                if (car.Exists()) { car.Dismiss(); }
            }
        }
    }
}
