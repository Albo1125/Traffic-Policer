using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Traffic_Policer.Ambientevents
{
    internal class BurnoutWhenStationary : AmbientEvent
    {
        public BurnoutWhenStationary(Ped Driver, bool createBlip, bool showMessage) : base(Driver, createBlip, showMessage, "Creating BurnoutWhenStationary event")
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
                    speed = car.Speed;
                    speed = speed - 1f;
                    if (speed <= 12f)
                    {
                        speed = 12.1f;
                    }
                    driver.Tasks.CruiseWithVehicle(car, speed, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));
                    if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                    {
                        API.LSPDFRPlusFunctions.AddQuestionToTrafficStop(driver, "What was that burnout about?", new List<string> { "Sorry, officer. Just having some fun.", "Traffic wasn't moving!", "What the hell do you care?", "Just testing my vehicle out, officer!" });
                    }
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

                        if (car.Speed < 2f)
                        {
                            driver.Tasks.PerformDrivingManeuver(VehicleManeuver.BurnOut);
                            GameFiber.Sleep(3000);
                        }
                        
                        driver.Tasks.CruiseWithVehicle(car, speed, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));

                        GameFiber.Wait(2500);
                        
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
