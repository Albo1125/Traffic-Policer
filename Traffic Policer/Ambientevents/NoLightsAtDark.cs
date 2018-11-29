using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;

namespace Traffic_Policer.Ambientevents
{
    internal class NoLightsAtDark : AmbientEvent
    {
        

        public NoLightsAtDark(Ped Driver, bool createBlip, bool showMessage) : base (Driver, createBlip, showMessage, "Creating no lights at dark event.")
        {
            MainLogic();
        }
        protected override void MainLogic()
        {
            
           
            speed = car.Speed - 3f;
            if (speed <= 12f)
            {
                speed = 12.1f;
            }
            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {
                try
                {
                    driver.Tasks.CruiseWithVehicle(car, speed, VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians);
                    if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                    {
                        API.LSPDFRPlusFunctions.AddQuestionToTrafficStop(driver, "Why are your lights off?", new List<string> { "Sorry, officer. I forgot", "It's not that dark, is it?", "What the hell do you care?", "I thought I had them on!" });
                    }
                    while (eventRunning)
                    {
                        GameFiber.Yield();
                        Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(driver, 786603);
                        if (car.Exists())
                        {

                            Rage.Native.NativeFunction.Natives.SET_VEHICLE_LIGHTS(car, 1);


                            if (Functions.IsPlayerPerformingPullover() && Vector3.Distance(Game.LocalPlayer.Character.Position, car.Position) < 20f)
                            {
                                performingPullover = true;
                                GameFiber.Wait(4000);
                                if (car.Exists())
                                {
                                    Rage.Native.NativeFunction.Natives.SET_VEHICLE_LIGHTS(car, 0);
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
                
                        
                   
                    
                    if (car.Exists())
                    {
                        Rage.Native.NativeFunction.Natives.SET_VEHICLE_LIGHTS(car, 0);
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
                    if (car.Exists())
                    {
                        Rage.Native.NativeFunction.Natives.SET_VEHICLE_LIGHTS(car, 0);
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
