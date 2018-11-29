using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using Traffic_Policer.Impairment_Tests;

namespace Traffic_Policer.Ambientevents
{
    /// <summary>
    /// Makes a driver swerve and under influence of drugs
    /// </summary>
    internal class DrugDriver : AmbientEvent
    {
        //public Ped driver;
        //public bool eventRunning;
        //public Vehicle car;
        //private float speed;
        //private Blip driverBlip;
        //private GameFiber DrivingStyleFiber;
        //private GameFiber AmbientEventMainFiber;
        

        public DrugDriver(Ped Driver, bool createBlip, bool showMessage) : base(Driver, createBlip, showMessage, "Creating drug driver event.")
        {
            //driver = Driver;
            //car = driver.CurrentVehicle;
            //driver.BlockPermanentEvents = true;
            //driver.IsPersistent = true;
            //if (createBlip)
            //{
            //    driverBlip = driver.AttachBlip();
            //    driverBlip.Color = System.Drawing.Color.Beige;
            //    driverBlip.Scale = 0.7f;
            //}
            //if (showMessage)
            //{
            //    Game.DisplayNotification("Creating drug driver event.");
            //}
            MainLogic();
        }

        protected override void MainLogic()
        {
            eventRunning = true;
            
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
            
            
            
            
            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {
                try
                {

                    //timeOut = timeOut * 1000;
                    AnimationSet drunkAnimset = new AnimationSet("move_m@drunk@verydrunk");
                    drunkAnimset.LoadAndWait();
                    driver.MovementAnimationSet = drunkAnimset;
                    
                    
                    Rage.Native.NativeFunction.Natives.SET_PED_IS_DRUNK(driver, true);

                    speed = car.Speed;
                    speed = speed - 1f;
                    if (speed <= 12f)
                    {
                        speed = 12.1f;
                    }

                    driver.Tasks.CruiseWithVehicle(car, speed, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));
                    DrivingStyleFiber = GameFiber.StartNew(delegate
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
