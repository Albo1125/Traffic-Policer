using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using Traffic_Policer.Extensions;
using Albo1125.Common.CommonLibrary;

namespace Traffic_Policer.Ambientevents
{
    internal class BrokenDownVehicle : AmbientEvent
    {
        private TupleList<Vector3, float> ValidTrafficStopSpawnPointsWithHeadings = new TupleList<Vector3, float>();
        private Tuple<Vector3, float> ChosenSpawnData;
        private Vector3 SpawnPoint;
        private float SpawnHeading;
        
        
        
        
        
        public static bool BrokenDownVehicleRunning = false;
        private string[] vehiclesToSelectFrom = new string[] {"DUKES", "BALLER", "BALLER2", "BISON", "BISON2", "BJXL", "CAVALCADE", "CHEETAH", "COGCABRIO", "ASEA", "ADDER", "FELON", "FELON2", "ZENTORNO",
        "WARRENER", "RAPIDGT", "INTRUDER", "FELTZER2", "FQ2", "RANCHERXL", "REBEL", "SCHWARZER", "COQUETTE", "CARBONIZZARE", "EMPEROR", "SULTAN", "EXEMPLAR", "MASSACRO",
        "DOMINATOR", "ASTEROPE", "PRAIRIE", "NINEF", "WASHINGTON", "CHINO", "CASCO", "INFERNUS", "ZTYPE", "DILETTANTE", "VIRGO", "F620", "PRIMO", "SULTAN", "EXEMPLAR", "F620", "FELON2", "FELON", "SENTINEL",
            "WINDSOR", "DOMINATOR", "DUKES", "GAUNTLET", "VIRGO", "ADDER", "BUFFALO", "ZENTORNO", "MASSACRO" };

        public BrokenDownVehicle(bool createBlip, bool displayMessage)
        {
            foreach (Tuple<Vector3, float> tuple in CommonVariables.TrafficStopSpawnPointsWithHeadings)
            {
                if ((Vector3.Distance(tuple.Item1, Game.LocalPlayer.Character.Position) < 300f) && (Vector3.Distance(tuple.Item1, Game.LocalPlayer.Character.Position) > 140f))
                {
                    if (Rage.Native.NativeFunction.Natives.CALCULATE_TRAVEL_DISTANCE_BETWEEN_POINTS(tuple.Item1.X, tuple.Item1.Y, tuple.Item1.Z, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z) < 500f)
                    {
                        ValidTrafficStopSpawnPointsWithHeadings.Add(tuple);
                    }
                }
            }
            if (ValidTrafficStopSpawnPointsWithHeadings.Count > 0)
            {
                ChosenSpawnData = ValidTrafficStopSpawnPointsWithHeadings[TrafficPolicerHandler.rnd.Next(ValidTrafficStopSpawnPointsWithHeadings.Count)];
                SpawnPoint = ChosenSpawnData.Item1;
                SpawnHeading = ChosenSpawnData.Item2;
                car = new Vehicle(vehiclesToSelectFrom[TrafficPolicerHandler.rnd.Next(vehiclesToSelectFrom.Length)], SpawnPoint, SpawnHeading);
                car.MakePersistent();
                driver = car.CreateRandomDriver();
                car.RandomiseLicencePlate();
                driver.MakeMissionPed();
                car.EngineHealth = 0;
                car.IsDriveable = false;
                car.Doors[4].Open(true);
                
                if (displayMessage) { Game.DisplayNotification("Creating Broken Down Vehicle Event."); }
                if (createBlip)
                {
                    driverBlip = driver.AttachBlip();
                    driverBlip.Color = System.Drawing.Color.Beige;
                    driverBlip.Scale = 0.7f;
                }
                MainLogic();
            }
        }

        protected override void MainLogic()
        {
            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {
                try
                {
                    BrokenDownVehicleRunning = true;
                    while (eventRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) < 65f)
                        {
                            break;
                        }
                        else if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 350f)
                        {
                            End();
                        }
                    }
                    if (eventRunning)
                    {
                        Game.DisplaySubtitle("~b~Officer! Officer! ~s~Over here! My vehicle has broken down!", 6000);
                        if (driverBlip.Exists()) { driverBlip.Delete(); }
                        driverBlip = driver.AttachBlip();
                        driverBlip.Flash(400, 4000);
                    }
                    while (eventRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) < 6f)
                        {
                            driver.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(5000);
                            break;
                        }
                        else if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 150f)
                        {
                            End();
                        }
                    }
                    if (eventRunning)
                    {
                        if (driverBlip.Exists()) { driverBlip.Delete(); }
                        Game.DisplaySubtitle("~b~Officer~s~, I cannot start my vehicle any more. Please help me fix it.", 5000);
                        GameFiber.Wait(4000);
                        Game.DisplayHelp("Attempt to repair the vehicle with ~b~" + TrafficPolicerHandler.kc.ConvertToString(TrafficPolicerHandler.RepairVehicleKey) + "~s~ or tow it away.");
                    }
                    while (eventRunning)
                    {
                        GameFiber.Yield();
                        if (Game.IsKeyDown(TrafficPolicerHandler.RepairVehicleKey))
                        {
                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.GetOffsetPosition(Vector3.RelativeFront * (car.Length) * 0.65f)) < 3f)
                            {
                                Game.LocalPlayer.Character.Tasks.GoStraightToPosition(car.FrontPosition, 1.3f, car.Heading + 180f, 1f, 3000).WaitForCompletion();
                                Rage.Native.NativeFunction.Natives.SET_VEHICLE_DOOR_OPEN(car, 4, false, false);
                                Game.LocalPlayer.Character.Tasks.PlayAnimation("missexile3", "ex03_dingy_search_case_base_michael", 0.8f, AnimationFlags.None).WaitForCompletion();
                                Rage.Native.NativeFunction.Natives.SET_VEHICLE_DOOR_SHUT(car, 4, false);
                                int roll = TrafficPolicerHandler.rnd.Next(5);
                                if (roll < 2)
                                {
                                    car.IsDriveable = true;
                                    car.EngineHealth = 100f;
                                    Game.DisplaySubtitle("~b~You: ~s~You should be able to drive your vehicle again!", 5000);
                                    GameFiber.Wait(5000);
                                    Game.DisplaySubtitle("~b~You: ~s~Just make sure to visit a garage as soon as possible!", 5000);
                                    GameFiber.Wait(5000);
                                    Game.DisplaySubtitle("~b~Thank you, officer! Take care!", 4000);
                                    driver.Tasks.FollowNavigationMeshToPosition(car.GetOffsetPosition(Vector3.RelativeLeft * 2f), car.Heading, 1.4f).WaitForCompletion(5000);
                                    driver.Tasks.EnterVehicle(car, 6000, -1).WaitForCompletion();
                                    driver.Tasks.CruiseWithVehicle(18f);
                                    GameFiber.Wait(5000);
                                    End();
                                }
                                else
                                {
                                    Game.DisplaySubtitle("~b~You: ~s~Your vehicle seems to be completely dead.", 5000);
                                    GameFiber.Wait(5000);
                                    Game.DisplaySubtitle("~b~You: ~s~I'm calling a tow truck to take it away.", 5000);
                                    GameFiber.Wait(2000);
                                    break;
                                }
                            }
                            else
                            {
                                Game.DisplayNotification("Move to the front of the vehicle to attempt to repair it.");
                            }

                        }
                        if (car.FindTowTruck().Exists() || Vector3.Distance(SpawnPoint, car.Position) > 15f)
                        {
                            break;
                        }
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 150f)
                        {
                            End();
                        }
                    }
                    while (eventRunning)
                    {
                        GameFiber.Yield();
                        Game.DisplayHelp("Call for a tow truck to take the broken down vehicle away.");
                        if (car.FindTowTruck().Exists() || Vector3.Distance(SpawnPoint, car.Position) > 15f)
                        {
                            Game.HideHelp();
                            Game.DisplaySubtitle("~b~You: ~s~You can pick up your vehicle later!", 5000);
                            GameFiber.Wait(5000);
                            int roll = TrafficPolicerHandler.rnd.Next(5);
                            if (roll < 3)
                            {
                                
                                Game.DisplaySubtitle("~b~Thank you, officer! I guess I'll have to walk home now.", 5000);
                                End();

                            }
                            else
                            {
                                Game.DisplaySubtitle("~b~Are you for real? You've taken my vehicle?", 5000);
                                GameFiber.Wait(5000);
                                Game.DisplaySubtitle("~r~You can take my vehicle, but I'll take your life!", 5000);
                                break;

                            }
                        }
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 150f)
                        {
                            End();
                        }
                    }
                    if (eventRunning)
                    {
                        driver.Inventory.GiveNewWeapon("WEAPON_ASSAULTSMG", -1, true);
                    }
                    while (eventRunning)
                    {
                        GameFiber.Yield();
                        if (driver.Exists() && !Functions.IsPedGettingArrested(driver) && driver.IsAlive)
                        {
                            driver.Tasks.FightAgainst(Game.LocalPlayer.Character).WaitForCompletion(1000);
                        }
                        else
                        {
                            break;
                        }
                    }

                }
                catch (System.Threading.ThreadAbortException e) { throw; }
                catch (Exception e) { Game.LogTrivial(e.ToString()); Game.DisplayNotification("Broken Down Vehicle encountered an error. Please send me your log file."); }
                finally { End(); }
            });
        }

        protected override void End()
        {
        
            if (car.Exists()) { car.Dismiss(); }
            if (driver.Exists()) { driver.Dismiss(); }
            BrokenDownVehicleRunning = false;
            base.End();

        }
   
    }
}

