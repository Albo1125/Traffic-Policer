using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using Traffic_Policer.Extensions;
using LSPD_First_Response.Mod.API;

namespace Traffic_Policer.Impairment_Tests
{
    internal enum DrugsLevels { POSITIVE, NEGATIVE };
    internal static class DrugTestKit
    {

        private static Dictionary<PoolHandle, DrugsLevels> pedCannabisLevels = new Dictionary<PoolHandle, DrugsLevels>();
        private static Dictionary<PoolHandle, DrugsLevels> pedCocaineLevels = new Dictionary<PoolHandle, DrugsLevels>();
        internal static void testNearestPedForDrugs()
        {
            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
            {
                TrafficPolicerHandler.PerformingImpairmentTest = true;
                GameFiber.StartNew(delegate
                {
                    Ped[] nearbypeds = Game.LocalPlayer.Character.GetNearbyPeds(1);
                    if (nearbypeds.Length != 0)
                    {
                        Ped nearestPed = nearbypeds[0];
                        if (nearestPed.Exists())
                        {
                            if (Vector3.Distance(nearestPed.Position, Game.LocalPlayer.Character.Position) < 2.5f && nearestPed.RelationshipGroup != "COP" && nearestPed.RelationshipGroup != "PLAYER" && nearestPed.IsHuman)
                            {
                                Game.LocalPlayer.Character.Inventory.GiveNewWeapon("WEAPON_UNARMED", 1, true);
                                addPedToDictionaries(nearestPed);
                                DrugsLevels CannabisLevel = pedCannabisLevels[nearestPed.Handle];
                                DrugsLevels CocaineLevel = pedCocaineLevels[nearestPed.Handle];
                                try
                                {
                                    if (!nearestPed.IsInAnyVehicle(false))
                                    {
                                        nearestPed.BlockPermanentEvents = true;
                                        nearestPed.IsPersistent = true;
                                    }
                                    Rage.Native.NativeFunction.Natives.SET_PED_STEALTH_MOVEMENT(Game.LocalPlayer.Character, 0, 0);
                                    Vector3 directionFromPedToNearest = (nearestPed.Position - Game.LocalPlayer.Character.Position);
                                    directionFromPedToNearest.Normalize();
                                    if (!nearestPed.IsInAnyVehicle(false))
                                    {
                                        nearestPed.Tasks.AchieveHeading(MathHelper.ConvertDirectionToHeading(directionFromPedToNearest) + 180f).WaitForCompletion(1500);
                                    }

                                    //Game.LocalPlayer.Character.Tasks.GoStraightToPosition(nearestPed.Position, MathHelper.ConvertDirectionToHeading(directionFromPedToNearest), 0.9f).WaitForCompletion(600);
                                    directionFromPedToNearest = (nearestPed.Position - Game.LocalPlayer.Character.Position);
                                    directionFromPedToNearest.Normalize();
                                    Game.LocalPlayer.Character.Tasks.AchieveHeading(MathHelper.ConvertDirectionToHeading(directionFromPedToNearest)).WaitForCompletion(1600);


                                    if (nearestPed.IsInAnyVehicle(false))
                                    {
                                        //Game.LocalPlayer.Character.Tasks.PlayAnimation("missfbi3_party_b", "walk_to_balcony_male2", 0.5f, AnimationFlags.None).WaitForCompletion(500);
                                        Game.LocalPlayer.Character.Tasks.PlayAnimation("amb@code_human_police_investigate@idle_b", "idle_e", 2f, 0);
                                        if (!nearestPed.CurrentVehicle.IsBike)
                                        {
                                            nearestPed.Tasks.PlayAnimation("amb@incar@male@smoking_low@idle_a", "idle_a", 2f, 0);
                                        }
                                        GameFiber.Sleep(2000);
                                    }
                                    else
                                    {
                                        nearestPed.Tasks.PlayAnimation("switch@michael@smoking", "michael_smoking_loop", 2f, AnimationFlags.SecondaryTask).WaitForCompletion(8000);
                                        Game.LocalPlayer.Character.Tasks.Clear();
                                    }
                                }
                                catch (Exception e) { }
                                finally
                                {
                                    //GameFiber.Sleep(1800);
                                    //Game.LocalPlayer.Character.Tasks.ClearImmediately();

                                    uint noti = Game.DisplayNotification("Waiting for ~b~drugalyzer~s~ result...");
                                    if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                                    {
                                        API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "Drugalyzer tests conducted");
                                    }

                                    GameFiber.Sleep(4000);
                                    Game.LocalPlayer.Character.Tasks.Clear();
                                    Game.RemoveNotification(noti);
                                    Game.DisplayNotification("~b~Cannabis: " + CannabisLevel.ToColouredString() + "~n~~b~Cocaine: " + CocaineLevel.ToColouredString());
                                    TrafficPolicerHandler.PerformingImpairmentTest = false;
                                    if (nearestPed.Exists())
                                    {
                                        if (!nearestPed.IsInAnyVehicle(false))
                                        {
                                            if (nearestPed.LastVehicle.Exists())
                                            {
                                                if (nearestPed.DistanceTo(nearestPed.LastVehicle) < 20f)
                                                {
                                                    if (DoesPedHaveDrugsInSystem(nearestPed) && !TrafficPolicerHandler.PedsToChargeWithDrugDriving.Contains(nearestPed))
                                                    {
                                                        TrafficPolicerHandler.PedsToChargeWithDrugDriving.Add(nearestPed);
                                                        if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                                                        {
                                                            API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "People caught driving over drugs limit");
                                                        }
                                                    }
                                                }
                                            }

                                            nearestPed.Tasks.StandStill(7000).WaitForCompletion(7000);
                                            if (nearestPed.Exists())
                                            {
                                                if (!Functions.IsPedGettingArrested(nearestPed) && !Functions.IsPedArrested(nearestPed) && !Functions.IsPedStoppedByPlayer(nearestPed))
                                                {
                                                    nearestPed.Dismiss();
                                                }
                                            }
                                        }
                                        else if (nearestPed.CurrentVehicle.Driver == nearestPed)
                                        {
                                            if (DoesPedHaveDrugsInSystem(nearestPed) && !TrafficPolicerHandler.PedsToChargeWithDrugDriving.Contains(nearestPed))
                                            {
                                                TrafficPolicerHandler.PedsToChargeWithDrugDriving.Add(nearestPed);
                                                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                                                {
                                                    API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "People caught driving over drugs limit");
                                                }
                                            }
                                        }

                                    }
                                }
                            }
                        }









                    }
                    TrafficPolicerHandler.PerformingImpairmentTest = false;
                });
            }
        }
        public static bool DoesPedHaveDrugsInSystem(Ped ped)
        {
            addPedToDictionaries(ped);
            return (pedCocaineLevels[ped.Handle] == DrugsLevels.POSITIVE || pedCannabisLevels[ped.Handle] == DrugsLevels.POSITIVE);
        }

        private static void addPedToDictionaries(Ped _ped)
        {
            if (!pedCannabisLevels.ContainsKey(_ped.Handle))
            {
                if (TrafficPolicerHandler.rnd.Next(8) == 0)
                {
                    pedCannabisLevels.Add(_ped.Handle, DrugsLevels.POSITIVE);
                }
                else
                {
                    pedCannabisLevels.Add(_ped.Handle, DrugsLevels.NEGATIVE);
                }
            }
            if (!pedCocaineLevels.ContainsKey(_ped.Handle))
            {
                if (TrafficPolicerHandler.rnd.Next(8) == 0)
                {
                    pedCocaineLevels.Add(_ped.Handle, DrugsLevels.POSITIVE);
                }
                else
                {
                    pedCocaineLevels.Add(_ped.Handle, DrugsLevels.NEGATIVE);
                }

            }
        }

        public static void SetPedDrugsLevels(Ped ped, DrugsLevels cannabisLevel, DrugsLevels cocaineLevel)
        {
            if (ped.Exists() && ped.IsValid())
            {
                Game.LogTrivial("Setting drug levels");
                if (!pedCannabisLevels.ContainsKey(ped.Handle))
                {
                    pedCannabisLevels.Add(ped.Handle, cannabisLevel);
                }
                else
                {
                    pedCannabisLevels[ped.Handle] = cannabisLevel;
                }
                if (!pedCocaineLevels.ContainsKey(ped.Handle))
                {
                    pedCocaineLevels.Add(ped.Handle, cocaineLevel);
                }
                else
                {
                    pedCocaineLevels[ped.Handle] = cocaineLevel;
                }


            }

        }
        public static void SetPedDrugsLevels(Ped ped, bool Cannabis, bool Cocaine)
        {
            DrugsLevels Cannabislevel;
            DrugsLevels Cocainelevel;
            if (Cannabis)
            {
                Cannabislevel = DrugsLevels.POSITIVE;
            }
            else
            {
                Cannabislevel = DrugsLevels.NEGATIVE;
            }
            if (Cocaine)
            {
                Cocainelevel = DrugsLevels.POSITIVE;
            }
            else
            {
                Cocainelevel = DrugsLevels.NEGATIVE;
            }
            SetPedDrugsLevels(ped, Cannabislevel, Cocainelevel);
        }
    }
}
