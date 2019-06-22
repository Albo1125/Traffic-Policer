using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using Traffic_Policer.Extensions;
using System.Windows.Forms;
using LSPD_First_Response.Mod.API;

namespace Traffic_Policer.Impairment_Tests
{
    public enum AlcoholLevels { Zero, UnderLimit, JustUnderLimit, Limit, JustOverLimit, OverLimit, OverDoubleLimit, OverTripleLimit}

    internal static class Breathalyzer
    {
        private static Dictionary<PoolHandle, AlcoholLevels> PedAlcoholLevels = new Dictionary<PoolHandle, AlcoholLevels>();
        private static Dictionary<PoolHandle, float> PedAlcoholLevelReadings = new Dictionary<PoolHandle, float>();

        public static float AlcoholLimit = 35;
        public static string AlcoholLimitUnit = "ug/100ml";

        public static Keys BreathalyzerKey = Keys.O;
        public static Keys BreathalyzerModifierKey = Keys.LShiftKey;
        

        internal static void TestNearestPedForAlcohol()
        {
            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false) && !TrafficPolicerHandler.PerformingImpairmentTest)
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

                                AddPedToDictionaries(nearestPed);
                                AlcoholLevels PedAlcoholLevel = PedAlcoholLevels[nearestPed.Handle];
                                float Reading = DetermineAlcoholReading(nearestPed, PedAlcoholLevel);
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
                                        if(!nearestPed.CurrentVehicle.IsBike)
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

                                    uint noti = Game.DisplayNotification("Waiting for ~b~breathalyzer~s~ result...");
                                    if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                                    {
                                        API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "Breathalyzer tests conducted");
                                    }

                                    GameFiber.Sleep(3000);
                                    Game.LocalPlayer.Character.Tasks.Clear();
                                    Game.RemoveNotification(noti);
                                    if (Reading == -1)
                                    {
                                        Game.DisplayNotification("The person ~r~failed to provide ~s~ a valid breath sample.");
                                    }
                                    else
                                    {
                                        Game.DisplayNotification("~b~Alcohol Reading: " + DetermineColourCode(PedAlcoholLevel) + Reading.ToString("n" + CountDigitsAfterDecimal(AlcoholLimit)) + AlcoholLimitUnit + ".~n~~b~Limit: " + AlcoholLimit.ToString() + "" + AlcoholLimitUnit + ".");
                                    }
                                    TrafficPolicerHandler.PerformingImpairmentTest = false;
                                    if (nearestPed.Exists())
                                    {
                                        if (!nearestPed.IsInAnyVehicle(false))
                                        {
                                            if (nearestPed.LastVehicle.Exists())
                                            {
                                                if (nearestPed.DistanceTo(nearestPed.LastVehicle) < 20f)
                                                {
                                                    if (IsPedOverTheLimit(nearestPed) && !TrafficPolicerHandler.PedsToChargeWithDrinkDriving.Contains(nearestPed))
                                                    {
                                                        TrafficPolicerHandler.PedsToChargeWithDrinkDriving.Add(nearestPed);
                                                        if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                                                        {
                                                            API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "People caught driving over alcohol limit");
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
                                            if (IsPedOverTheLimit(nearestPed) && !TrafficPolicerHandler.PedsToChargeWithDrinkDriving.Contains(nearestPed))
                                            {
                                                TrafficPolicerHandler.PedsToChargeWithDrinkDriving.Add(nearestPed);
                                                if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                                                {
                                                    API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "People caught driving over alcohol limit");
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

        private static string DetermineColourCode(AlcoholLevels AlcoholLevel)
        {
            string colourcode;
            if (AlcoholLevel == AlcoholLevels.Zero)
            {
                colourcode = "~g~";
            }
            else if (AlcoholLevel == AlcoholLevels.UnderLimit || AlcoholLevel == AlcoholLevels.JustUnderLimit)
            {
                colourcode = "~y~";
            }
            else
            {
                colourcode = "~r~";
            }
            return colourcode;
        }

        public static bool IsPedOverTheLimit(Ped ped)
        {

            AddPedToDictionaries(ped);
            AlcoholLevels PedAlcoholLevel = PedAlcoholLevels[ped.Handle];
            return !(PedAlcoholLevel == AlcoholLevels.Zero || PedAlcoholLevel == AlcoholLevels.UnderLimit || PedAlcoholLevel == AlcoholLevels.JustUnderLimit);
        }

        private static float DetermineAlcoholReading(Ped ped, AlcoholLevels AlcoholLevel)
        {
            if (PedAlcoholLevelReadings.ContainsKey(ped.Handle))
            {
                return PedAlcoholLevelReadings[ped.Handle];
            }
            float reading = 0;
            if (TrafficPolicerHandler.rnd.Next(1, 101) <= TrafficPolicerHandler.FailToProvideChance)
            {
                reading = -1;
            }
            else
            {
                if (AlcoholLevel == AlcoholLevels.UnderLimit)
                {
                    reading = AlcoholLimit * (float)Decimal.Divide(TrafficPolicerHandler.rnd.Next(2, 6), 10);
                }
                else if (AlcoholLevel == AlcoholLevels.JustUnderLimit)
                {
                    reading = AlcoholLimit * (float)Decimal.Divide(TrafficPolicerHandler.rnd.Next(6, 10), 10);
                }
                else if (AlcoholLevel == AlcoholLevels.Limit)
                {
                    reading = AlcoholLimit;
                }
                else if (AlcoholLevel == AlcoholLevels.JustOverLimit)
                {
                    reading = AlcoholLimit * (float)Decimal.Divide(TrafficPolicerHandler.rnd.Next(11, 15), 10);
                }
                else if (AlcoholLevel == AlcoholLevels.OverLimit)
                {
                    reading = AlcoholLimit * (float)Decimal.Divide(TrafficPolicerHandler.rnd.Next(15, 20), 10);
                }
                else if (AlcoholLevel == AlcoholLevels.OverDoubleLimit)
                {
                    reading = AlcoholLimit * (float)Decimal.Divide(TrafficPolicerHandler.rnd.Next(20, 30), 10);
                }
                else if (AlcoholLevel == AlcoholLevels.OverTripleLimit)
                {
                    reading = AlcoholLimit * (float)Decimal.Divide(TrafficPolicerHandler.rnd.Next(30, 40), 10);
                }
            }

            //Game.LogTrivial("Reading: " + reading.ToString());
            //Game.LogTrivial("Level: " + AlcoholLevel.ToString());
            //reading = (float)Math.Round(reading, CountDigitsAfterDecimal(AlcoholLimit));
            PedAlcoholLevelReadings.Add(ped.Handle, reading);
            
            return reading;
        }

        public static AlcoholLevels GetRandomOverTheLimitAlcoholLevel()
        {
            AlcoholLevels AlcoholLevel = AlcoholLevels.Limit;
            int roll = TrafficPolicerHandler.rnd.Next(9);
            if (roll < 3)
            {
                AlcoholLevel = AlcoholLevels.JustOverLimit;
            }
            else if (roll < 7)
            {
                AlcoholLevel = AlcoholLevels.OverLimit;
            }
            else if (roll == 7)
            {
                AlcoholLevel = AlcoholLevels.OverDoubleLimit;
            }
            else if (roll == 8)
            {
                AlcoholLevel = AlcoholLevels.OverTripleLimit;
            }
            return AlcoholLevel;
        }

        public static AlcoholLevels GetRandomUnderTheLimitAlcoholLevel()
        {
            AlcoholLevels AlcoholLevel = AlcoholLevels.Zero;
            int roll = TrafficPolicerHandler.rnd.Next(8);
            if (roll < 6)
            {
                AlcoholLevel = AlcoholLevels.Zero;
            }
            else if (roll == 6)
            {
                AlcoholLevel = AlcoholLevels.UnderLimit;
            }
            else if (roll == 7)
            {
                AlcoholLevel = AlcoholLevels.JustUnderLimit;
            }
            return AlcoholLevel;
        }

        private static void AddPedToDictionaries(Ped ped)
        {
            if (!PedAlcoholLevels.ContainsKey(ped.Handle))
            {
                AlcoholLevels AlcoholLevel = AlcoholLevels.Zero;
                if (TrafficPolicerHandler.rnd.Next(8) == 0)
                {
                    AlcoholLevel = GetRandomOverTheLimitAlcoholLevel();
                }
                else
                {
                    AlcoholLevel = GetRandomUnderTheLimitAlcoholLevel();
                }
                PedAlcoholLevels.Add(ped.Handle, AlcoholLevel);
            }
        }
        public static void SetPedAlcoholLevels(Ped ped, AlcoholLevels AlcoholLevel)
        {
            if (ped.Exists() && ped.IsValid())
            {
                Game.LogTrivial("Setting alcohol levels");
                if (!PedAlcoholLevels.ContainsKey(ped.Handle))
                {
                    PedAlcoholLevels.Add(ped.Handle, AlcoholLevel);
                }
                else
                {
                    PedAlcoholLevels[ped.Handle] = AlcoholLevel;
                    if (PedAlcoholLevelReadings.ContainsKey(ped.Handle))
                    {
                        PedAlcoholLevelReadings.Remove(ped.Handle);
                    }
                }
            }
        }
        private static int CountDigitsAfterDecimal(float value)
        {
            bool start = false;
            int count = 0;
            foreach (var s in value.ToString())
            {
                if (s == '.' || s == ',')
                {
                    start = true;
                }
                else if (start)
                {
                    count++;
                }
            }
            //Game.LogTrivial("Decimal count: " + count.ToString());
            return count + 1;
        }
    }
    


}
