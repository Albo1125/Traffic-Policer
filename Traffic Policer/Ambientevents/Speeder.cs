using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;

namespace Traffic_Policer.Ambientevents
{
    
    internal class Speeder : AmbientEvent
    {
        
        private float oldSpeed;
        private float oldDriveInertia;
        private float oldInitialDriveForce;
        
        
        public Speeder(Ped Driver, bool createBlip, bool showMessage) : base(Driver, createBlip, showMessage, "Creating speeder event.")
        {
            MainLogic();
        }

        
        protected override void MainLogic()
        {
            
            
            oldSpeed = car.Speed;
            if (oldSpeed < 10f)
            {
                if (driverBlip.Exists())
                {
                    driverBlip.Delete();
                }
                return;
            }
            car.Mods.InstallModKit();
            if ((car.Mods.EngineModCount - 2) >= 0)
            {
                car.Mods.EngineModIndex = car.Mods.EngineModCount - 2;
            }

            if ((car.Mods.ExhaustModCount - 2) >= 0)
            {
                car.Mods.ExhaustModIndex = car.Mods.ExhaustModCount - 2;
            }
            if ((car.Mods.TransmissionModCount - 2) >= 0)
            {
                car.Mods.TransmissionModIndex = car.Mods.TransmissionModCount - 2;
            }
            car.Mods.HasTurbo = true;
            oldDriveInertia = car.HandlingData.DriveInertia;
            oldInitialDriveForce = car.HandlingData.InitialDriveForce;
            

            
            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {
                try {
                    float newSpeed;
                    eventRunning = true;
                    
                    if (oldSpeed >= 27f)
                    {
                        newSpeed = 55f;
                        if (car.TopSpeed <= 55f)
                        {
                            car.TopSpeed = 55f;
                        }
                    }
                    else
                    {
                        newSpeed = oldSpeed * 1.8f;
                        if (newSpeed < 18f) { newSpeed = 18.1f; }
                        if (newSpeed > 40f) { newSpeed = 40f; }

                    }
                    
                    car.HandlingData.DriveInertia = oldDriveInertia * 1.7f;
                    car.HandlingData.InitialDriveForce = oldInitialDriveForce * 1.7f;

                    driver.Tasks.ClearImmediately();
                    driver.WarpIntoVehicle(car, -1);
                    driver.KeepTasks = true;
                    

                    driver.Tasks.CruiseWithVehicle(car, newSpeed, VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians);

                    while (eventRunning)
                    {
                        GameFiber.Yield();
                        Rage.Native.NativeFunction.Natives.SET_DRIVE_TASK_DRIVING_STYLE(driver, 786603);
                        if (Functions.IsPlayerPerformingPullover())
                        {
                            performingPullover = true;
                            break;
                        }
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 300f)
                        {
                            eventRunning = false;

                            break;
                        }


                    }

                } catch(Exception e)
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
        protected override void End()
        {
            if (car.Exists())
            {
                car.HandlingData.DriveInertia = oldDriveInertia;
                car.HandlingData.InitialDriveForce = oldInitialDriveForce;
            }
            base.End();
        }
    }
}
