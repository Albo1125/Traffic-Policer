using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;


namespace Traffic_Policer.Ambientevents
{
    /// <summary>
    /// Makes a driver swerve and drunk (with breathalyzer)
    /// </summary>
    internal class DrunkDriver : AmbientEvent
    {
        
        
        public DrunkDriver(Ped Driver, bool createBlip, bool showMessage) : base(Driver, createBlip, showMessage, "Creating drunk driver event.")
        {
            MainLogic();
        }

        protected override void MainLogic()
        {
            eventRunning = true;
            
            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {
                try
                {
                    
                    
                    AnimationSet drunkAnimset = new AnimationSet("move_m@drunk@verydrunk");
                    drunkAnimset.LoadAndWait();
                    driver.MovementAnimationSet = drunkAnimset;
                    Impairment_Tests.Breathalyzer.SetPedAlcoholLevels(driver, Impairment_Tests.Breathalyzer.GetRandomOverTheLimitAlcoholLevel());
                    
                    Rage.Native.NativeFunction.Natives.SET_PED_IS_DRUNK(driver, true);

                    speed = car.Speed;
                    speed = speed - 1f;
                    if (speed <= 12f)
                    {
                        speed = 12.1f;
                    }

                    driver.Tasks.CruiseWithVehicle(car, speed, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));
                    GameFiber.StartNew(delegate
                    {
                        while (eventRunning)
                        {
                            Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(driver, 786603);
                            GameFiber.Yield();
                        }
                    });
                    while (eventRunning)
                    {

                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.SwerveRight);
                        GameFiber.Sleep(250);
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.SwerveLeft);
                        GameFiber.Sleep(500);
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.SwerveRight);
                        GameFiber.Sleep(600);
                        driver.Tasks.CruiseWithVehicle(car, speed, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                        GameFiber.Sleep(4500);
                        if (Functions.IsPlayerPerformingPullover())
                        {
                            driver.Armor = 69;
                            eventRunning = false;
                            performingPullover = true;
                            break;
                        }
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 300f)
                        {
                            eventRunning = false;

                            break;
                        }
                        driver.Armor = 69;
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
