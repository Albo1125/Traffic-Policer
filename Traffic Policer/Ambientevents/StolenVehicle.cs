using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using Rage.Native;
using Albo1125.Common.CommonLibrary;

namespace Traffic_Policer.Ambientevents
{
    internal class StolenVehicle : AmbientEvent
    {
        
        private Vector3 spawnPoint;
        
        
        private LHandle pursuit;
        
        private string[] vehiclesToSelectFrom = new string[] {"DUKES", "BALLER", "BALLER2", "BISON", "BISON2", "BJXL", "CAVALCADE", "CHEETAH", "COGCABRIO", "ASEA", "ADDER", "FELON", "FELON2", "ZENTORNO",
        "WARRENER", "RAPIDGT", "INTRUDER", "FELTZER2", "FQ2", "RANCHERXL", "REBEL", "SCHWARZER", "COQUETTE", "CARBONIZZARE", "EMPEROR", "SULTAN", "EXEMPLAR", "MASSACRO",
        "DOMINATOR", "ASTEROPE", "PRAIRIE", "NINEF", "WASHINGTON", "CHINO", "CASCO", "INFERNUS", "ZTYPE", "DILETTANTE", "VIRGO", "F620", "PRIMO", "SULTAN", "EXEMPLAR", "F620", "FELON2", "FELON", "SENTINEL",
            "WINDSOR", "DOMINATOR", "DUKES", "GAUNTLET", "VIRGO", "ADDER", "BUFFALO", "ZENTORNO", "MASSACRO" };

        public StolenVehicle(bool createBlip, bool displayMessage) : base (displayMessage, "Creating stolen vehicle event.")
        {
            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(140f));
            while (Vector3.Distance(Game.LocalPlayer.Character.Position, spawnPoint) < 120f)
            {
                GameFiber.Yield();
                spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(140f));
            }
            car = new Vehicle(vehiclesToSelectFrom[TrafficPolicerHandler.rnd.Next(vehiclesToSelectFrom.Length)], spawnPoint);

            car.RandomiseLicencePlate();
            Vector3 directionFromVehicleToPed = (Game.LocalPlayer.Character.Position - car.Position);
            directionFromVehicleToPed.Normalize();

            float heading = MathHelper.ConvertDirectionToHeading(directionFromVehicleToPed);
            car.Heading = heading;
            car.IsStolen = true;
            car.MustBeHotwired = true;
            car.AlarmTimeLeft = new TimeSpan(0, 5, 0);
            driver = car.CreateRandomDriver();
            
            driver.BlockPermanentEvents = true;
            driver.IsPersistent = true;
            
            if (createBlip)
            {
                driverBlip = driver.AttachBlip();
                driverBlip.Color = System.Drawing.Color.Beige;
                driverBlip.Scale = 0.7f;
            }
            MainLogic();
        }

        protected override void MainLogic()
        {
            
        
            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {
                try
                {
                    eventRunning = true;


                    //driver.Tasks.EnterVehicle(car, 5000, -1).WaitForCompletion();
                    //driver.WarpIntoVehicle(car, -1);
                    car.Windows[0].Smash();
                    //car.IndicatorLightsStatus = VehicleIndicatorLightsStatus.Both;
                    
                    driver.Tasks.CruiseWithVehicle(car, 30f, VehicleDrivingFlags.Emergency);
                    GameFiber.Sleep(6000);




                    while (eventRunning)
                    {
                        try
                        {
                            if (GameFiber.CanSleepNow)
                            {
                                GameFiber.Sleep(1000);
                            }


                            if (Functions.IsPlayerPerformingPullover())
                            {
                                if (Functions.GetPulloverSuspect(Functions.GetCurrentPullover()) == driver)
                                {
                                    performingPullover = true;
                                    if (TrafficPolicerHandler.rnd.Next(5) < 3)
                                    {
                                        GameFiber.Sleep(100);
                                        Game.HideHelp();
                                        driver = Albo1125.Common.CommonLibrary.ExtensionMethodsLSPDFR.ClonePed(driver, true);
                                        Game.LocalPlayer.Character.CurrentVehicle.IsSirenOn = true;
                                        Game.LocalPlayer.Character.CurrentVehicle.IsSirenSilent = false;
                                        pursuit = Functions.CreatePursuit();
                                        Functions.AddPedToPursuit(pursuit, driver);
                                        Functions.PlayScannerAudioUsingPosition("WE_HAVE CRIME_RESIST_ARREST IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);
                                    }
                                    break;
                                }
                            }
                            if (car.AlarmTimeLeft.TotalSeconds <= 0)
                            {
                                car.AlarmTimeLeft = new TimeSpan(0, 5, 0);
                                
                            }
                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 300f)
                            {
                                eventRunning = false;

                                break;
                            }






                        }
                        catch (Exception e)
                        {

                            continue;
                        }
                    }
                    
                    
                    
                }
                catch (Exception e)
                {
                    eventRunning = false;
                    if (driver.Exists())
                    {
                        driver.Dismiss();
                    }
                    if (driverBlip.Exists())
                    {
                        driverBlip.Delete();
                    }
                    
                }
                finally
                {
                    End();
                }


            });
        }
        
    }
}
