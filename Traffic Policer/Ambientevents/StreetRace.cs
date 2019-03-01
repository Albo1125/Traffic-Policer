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
    internal class StreetRace : AmbientEvent
    {
        
        
        private Vehicle vehRacer1;
        private Vehicle vehRacer2;
        private string[] carModels1 = { "NEMESIS","SULTAN",  "EXEMPLAR", "F620", "FELON2", "FELON", "SENTINEL", "WINDSOR", "DOMINATOR", "DUKES", "GAUNTLET", "VIRGO", "BLISTA2", "BUFFALO", "HEXER", "ZENTORNO", "MASSACRO" };
        private string[] carModels2 = { "NEMESIS", "SULTAN",  "EXEMPLAR", "F620", "FELON2", "FELON", "SENTINEL", "WINDSOR", "DOMINATOR", "DUKES", "GAUNTLET", "VIRGO", "BLISTA2", "BUFFALO", "HEXER", "ZENTORNO", "MASSACRO" };
        
        private Ped drvPed1;
        private Ped drvPed2;
        private Vector3 SpawnPoint;
        private Vector3 SpawnPoint1;
        private Blip drvBlip1;
        private Blip drvBlip2;
        private LHandle pursuit;


        /// <summary>
        /// Having a method with the same name as the class calls this method when the class is created
        /// </summary>
        public StreetRace(bool createBlip, bool showMessage) : base (showMessage, "Creating street race event.")
        {


            SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(100f));
            while (Vector3.Distance(Game.LocalPlayer.Character.Position, SpawnPoint) < 90f)
            {
                GameFiber.Yield();
                SpawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(100f));
            }
            
            drvPed1 = new Ped(SpawnPoint);

            Vector3 directionFromVehicleToPed = (Game.LocalPlayer.Character.Position - drvPed1.Position);
            directionFromVehicleToPed.Normalize();

            float heading = MathHelper.ConvertDirectionToHeading(directionFromVehicleToPed);
            drvPed1.Heading = heading;

            SpawnPoint1 = drvPed1.GetOffsetPosition(Vector3.RelativeBack * 10f);
            drvPed2 = new Ped(SpawnPoint1);
            drvPed2.Heading = drvPed1.Heading;
            drvPed1.BlockPermanentEvents = true;
            drvPed2.BlockPermanentEvents = true;
            drvPed1.IsPersistent = true;
            drvPed2.IsPersistent = true;

            vehRacer1 = new Vehicle(carModels1[MathHelper.GetRandomInteger(carModels1.Length - 1)], SpawnPoint);
            vehRacer1.Heading = drvPed1.Heading;
            vehRacer1.IsPersistent = true;
            vehRacer1.RandomiseLicencePlate();

            if (vehRacer1.Exists())
            {
                Random rnd = new Random();

                int randomNumber = rnd.Next(4);

                if (randomNumber == 1)
                {
                    vehRacer1.Mods.InstallModKit();
                    vehRacer1.Mods.ApplyAllMods();
                }
                else
                {
                    vehRacer1.Mods.InstallModKit();

                    vehRacer1.Mods.EngineModIndex = vehRacer1.Mods.EngineModCount - 1;

                    vehRacer1.Mods.ExhaustModIndex = vehRacer1.Mods.ExhaustModCount - 1;

                    vehRacer1.Mods.TransmissionModIndex = vehRacer1.Mods.TransmissionModCount - 1;

                    VehicleWheelType wheelType = MathHelper.Choose(VehicleWheelType.Sport, VehicleWheelType.SUV, VehicleWheelType.HighEnd);
                    int wheelModIndex = MathHelper.GetRandomInteger(vehRacer1.Mods.GetWheelModCount(wheelType));
                    vehRacer1.Mods.SetWheelMod(wheelType, wheelModIndex, true);

                    vehRacer1.Mods.HasTurbo = true;

                    vehRacer1.Mods.HasXenonHeadlights = true;
                }
            }


            vehRacer2 = new Vehicle(carModels2[MathHelper.GetRandomInteger(carModels2.Length - 1)], SpawnPoint1);
            vehRacer2.Heading = drvPed2.Heading;
            vehRacer2.IsPersistent = true;
            vehRacer2.RandomiseLicencePlate();
            if (vehRacer2.Exists())
            {
                Random rnd = new Random();

                int randomNumber = rnd.Next(4);

                if (randomNumber == 1)
                {
                    vehRacer2.Mods.InstallModKit();
                    vehRacer2.Mods.ApplyAllMods();
                }
                else
                {
                    vehRacer2.Mods.InstallModKit();

                    vehRacer2.Mods.EngineModIndex = vehRacer2.Mods.EngineModCount - 1;


                    vehRacer2.Mods.ExhaustModIndex = vehRacer2.Mods.ExhaustModCount - 1;

                    vehRacer2.Mods.TransmissionModIndex = vehRacer2.Mods.TransmissionModCount - 1;

                    VehicleWheelType wheelType = MathHelper.Choose(VehicleWheelType.Sport, VehicleWheelType.SUV, VehicleWheelType.HighEnd);
                    int wheelModIndex = MathHelper.GetRandomInteger(vehRacer2.Mods.GetWheelModCount(wheelType));
                    vehRacer2.Mods.SetWheelMod(wheelType, wheelModIndex, true);

                    vehRacer2.Mods.HasTurbo = true;

                    vehRacer2.Mods.HasXenonHeadlights = true;
                }
            }
            if (createBlip)
            {
                drvBlip1 = drvPed1.AttachBlip();
                drvBlip1.Color = System.Drawing.Color.Beige;
                drvBlip1.Scale = 0.7f;

                drvBlip2 = drvPed2.AttachBlip();
                drvBlip2.Color = System.Drawing.Color.Beige;
                drvBlip2.Scale = 0.7f;
            }


            MainLogic();

        }
        protected override void MainLogic()
        {



            if (!drvPed1.Exists()) return;
            if (!drvPed2.Exists()) return;
            if (!vehRacer1.Exists()) return;
            if (!vehRacer2.Exists()) return;



            TrafficPolicerHandler.driversConsidered.Add(drvPed1);
            TrafficPolicerHandler.driversConsidered.Add(drvPed2);
            //Make some kind of cleanup function to be called


            drvPed1.WarpIntoVehicle(vehRacer1, -1);
            drvPed2.WarpIntoVehicle(vehRacer2, -1); //-1 is driverseat, 0 is passengerseat





            AmbientEventMainFiber = GameFiber.StartNew(delegate

            { //A multitasking fiber.
                try
                {
                    bool pursuitCreated = false;
                    bool teleported = false;
                    int closeCount = 0;
                    int waitingCount = 0;
                    drvPed1.Tasks.CruiseWithVehicle(vehRacer1, 80f, VehicleDrivingFlags.Emergency); //Make the ped drive
                    Rage.Native.NativeFunction.Natives.TASK_VEHICLE_CHASE(drvPed2, drvPed1);
                    while (eventRunning)
                    {//main loop, runs until broken with break;

                        GameFiber.Wait(10);
                        waitingCount++;




                        if (!pursuitCreated)
                        {
                            if (!teleported)
                            {
                                if ((waitingCount > 100) && waitingCount < 130)
                                {

                                    if (Vector3.Distance(drvPed1.Position, drvPed2.Position) > 40f)
                                    {
                                        vehRacer2.Position = vehRacer1.GetOffsetPosition(Vector3.RelativeBack * 5f);
                                        vehRacer2.Heading = vehRacer1.Heading;
                                        teleported = true;
                                    }
                                }
                            }

                            if ((Vector3.Distance(Game.LocalPlayer.Character.Position, vehRacer1.Position) < 20f) || (Vector3.Distance(Game.LocalPlayer.Character.Position, vehRacer2.Position) < 20f))
                            {

                                if (Functions.IsPlayerPerformingPullover() && Game.LocalPlayer.Character.IsInAnyVehicle(false))
                                {
                                    if ((Functions.GetPulloverSuspect(Functions.GetCurrentPullover()) == drvPed1) || (Functions.GetPulloverSuspect(Functions.GetCurrentPullover()) == drvPed2))
                                    {
                                        Game.LogTrivial("Initiating Street Race Pursuit");
                                        if (!drvPed1.Exists()) { End(); return; }
                                        if (!drvPed2.Exists()) { End(); return; }
                                        if (!vehRacer1.Exists()) { End(); return; }
                                        if (!vehRacer2.Exists()) { End(); return; }
                                        if (drvPed1.IsInVehicle(vehRacer1, false))
                                        {
                                            drvPed1.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
                                        }
                                        if (drvPed2.IsInVehicle(vehRacer2, false))
                                        {
                                            drvPed2.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
                                        }
                                        GameFiber.Wait(200);
                                        if (drvPed1.IsInVehicle(vehRacer1, false))
                                        {
                                            drvPed1.Tasks.PerformDrivingManeuver(VehicleManeuver.BurnOut);
                                        }
                                        if (drvPed2.IsInVehicle(vehRacer2, false))
                                        {
                                            drvPed2.Tasks.PerformDrivingManeuver(VehicleManeuver.BurnOut);
                                        }
                                        GameFiber.Wait(2000);

                                        if (drvBlip1.Exists()) { drvBlip1.Delete(); }
                                        if (drvBlip2.Exists()) { drvBlip2.Delete(); }
                                        drvPed1 = drvPed1.ClonePed();
                                        TrafficPolicerHandler.driversConsidered.Add(drvPed1);
                                        drvPed2 = drvPed2.ClonePed();
                                        TrafficPolicerHandler.driversConsidered.Add(drvPed2);
                                        pursuit = Functions.CreatePursuit();
                                        Functions.AddPedToPursuit(pursuit, drvPed1);
                                        Functions.AddPedToPursuit(pursuit, drvPed2);
                                        Functions.SetPursuitIsActiveForPlayer(pursuit, true);

                                        if (Game.LocalPlayer.Character.IsInAnyVehicle(false))
                                        {
                                            Game.LocalPlayer.Character.CurrentVehicle.IsSirenOn = true;
                                            Game.LocalPlayer.Character.CurrentVehicle.IsSirenSilent = false;
                                        }
                                        GameFiber.Wait(300);
                                        drvPed1.PlayAmbientSpeech("GENERIC_CURSE_HIGH", true);
                                        drvPed2.PlayAmbientSpeech("GENERIC_CURSE_HIGH", true);
                                        GameFiber.Wait(800);
                                        Functions.PlayScannerAudioUsingPosition("WE_HAVE VEHICLES_RACING IN_OR_ON_POSITION", Game.LocalPlayer.Character.Position);

                                        pursuitCreated = true;
                                        Game.LogTrivial("Street race pursuit created.");

                                        break;
                                    }
                                }


                            }
                            else if (Vector3.Distance(Game.LocalPlayer.Character.Position, drvPed1.Position) > 350f)
                            {
                                
                                End();
                                return;
                            }

                        }

                    }

                    if (pursuit == null) { End(); }
                    while (eventRunning && Functions.IsPursuitStillRunning(pursuit))
                    {

                        GameFiber.Yield();
                        if (drvPed1.Exists())
                        {

                            if (Vector3.Distance(drvPed1.Position, Game.LocalPlayer.Character.Position) > 2000f)
                            {
                                if (!Functions.IsPedArrested(drvPed1))
                                {
                                    Game.DisplayNotification("A street racer has ~r~escaped");
                                }

                                drvPed1.Delete();
                            }
                        }
                        if (drvPed2.Exists())
                        {
                            if (Vector3.Distance(drvPed2.Position, Game.LocalPlayer.Character.Position) > 2000f)
                            {
                                if (!Functions.IsPedArrested(drvPed2))
                                {
                                    Game.DisplayNotification("A street racer has ~r~escaped");
                                }

                                drvPed2.Delete();
                            }
                        }


                    }
                    base.End();
                }
                catch (Exception e)
                {
                    End();
                }
            
            });

        }

        protected override void End()
        {
            if (drvPed1.Exists()) { drvPed1.Delete(); }

            if (drvPed2.Exists()) { drvPed2.Delete(); }
            if (vehRacer1.Exists()) { vehRacer1.Delete(); }
            if (vehRacer2.Exists()) { vehRacer2.Delete(); }
            if (drvBlip1.Exists()) { drvBlip1.Delete(); }
            if (drvBlip2.Exists()) { drvBlip2.Delete(); }
            base.End();
        }

    }
}
    

