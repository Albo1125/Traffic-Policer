using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using Albo1125.Common.CommonLibrary;

namespace Traffic_Policer.Ambientevents
{
    internal class MotorcyclistNoHelmet : AmbientEvent
    {
        
        
        
        
        private Vector3 spawnPoint;
        
        
        private string[] bikesToSelectFrom = new string[] { "BATI", "BATI2", "AKUMA", "BAGGER", "DOUBLE", "NEMESIS", "HEXER" };
        
        public MotorcyclistNoHelmet(bool createBlip, bool displayMessage) : base(displayMessage, "Creating motorcyclist without a helmet event.")
        {
            try
            {
                spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(120f));
                while (Vector3.Distance(Game.LocalPlayer.Character.Position, spawnPoint) < 80f)
                {
                    GameFiber.Yield();
                    spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(120f));
                }
                car = new Vehicle(bikesToSelectFrom[TrafficPolicerHandler.rnd.Next(bikesToSelectFrom.Length)], spawnPoint);
                car.IsPersistent = true;
                car.RandomiseLicencePlate();
                Vector3 directionFromVehicleToPed = (Game.LocalPlayer.Character.Position - car.Position);
                directionFromVehicleToPed.Normalize();

                float heading = MathHelper.ConvertDirectionToHeading(directionFromVehicleToPed);
                car.Heading = heading;
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
             
             catch (Exception e)
             {
                  Game.LogTrivial(e.ToString());
                  End();
             }
        }
        
        protected override void MainLogic()
        {
            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {
                try
                {
                    eventRunning = true;
                    
                    driver.Tasks.CruiseWithVehicle(car, 18f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.YieldToCrossingPedestrians);
                    driver.RemoveHelmet(true);
                    if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                    {
                        API.LSPDFRPlusFunctions.AddQuestionToTrafficStop(driver, "Where was your helmet?", new List<string> { "Sorry, officer. I forgot", "It's a bit uncomfortable to wear, officer.", "What the hell do you care?", "I was enjoying the lovely weather, officer." });
                    }
                    while (eventRunning)
                    {
                        GameFiber.Yield();
                        if (Functions.IsPlayerPerformingPullover())
                        {

                            eventRunning = false;
                            performingPullover = true;
                            break;
                        }
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 300f)
                        {
                            eventRunning = false;

                            break;
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
