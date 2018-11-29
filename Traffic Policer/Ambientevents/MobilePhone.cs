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
    /// Creates an event to give the specified driver a mobile phone and make them swerve.
    /// </summary>
    internal class MobilePhone : AmbientEvent
    {
        

        public MobilePhone(Ped Driver, bool createBlip, bool showMessage) : base (Driver, createBlip, showMessage, "Creating mobile phone event.")
        {
            MainLogic();
        }

        
        protected override void MainLogic()
        {
            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {
                try
                {
                    if (speed <= 12f)
                    {
                        speed = 12.1f;
                    }
                    driver.Tasks.ClearImmediately();
                    driver.WarpIntoVehicle(car, -1);
                    driver.KeepTasks = true;


                    Rage.Native.NativeFunction.Natives.TASK_USE_MOBILE_PHONE_TIMED(100000);
                    driver.Tasks.CruiseWithVehicle(car, speed, VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians);
                    if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                    {
                        API.LSPDFRPlusFunctions.AddQuestionToTrafficStop(driver, "Why were you on your phone?", new List<string> { "Sorry, officer. It was really important.", "I was just texting someone. Got a problem with that?", "What the hell do you care?", "It was my mother calling, officer!" });
                    }
                    DrivingStyleFiber = GameFiber.StartNew(delegate
                    {

                        while (eventRunning)
                        {
                            GameFiber.Yield();
                            Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(driver, 786603);

                        }


                    });

                    while (eventRunning)
                    {
                        if (Functions.IsPlayerPerformingPullover())
                        {
                            eventRunning = false;
                            performingPullover = true;
                            driver.Tasks.ClearSecondary();
                            break;
                        }
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 300f)
                        {
                            eventRunning = false;

                            break;
                        }
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.SwerveLeft);
                        if (GameFiber.CanSleepNow)
                        {
                            GameFiber.Sleep(200);
                        }
                        driver.Tasks.PerformDrivingManeuver(VehicleManeuver.SwerveRight);
                        if (GameFiber.CanSleepNow)
                        {
                            GameFiber.Sleep(300);
                        }
                        driver.Tasks.CruiseWithVehicle(car, speed, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));
                        if (GameFiber.CanSleepNow)
                        {
                            GameFiber.Sleep(6000);
                        }
                        
                    }

                    
                }
                catch (Exception e)
                {
                    eventRunning = false;
                    if (driver.Exists())
                    {
                        if (Rage.Native.NativeFunction.Natives.IS_PED_RUNNING_MOBILE_PHONE_TASK<bool>(driver))
                        {
                            //Rage.Native.NativeFunction.Natives.TASK_USE_MOBILE_PHONE(driver, 0);
                            driver.Tasks.ClearSecondary();

                        }

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
        protected override void End()
        {
            if (driver.Exists())
            { 
                driver.KeepTasks = false;
                if (Rage.Native.NativeFunction.Natives.IS_PED_RUNNING_MOBILE_PHONE_TASK<bool>(driver))
                {
                    driver.Tasks.ClearSecondary();

                }
            }
            base.End();
        }

    }
}
