using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;

namespace Traffic_Policer.Ambientevents
{
    internal class NoBrakeLights : AmbientEvent
    {
        

        public NoBrakeLights(Ped Driver, bool createBlip, bool showMessage) : base(Driver, createBlip, showMessage, "Creating no brake lights event.")
        {
            MainLogic();
        }
        protected override void MainLogic()
        {
            
            
            
            speed = car.Speed - 1f;
            if (speed <= 12f)
            {
                speed = 12.1f;
            }
            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {
                try
                {
                    driver.Tasks.CruiseWithVehicle(car, speed, VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians);

                    while (eventRunning)
                    {
                        GameFiber.Yield();
                        Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(driver, 786603);
                        if (car.Exists())
                        {

                            Rage.Native.NativeFunction.Natives.SET_VEHICLE_BRAKE_LIGHTS(car, false);


                            if (Functions.IsPlayerPerformingPullover() && Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 20f)
                            {
                                performingPullover = true;
                                while (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                                {
                                    GameFiber.Yield();
                                    Rage.Native.NativeFunction.Natives.SET_VEHICLE_BRAKE_LIGHTS(car, false);
                                }
                                break;
                            }
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
