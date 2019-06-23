using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Management;
using System.Net;
using System.Reflection;
using Rage.Native;

[assembly: Rage.Attributes.Plugin("Traffic Policer", Description = "INSTALL IN PLUGINS/LSPDFR. Enhances traffic policing in LSPDFR", Author = "Albo1125")]
namespace Traffic_Policer
{
    public class EntryPoint
    {
        public static void Main()
        {
            Game.DisplayNotification("You have installed Traffic Policer incorrectly and in the wrong folder: you must install it in Plugins/LSPDFR. It will then be automatically loaded when going on duty - you must NOT load it yourself via RAGEPluginHook. This is also explained in the Readme and Documentation. You will now be redirected to the installation tutorial.");
            GameFiber.Wait(5000);
            Process.Start("https://youtu.be/af434m72rIo");
            return;
        }
    }

    internal class TrafficPolicerHandler
    {


        public static List<GameFiber> AmbientEventGameFibersToAbort = new List<GameFiber>();

        public static List<Ped> PedsToChargeWithDrinkDriving = new List<Ped>();
        public static List<Ped> PedsToChargeWithDrugDriving = new List<Ped>();

        private static Random eventRnd = new Random();

        private static void eventCreator()
        {

            GameFiber.StartNew(delegate
            {
                while (true)
                {
                    GameFiber.Wait(100);                  

                    driversConsidered.RemoveAll(x => !x.Exists());
                    if (!Functions.IsCalloutRunning() && !Functions.IsPlayerPerformingPullover() && Functions.GetActivePursuit() == null && !Ambientevents.BrokenDownVehicle.BrokenDownVehicleRunning 
                    && NextEventStopwatch.ElapsedMilliseconds >= nextEventTimer)
                    {
                        Vehicle[] vehs = Game.LocalPlayer.Character.GetNearbyVehicles(15);
                        foreach (Vehicle car in vehs)
                        {
                            if (!car.Exists()) { break; }

                            if (Ambientevents.BrokenDownVehicle.BrokenDownVehicleRunning) { break; }
                            if (NextEventStopwatch.ElapsedMilliseconds < nextEventTimer) { break; }


                            if (car.HasDriver && car.Driver.Exists() && !car.IsPoliceVehicle && !car.HasSiren)
                            {
                                Ped driver = car.Driver;
                                if (driver == Game.LocalPlayer.Character)
                                {
                                    driversConsidered.Add(driver);
                                }

                                if (!driversConsidered.Contains(driver))
                                {
                                    if (eventRnd.Next(mobilePhoneChance) == 1)
                                    {
                                        Game.LogTrivial("Creating phone event");
                                        new Ambientevents.MobilePhone(driver, blipStatus, showAmbientEventDescriptionMessage);
                                        SetNextEventStopwatch();
                                    }
                                    else if (car.Speed >= 11f && eventRnd.Next(speederChance) == 1)
                                    {

                                        Game.LogTrivial("Creating Speeder Event");
                                        new Ambientevents.Speeder(driver, blipStatus, showAmbientEventDescriptionMessage);
                                        SetNextEventStopwatch();

                                    }

                                    else if ((World.TimeOfDay.Hours <= 7 || World.TimeOfDay.Hours >= 20) && eventRnd.Next(noLightAtDarkChance) == 1)
                                    {
                                        Game.LogTrivial("Creating No Lights at Dark event");
                                        new Ambientevents.NoLightsAtDark(driver, blipStatus, showAmbientEventDescriptionMessage);
                                        SetNextEventStopwatch();

                                    }
                                    else if (eventRnd.Next(noBrakeLightsChance) == 1)
                                    {
                                        Game.LogTrivial("Creating No Brake Lights event");
                                        new Ambientevents.NoBrakeLights(driver, blipStatus, showAmbientEventDescriptionMessage);
                                        SetNextEventStopwatch();
                                    }
                                    else if (eventRnd.Next(BurnoutWhenStationaryChance) == 1)
                                    {
                                        Game.LogTrivial("Creating BurnoutWhenStationary event");
                                        new Ambientevents.BurnoutWhenStationary(driver, blipStatus, showAmbientEventDescriptionMessage);
                                        SetNextEventStopwatch();
                                    }
                                    else if (eventRnd.Next(RevEngineWhenStationaryChance) == 1)
                                    {
                                        Game.LogTrivial("Creating RevEngineWhenStationary event");
                                        new Ambientevents.RevEngineWhenStationary(driver, blipStatus, showAmbientEventDescriptionMessage);
                                        SetNextEventStopwatch();
                                    }
                                    else if (eventRnd.Next(drunkDriverChance) == 1)
                                    {
                                        Game.LogTrivial("Creating drunk driver event");
                                        new Ambientevents.DrunkDriver(driver, blipStatus, showAmbientEventDescriptionMessage);
                                        SetNextEventStopwatch();
                                    }
                                    else if (eventRnd.Next(drugDriverChance) == 1)
                                    {
                                        Game.LogTrivial("Creating drug driver event");
                                        new Ambientevents.DrugDriver(driver, blipStatus, showAmbientEventDescriptionMessage);
                                        SetNextEventStopwatch();
                                    }
                                    else if (eventRnd.Next(unroadworthyVehicleChance) == 1)
                                    {
                                        Game.LogTrivial("Creating unroadworthy vehicle event");
                                        new Ambientevents.UnroadworthyVehicle(driver, blipStatus, showAmbientEventDescriptionMessage);

                                        SetNextEventStopwatch();
                                    }
                                    else if (eventRnd.Next(motorcyclistWithoutHelmetChance) == 1)
                                    {
                                        Game.LogTrivial("Creating motorcyclist event");
                                        new Ambientevents.MotorcyclistNoHelmet(blipStatus, showAmbientEventDescriptionMessage);
                                        SetNextEventStopwatch();
                                    }
                                    else if (eventRnd.Next(BrokenDownVehicleChance) == 1)
                                    {
                                        Game.LogTrivial("Creating Broken Down Vehicle Event");
                                        new Ambientevents.BrokenDownVehicle(blipStatus, showAmbientEventDescriptionMessage);
                                        SetNextEventStopwatch();
                                    }
                                    else if (eventRnd.Next(stolenVehicleChance) == 1)
                                    {
                                        Game.LogTrivial("Creating stolen vehicle event");
                                        new Ambientevents.StolenVehicle(blipStatus, showAmbientEventDescriptionMessage);
                                        SetNextEventStopwatch();
                                    }
                                    else if (eventRnd.Next(streetRaceChance) == 1)
                                    {
                                        Game.LogTrivial("Creating Street Race Event");
                                        new Ambientevents.StreetRace(blipStatus, showAmbientEventDescriptionMessage);
                                        SetNextEventStopwatch();
                                    }

                                    driversConsidered.Add(driver);
                                }
                            }
                        }
                    }
                }

            });
        }

        private static void registerCallouts()
        {
            if (ownerWantedCalloutEnabled)
            {
                Functions.RegisterCallout(typeof(Callouts.OwnerWanted));
                for (int i = 1; i < ownerWantedFrequency; i++)
                {
                    Functions.RegisterCallout(typeof(Callouts.OwnerWanted));
                }
            }
            if (drugsRunnersEnabled)
            {
                Functions.RegisterCallout(typeof(Callouts.DrugsRunners));
                for (int i = 1; i < drugsRunnersFrequency; i++)
                {
                    Functions.RegisterCallout(typeof(Callouts.DrugsRunners));
                }
            }

            if (DUIEnabled)
            {
                Functions.RegisterCallout(typeof(Callouts.DriverUnderTheInfluence));
                for (int i = 1; i < DUIFrequency; i++)
                {
                    Functions.RegisterCallout(typeof(Callouts.DriverUnderTheInfluence));
                }
            }
        }

        private static void loadValuesFromIniFile()
        {
            try
            {
                drunkDriverChance = Int32.Parse(getDrunkDriverChance()) + 1;
                mobilePhoneChance = initialiseFile().ReadInt32("Ambient Event Chances", "MobilePhone", 100) + 1;
                speederChance = Int32.Parse(getSpeederChance()) + 1;
                drugDriverChance = initialiseFile().ReadInt32("Ambient Event Chances", "DrugDriver", 140) + 1;
                noLightAtDarkChance = initialiseFile().ReadInt32("Ambient Event Chances", "NoLightsAtDark", 110) + 1;
                noBrakeLightsChance = initialiseFile().ReadInt32("Ambient Event Chances", "NoBrakeLights", 150) + 1;
                BrokenDownVehicleChance = initialiseFile().ReadInt32("Ambient Event Chances", "BrokenDownVehicle", 220) + 1;
                BurnoutWhenStationaryChance = initialiseFile().ReadInt32("Ambient Event Chances", "BurnoutWhenStationary", 190) + 1;
                RevEngineWhenStationaryChance = initialiseFile().ReadInt32("Ambient Event Chances", "RevEngineWhenStationary", 190) + 1;
                NumberOfAmbientEventsBeforeTimer = initialiseFile().ReadInt32("Ambient Event Chances", "NumberOfAmbientEventsBeforeTimer");
                if (NumberOfAmbientEventsBeforeTimer < 1) { NumberOfAmbientEventsBeforeTimer = 1; }
                motorcyclistWithoutHelmetChance = Int32.Parse(getMotorcyclistWithoutHelmetChance()) + 1;
                unroadworthyVehicleChance = Int32.Parse(getUnroadworthyVehicleChance()) + 1;
                streetRaceChance = Int32.Parse(getStreetRaceChance()) + 1;
                getStolenVehicleChance();
                blipStatus = bool.Parse(getBlipStatus());
                showAmbientEventDescriptionMessage = bool.Parse(getShowAmbientEventDescriptionMessage());
                parkingTicketKey = (Keys)kc.ConvertFromString(getParkingTicketKey());
                trafficStopFollowKey = (Keys)kc.ConvertFromString(getTrafficStopFollowKey());

                parkModifierKey = (Keys)kc.ConvertFromString(getParkModifierKey());
                trafficStopFollowModifierKey = (Keys)kc.ConvertFromString(getTrafficStopFollowModifierKey());
                roadManagementMenuKey = (Keys)kc.ConvertFromString(getRoadManagementMenuKey());
                drugsTestKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Keybindings", "DrugalyzerKey", "O"));
                drugsTestModifierKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Keybindings", "DrugalyzerModifierKey", "LControlKey"));
                trafficStopMimicKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Keybindings", "TrafficStopMimicKey"));
                trafficStopMimicModifierKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Keybindings", "TrafficStopMimicModifierKey"));
                RoadManagementModifierKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Keybindings", "RoadManagementMenuModifierKey", "None"));
                RepairVehicleKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Keybindings", "RepairBrokenDownVehicleKey", "T"));
                RoadSigns.placeSignShortcutKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Keybindings", "PlaceSignShortcutKey", "J"));
                RoadSigns.placeSignShortcutModifierKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Keybindings", "PlaceSignShortcutModifierKey", "LControlKey"));
                RoadSigns.removeAllSignsKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Keybindings", "RemoveAllSignsKey", "J"));
                RoadSigns.removeAllSignsModifierKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Keybindings", "RemoveAllSignsModifierKey", "None"));

                SpeedChecker.ToggleSpeedCheckerKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Speed Checker Settings", "ToggleSpeedCheckerKey"));
                SpeedChecker.ToggleSpeedCheckerModifierKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Speed Checker Settings", "ToggleSpeedCheckerModifierKey"));
                SpeedChecker.SpeedUnit = initialiseFile().ReadString("Speed Checker Settings", "SpeedUnit");
                if (SpeedChecker.SpeedUnit != "MPH" && SpeedChecker.SpeedUnit != "KMH")
                {
                    SpeedChecker.SpeedUnit = "MPH";
                }

                SpeedChecker.speedgunWeapon = initialiseFile().ReadString("Speed Checker Settings", "SpeedgunWeaponAsset", "WEAPON_MARKSMANPISTOL");

                SpeedChecker.PositionUpKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Speed Checker Settings", "PositionUpKey", "NumPad9"));
                SpeedChecker.PositionRightKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Speed Checker Settings", "PositionRightKey", "NumPad6"));
                SpeedChecker.PositionResetKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Speed Checker Settings", "PositionResetKey", "NumPad5"));
                SpeedChecker.PositionLeftKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Speed Checker Settings", "PositionLeftKey", "NumPad4"));
                SpeedChecker.PositionForwardKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Speed Checker Settings", "PositionForwardKey", "NumPad8"));
                SpeedChecker.PositionDownKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Speed Checker Settings", "PositionDownKey", "NumPad3"));
                SpeedChecker.PositionBackwardKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Speed Checker Settings", "PositionBackwardKey", "NumPad2"));
                SpeedChecker.SecondaryDisableKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Speed Checker Settings", "SecondaryDisableKey", "Back"));
                SpeedChecker.MaxSpeedUpKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Speed Checker Settings", "MaxSpeedUpKey", "PageUp"));
                SpeedChecker.MaxSpeedDownKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Speed Checker Settings", "MaxSpeedDownKey", "PageDown"));
                SpeedChecker.FlagChance = initialiseFile().ReadInt32("Speed Checker Settings", "BringUpFlagChance");
                if (SpeedChecker.FlagChance < 1) { SpeedChecker.FlagChance = 1; }
                else if (SpeedChecker.FlagChance > 100) { SpeedChecker.FlagChance = 100; }
                SpeedChecker.SpeedToColourAt = initialiseFile().ReadInt32("Speed Checker Settings", "SpeedToColourAt");
                SpeedChecker.PlayFlagBlip = initialiseFile().ReadBoolean("Speed Checker Settings", "PlayFlagBlip");

                SpeedChecker.StartStopAverageSpeedCheckKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Speed Checker Settings", "StartStopAverageSpeedCheckKey", "PageUp"));
                SpeedChecker.ResetAverageSpeedCheckKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Speed Checker Settings", "ResetAverageSpeedCheckKey", "PageDown"));
                DUIEnabled = initialiseFile().ReadBoolean("Callouts", "DriverUnderTheInfluenceEnabled");
                DUIFrequency = initialiseFile().ReadInt32("Callouts", "DriverUnderTheInfluenceFrequency");

                //MimickMeDistanceModifier = initialiseFile().ReadSingle("Features", "MimickMeDistanceModifier", 19f);
                TrafficStopAssist.VehicleDoorLockDistance = initialiseFile().ReadSingle("Features", "VehicleDoorLockDistance", 5.2f);
                TrafficStopAssist.VehicleDoorUnlockDistance = initialiseFile().ReadSingle("Features", "VehicleDoorUnlockDistance", 3.5f);
                AutoVehicleDoorLock = initialiseFile().ReadBoolean("Features", "AutoVehicleDoorLock", true);
                OtherUnitRespondingAudio = initialiseFile().ReadBoolean("Features", "OtherUnitRespondingAudio", true);

                CustomPulloverLocationKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Keybindings", "CustomPulloverLocationKey", "W"));
                CustomPulloverLocationModifierKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Keybindings", "CustomPulloverLocationModifierKey", "LControlKey"));

                markMapKey = (Keys)kc.ConvertFromString(getMarkMapKey());
                courtKey = (Keys)kc.ConvertFromString(getCourtKey());
                dispatchCautionMessages = initialiseFile().ReadBoolean("Callouts", "DispatchCautionMessages", true);
                VehicleDetails.AutomaticDetailsChecksEnabledBaseSetting = initialiseFile().ReadBoolean("Features", "VehicleDetailsChecksEnabled");
                ownerWantedCalloutEnabled = bool.Parse(getOwnerWantedCallout());
                ownerWantedFrequency = ownerWantedFrequent();
                drugsRunnersEnabled = bool.Parse(getDrugsRunnersCallout());
                drugsRunnersFrequency = drugsRunnersFrequent();

                Impairment_Tests.Breathalyzer.BreathalyzerKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Breathalyzer Settings", "BreathalyzerKey"));
                Impairment_Tests.Breathalyzer.BreathalyzerModifierKey = (Keys)kc.ConvertFromString(initialiseFile().ReadString("Breathalyzer Settings", "BreathalyzerModifierKey"));
                Impairment_Tests.Breathalyzer.AlcoholLimit = initialiseFile().ReadSingle("Breathalyzer Settings", "AlcoholLimit");
                Impairment_Tests.Breathalyzer.AlcoholLimitUnit = initialiseFile().ReadString("Breathalyzer Settings", "AlcoholLimitUnit");

                FailToProvideChance = initialiseFile().ReadInt32("Breathalyzer Settings", "FailToProvideChance", 7);

                getNextEventTimer();

                DetermineUnitBeatStrings();

            }
            catch (Exception e)
            {
                drunkDriverChance = 190;
                mobilePhoneChance = 95;
                speederChance = 90;
                unroadworthyVehicleChance = 200;
                motorcyclistWithoutHelmetChance = 180;
                streetRaceChance = 250;
                stolenVehicleChance = 190;
                drugDriverChance = 140;
                noLightAtDarkChance = 120;
                blipStatus = true;
                showAmbientEventDescriptionMessage = false;
                parkingTicketKey = Keys.E;
                trafficStopFollowKey = Keys.T;

                roadManagementMenuKey = Keys.F6;
                parkModifierKey = Keys.LControlKey;
                drugsTestKey = Keys.O;
                drugsTestModifierKey = Keys.LControlKey;
                trafficStopFollowModifierKey = Keys.LControlKey;

                markMapKey = Keys.D9;
                courtKey = Keys.D0;
                RoadManagementModifierKey = Keys.None;
                nextEventTimer = 15000;

                dispatchCautionMessages = true;
                ownerWantedCalloutEnabled = true;
                ownerWantedFrequency = 3;
                trafficStopMimicKey = Keys.R;
                trafficStopMimicModifierKey = Keys.LControlKey;
                Game.LogTrivial(e.ToString());
                Game.LogTrivial("Loading default Traffic Policer INI file - Error detected in user's INI file.");
                Game.DisplayNotification("~r~~h~Error~s~ reading Traffic Policer ini file. Default values set; replace with default INI file!");
                Albo1125.Common.CommonLibrary.ExtensionMethods.DisplayPopupTextBoxWithConfirmation("Traffic Policer INI file", "Error reading Traffic Policer INI file. To fix this, replace your current INI file with the original one from the download. Loading default values...", true);

            }

        }
        public static int FailToProvideChance = 7;
        public static Keys CustomPulloverLocationKey = Keys.W;
        public static Keys CustomPulloverLocationModifierKey = Keys.LControlKey;
        public static Keys RoadManagementModifierKey { get; set; }
        private static int noBrakeLightsChance { get; set; }
        private static int noLightAtDarkChance { get; set; }
        public static Keys trafficStopMimicModifierKey { get; set; }
        public static bool dispatchCautionMessages { get; set; }
        private static bool messy { get; set; }
        private static int HYDLATHAHIE { get; set; }
        public static KeysConverter kc = new KeysConverter();
        public static Keys courtKey { get; set; }
        public static Keys markMapKey { get; set; }
        private static int ownerWantedFrequency { get; set; }
        private static bool ownerWantedCalloutEnabled { get; set; }

        private static Keys parkingTicketKey { get; set; }
        public static Keys trafficStopFollowModifierKey { get; set; }
        public static Keys trafficStopFollowKey { get; set; }
        private static Keys parkModifierKey { get; set; }

        public static Keys roadManagementMenuKey { get; set; }
        public static List<Ped> driversConsidered { get; set; }
        public static List<Vehicle> vehiclesTicketedForParking { get; set; }
        private static bool showAmbientEventDescriptionMessage { get; set; }
        private static bool blipStatus { get; set; }
        private static int drunkDriverChance { get; set; }
        private static int BurnoutWhenStationaryChance = 190;
        private static int mobilePhoneChance { get; set; }
        private static int speederChance { get; set; }
        private static int unroadworthyVehicleChance { get; set; }
        private static int BrokenDownVehicleChance = 220;
        private static int drugDriverChance { get; set; }
        private static int motorcyclistWithoutHelmetChance { get; set; }
        private static int stolenVehicleChance { get; set; }
        private static int streetRaceChance { get; set; }
        private static int RevEngineWhenStationaryChance = 190;
        private static Vehicle[] nearbyVehicles { get; set; }
        public static Random rnd = new Random();
        private static int NumberOfAmbientEventsBeforeTimer = 1;
        public static bool isOwnerWantedCalloutRunning = false;
        public static bool driverChangedDueToKeys = false;
        private static bool drugsRunnersEnabled { get; set; }
        private static int drugsRunnersFrequency { get; set; }
        private static bool DUIEnabled = true;
        private static int DUIFrequency = 2;
        public static bool isSomeoneRunningTheLight { get; set; }
        private static int nextEventTimer { get; set; }
        public static bool PerformingImpairmentTest = false;
        private static string colorName { get; set; }
        private static Keys drugsTestKey { get; set; }
        private static Keys drugsTestModifierKey { get; set; }
        public static bool isSomeoneFollowing { get; set; }

        public static Keys trafficStopMimicKey { get; set; }

        //public static AppDomain VehicleSearchDomain = Albo1125.Common.AppDomainHelper.GetAppDomainByName("VehicleSearch_AppDomain");
        public static bool AutoVehicleDoorLock = true;
        public static Keys RepairVehicleKey = Keys.T;
        public static bool OtherUnitRespondingAudio = true;
        public static string DivisionUnitBeat = "1-ADAM-12";
        public static string DivisionUnitBeatAudioString = "DIV_01 ADAM BEAT_12";


        public static bool IsBritishPolicingScriptRunning = false;
        public static bool IsLSPDFRPlusRunning = false;
        
        /// <summary>
        /// The main loop of the plugin
        /// </summary>
        internal static void mainLoop()
        {
            Game.LogTrivial("Traffic Policer.Mainloop started");
            
            Game.LogTrivial("Loading Traffic Policer settings...");
            loadValuesFromIniFile();
            registerCallouts();

            isSomeoneFollowing = false;
            NextEventStopwatch.Start();
            driversConsidered = new List<Ped>();
            vehiclesTicketedForParking = new List<Vehicle>();
            isSomeoneRunningTheLight = false;
            eventCreator();


            RoadSigns.RoadSignsMainLogic();

            if (SpeedChecker.ToggleSpeedCheckerKey != Keys.None)
            {
                SpeedChecker.Main();
            }
            Game.LogTrivial("Traffic Policer by Albo1125 has been loaded successfully!");
            GameFiber.StartNew(delegate
            {
                GameFiber.Wait(12000);
                uint startnot = Game.DisplayNotification("~g~Traffic Officer ~b~" + DivisionUnitBeat + " ~s~reporting for duty!");
                GameFiber.Sleep(6000);
                Game.RemoveNotification(startnot);
                IsBritishPolicingScriptRunning = IsLSPDFRPluginRunning("British Policing Script", new Version("0.8.0.0"));
                IsLSPDFRPlusRunning = IsLSPDFRPluginRunning("LSPDFR+", new Version("1.4.0.0"));

                //Low priority loop
                while (true)
                {
                    GameFiber.Wait(1000);
                    if (IsBritishPolicingScriptRunning || IsLSPDFRPlusRunning)
                    {
                        foreach (Ped ped in PedsToChargeWithDrinkDriving.ToArray())
                        {
                            if (!ped.Exists()) { PedsToChargeWithDrinkDriving.Remove(ped); }
                            else if (Functions.IsPedArrested(ped) && PedsToChargeWithDrinkDriving.Contains(ped))
                            {
                                PedsToChargeWithDrinkDriving.Remove(ped);
                                if (IsBritishPolicingScriptRunning)
                                {
                                    API.BritishPolicingScriptFunctions.CreateNewCourtCase(ped, "drink driving", true, "Fined " + TrafficPolicerHandler.rnd.Next(150, 800).ToString() + " pounds and disqualified from driving for "
                                        + TrafficPolicerHandler.rnd.Next(12, 25).ToString() + " months");
                                }
                                else if (IsLSPDFRPlusRunning)
                                {
                                    API.LSPDFRPlusFunctions.CreateCourtCase(Functions.GetPersonaForPed(ped), "driving under the influence of alcohol", 100, API.LSPDFRPlusFunctions.DetermineFineSentence(390, 1000) +
                                        " License suspended for " + TrafficPolicerHandler.rnd.Next(1, 7) + " months.");
                                }

                            }
                        }
                        foreach (Ped ped in PedsToChargeWithDrugDriving.ToArray())
                        {
                            if (!ped.Exists()) { PedsToChargeWithDrugDriving.Remove(ped); }
                            else if (Functions.IsPedArrested(ped) && PedsToChargeWithDrugDriving.Contains(ped))
                            {
                                PedsToChargeWithDrugDriving.Remove(ped);
                                if (IsBritishPolicingScriptRunning)
                                {
                                    API.BritishPolicingScriptFunctions.CreateNewCourtCase(ped, "drug driving", true, "Fined " + TrafficPolicerHandler.rnd.Next(150, 800).ToString() + " pounds and disqualified from driving for "
                                        + TrafficPolicerHandler.rnd.Next(12, 25).ToString() + " months");
                                }
                                else if (IsLSPDFRPlusRunning)
                                {
                                    API.LSPDFRPlusFunctions.CreateCourtCase(Functions.GetPersonaForPed(ped), "driving under the influence of drugs", 100, API.LSPDFRPlusFunctions.DetermineFineSentence(390, 1000) +
                                        " License suspended for " + TrafficPolicerHandler.rnd.Next(1, 7) + " months.");
                                }

                            }
                        }

                        foreach (GameFiber fiber in AmbientEventGameFibersToAbort.ToArray())
                        {
                            AmbientEventGameFibersToAbort.Remove(fiber);
                            if (fiber != null)
                            {

                                if (fiber.IsAlive)
                                {

                                    fiber.Abort();
                                }
                            }


                        }
                    }
                }

            });

            GameFiber.StartNew(delegate
            {
                while (true)
                {

                    GameFiber.Yield();

                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(parkModifierKey) || (parkModifierKey == Keys.None))
                    {
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(parkingTicketKey))
                        {
                            ParkingTicket parkingTicket = new ParkingTicket();
                        }
                    }
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(drugsTestModifierKey) || (drugsTestModifierKey == Keys.None))
                    {
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(drugsTestKey))
                        {
                            if (!PerformingImpairmentTest)
                            {
                                Impairment_Tests.DrugTestKit.testNearestPedForDrugs();
                            }
                        }
                    }
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(Impairment_Tests.Breathalyzer.BreathalyzerModifierKey) || (Impairment_Tests.Breathalyzer.BreathalyzerModifierKey == Keys.None))
                    {
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(Impairment_Tests.Breathalyzer.BreathalyzerKey))
                        {
                            if (!PerformingImpairmentTest)
                            {
                                Impairment_Tests.Breathalyzer.TestNearestPedForAlcohol();
                            }
                        }
                    }
                    if (Functions.IsPlayerPerformingPullover())
                    {

                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(trafficStopFollowModifierKey) || (trafficStopFollowModifierKey == Keys.None))
                        {
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(trafficStopFollowKey))
                            {
                                if (!isSomeoneFollowing)
                                {
                                    TrafficStopAssist.followMe();
                                }
                                else { isSomeoneFollowing = false; }
                            }
                        }
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(trafficStopMimicModifierKey) || (trafficStopMimicModifierKey == Keys.None))
                        {
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(trafficStopMimicKey))
                            {
                                if (!isSomeoneFollowing)
                                {
                                    TrafficStopAssist.mimicMe();
                                }
                                else
                                {
                                    isSomeoneFollowing = false;
                                }
                            }
                        }
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(CustomPulloverLocationModifierKey) || (CustomPulloverLocationModifierKey == Keys.None))
                        {
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(CustomPulloverLocationKey))
                            {
                                if (!isSomeoneFollowing)
                                {
                                    TrafficStopAssist.SetCustomPulloverLocation();
                                }
                                else
                                {
                                    Game.LogTrivial("Already doing custom pullover location.");
                                }
                            }
                        }

                        if (!isSomeoneRunningTheLight)
                        {
                            TrafficStopAssist.checkForceRedLightRun();
                        }
                    }
                }
            });

            while (true)
            {
                GameFiber.Yield();
                TrafficStopAssist.checkForYieldDisable();
                if (AutoVehicleDoorLock)
                {
                    TrafficStopAssist.LockPlayerDoors();
                }

                VehicleDetails.CheckForTextEntry();
            }


        }
        public static bool IsLSPDFRPluginRunning(string Plugin, Version minversion = null)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                AssemblyName an = assembly.GetName(); if (an.Name.ToLower() == Plugin.ToLower())
                {
                    if (minversion == null || an.Version.CompareTo(minversion) >= 0) { return true; }
                }
            }
            return false;
        }


        public static Assembly LSPDFRResolveEventHandler(object sender, ResolveEventArgs args) { foreach (Assembly assembly in Functions.GetAllUserPlugins()) { if (args.Name.ToLower().Contains(assembly.GetName().Name.ToLower())) { return assembly; } } return null; }


        private static void DetermineUnitBeatStrings()
        {
            string Division = "DIV_" + initialiseFile().ReadInt32("General", "Division", 1).ToString("D2");
            string UnitType = initialiseFile().ReadString("General", "UnitType", "ADAM").ToUpper();
            string Beat = "BEAT_" + initialiseFile().ReadInt32("General", "Beat", 12).ToString("D2");
            DivisionUnitBeatAudioString = Division + " " + UnitType + " " + Beat;
            if (string.IsNullOrWhiteSpace(initialiseFile().ReadString("General", "OfficerDisplayNameOverride", "")))
            {
                DivisionUnitBeat = initialiseFile().ReadString("General", "Division", "1") + "-" + initialiseFile().ReadString("General", "UnitType", "ADAM") + "-" + initialiseFile().ReadString("General", "Beat", "12");
            }
            else
            {
                DivisionUnitBeat = initialiseFile().ReadString("General", "OfficerDisplayNameOverride", "");
                Game.LogTrivial("Traffic Policer Officer Name overridden.");
            }

        }
        private static int AmbientEventsPassed = 0;
        private static Stopwatch NextEventStopwatch = new Stopwatch();
        private static void SetNextEventStopwatch()
        {
            AmbientEventsPassed++;
            if (AmbientEventsPassed >= NumberOfAmbientEventsBeforeTimer)
            {
                NextEventStopwatch.Reset();
                NextEventStopwatch.Start();
            }
        }
        public static InitializationFile initialiseFile()
        {
            InitializationFile ini = new InitializationFile("Plugins/LSPDFR/Traffic Policer.ini");
            ini.Create();
            return ini;
        }
        private static string getDrunkDriverChance()
        {
            InitializationFile ini = initialiseFile();

            //ReadString takes 3 parameters: the first is the category, the second is the name of the entry and the third is the default value should the user leave the field blank.
            //Take a look at the example .ini file to understand this better.
            string chance = ini.ReadString("Ambient Event Chances", "DrunkDriver", "180");
            return chance;
        }

        private static string getSpeederChance()
        {
            InitializationFile ini = initialiseFile();

            //ReadString takes 3 parameters: the first is the category, the second is the name of the entry and the third is the default value should the user leave the field blank.
            //Take a look at the example .ini file to understand this better.
            string chance = ini.ReadString("Ambient Event Chances", "Speeder", "90");
            return chance;
        }
        private static string getUnroadworthyVehicleChance()
        {
            InitializationFile ini = initialiseFile();

            //ReadString takes 3 parameters: the first is the category, the second is the name of the entry and the third is the default value should the user leave the field blank.
            //Take a look at the example .ini file to understand this better.
            string chance = ini.ReadString("Ambient Event Chances", "UnroadworthyVehicle", "150");
            return chance;
        }
        private static string getMotorcyclistWithoutHelmetChance()
        {
            InitializationFile ini = initialiseFile();
            string chance = ini.ReadString("Ambient Event Chances", "MotorcyclistWithoutHelmet", "170");
            return chance;
        }
        private static string getStreetRaceChance()
        {
            InitializationFile ini = initialiseFile();
            string chance = ini.ReadString("Ambient Event Chances", "StreetRace", "200");
            return chance;
        }
        private static void getStolenVehicleChance()
        {
            InitializationFile ini = initialiseFile();
            stolenVehicleChance = ini.ReadInt32("Ambient Event Chances", "StolenVehicle", 190);
            stolenVehicleChance++;
        }
        private static string getBlipStatus()
        {
            InitializationFile ini = initialiseFile();
            string blipStatus = ini.ReadString("General", "CreateBlipsForAmbientEvents", "true");
            return blipStatus;
        }
        private static string getShowAmbientEventDescriptionMessage()
        {
            InitializationFile ini = initialiseFile();
            string ShowAmbientEventDescriptionMessage = ini.ReadString("General", "ShowAmbientEventDescriptionMessage", "false");
            return ShowAmbientEventDescriptionMessage;
        }
        private static string getParkingTicketKey()
        {
            InitializationFile ini = initialiseFile();
            string key = ini.ReadString("Keybindings", "ParkingTicketKey", "E");
            return key;
        }
        private static string getParkModifierKey()
        {
            InitializationFile ini = initialiseFile();
            string key = ini.ReadString("Keybindings", "ParkModifierKey", "LControlKey");
            return key;
        }
        private static string getShowIniMessage()
        {
            InitializationFile ini = initialiseFile();
            string show = ini.ReadString("General", "ShowStartupIniFileMessage", "true");
            return show;
        }

        private static string getOwnerWantedCallout()
        {
            InitializationFile ini = initialiseFile();
            string callout = ini.ReadString("Callouts", "OwnerWantedEnabled", "true");
            return callout;
        }
        private static string getDrugsRunnersCallout()
        {
            InitializationFile ini = initialiseFile();
            string callout = ini.ReadString("Callouts", "DrugsRunnersEnabled", "true");
            return callout;
        }
        private static string getTrafficStopFollowKey()
        {
            InitializationFile ini = initialiseFile();
            string key = ini.ReadString("Keybindings", "TrafficStopFollowKey", "T");
            return key;
        }
        private static string getTrafficStopFollowModifierKey()
        {
            InitializationFile ini = initialiseFile();
            string key = ini.ReadString("Keybindings", "TrafficStopFollowModifierKey", "LControlKey");
            return key;
        }
        private static string getRoadManagementMenuKey()
        {
            InitializationFile ini = initialiseFile();
            string key = ini.ReadString("Keybindings", "RoadManagementMenuKey", "F6");
            return key;
        }
        private static int ownerWantedFrequent()
        {
            InitializationFile ini = initialiseFile();
            int result = ini.ReadInt32("Callouts", "OwnerWantedFrequency", 2);
            if (result < 1)
            {
                result = 1;
            }
            return result;
        }
        private static int drugsRunnersFrequent()
        {
            InitializationFile ini = initialiseFile();
            int result = ini.ReadInt32("Callouts", "DrugsRunnersFrequency", 2);
            if (result < 1)
            {
                result = 1;
            }
            return result;
        }
        private static string getUserName()
        {
            InitializationFile ini = initialiseFile();
            string name = ini.ReadString("General", "Name", "");
            return name;
        }
        private static string getMarkMapKey()
        {
            InitializationFile ini = initialiseFile();
            string key = ini.ReadString("Keybindings", "MarkMapKey", "D9");
            return key;
        }
        private static string getCourtKey()
        {
            InitializationFile ini = initialiseFile();
            string key = ini.ReadString("Keybindings", "CourtKey", "D0");
            return key;
        }

        private static void getNextEventTimer()
        {
            InitializationFile ini = initialiseFile();
            nextEventTimer = ini.ReadInt32("Ambient Event Chances", "NextEventTimer", 35);
            if (nextEventTimer < 5)
            {
                nextEventTimer = 5;
            }
            if (nextEventTimer > 200)
            {
                nextEventTimer = 200;
            }
            nextEventTimer *= 1000;
        }

        internal static void Initialise()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LSPDFRResolveEventHandler);
            
            GameFiber.StartNew(delegate
            {
                Game.LogTrivial("Traffic Policer is not in beta.");
                mainLoop();
                Game.LogTrivial("Traffic Policer, developed by Albo1125, has been loaded successfully!");
                GameFiber.Wait(6000);
                Game.DisplayNotification("~b~Traffic Policer~s~, developed by ~b~Albo1125, ~s~has been loaded ~g~successfully.");
            });
        }
    }
}
