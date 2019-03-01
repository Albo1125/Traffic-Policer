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
using System.Diagnostics;

namespace Traffic_Policer
{
    internal static class SpeedChecker
    {
        private static int CheckPoint;
        private static Vector3 CheckPointPosition;
        private static string TargetModel = "";
        public static string SpeedUnit = "MPH";
        private static int TargetSpeed = 0;
        private static string TargetFlag = "";
        //private static string TargetSpeedLimit = "";
        public static Keys SecondaryDisableKey = Keys.Back;
        public static Keys ToggleSpeedCheckerKey = Keys.F5;
        public static Keys ToggleSpeedCheckerModifierKey = Keys.None;
        public static Keys PositionResetKey = Keys.NumPad5;
        public static Keys PositionForwardKey = Keys.NumPad8;
        public static Keys PositionBackwardKey = Keys.NumPad2;
        public static Keys PositionRightKey = Keys.NumPad6;
        public static Keys PositionLeftKey = Keys.NumPad4;
        public static Keys PositionUpKey = Keys.NumPad9;

        public static Keys PositionDownKey = Keys.NumPad3;
        public static Keys MaxSpeedUpKey = Keys.PageUp;
        public static Keys MaxSpeedDownKey = Keys.PageDown;
        private static Color FlagsTextColour = Color.White;
        private static string TargetLicencePlate = "";
        private static List<Vehicle> VehiclesFlagged = new List<Vehicle>();
        private static List<Vehicle> VehiclesBlipPlayedFor = new List<Vehicle>();
        private static List<Ped> FlaggedDrivers = new List<Ped>();
        public static int FlagChance = 15;
        private static Color SpeedColour = Color.White;
        public static int SpeedToColourAt = 70;
        private static int xOffset = 0;
        private static int yOffset = 0;
        private static int zOffset = 0;
        private static System.Media.SoundPlayer FlagBlipPlayer = new System.Media.SoundPlayer("lspdfr/audio/scanner/Traffic Policer Audio/FLAG_BLIP.wav");
        public static bool PlayFlagBlip = true;
        private static List<Vehicle> VehiclesNotFlagged = new List<Vehicle>();

        enum SpeedCheckerStates { Average, FixedPoint, Speedgun, Off }
        private static SpeedCheckerStates CurrentSpeedCheckerState = SpeedCheckerStates.Off;

        private static Vector3 LastAverageSpeedCheckReferencePoint;
        private static Stopwatch AverageSpeedCheckStopwatch = new Stopwatch();
        private static float AverageSpeedCheckDistance = 0f;
        private static bool MeasuringAverageSpeed = false;
        private static float AverageSpeedCheckSecondsPassed = 0f;
        private static int AverageSpeedCheckCurrentSpeed = 0;
        private static float AverageSpeed = 0f;
        public static Keys StartStopAverageSpeedCheckKey = Keys.PageUp;
        public static Keys ResetAverageSpeedCheckKey = Keys.PageDown;
        private static Color AverageSpeedCheckerColor = Color.White;

        public static WeaponAsset speedgunWeapon = "WEAPON_MARKSMANPISTOL";

        public static void Main()
        {
            GameFiber.StartNew(delegate
            {
                Game.RawFrameRender += DrawVehicleInfo;
                LowPriority();
                Game.LogTrivial("Traffic Policer Speed Checker started.");
                try
                {
                    while (true)
                    {
                        GameFiber.Yield();                       

                        if (CurrentSpeedCheckerState == SpeedCheckerStates.Speedgun)
                        {
                            if (Game.LocalPlayer.Character.Inventory.EquippedWeapon == null || Game.LocalPlayer.Character.Inventory.EquippedWeapon.Asset != speedgunWeapon
                            || (Game.LocalPlayer.Character.CurrentVehicle.Exists() && Game.LocalPlayer.Character.CurrentVehicle.Speed >= 3f))
                            {
                                CurrentSpeedCheckerState = SpeedCheckerStates.Off;
                                continue;
                            }

                            Game.DisableControlAction(0, GameControl.Attack, true);
                            Game.DisableControlAction(0, GameControl.Attack2, true);
                            Game.DisableControlAction(0, GameControl.MeleeAttack1, true);
                            Game.DisableControlAction(0, GameControl.MeleeAttack2, true);
                            Game.DisableControlAction(0, GameControl.VehicleAttack, true);
                            //Game.DisableControlAction(0, GameControl.VehicleAttack2, true);
                            if (NativeFunction.Natives.IS_DISABLED_CONTROL_JUST_PRESSED<bool>(0, 24))
                            {
                                Vehicle veh = null;
                                try
                                {
                                    unsafe
                                    {
                                        uint entityHandle;
                                        NativeFunction.Natives.x2975C866E6713290(Game.LocalPlayer, new IntPtr(&entityHandle)); // Stores the entity the player is aiming at in the uint provided in the second parameter.
                                        Entity ent = World.GetEntityByHandle<Rage.Entity>(entityHandle);
                                        if (ent is Ped)
                                        {
                                            veh = ((Ped)ent).CurrentVehicle;
                                        }
                                        else if (ent is Vehicle)
                                        {
                                            veh = (Vehicle)ent;
                                        }

                                    }
                                }
                                catch (Exception e) { }
                                if (veh.Exists())
                                {
                                    TargetModel = veh.Model.Name;
                                    TargetModel = char.ToUpper(TargetModel[0]) + TargetModel.Substring(1).ToLower();
                                    if (SpeedUnit == "MPH")
                                    {
                                        TargetSpeed = (int)Math.Round(MathHelper.ConvertMetersPerSecondToMilesPerHour(veh.Speed));
                                    }
                                    else
                                    {
                                        TargetSpeed = MathHelper.ConvertMetersPerSecondToKilometersPerHourRounded(veh.Speed);
                                    }
                                    if (TargetSpeed >= SpeedToColourAt)
                                    {
                                        SpeedColour = Color.Red;
                                        if (PlayFlagBlip)
                                        {
                                            if (!VehiclesBlipPlayedFor.Contains(veh))
                                            {
                                                VehiclesBlipPlayedFor.Add(veh);
                                                FlagBlipPlayer.Play();
                                                Game.DisplayNotification("~s~Model: ~b~" + TargetModel + "~n~~s~Speed: " + (SpeedColour == Color.Red ? "~r~" : "") + TargetSpeed + " " + SpeedUnit);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        SpeedColour = Color.White;
                                    }
                                }
                            }
                        }                       

                        if (Game.LocalPlayer.Character.IsInAnyVehicle(false) && (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(ToggleSpeedCheckerModifierKey) || ToggleSpeedCheckerModifierKey == Keys.None))
                        {
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(ToggleSpeedCheckerKey))
                            {
                                if (CurrentSpeedCheckerState != SpeedCheckerStates.Off && CurrentSpeedCheckerState != SpeedCheckerStates.Speedgun)
                                {
                                    GameFiber.Wait(200);
                                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(ToggleSpeedCheckerKey))
                                    {
                                        CurrentSpeedCheckerState = SpeedCheckerStates.Off;
                                        NativeFunction.Natives.DELETE_CHECKPOINT(CheckPoint);
                                        ResetAverageSpeedCheck();
                                        Game.HideHelp();
                                    }
                                    else
                                    {
                                        Game.DisplaySubtitle("~h~Hold Speed Checker toggle to disable.", 3000);
                                        if (CurrentSpeedCheckerState == SpeedCheckerStates.Average)
                                        {
                                            ResetAverageSpeedCheck();
                                            CurrentSpeedCheckerState = SpeedCheckerStates.FixedPoint;
                                            DisplayMaxSpeedMessage();
                                        }
                                        else if (CurrentSpeedCheckerState == SpeedCheckerStates.FixedPoint)
                                        {
                                            NativeFunction.Natives.DELETE_CHECKPOINT(CheckPoint);
                                            CurrentSpeedCheckerState = SpeedCheckerStates.Average;
                                            DisplayAverageSpeedCheckInstructions();
                                        }
                                    }
                                }
                                else if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                                {
                                    if (CurrentSpeedCheckerState == SpeedCheckerStates.Speedgun)
                                    {
                                        Game.DisplaySubtitle("Please unequip your speedgun first.");
                                    }
                                    else
                                    {
                                        if (Game.LocalPlayer.Character.CurrentVehicle.Speed > 6f)
                                        {
                                            CurrentSpeedCheckerState = SpeedCheckerStates.Average;
                                            DisplayAverageSpeedCheckInstructions();
                                        }
                                        else
                                        {
                                            CurrentSpeedCheckerState = SpeedCheckerStates.FixedPoint;

                                            DisplayMaxSpeedMessage();

                                        }
                                    }




                                }
                                CheckPointPosition = Game.LocalPlayer.Character.GetOffsetPosition(new Vector3(0f, 8f, -1f));
                                //xOffset = 0;
                                //yOffset = 0;
                                //zOffset = 0;
                            }

                        }
                        if (CurrentSpeedCheckerState != SpeedCheckerStates.Off && CurrentSpeedCheckerState != SpeedCheckerStates.Speedgun)
                        {
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(SecondaryDisableKey) || !Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                CurrentSpeedCheckerState = SpeedCheckerStates.Off;
                                NativeFunction.Natives.DELETE_CHECKPOINT(CheckPoint);
                                CheckPointPosition = Game.LocalPlayer.Character.GetOffsetPosition(new Vector3(0f, 8f, -1f));

                                ResetAverageSpeedCheck();
                                Game.HideHelp();
                            }
                            //xOffset = 0;
                            //yOffset = 0;
                            //zOffset = 0;
                        }

                        if (CurrentSpeedCheckerState == SpeedCheckerStates.FixedPoint && Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {

                            CheckPointPosition = Game.LocalPlayer.Character.GetOffsetPosition(new Vector3((float)xOffset + 0.5f, (float)(yOffset + 8), (float)(-1 + zOffset)));

                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(PositionResetKey))
                            {
                                xOffset = 0;
                                yOffset = 0;
                                zOffset = 0;

                            }
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(PositionForwardKey))
                            {
                                yOffset++;
                            }
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(PositionBackwardKey))
                            {
                                yOffset--;
                            }
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(PositionRightKey))
                            {
                                xOffset++;
                            }
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(PositionLeftKey))
                            {
                                xOffset--;
                            }
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(PositionUpKey))
                            {
                                zOffset++;
                            }
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(PositionDownKey))
                            {
                                zOffset--;
                            }
                            NativeFunction.Natives.DELETE_CHECKPOINT(CheckPoint);
                            CheckPoint = NativeFunction.Natives.CREATE_CHECKPOINT<int>(46, CheckPointPosition.X, CheckPointPosition.Y, CheckPointPosition.Z, CheckPointPosition.X, CheckPointPosition.Y, CheckPointPosition.Z, 3.5f, 255, 0, 0, 255, 0);
                            NativeFunction.Natives.SET_CHECKPOINT_CYLINDER_HEIGHT(CheckPoint, 2f, 2f, 2f);
                        }

                        if ((CurrentSpeedCheckerState == SpeedCheckerStates.FixedPoint && Game.LocalPlayer.Character.IsInAnyVehicle(false)) || CurrentSpeedCheckerState == SpeedCheckerStates.Speedgun)
                        {
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(MaxSpeedUpKey))
                            {
                                SpeedToColourAt += 5;
                                DisplayMaxSpeedMessage();
                            }
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(MaxSpeedDownKey))
                            {
                                SpeedToColourAt -= 5;
                                if (SpeedToColourAt < 0) { SpeedToColourAt = 0; }
                                DisplayMaxSpeedMessage();
                            }
                        }

                        else if (CurrentSpeedCheckerState == SpeedCheckerStates.Average && Game.LocalPlayer.Character.IsInAnyVehicle(false))
                        {
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(StartStopAverageSpeedCheckKey))
                            {
                                if (MeasuringAverageSpeed)
                                {
                                    StopAverageSpeedCheck();
                                }
                                else if (!MeasuringAverageSpeed && AverageSpeedCheckSecondsPassed == 0f)
                                {
                                    StartAverageSpeedCheck();
                                }
                                else
                                {
                                    Game.DisplayHelp("Reset the average speed check first using ~b~" + TrafficPolicerHandler.kc.ConvertToString(ResetAverageSpeedCheckKey));
                                }
                            }
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(ResetAverageSpeedCheckKey))
                            {
                                if (!MeasuringAverageSpeed)
                                {
                                    ResetAverageSpeedCheck();
                                }
                                else
                                {
                                    Game.DisplayHelp("Stop current average speed check first using ~b~" + TrafficPolicerHandler.kc.ConvertToString(StartStopAverageSpeedCheckKey));
                                }
                            }
                        }
                    }
                }
                catch (Exception e) { NativeFunction.Natives.DELETE_CHECKPOINT(CheckPoint); throw; }

            });
        }

        private static void LowPriority()
        {
            GameFiber.StartNew(delegate
            {
                while (true)
                {
                    GameFiber.Wait(100);

                    foreach (Ped flaggeddriver in FlaggedDrivers.ToArray())
                    {
                        if (flaggeddriver.Exists())
                        {
                            if (flaggeddriver.DistanceTo(Game.LocalPlayer.Character) > 300f)
                            {
                                flaggeddriver.IsPersistent = false;
                                //flaggeddriver.Dismiss();
                                FlaggedDrivers.Remove(flaggeddriver);

                            }
                            else if (Functions.IsPlayerPerformingPullover())
                            {
                                if (Functions.GetPulloverSuspect(Functions.GetCurrentPullover()) == flaggeddriver)
                                {
                                    FlaggedDrivers.Remove(flaggeddriver);

                                }
                            }
                            else if (Functions.GetActivePursuit() != null)
                            {
                                if (Functions.GetPursuitPeds(Functions.GetActivePursuit()).Contains(flaggeddriver))
                                {
                                    FlaggedDrivers.Remove(flaggeddriver);

                                }
                            }
                        }
                        else
                        {
                            FlaggedDrivers.Remove(flaggeddriver);
                        }

                    }

                    if (CurrentSpeedCheckerState != SpeedCheckerStates.Speedgun && Game.LocalPlayer.Character.Inventory.EquippedWeapon != null &&
                    Game.LocalPlayer.Character.Inventory.EquippedWeapon.Asset == speedgunWeapon && !Game.LocalPlayer.Character.CurrentVehicle.Exists())
                    {
                        CurrentSpeedCheckerState = SpeedCheckerStates.Speedgun;
                        DisplayMaxSpeedMessage();
                    }

                    if (CurrentSpeedCheckerState == SpeedCheckerStates.FixedPoint && Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    {
                        Entity[] WorldVehicles = World.GetEntities(CheckPointPosition, 7, GetEntitiesFlags.ConsiderAllVehicles | GetEntitiesFlags.ExcludePlayerVehicle);
                        foreach (Vehicle veh in WorldVehicles)
                        {
                            if (veh.Exists() && veh != Game.LocalPlayer.Character.CurrentVehicle && veh.DistanceTo(CheckPointPosition) <= 6.5f)
                            {
                                bool ShowVehicleNotification = false;
                                TargetModel = veh.Model.Name;
                                TargetModel = char.ToUpper(TargetModel[0]) + TargetModel.Substring(1).ToLower();
                                if (SpeedUnit == "MPH")
                                {
                                    TargetSpeed = (int)Math.Round(MathHelper.ConvertMetersPerSecondToMilesPerHour(veh.Speed));

                                }
                                else
                                {
                                    TargetSpeed = MathHelper.ConvertMetersPerSecondToKilometersPerHourRounded(veh.Speed);
                                }
                                if (TargetSpeed >= SpeedToColourAt)
                                {
                                    SpeedColour = Color.Red;
                                    if (PlayFlagBlip)
                                    {

                                        if (!VehiclesBlipPlayedFor.Contains(veh))
                                        {
                                            VehiclesBlipPlayedFor.Add(veh);
                                            FlagBlipPlayer.Play();
                                            ShowVehicleNotification = true;
                                        }

                                    }
                                }
                                else
                                {
                                    SpeedColour = Color.White;
                                }
                                //TargetSpeedLimit = GetSpeedLimit(veh.Position, SpeedUnit);

                                TargetFlag = "";
                                TargetLicencePlate = veh.LicensePlate;
                                FlagsTextColour = Color.White;
                                if ((TrafficPolicerHandler.rnd.Next(101) <= FlagChance || VehiclesFlagged.Contains(veh)) && !veh.HasSiren && !VehiclesNotFlagged.Contains(veh))
                                {
                                    if (!VehiclesFlagged.Contains(veh))
                                    {
                                        VehiclesFlagged.Add(veh);
                                    }
                                    if (!VehicleDetails.IsVehicleInDetailsDatabase(veh))
                                    {
                                        VehicleDetails.AddVehicleToDetailsDatabase(veh, 25);
                                    }

                                    if (veh.IsStolen || VehicleDetails.GetInsuranceStatusForVehicle(veh) != EVehicleDetailsStatus.Valid)
                                    {
                                        if (veh.IsStolen)
                                        {
                                            TargetFlag = "Stolen";
                                            FlagsTextColour = Color.Red;
                                        }
                                        else if (VehicleDetails.GetInsuranceStatusForVehicle(veh) != EVehicleDetailsStatus.Valid)
                                        {
                                            TargetFlag = "Uninsured";
                                            FlagsTextColour = Color.Red;
                                        }
                                    }

                                    else
                                    {
                                        if (veh.HasDriver && veh.Driver.Exists())
                                        {
                                            if (Functions.GetPersonaForPed(veh.Driver).Wanted)
                                            {
                                                TargetFlag = "Owner Wanted";
                                                FlagsTextColour = Color.Red;
                                            }
                                            else if (Functions.GetPersonaForPed(veh.Driver).ELicenseState == LSPD_First_Response.Engine.Scripting.Entities.ELicenseState.Suspended)
                                            {
                                                TargetFlag = "Licence Suspended";
                                                FlagsTextColour = Color.Red;
                                            }
                                            else if (Functions.GetPersonaForPed(veh.Driver).ELicenseState == LSPD_First_Response.Engine.Scripting.Entities.ELicenseState.Expired)
                                            {
                                                TargetFlag = "Licence Expired";
                                                FlagsTextColour = Color.Orange;
                                            }
                                            else if (Functions.GetPersonaForPed(veh.Driver).Birthday.Month == DateTime.Now.Month && Functions.GetPersonaForPed(veh.Driver).Birthday.Day == DateTime.Now.Day)
                                            {
                                                TargetFlag = "Owner's Birthday";
                                                FlagsTextColour = Color.Green;
                                            }

                                            if (TargetFlag != "")
                                            {
                                                if (!FlaggedDrivers.Contains(veh.Driver))
                                                {
                                                    if (!veh.Driver.IsPersistent)
                                                    {
                                                        FlaggedDrivers.Add(veh.Driver);
                                                        veh.Driver.IsPersistent = true;


                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if (PlayFlagBlip)
                                    {
                                        if (TargetFlag != "")
                                        {
                                            if (!VehiclesBlipPlayedFor.Contains(veh))
                                            {
                                                VehiclesBlipPlayedFor.Add(veh);
                                                FlagBlipPlayer.Play();
                                                ShowVehicleNotification = true;
                                            }
                                        }
                                    }


                                }
                                if (TargetFlag == "")
                                {
                                    VehiclesNotFlagged.Add(veh);
                                }

                                if (ShowVehicleNotification)
                                {
                                    Game.DisplayNotification("Plate: ~b~" + TargetLicencePlate + "~n~~s~Model: ~b~" + TargetModel + "~n~~s~Speed: " + (SpeedColour == Color.Red ? "~r~" : "") + TargetSpeed + " " + SpeedUnit + "~n~~s~Flags: ~r~" + TargetFlag);
                                }


                            }
                        }
                    }

                    else if (CurrentSpeedCheckerState == SpeedCheckerStates.Average && Game.LocalPlayer.Character.IsInAnyVehicle(false))
                    {

                        if (SpeedUnit == "MPH")
                        {
                            AverageSpeedCheckCurrentSpeed = (int)Math.Round(MathHelper.ConvertMetersPerSecondToMilesPerHour(Game.LocalPlayer.Character.CurrentVehicle.Speed));

                        }
                        else
                        {
                            AverageSpeedCheckCurrentSpeed = MathHelper.ConvertMetersPerSecondToKilometersPerHourRounded(Game.LocalPlayer.Character.CurrentVehicle.Speed);
                        }
                        if (MeasuringAverageSpeed)
                        {
                            AverageSpeedCheckDistance += Vector3.Distance(LastAverageSpeedCheckReferencePoint, Game.LocalPlayer.Character.CurrentVehicle.Position);
                            LastAverageSpeedCheckReferencePoint = Game.LocalPlayer.Character.CurrentVehicle.Position;
                            AverageSpeedCheckSecondsPassed = ((float)AverageSpeedCheckStopwatch.ElapsedMilliseconds) / 1000;
                        }


                    }
                }
            });
        }
        private static void ResetAverageSpeedCheck()
        {
            AverageSpeedCheckStopwatch.Reset();
            AverageSpeed = 0f;
            AverageSpeedCheckDistance = 0f;
            AverageSpeedCheckSecondsPassed = 0f;
            MeasuringAverageSpeed = false;
            AverageSpeedCheckerColor = Color.White;
            MeasuringAverageSpeed = false;
        }

        private static void StartAverageSpeedCheck()
        {
            AverageSpeedCheckStopwatch.Start();
            LastAverageSpeedCheckReferencePoint = Game.LocalPlayer.Character.CurrentVehicle.Position;
            MeasuringAverageSpeed = true;
            AverageSpeedCheckerColor = Color.Yellow;
        }
        private static void StopAverageSpeedCheck()
        {
            if (SpeedUnit == "MPH")
            {

                AverageSpeed = (AverageSpeedCheckDistance * 0.000621371f) / (AverageSpeedCheckSecondsPassed / 3600);

            }
            else
            {
                AverageSpeed = AverageSpeedCheckDistance / (AverageSpeedCheckSecondsPassed / 3600);
            }
            AverageSpeedCheckStopwatch.Stop();
            MeasuringAverageSpeed = false;
            AverageSpeedCheckerColor = Color.LightBlue;
        }

        private static void DisplayMaxSpeedMessage()
        {
            Game.DisplayHelp("Max Speed: ~r~" + SpeedToColourAt.ToString() + SpeedUnit + "~n~~s~Configure with ~b~" + TrafficPolicerHandler.kc.ConvertToString(MaxSpeedUpKey) + "~s~ and ~b~" + TrafficPolicerHandler.kc.ConvertToString(MaxSpeedDownKey), 3000);
        }

        private static void DisplayAverageSpeedCheckInstructions()
        {
            Game.DisplayHelp("Average Speed Check. ~b~" + TrafficPolicerHandler.kc.ConvertToString(StartStopAverageSpeedCheckKey) + "~s~: Start/Stop. ~b~" + TrafficPolicerHandler.kc.ConvertToString(ResetAverageSpeedCheckKey) + "~s~: Reset", 6000);
        }

        private static void DrawVehicleInfo(object sender, GraphicsEventArgs e)
        {
            if (CurrentSpeedCheckerState == SpeedCheckerStates.FixedPoint)
            {
                Rectangle drawRect = new Rectangle(1, 250, 230, 117);
                e.Graphics.DrawRectangle(drawRect, Color.FromArgb(200, Color.Black));
                e.Graphics.DrawText("Plate: " + TargetLicencePlate, "Arial Bold", 20.0f, new PointF(3f, 253f), Color.White, drawRect);
                e.Graphics.DrawText("Model: " + TargetModel, "Arial Bold", 20.0f, new PointF(3f, 278f), Color.White, drawRect);

                e.Graphics.DrawText("Speed: " + TargetSpeed + " " + SpeedUnit, "Arial Bold", 20.0f, new PointF(3f, 303f), SpeedColour, drawRect);
                //e.Graphics.DrawText(TargetSpeedLimit, "Arial Bold", 15.0f, new PointF(3f, 293f), Color.White, drawRect);
                e.Graphics.DrawText("Flags: " + TargetFlag, "Arial Bold", 20.0f, new PointF(3f, 328f), FlagsTextColour, drawRect);

            }
            else if (CurrentSpeedCheckerState == SpeedCheckerStates.Speedgun)
            {
                Rectangle drawRect = new Rectangle(1, 250, 230, 70);
                e.Graphics.DrawRectangle(drawRect, Color.FromArgb(200, Color.Black));
                e.Graphics.DrawText("Model: " + TargetModel, "Arial Bold", 20.0f, new PointF(3f, 253f), Color.White, drawRect);
                e.Graphics.DrawText("Speed: " + TargetSpeed + " " + SpeedUnit, "Arial Bold", 20.0f, new PointF(3f, 278f), SpeedColour, drawRect);

            }
            else if (CurrentSpeedCheckerState == SpeedCheckerStates.Average)
            {
                Rectangle drawRect = new Rectangle(1, 250, 230, 117);
                e.Graphics.DrawRectangle(drawRect, Color.FromArgb(200, Color.Black));
                e.Graphics.DrawText("D: " + AverageSpeedCheckDistance + "m", "Arial Bold", 20.0f, new PointF(3f, 253f), Color.White, drawRect);
                e.Graphics.DrawText("T: " + AverageSpeedCheckSecondsPassed.ToString("N2") + "s", "Arial Bold", 20.0f, new PointF(3f, 278f), Color.White, drawRect);
                e.Graphics.DrawText("O: " + AverageSpeedCheckCurrentSpeed + " " + SpeedUnit, "Arial Bold", 20.0f, new PointF(3f, 303f), Color.White, drawRect);
                e.Graphics.DrawText("S: " + AverageSpeed.ToString("N2") + " " + SpeedUnit, "Arial Bold", 20.0f, new PointF(3f, 328f), AverageSpeedCheckerColor, drawRect);

            }
        }
    }
}
