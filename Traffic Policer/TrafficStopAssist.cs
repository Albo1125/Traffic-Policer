using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using System.Windows.Forms;
using Rage.Native;
using System.Drawing;
using RAGENativeUI;
using RAGENativeUI.Elements;
using Albo1125.Common.CommonLibrary;

namespace Traffic_Policer
{
    internal static class TrafficStopAssist
    {
        private static uint notHandle;
        private static Blip blip;
        private static string[] numbers = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9"};

        private static int CheckPoint = 0;
        public static void SetCustomPulloverLocation()
        {
            TrafficPolicerHandler.isSomeoneFollowing = true;
            GameFiber.StartNew(delegate
            {
                try
                {

                    if (!Functions.IsPlayerPerformingPullover())
                    {
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }

                    if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    {
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }
                    Vehicle playerCar = Game.LocalPlayer.Character.CurrentVehicle;
                    Vehicle stoppedCar = (Vehicle)World.GetClosestEntity(playerCar.GetOffsetPosition(Vector3.RelativeFront * 8f), 8f, (GetEntitiesFlags.ConsiderGroundVehicles | GetEntitiesFlags.ConsiderBoats | GetEntitiesFlags.ExcludeEmptyVehicles | GetEntitiesFlags.ExcludeEmergencyVehicles));
                    if (stoppedCar == null)
                    {
                        Game.DisplayNotification("Unable to detect the pulled over vehicle. Make sure you're behind the vehicle and try again.");
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }
                    if (!stoppedCar.IsValid() || (stoppedCar == playerCar))
                    {
                        Game.DisplayNotification("Unable to detect the pulled over vehicle. Make sure you're behind the vehicle and try again.");
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }
                    if (stoppedCar.Speed > 0.2f && !ExtensionMethods.IsPointOnWater(stoppedCar.Position))
                    {
                        Game.DisplayNotification("The vehicle must be stopped before you can do this.");
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }
                    string modelName = stoppedCar.Model.Name.ToLower();
                    if (numbers.Contains<string>(modelName.Last().ToString()))
                    {
                        modelName = modelName.Substring(0, modelName.Length - 1);
                    }
                    modelName = char.ToUpper(modelName[0]) + modelName.Substring(1);
                    Ped pulledDriver = stoppedCar.Driver;
                    if (!pulledDriver.IsPersistent || Functions.GetPulloverSuspect(Functions.GetCurrentPullover()) != pulledDriver)
                    {
                        Game.DisplayNotification("Unable to detect the pulled over vehicle. Make sure you're in front of the vehicle and try again.");
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }

                    Blip blip = pulledDriver.AttachBlip();
                    blip.Flash(500, -1);
                    blip.Color = System.Drawing.Color.Aqua;
                    playerCar.BlipSiren(true);
                    Vector3 CheckPointPosition = Game.LocalPlayer.Character.GetOffsetPosition(new Vector3(0f, 8f, -1f));
                    CheckPoint = NativeFunction.Natives.CREATE_CHECKPOINT<int>(46, CheckPointPosition.X, CheckPointPosition.Y, CheckPointPosition.Z, CheckPointPosition.X, CheckPointPosition.Y, CheckPointPosition.Z, 3.5f, 255, 0, 0, 255, 0); ;
                    float xOffset = 0;
                    float yOffset = 0;
                    float zOffset = 0;
                    bool SuccessfulSet = false;
                    while (true)
                    {
                        GameFiber.Wait(70);
                        Game.DisplaySubtitle("Set your desired pullover location. Hold ~b~Enter ~s~when done.", 100);
                        CheckPointPosition = Game.LocalPlayer.Character.GetOffsetPosition(new Vector3((float)xOffset + 0.5f, (float)(yOffset + 8), (float)(-1 + zOffset)));
                        if (!TrafficPolicerHandler.isSomeoneFollowing)
                        {
                            break;
                        }
                        if (!Functions.IsPlayerPerformingPullover())
                        {
                            Game.DisplayNotification("You cancelled the ~b~Traffic Stop.");
                            break;
                        }
                        if (!Game.LocalPlayer.Character.IsInVehicle(playerCar, false))
                        {
                            break;
                        }

                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(SpeedChecker.PositionResetKey))
                        {
                            xOffset = 0;
                            yOffset = 0;
                            zOffset = 0;

                        }
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(SpeedChecker.PositionForwardKey))
                        {
                            yOffset++;
                        }
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(SpeedChecker.PositionBackwardKey))
                        {
                            yOffset--;
                        }
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(SpeedChecker.PositionRightKey))
                        {
                            xOffset++;
                        }
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(SpeedChecker.PositionLeftKey))
                        {
                            xOffset--;
                        }
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(SpeedChecker.PositionUpKey))
                        {
                            zOffset++;
                        }
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(SpeedChecker.PositionDownKey))
                        {
                            zOffset--;
                        }
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(Keys.Enter))
                        {
                            SuccessfulSet = true;
                            break;
                        }


                        NativeFunction.Natives.DELETE_CHECKPOINT(CheckPoint);
                        CheckPoint = NativeFunction.Natives.CREATE_CHECKPOINT<int>(46, CheckPointPosition.X, CheckPointPosition.Y, CheckPointPosition.Z, CheckPointPosition.X, CheckPointPosition.Y, CheckPointPosition.Z, 3f, 255, 0, 0, 255, 0);
                        NativeFunction.Natives.SET_CHECKPOINT_CYLINDER_HEIGHT(CheckPoint, 2f, 2f, 2f);
                    }
                    NativeFunction.Natives.DELETE_CHECKPOINT(CheckPoint);
                    if (SuccessfulSet)
                    {
                        try
                        {
                            Game.LocalPlayer.Character.Tasks.PlayAnimation("friends@frj@ig_1", "wave_c", 1f, AnimationFlags.SecondaryTask | AnimationFlags.UpperBodyOnly);
                        }
                        catch { }
                        while (true)
                        {
                            GameFiber.Yield();
                            if (Vector3.Distance(pulledDriver.Position, Game.LocalPlayer.Character.Position) > 25f) { Game.DisplaySubtitle("~h~~r~Stay close to the vehicle.", 700); }
                            
                            if (!Functions.IsPlayerPerformingPullover())
                            {
                                Game.DisplayNotification("You cancelled the ~b~Traffic Stop.");
                                break;
                            }
                            if (!Game.LocalPlayer.Character.IsInVehicle(playerCar, false))
                            {
                                break;
                            }
                            if (!TrafficPolicerHandler.isSomeoneFollowing) { break; }
                            Rage.Task drivetask = pulledDriver.Tasks.DriveToPosition(CheckPointPosition, 12f, VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.AllowMedianCrossing | VehicleDrivingFlags.YieldToCrossingPedestrians);
                            GameFiber.Wait(700);
                            if (!drivetask.IsActive) { break; }
                            if (Vector3.Distance(pulledDriver.Position, CheckPointPosition) < 1.5f) { break; }
                            

                        }
                        if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                        {
                            API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "Custom pullover locations set");
                        }
                        Game.LogTrivial("Done custom pullover location");
                        if (stoppedCar.Exists())
                        {
                            if (pulledDriver.Exists())
                            {
                                pulledDriver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);

                            }
                        }

                        if (blip.Exists()) { blip.Delete(); }
                    }
                }
                catch (Exception e)
                {
                    NativeFunction.Natives.DELETE_CHECKPOINT(CheckPoint);
                    if (blip.Exists()) { blip.Delete(); }
                    Game.LogTrivial(e.ToString());
                    Game.LogTrivial("CustomPulloverLocationError handled.");
                }
                finally
                {
                    TrafficPolicerHandler.isSomeoneFollowing = false;
                }



            });
        }


        public static void mimicMe()
        {
            TrafficPolicerHandler.isSomeoneFollowing = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    //Safety checks
                    if (!Functions.IsPlayerPerformingPullover())
                    {
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }
                    Game.LogTrivial("Mimicking");
                    if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    {
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }
                    Vehicle playerCar = Game.LocalPlayer.Character.CurrentVehicle;
                    Vehicle stoppedCar = (Vehicle)World.GetClosestEntity(playerCar.GetOffsetPosition(Vector3.RelativeFront * 8f), 8f, (GetEntitiesFlags.ConsiderGroundVehicles | GetEntitiesFlags.ConsiderBoats | GetEntitiesFlags.ExcludeEmptyVehicles | GetEntitiesFlags.ExcludeEmergencyVehicles));
                    if (stoppedCar == null)
                    {
                        Game.DisplayNotification("Unable to detect the pulled over vehicle. Make sure you're behind the vehicle and try again.");
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }
                    if (!stoppedCar.IsValid() || (stoppedCar == playerCar))
                    {
                        Game.DisplayNotification("Unable to detect the pulled over vehicle. Make sure you're behind the vehicle and try again.");
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }
                    if (stoppedCar.Speed > 0.2f && !ExtensionMethods.IsPointOnWater(stoppedCar.Position))
                    {
                        Game.DisplayNotification("The vehicle must be stopped before they can mimic you.");
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }
                    string modelName = stoppedCar.Model.Name.ToLower();
                    if (numbers.Contains<string>(modelName.Last().ToString()))
                    {
                        modelName = modelName.Substring(0, modelName.Length - 1);
                    }
                    modelName = char.ToUpper(modelName[0]) + modelName.Substring(1);
                    Ped pulledDriver = stoppedCar.Driver;
                    if (!pulledDriver.IsPersistent || Functions.GetPulloverSuspect(Functions.GetCurrentPullover()) != pulledDriver)
                    {
                        Game.DisplayNotification("Unable to detect the pulled over vehicle. Make sure you're in front of the vehicle and try again.");
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }

                    //After checking everything
                    Blip blip = pulledDriver.AttachBlip();
                    blip.Flash(500, -1);
                    blip.Color = System.Drawing.Color.Aqua;
                    playerCar.BlipSiren(true);

                    Game.DisplayNotification("The blipped ~r~" + modelName + "~s~ is now mimicking you.");
                    Game.DisplayNotification("Press ~b~" + Albo1125.Common.CommonLibrary.ExtensionMethods.GetKeyString(TrafficPolicerHandler.trafficStopMimicKey, TrafficPolicerHandler.trafficStopMimicModifierKey) + " ~s~to stop the ~r~" + modelName + ".");
                    try
                    {
                        Game.LocalPlayer.Character.Tasks.PlayAnimation("friends@frj@ig_1", "wave_c", 1f, AnimationFlags.SecondaryTask | AnimationFlags.UpperBodyOnly);
                    }
                    catch { }
                    GameFiber.Sleep(100);
                    float speed = 10f;
                    //Game.LogTrivial("Vehicle Length: " + stoppedCar.Length.ToString());
                    bool CanBoost = true;
                    int CheckPoint = 0;
                    if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                    {
                        API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "Vehicles Mimicked");
                    }
                    //Driving loop
                    while (true)
                    {
                        
                        float modifier = stoppedCar.Length * 3.95f;
                        if (modifier < 20f)
                        {
                            modifier = 20f;
                        }
                        if (modifier > 34f)
                        {
                            modifier = 34f;
                        }
                        //NativeFunction.Natives.DELETE_CHECKPOINT(CheckPoint);
                        //CheckPoint = NativeFunction.Natives.CREATE_CHECKPOINT<int>(46, playerCar.GetOffsetPosition(Vector3.RelativeFront * modifier).X, playerCar.GetOffsetPosition(Vector3.RelativeFront * modifier).Y, playerCar.GetOffsetPosition(Vector3.RelativeFront * modifier).Z, playerCar.GetOffsetPosition(Vector3.RelativeFront * modifier).X, playerCar.GetOffsetPosition(Vector3.RelativeFront * modifier).Y, playerCar.GetOffsetPosition(Vector3.RelativeFront * modifier).Z, 2f, 255, 0, 0, 255, 0);
                        //NativeFunction.Natives.SET_CHECKPOINT_CYLINDER_HEIGHT(CheckPoint, 2f, 2f, 2f);
                        if (Vector3.Distance(pulledDriver.GetOffsetPosition(Vector3.RelativeFront * 3f), playerCar.GetOffsetPosition(Vector3.RelativeFront *  modifier)) < Vector3.Distance(pulledDriver.GetOffsetPosition(Vector3.RelativeBack * 1f), playerCar.GetOffsetPosition(Vector3.RelativeFront * modifier)))
                        {
                            
                            pulledDriver.Tasks.DriveToPosition(playerCar.GetOffsetPosition(Vector3.RelativeFront * modifier), speed, VehicleDrivingFlags.IgnorePathFinding);
                            //if (speed - pulledDriver.Speed > 3f && playerCar.Speed > 5f && CanBoost)
                            //{
                            //    NativeFunction.Natives.SET_VEHICLE_FORWARD_SPEED(stoppedCar, speed);
                            //    CanBoost = false;
                            //}
                            //if (playerCar.Speed < 0.2f) { CanBoost = true; }
                            
                        }
                        else
                        {
                            pulledDriver.Tasks.DriveToPosition(playerCar.GetOffsetPosition(Vector3.RelativeFront *modifier), speed, VehicleDrivingFlags.Reverse);
                        }

                        GameFiber.Sleep(60);

                        if (!TrafficPolicerHandler.isSomeoneFollowing)
                        {
                            break;
                        
                        }
                        if (!Functions.IsPlayerPerformingPullover())
                        {
                            Game.DisplayNotification("You cancelled the ~b~Traffic Stop.");
                            break;
                        }
                        if (!Game.LocalPlayer.Character.IsInVehicle(playerCar, false))
                        {
                            break;
                        }
                        
                        if (Vector3.Distance(playerCar.Position, stoppedCar.Position) > 45f)
                        {
                            stoppedCar.Position = playerCar.GetOffsetPosition(Vector3.RelativeFront * 10f);
                            stoppedCar.Heading = playerCar.Heading;
                            blip.Delete();
                            blip = pulledDriver.AttachBlip();
                            blip.Flash(500, -1);
                            blip.Color = System.Drawing.Color.Aqua;
                        }
                        speed = Game.LocalPlayer.Character.CurrentVehicle.Speed + 4f;
                        if (speed < 10f) { speed = 10f; }
                        else if (speed > 20f)
                        {
                            speed = 20f;
                        }


                    }
                    
                    Game.DisplayNotification("The ~r~" + modelName + "~s~ is no longer mimicking you.");
                    Game.LogTrivial("Done mimicking");
                    if (stoppedCar.Exists())
                    {
                        //NativeFunction.Natives.SET_VEHICLE_FORWARD_SPEED(stoppedCar, 2f);
                        if (pulledDriver.Exists())
                        {
                            pulledDriver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
                        }
                    }
                    //NativeFunction.Natives.DELETE_CHECKPOINT(CheckPoint);
                    if (blip.Exists()) { blip.Delete(); }
                }
                catch (Exception e)
                {
                    if (blip.Exists()) { blip.Delete(); }
                    Game.LogTrivial(e.ToString());
                    Game.LogTrivial("Error handled.");
                }
                finally
                {
                    TrafficPolicerHandler.isSomeoneFollowing = false;
                }



            });
        }
        public static string TestCallback (Ped ped)
        {
            return "I am a male: " + ped.IsMale.ToString();
        }


        public static void followMe()
        {
            TrafficPolicerHandler.isSomeoneFollowing = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    if (!Functions.IsPlayerPerformingPullover())
                    {
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }
                    Game.LogTrivial("Following");
                    Ped playerPed = Game.LocalPlayer.Character;
                    if (!playerPed.IsInAnyVehicle(false))
                    {
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }

                    Vehicle playerCar = playerPed.CurrentVehicle;
                    
                    Vehicle stoppedCar = (Vehicle)World.GetClosestEntity(playerCar.GetOffsetPosition(Vector3.RelativeBack * 10f), 10f, (GetEntitiesFlags.ConsiderGroundVehicles | GetEntitiesFlags.ConsiderBoats | GetEntitiesFlags.ExcludeEmptyVehicles | GetEntitiesFlags.ExcludeEmergencyVehicles));
                    if (stoppedCar == null)
                    {
                        Game.DisplayNotification("Unable to detect the pulled over vehicle. Make sure you're in front of the vehicle and try again.");
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }
                    if (!stoppedCar.IsValid() || (stoppedCar == playerCar))
                    {
                        Game.DisplayNotification("Unable to detect the pulled over vehicle. Make sure you're in front of the vehicle and try again.");
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }
                    if (stoppedCar.Speed > 0.2f && !ExtensionMethods.IsPointOnWater(stoppedCar.Position))
                    {
                        Game.DisplayNotification("The vehicle must be stopped before they can follow you.");
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }
                    string modelName = stoppedCar.Model.Name.ToLower();
                    if (numbers.Contains<string>(modelName.Last().ToString()))
                    {
                        modelName = modelName.Substring(0, modelName.Length - 1);
                    }
                    modelName = char.ToUpper(modelName[0]) + modelName.Substring(1);
                    Ped pulledDriver = stoppedCar.Driver;
                    if (!pulledDriver.IsPersistent || Functions.GetPulloverSuspect(Functions.GetCurrentPullover()) != pulledDriver)
                    {
                        Game.DisplayNotification("Unable to detect the pulled over vehicle. Make sure you're in front of the vehicle and try again.");
                        TrafficPolicerHandler.isSomeoneFollowing = false;
                        return;
                    }
                    Blip blip = pulledDriver.AttachBlip();
                    blip.Flash(500, -1);
                    blip.Color = System.Drawing.Color.Aqua;
                    playerCar.BlipSiren(true);
                    pulledDriver.Tasks.DriveToPosition(playerCar.GetOffsetPosition(Vector3.RelativeBack * 3f), 7f, VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians);

                    Game.DisplayNotification("The blipped ~r~" + modelName + "~s~ is now following you.");
                    Game.DisplayNotification("Press ~b~" + ExtensionMethods.GetKeyString(TrafficPolicerHandler.trafficStopFollowKey, TrafficPolicerHandler.trafficStopFollowModifierKey) + " ~s~to stop the ~r~" + modelName + ".");
                    try
                    {
                        playerPed.Tasks.PlayAnimation("friends@frj@ig_1", "wave_c", 1f, AnimationFlags.SecondaryTask | AnimationFlags.UpperBodyOnly);
                    }
                    catch { }
                    GameFiber.Sleep(100);
                    float speed = 7f;
                    while (true)
                    {
                        
                        pulledDriver.Tasks.DriveToPosition(playerCar.GetOffsetPosition(Vector3.RelativeBack * 3f), speed, VehicleDrivingFlags.IgnorePathFinding);
                        GameFiber.Sleep(60);

                        if (!TrafficPolicerHandler.isSomeoneFollowing)
                        {
                            break;
                        }
                        
                        if (!Functions.IsPlayerPerformingPullover())
                        {
                            Game.DisplayNotification("You cancelled the ~b~Traffic Stop.");
                            break;
                        }
                        if (!playerPed.IsInVehicle(playerCar, false))
                        {
                            break;
                        }
                        speed = playerCar.Speed;
                        if (Vector3.Distance(playerCar.Position, stoppedCar.Position) > 45f)
                        {
                            stoppedCar.Position = playerCar.GetOffsetPosition(Vector3.RelativeBack * 7f);
                            stoppedCar.Heading = playerCar.Heading;
                            blip.Delete();
                            blip = pulledDriver.AttachBlip();
                            blip.Flash(500, -1);
                            blip.Color = System.Drawing.Color.Aqua;
                        }
                        else
                        {
                            
                            if (speed > 17f) { speed = 17f; }
                            else if (speed < 6.5f)
                            {
                                speed = 6.5f;
                            }
                            if (Vector3.Distance(playerCar.Position, stoppedCar.Position) > 21f)
                            {
                                speed = 17f;
                            }
                        }
                        

                    }
                    if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                    {
                        API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "Vehicles made to follow you");
                    }
                    Game.DisplayNotification("The ~r~" + modelName + "~s~ is no longer following you.");
                    Game.LogTrivial("Done following");
                    if (blip.Exists()) { blip.Delete(); }
                }
                catch (Exception e)
                {
                    if (blip.Exists()) { blip.Delete(); }
                    Game.LogTrivial(e.ToString());
                    Game.LogTrivial("Error handled.");
                }
                finally
                {
                    TrafficPolicerHandler.isSomeoneFollowing = false;
                }
            });
        }

        public static void checkForYieldDisable()
        {
            Ped playerPed = Game.LocalPlayer.Character;
            if (!playerPed) { return; }
            if (Functions.IsPlayerPerformingPullover())
            {

                if (playerPed.IsInAnyVehicle(false))
                {
                    if (playerPed.CurrentVehicle.IsPoliceVehicle)
                    {
                        playerPed.CurrentVehicle.ShouldVehiclesYieldToThisVehicle = false;
                    }
                }
            }
            else
            {
                if (playerPed.IsInAnyVehicle(false))
                {
                    if (playerPed.CurrentVehicle.IsPoliceVehicle)
                    {
                        playerPed.CurrentVehicle.ShouldVehiclesYieldToThisVehicle = true;
                    }
                }
            }

        }
        
        public static float VehicleDoorLockDistance = 5.2f;
        public static float VehicleDoorUnlockDistance = 3.5f;
        public static List<Vehicle> PlayerVehicles = new List<Vehicle>();
        public static void LockPlayerDoors()
        {
            if (Game.LocalPlayer.Character.IsInAnyVehicle(false) && Game.LocalPlayer.Character.CurrentVehicle.HasSiren)
            {
                Vehicle veh = Game.LocalPlayer.Character.CurrentVehicle;
                if (veh && veh.Model.IsCar && !PlayerVehicles.Contains(veh))
                {
                    PlayerVehicles.Add(veh);
                }
            }

            foreach (Vehicle veh in PlayerVehicles.ToArray())
            {
                if (veh.Exists())
                {
                    if (Game.LocalPlayer.Character.IsInVehicle(veh, false))
                    {
                        if (veh.LockStatus != VehicleLockStatus.Locked)
                        {
                            if (veh.Speed > 4f)
                            {
                                veh.LockStatus = VehicleLockStatus.Locked;
                            }
                        }
                        else if (veh.LockStatus == VehicleLockStatus.Locked)
                        {
                            if (veh.Speed < 0.2f)
                            {
                                veh.LockStatus = VehicleLockStatus.Unlocked;
                            }
                        }
                    }
                    else
                    {
                        if (veh.LockStatus != VehicleLockStatus.Locked)
                        {
                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, veh.Position) > VehicleDoorLockDistance)
                            {
                                veh.LockStatus = VehicleLockStatus.Locked;
                            }

                        }
                        else if (veh.LockStatus == VehicleLockStatus.Locked)
                        {
                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, veh.Position) < VehicleDoorUnlockDistance)
                            {
                                veh.LockStatus = VehicleLockStatus.Unlocked;
                            }
                        }
                    }
                }

                else
                {
                    PlayerVehicles.Remove(veh);
                }
            }
        }

        public static void checkForceRedLightRun()
        {
            TrafficPolicerHandler.isSomeoneRunningTheLight = true;

            try
            {
                if (Game.LocalPlayer.IsPressingHorn)
                {
                    GameFiber.Sleep(800);
                    if (Game.LocalPlayer.IsPressingHorn)
                    {
                        Game.LogTrivial("Player pressing horn");
                        Vehicle playerCar = Game.LocalPlayer.Character.CurrentVehicle;

                        Ped stoppedCarDriver = Functions.GetPulloverSuspect(Functions.GetCurrentPullover());

                        Vehicle stoppedCar = stoppedCarDriver.CurrentVehicle;
                        if (stoppedCar.IsStoppedAtTrafficLights)
                        {
                            Game.LogTrivial("Forcing run red light.");
                            stoppedCar.Driver.Tasks.ClearImmediately();
                            stoppedCarDriver.WarpIntoVehicle(stoppedCar, -1);
                            stoppedCar.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraight).WaitForCompletion(2000);
                            stoppedCar.Driver.Tasks.ClearImmediately();
                            stoppedCarDriver.WarpIntoVehicle(stoppedCar, -1);

                        }
                    }
                }
            }
            catch (Exception e)
            {
                Game.LogTrivial(e.ToString());
                Game.LogTrivial("Traffic Policer handled the exception successfully.");
            }
            finally
            {
                TrafficPolicerHandler.isSomeoneRunningTheLight = false;
            }


        }   
    }
}
