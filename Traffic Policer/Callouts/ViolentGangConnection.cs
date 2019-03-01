using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;

using Albo1125.Common.CommonLibrary;

namespace Traffic_Policer.Callouts
{
    //[CalloutInfo("ViolentGangConnection", CalloutProbability.Always)]
    internal class ViolentGangConnection : Callout
    {
        private Vehicle car; // a rage vehicle
        private Ped driver; // a rage ped
        private Ped passenger;
        private Vector3 spawnPoint; // a Vector3
        private Blip driverBlip; // a rage blip
        private LHandle pursuit; // an API pursuit handle
        private bool calloutStarted;
        private bool calloutFinished;
        public Ped playerPed;
        private List<Blip> groupBlips = new List<Blip>();
        
        private Random rnd = new Random();
        private string[] vehiclesToSelectFrom = new string[] {"DUKES", "BALLER", "BALLER2", "BISON", "BISON2", "BISON3", "BJXL", "CAVALCADE", "CHEETAH", "COGCABRIO", "ASEA", "ADDER", "FELON", "FELON2", "ZENTORNO",
        "WARRENER" };
        private string[] meleeWeaponsToSelectFrom = new string[] { "WEAPON_KNIFE", "WEAPON_NIGHTSTICK", "WEAPON_GOLFCLUB", "WEAPON_BAT", "WEAPON_CROWBAR" };
        private Vector3 groveStreetLocation = new Vector3(110.627f, -1949.614f, 20.49053f);
        private Group group;
        private List<Ped> pedsInGroup = new List<Ped>();
        

        public override bool OnBeforeCalloutDisplayed()
        {
            playerPed = Game.LocalPlayer.Character;
            if (Vector3.Distance(playerPed.Position, groveStreetLocation) > 900f)
            {
                return false;
            }
            spawnPoint = World.GetNextPositionOnStreet(playerPed.Position.Around2D(400f));
            while ((Vector3.Distance(playerPed.Position, spawnPoint) < 300f) || (Vector3.Distance(spawnPoint, groveStreetLocation) < 250f)) 
            {
                spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(400f));
            }
            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 5f);
            CalloutMessage = "~o~ANPR Hit:~b~ Connection to ~r~violent gang activity.";
            CalloutPosition = spawnPoint;

            return base.OnBeforeCalloutDisplayed();
        }

        public override void OnCalloutNotAccepted()
        {
            base.OnCalloutNotAccepted();
            if (driver.Exists()) { driver.Delete(); }
            if (passenger.Exists()) { passenger.Delete(); }
            if (car.Exists()) { car.Delete(); }
        }

        public override bool OnCalloutAccepted()
        {
            while ((Vector3.Distance(playerPed.Position, spawnPoint) < 300f) || (Vector3.Distance(spawnPoint, groveStreetLocation) < 250f))
            {
                spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around2D(400f));
            }
            driver = new Ped(spawnPoint);
            TrafficPolicerHandler.driversConsidered.Add(driver);
            driver.BlockPermanentEvents = true;

            car = new Vehicle(vehiclesToSelectFrom[rnd.Next(vehiclesToSelectFrom.Length)], spawnPoint);
            car.RandomiseLicencePlate();
            
            car.IsPersistent = true;
            driver.WarpIntoVehicle(car, -1);
            driverBlip = driver.AttachBlip();
            passenger = new Ped(spawnPoint);
            passenger.BlockPermanentEvents = true;
            passenger.WarpIntoVehicle(car, 0);
            group = new Group(driver);
            group.AddMember(passenger);
            return base.OnCalloutAccepted();
        }

        public override void Process()
        {
            base.Process();
            if (!calloutStarted)
            {
                situationOne();
            }
            if (calloutFinished)
            {
                End();
            }
        }
        public override void End()
        {
            base.End();
            if (car.Exists()) { car.IsDriveable = true; }
            foreach (Blip blip in groupBlips)
            {
                if (blip.Exists())
                {
                    blip.Delete();
                }
            }
            try
            {
                if (Functions.IsPursuitStillRunning(pursuit))
                {
                    Functions.ForceEndPursuit(pursuit);
                }
            }
            catch { }
            if (!calloutFinished)
            {
                if (driver.Exists()) { driver.Delete(); }
                if (car.Exists()) { car.Delete(); }
                if (passenger.Exists()) { passenger.Delete(); }
                if (driverBlip.Exists()) { driverBlip.Delete(); }

            }
            else
            {
                Game.DisplayNotification("ANPR Hit callout ended.");
                if (driverBlip.Exists()) { driverBlip.Delete(); }
                if (driver.Exists())
                {
                    driver.Dismiss();
                }
                if (car.Exists()) { car.Dismiss(); }
                if (passenger.Exists() && !passenger.IsInVehicle(car, false))
                {

                    passenger.Dismiss();
                }
            }
        }

        private void situationOne()
        {
            calloutStarted = true;
            GameFiber.StartNew(delegate
            {
                try {
                    Game.DisplayNotification("Stop the vehicle as soon as possible and ~b~deal with the occupants.");
                    Game.DisplayNotification("Try stopping the vehicle before it reaches any ~r~dangerous neighbourhoods ~s~if possible.");
                    driverBlip.EnableRoute(System.Drawing.Color.Red);
                    driver.Armor += 45;
                    passenger.Armor += 45;
                    driver.Health += 90;
                    passenger.Health += 90;
                    driver.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_SMG"), 500, false);
                    passenger.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_SMG"), 500, false);
                    pedsInGroup.Add(driver);
                    pedsInGroup.Add(passenger);
                    driver.Tasks.DriveToPosition(groveStreetLocation, 50f, (VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians));
                    
                    while (!Functions.IsPlayerPerformingPullover())
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(driver.Position, groveStreetLocation) < 10f)
                        {
                            break;
                        }
                    }
                    
                    if (Functions.IsPlayerPerformingPullover())
                    {
                        GameFiber.Sleep(2000);
                        driver.Tasks.ClearImmediately();
                        driver.WarpIntoVehicle(car, -1);
                        pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(pursuit, driver);
                    }
                    while (Vector3.Distance(driver.Position, groveStreetLocation) > 30f)
                    {
                        driver.Tasks.DriveToPosition(groveStreetLocation, 50f, VehicleDrivingFlags.None);
                        GameFiber.Yield();
                    }
                    while (Vector3.Distance(playerPed.Position, groveStreetLocation) > 50f)
                    {
                        GameFiber.Yield();
                        driver.Tasks.DriveToPosition(groveStreetLocation, 20f, VehicleDrivingFlags.None);
                    }
                    if (driverBlip.Exists())
                    {
                        driverBlip.Delete();
                    }
                    Ped[] nearbyPeds = driver.GetNearbyPeds(7);
                    foreach (Ped ped in nearbyPeds)
                    {
                        if (!group.IsMember(ped) && (ped!=passenger))
                        {

                            group.AddMember(ped);
                            ped.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_MICROSMG"), 500, true);
                            pedsInGroup.Add(ped);
                        }

                    }
                    int waitingCount = 0;
                    while (Vector3.Distance(playerPed.Position, driver.Position) > 30f)
                    {
                        GameFiber.Sleep(50);
                        waitingCount++;
                        if (waitingCount == 200) { break; }
                    }
                    try
                    {
                        if (Functions.IsPursuitStillRunning(pursuit))
                        {
                            Functions.ForceEndPursuit(pursuit);
                        }
                    }
                    catch { }
                    driver.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(3000);
                    Rage.Native.NativeFunction.Natives.TaskCombatPed(driver, playerPed, 0, 16);
                    foreach (Ped ped in pedsInGroup)
                    {
                        Blip pedBlip = ped.AttachBlip();
                        
                        groupBlips.Add(pedBlip);
                    }
                    while (true)
                    {
                        try {
                            if (driver.Exists())
                            {

                                if (Rage.Native.NativeFunction.Natives.S_PED_SPRINTING<bool>(driver))
                                {
                                    driver.Tasks.ClearImmediately();
                                }
                                driver.BlockPermanentEvents = true;



                                Rage.Native.NativeFunction.Natives.TaskCombatPed(driver, playerPed, 0, 16);

                            }
                            if (passenger.Exists())
                            {

                                if (Rage.Native.NativeFunction.Natives.S_PED_SPRINTING<bool>(passenger))
                                {
                                    passenger.Tasks.ClearImmediately();
                                }
                                passenger.BlockPermanentEvents = true;
                                Rage.Native.NativeFunction.Natives.TaskCombatPed(passenger, playerPed, 0, 16);

                            }
                            if (playerPed.IsDead)
                            {
                                break;
                            }
                            if (pedsInGroup.Count == 0)
                            {
                                break;
                            }

                            foreach (Ped ped in pedsInGroup)
                            {
                                if (ped.Exists())
                                {
                                    if (ped.IsDead)
                                    {
                                        pedsInGroup.Remove(ped);
                                    }
                                }
                            }







                            GameFiber.Yield();
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    calloutFinished = true;
                }
                catch (Exception e)
                {
                    Game.LogTrivial(e.ToString());
                    Game.DisplayNotification("Callout crashed");
                    End();
                }
            });
        }
    }
}
