using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using Rage.Native;
using Albo1125.Common.CommonLibrary;
using System.Windows.Forms;

namespace Traffic_Policer
{
    internal class RoadSigns
    {
        public static List<Rage.Object> roadSignsDropped = new List<Rage.Object>();
        public static TupleList<Rage.Object, Rage.Object, Ped> RoadSignsWithInvisWallsAndPeds = new TupleList<Rage.Object, Rage.Object, Ped>();
        public static List<UInt32> speedZones = new List<UInt32>();

        public static Keys placeSignShortcutKey = Keys.J;
        public static Keys placeSignShortcutModifierKey = Keys.LControlKey;
        public static Keys removeAllSignsKey = Keys.J;
        public static Keys removeAllSignsModifierKey = Keys.None;

        private static string[] barriersToChooseFrom = new string[] { "prop_barrier_work05" , "PROP_MP_BARRIER_01", "PROP_MP_BARRIER_01B",  "PROP_MP_BARRIER_02", "PROP_MP_BARRIER_02B", "PROP_CONSIGN_02A",  "PROP_MP_ARROW_BARRIER_01", "PROP_MP_ARROW_BARRIER_01" };
        private static string[] conesToChooseFrom = new string[] { "PROP_ROADCONE01A", "PROP_ROADCONE01B", "PROP_ROADCONE01C", "PROP_ROADCONE02A", "PROP_ROADCONE02B", "PROP_ROADCONE02C" };
        public static void dropSign(string selectedSign, bool swapHeading, Vector3 Location, float HeadingModifier)
        {
            GameFiber.StartNew(delegate
            {
                try
                {
                    //string selectedCone = barriersToChooseFrom[EntryPoint.rnd.Next(barriersToChooseFrom.Length)];
                    //string selectedCone = "PROP_MP_ARROW_BARRIER_01";
                    if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                    {
                        API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "Road signs placed");
                    }
                    Rage.Object trafficCone = new Rage.Object(selectedSign, Location);
                    trafficCone.IsPersistent = true;
                    trafficCone.IsInvincible = true;
                    trafficCone.Rotation = RotationToPlaceAt;
                    if (swapHeading)
                    {
                        trafficCone.Heading = Game.LocalPlayer.Character.Heading + 180f;
                    }
                    trafficCone.Heading += HeadingModifier;

                    trafficCone.IsPositionFrozen = false;
                    if (TrafficSignPreview.Exists())
                    {
                        TrafficSignPreview.SetPositionZ(TrafficSignPreview.Position.Z + 3f);
                    }
                    int waitCount = 0;
                    while (trafficCone.HeightAboveGround > 0.01f)
                    {
                        trafficCone.SetPositionZ(trafficCone.Position.Z - (trafficCone.HeightAboveGround * 0.75f));
                        waitCount++;
                        if (waitCount >= 1000)
                        {
                            break;
                        }
                        
                    }

                    if (trafficCone.Exists())
                    {                        
                        trafficCone.IsPositionFrozen = true;
                        roadSignsDropped.Add(trafficCone);
                        UInt32 handle = World.AddSpeedZone(trafficCone.Position, 5f, 5f);
                        speedZones.Add(handle);
                        Rage.Object invWall = new Rage.Object("p_ice_box_01_s", trafficCone.Position);
                        invWall.IsPersistent = true;
                        Ped invPed = new Ped(trafficCone.Position);
                        invPed.MakeMissionPed();
                        invPed.IsVisible = false;
                        invPed.IsPositionFrozen = true;
                        
                        invWall.Heading = Game.LocalPlayer.Character.Heading;
                        invWall.IsVisible = false;
                        RoadSignsWithInvisWallsAndPeds.Add(trafficCone, invWall, invPed);
                        
                    }
                   
                }
                catch (Exception e)
                {
                    Game.LogTrivial(e.ToString());
                }
            });
        }
        public static void RemoveNearestSign()
        {
            if (RoadSignsWithInvisWallsAndPeds.Count > 0)
            {
                Tuple<Rage.Object, Rage.Object, Ped> coneset = (from x in RoadSignsWithInvisWallsAndPeds orderby x.Item1.DistanceTo(Game.LocalPlayer.Character.Position) select x).FirstOrDefault();
                if (coneset.Item1.Exists())
                {
                    coneset.Item1.Delete();
                }
                if (coneset.Item2.Exists())
                {
                    coneset.Item2.Delete();
                }
                if (coneset.Item3.Exists())
                {
                    coneset.Item3.Delete();
                }
                World.RemoveSpeedZone(speedZones[RoadSignsWithInvisWallsAndPeds.IndexOf(coneset)]);
                speedZones.RemoveAt(RoadSignsWithInvisWallsAndPeds.IndexOf(coneset));
                RoadSignsWithInvisWallsAndPeds.Remove(coneset);

            }
        }
        public static void removeAllSigns()
        {
            try
            {
                //for (int i = roadSignsDropped.Count - 1; i >= 0; i--)
                //{
                //    // some code
                //    // safePendingList.RemoveAt(i);

                //    if (roadSignsDropped[i].Exists())
                //    {
                //        roadSignsDropped[i].Delete();
                //    }
                //    roadSignsDropped.RemoveAt(i);


                //}

                foreach (Tuple<Rage.Object, Rage.Object, Ped> coneset in RoadSignsWithInvisWallsAndPeds.ToArray())
                {
                    if (coneset.Item1.Exists())
                    {
                        coneset.Item1.Delete();
                    }
                    if (coneset.Item2.Exists())
                    {
                        coneset.Item2.Delete();
                    }
                    if (coneset.Item3.Exists())
                    {
                        coneset.Item3.Delete();
                    }
                    RoadSignsWithInvisWallsAndPeds.Remove(coneset);
                }
                for (int i = speedZones.Count - 1; i >= 0; i--)
                {
                    // some code
                    // safePendingList.RemoveAt(i);

                    World.RemoveSpeedZone(speedZones[i]);
                    speedZones.RemoveAt(i);


                }
                Game.LogTrivial("All signs removed");
            }
            catch (Exception e)
            {
                
            }
        }
        public static void removeLastSign()
        {
            try
            {
                //if (roadSignsDropped.Count > 0)
                //{
                //    if (roadSignsDropped[roadSignsDropped.Count - 1].Exists())
                //    {
                //        roadSignsDropped[roadSignsDropped.Count - 1].Delete();
                //    }
                //    roadSignsDropped.RemoveAt(roadSignsDropped.Count - 1);
                //}
                if (RoadSignsWithInvisWallsAndPeds.Count > 0)
                {
                    Tuple<Rage.Object, Rage.Object, Ped> coneset = RoadSignsWithInvisWallsAndPeds[RoadSignsWithInvisWallsAndPeds.Count - 1];
                    if (coneset.Item1.Exists())
                    {
                        coneset.Item1.Delete();
                    }
                    if (coneset.Item2.Exists())
                    {
                        coneset.Item2.Delete();
                    }
                    if (coneset.Item3.Exists())
                    {
                        coneset.Item3.Delete();
                    }
                    RoadSignsWithInvisWallsAndPeds.Remove(coneset);
                }

                if (speedZones.Count > 0)
                {
                    World.RemoveSpeedZone(speedZones[speedZones.Count - 1]);
                    speedZones.RemoveAt(speedZones.Count - 1);
                }
                Game.LogTrivial("Last sign removed");
            }
            catch (Exception e)
            {
                
            }
        }

        private static Vector3 PositionToPlaceAt;
        private static Rotator RotationToPlaceAt;
        private static Vector3 DetermineSignSpawn(string Direction, float Distance)
        {
            
            if (Direction == "1")
            {
                return PositionToPlaceAt + (Vector3.RelativeFront * Distance);
            }
            else if (Direction == "2")
            {
                return PositionToPlaceAt + (Vector3.RelativeBack * Distance);
            }
            else if (Direction == "3")
            {
                return PositionToPlaceAt + (Vector3.RelativeLeft * Distance);
            }
            else if (Direction == "4")
            {
                return PositionToPlaceAt + (Vector3.RelativeRight * Distance);
            }
            else
            {
                return Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront * Distance);
            }
        }

        public static void RoadSignsMainLogic()
        {
            GameFiber.StartNew(delegate
            {
                createRoadSignsMenu();
                try
                {
                    while (true)
                    {
                        GameFiber.Yield();
                        if (PlaceSignMenu.Visible && EnablePreviewItem.Checked)
                        {
                            if (TrafficSignPreview.Exists())
                            {
                                if (TrafficSignPreview.DistanceTo2D(DetermineSignSpawn(SpawnDirectionListItem.Collection[SpawnDirectionListItem.Index].Value.ToString(), float.Parse(SpawnMultiplierListItem.Collection[SpawnMultiplierListItem.Index].Value.ToString()))) > 0.4f)
                                {

                                    TrafficSignPreview.Position = DetermineSignSpawn(SpawnDirectionListItem.Collection[SpawnDirectionListItem.Index].Value.ToString(), float.Parse(SpawnMultiplierListItem.Collection[SpawnMultiplierListItem.Index].Value.ToString()));
                                    TrafficSignPreview.SetPositionZ(TrafficSignPreview.Position.Z + 3f);
                                }
                                TrafficSignPreview.Rotation = RotationToPlaceAt;
                                if (barriersList.Collection[barriersList.Index].Value.ToString() == "Stripes Left")
                                {
                                    //TrafficSignPreview.Heading = Game.LocalPlayer.Character.Heading + 180f;
                                    TrafficSignPreview.Heading += 180f;
                                }
                                TrafficSignPreview.Heading += float.Parse(HeadingItem.Collection[HeadingItem.Index].Value.ToString());
                                int waitCount = 0;
                                
                                while (TrafficSignPreview.HeightAboveGround > 0.01f)
                                {
                                    GameFiber.Yield();
                                    TrafficSignPreview.SetPositionZ(TrafficSignPreview.Position.Z - (TrafficSignPreview.HeightAboveGround * 0.75f));
                                    //Game.LogTrivial("Heighaboveground: " + TrafficSignPreview.HeightAboveGround);
                                    waitCount++;
                                    if (waitCount >= 1000)
                                    {
                                        break;
                                    }

                                }
                                TrafficSignPreview.IsPositionFrozen = true;
                                TrafficSignPreview.Opacity = 0.7f;
                                TrafficSignPreview.NeedsCollision = false;
                                NativeFunction.Natives.SET_ENTITY_COLLISION(TrafficSignPreview, false, false);
                            }
                            if (SignTypeToPlace == SignTypes.Barrier && !TrafficSignPreview.Exists())
                            {
                                if (TrafficSignPreview.Exists()) { TrafficSignPreview.Delete(); }
                                TrafficSignPreview = new Rage.Object(barriersToChooseFrom[barriersList.Index], DetermineSignSpawn(SpawnDirectionListItem.Collection[SpawnDirectionListItem.Index].Value.ToString(), float.Parse(SpawnMultiplierListItem.Collection[SpawnMultiplierListItem.Index].Value.ToString())));
                                TrafficSignPreview.Rotation = RotationToPlaceAt;
                                TrafficSignPreview.NeedsCollision = false;
                                
                            }
                            else if (SignTypeToPlace == SignTypes.Cone && !TrafficSignPreview.Exists())
                            {
                                if (TrafficSignPreview.Exists()) { TrafficSignPreview.Delete(); }
                                TrafficSignPreview = new Rage.Object(conesToChooseFrom[conesList.Index], DetermineSignSpawn(SpawnDirectionListItem.Collection[SpawnDirectionListItem.Index].Value.ToString(), float.Parse(SpawnMultiplierListItem.Collection[SpawnMultiplierListItem.Index].Value.ToString())));
                                TrafficSignPreview.Rotation = RotationToPlaceAt;
                                TrafficSignPreview.NeedsCollision = false;
                                
                            }
                            
                        }
                        else
                        {
                            if (TrafficSignPreview.Exists()) { TrafficSignPreview.Delete(); }
                            
                        }

                        if (!PlaceSignMenu.Visible)
                        {
                            if (!Game.LocalPlayer.Character.IsInAnyVehicle(false))
                            {
                                if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyCombinationDownComputerCheck(placeSignShortcutKey, placeSignShortcutModifierKey))
                                {
                                    RotationToPlaceAt = Game.LocalPlayer.Character.Rotation;
                                    dropSign(SignTypeToPlace == SignTypes.Barrier ? barriersToChooseFrom[barriersList.Index] : conesToChooseFrom[conesList.Index], false, Game.LocalPlayer.Character.GetOffsetPositionFront(2), 0);
                                    Game.LogTrivial("Shortcut Sign dropped");
                                    Rage.Native.NativeFunction.Natives.SET_PED_STEALTH_MOVEMENT(Game.LocalPlayer.Character, 0, 0);
                                }
                                else if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyCombinationDownComputerCheck(removeAllSignsKey, removeAllSignsModifierKey))
                                {
                                    removeAllSigns();
                                }
                            }
                        }
                    }
                }
                catch
                {
                    removeAllSigns();
                    if (TrafficSignPreview.Exists()) { TrafficSignPreview.Delete(); }
                }
            });
        }

        private static UIMenu roadManagementMenu;
        private static UIMenuListItem barriersList;
        private static UIMenuListItem conesList;
        private static UIMenuListItem SpawnDirectionListItem;
        private static UIMenuListItem SpawnMultiplierListItem;
        private static UIMenuItem removeLastDroppedSignItem;
        private static UIMenuItem RemoveNearestSignItem;
        private static UIMenuItem removeAllSignsItem;
        private static UIMenuCheckboxItem EnablePreviewItem;
        private static UIMenuListItem HeadingItem;

        private static UIMenu PlaceSignMenu;
        private static UIMenuItem PlaceSignItem;
        private static UIMenuCheckboxItem UpdateSignPositionItem;

        private static MenuPool _menuPool;
        private static void createRoadSignsMenu()
        {
            Game.FrameRender += Process;
            _menuPool = new MenuPool();

            roadManagementMenu = new UIMenu("Road Management", "~b~Select the sign to place");
            _menuPool.Add(roadManagementMenu);

            
            List<string> availableBarriers = new List<string>
            {
                "Police Do Not Cross",
                "Concrete 1",
                "Concrete 2",
                "Roadwork Stripes",
                "Plain Stripes",
                "Stopped Vehicles",
                "Stripes Right",
                "Stripes Left",
                
            };
            roadManagementMenu.AddItem(barriersList = new UIMenuListItem("Barriers", "", availableBarriers));

            List<string> availableCones = new List<string>
            {
                "Large Striped 1",
                "Large Striped 2",
                "Large not Striped",
                "Small Striped 1",
                "Small Striped 2",
                "Small not Striped"

            };
            roadManagementMenu.AddItem(conesList = new UIMenuListItem("Cones", "", availableCones));
            removeLastDroppedSignItem = new UIMenuItem("Remove Last Sign");
            roadManagementMenu.AddItem(removeLastDroppedSignItem);
            roadManagementMenu.AddItem(RemoveNearestSignItem = new UIMenuItem("Remove Nearest Sign"));
            removeAllSignsItem = new UIMenuItem("Remove All Signs");
            roadManagementMenu.AddItem(removeAllSignsItem);
            barriersList.OnListChanged += OnListChanged;
            conesList.OnListChanged += OnListChanged;
            roadManagementMenu.RefreshIndex();

            roadManagementMenu.OnItemSelect += OnItemSelect;
            roadManagementMenu.MouseControlsEnabled = false;
            roadManagementMenu.AllowCameraMovement = true;

            PlaceSignMenu = new UIMenu("Road Management", "~b~Placement options");
            PlaceSignMenu.AddItem(PlaceSignItem = new UIMenuItem("Place Sign"));

            PlaceSignMenu.AddItem(UpdateSignPositionItem = new UIMenuCheckboxItem("Update sign position", true, "If checked, contantly updates the sign's position offset to your character's current position."));
            List<string> availableSpawnDirections = new List<string>
            {
                "1",
                "2",
                "3",
                "4"
            };
            PlaceSignMenu.AddItem(SpawnDirectionListItem = new UIMenuListItem("Direction", "", availableSpawnDirections));

            List<string> availableSpawnMultipliers = new List<string>
            {
                "2","3","4","5","6","7","8","9","10"
            };
            PlaceSignMenu.AddItem(SpawnMultiplierListItem = new UIMenuListItem("Distance", "", availableSpawnMultipliers));
            List<string> AvailableHeadingModifiers = new List<string>
            {
                "0", "45", "90", "135", "180", "225", "270", "315",
            };
            PlaceSignMenu.AddItem(HeadingItem = new UIMenuListItem("Rotation in degrees", "", AvailableHeadingModifiers));
            PlaceSignMenu.AddItem(EnablePreviewItem = new UIMenuCheckboxItem("Enable Preview", true));
            PlaceSignMenu.RefreshIndex();

            PlaceSignMenu.OnItemSelect += OnItemSelect;
            PlaceSignMenu.MouseControlsEnabled = false;
            PlaceSignMenu.AllowCameraMovement = true;
            PlaceSignMenu.ParentMenu = roadManagementMenu;
            _menuPool.Add(PlaceSignMenu);



            //roadManagementMenu.ResetCursorOnOpen = false;


        }
        
        private enum SignTypes { Cone, Barrier};
        private static SignTypes SignTypeToPlace = SignTypes.Barrier;
        public static void OnItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (sender == roadManagementMenu)
            {

                if (selectedItem == barriersList)
                {
                    SignTypeToPlace = SignTypes.Barrier;
                    sender.Visible = false;
                    PlaceSignItem.Text = "Place " + barriersList.Collection[barriersList.Index].Value.ToString();
                    PlaceSignMenu.RefreshIndex();
                    PlaceSignMenu.Visible = true;
                    PositionToPlaceAt = Game.LocalPlayer.Character.Position;
                    RotationToPlaceAt = Game.LocalPlayer.Character.Rotation;
                }

                else if (selectedItem == conesList)
                {
                    SignTypeToPlace = SignTypes.Cone;
                    sender.Visible = false;
                    PlaceSignItem.Text = "Place " + conesList.Collection[conesList.Index].Value.ToString();
                    PlaceSignMenu.RefreshIndex();
                    PlaceSignMenu.Visible = true;
                    PositionToPlaceAt = Game.LocalPlayer.Character.Position;
                    RotationToPlaceAt = Game.LocalPlayer.Character.Rotation;

                }

                else if (selectedItem == removeLastDroppedSignItem)
                {
                    removeLastSign();
                }
                else if (selectedItem == RemoveNearestSignItem)
                {
                    RemoveNearestSign();
                }
                else if (selectedItem == removeAllSignsItem)
                {
                    removeAllSigns();
                }
            }
            else if (sender == PlaceSignMenu)
            {
                if (selectedItem == PlaceSignItem)
                {
                    
                    string direction = SpawnDirectionListItem.Collection[SpawnDirectionListItem.Index].Value.ToString();
                    float multiplier = float.Parse(SpawnMultiplierListItem.Collection[SpawnMultiplierListItem.Index].Value.ToString());
                    Vector3 spawn = DetermineSignSpawn(direction, multiplier);
                    float Heading = float.Parse(HeadingItem.Collection[HeadingItem.Index].Value.ToString());
                    if (SignTypeToPlace == SignTypes.Barrier)
                    {
                        string selectedsign = barriersToChooseFrom[barriersList.Index];
                        if (barriersList.Collection[barriersList.Index].Value.ToString() == "Stripes Left")
                        {

                            dropSign(selectedsign, true, spawn, Heading);
                        }
                        else
                        {
                            dropSign(selectedsign, false, spawn, Heading);
                        }
                        Game.LogTrivial("Barrier Placed");
                    }
                    else if (SignTypeToPlace == SignTypes.Cone)
                    {
                        string selectedsign = conesToChooseFrom[conesList.Index];
                        dropSign(selectedsign, false, spawn, Heading);
                        Game.LogTrivial("Cone Placed");
                    }
                }
                
            }
        }
        public static void OnListChanged(UIMenuItem sender, int index)
        {
            if (sender == barriersList)
            {
                //BarriersListSelected = false;

            }
            else if (sender == conesList)
            {
                //BarriersListSelected = true;
            }
        }
        private static Rage.Object TrafficSignPreview;
        //private static bool BarriersListSelected = false;
        public static void Process(object sender, GraphicsEventArgs e)
        {
            _menuPool.ProcessMenus();
            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(TrafficPolicerHandler.RoadManagementModifierKey) || TrafficPolicerHandler.RoadManagementModifierKey == System.Windows.Forms.Keys.None)
            {
                if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(TrafficPolicerHandler.roadManagementMenuKey))
                {
                    roadManagementMenu.Visible = !roadManagementMenu.Visible;
                    

                }
            }
            if (PlaceSignMenu.Visible)
            {
                if (UpdateSignPositionItem.Checked)
                {
                    PositionToPlaceAt = Game.LocalPlayer.Character.Position;
                    RotationToPlaceAt = Game.LocalPlayer.Character.Rotation;
                }
            }          
        }
    }
}
