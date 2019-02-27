using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using Rage.Native;


namespace Traffic_Policer
{
    /// <summary>
    /// Allows the player to report a vehicle for illegal parking
    /// </summary>
    internal class ParkingTicket
    {
        public Ped playerPed = Game.LocalPlayer.Character;
        public Vehicle car;
        private string licencePlateAudioMessage = "";
        private string licencePlateNotificationMessage = "";
        private string[] vowels = new string[] { "a", "e", "o", "i", "u" };
        private string[] numbers = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        private string article;

        public ParkingTicket(bool create = true)
        {
            if (!create) { return; }
            Vehicle[] nearbyCars = playerPed.GetNearbyVehicles(1);
            if (nearbyCars.Length == 0)
            {
                return;
            }
            if (Functions.IsPlayerPerformingPullover())
            {
                Game.DisplayHelp("Finish your ~b~traffic stop ~s~before reporting a vehicle for a parking offence.");
                return;
            }

            car = nearbyCars[0];
            car.IsPersistent = true;
            if (Vector3.Distance(playerPed.Position, car.Position) > 3.7f)
            {
                return;
            }
            if (TrafficPolicerHandler.vehiclesTicketedForParking.Contains(car))
            {
                Game.DisplayNotification("You have already given that vehicle a ~b~parking ticket.");
                Functions.PlayScannerAudio("BEEP");
                return;
            }
            if (car.IsPoliceVehicle)
            {
                Game.DisplayHelp("~b~Police vehicles ~s~are exempt from parking laws.", 5000);
                Functions.PlayScannerAudio("BEEP");
                return;
            }
            if (Vector3.Distance(playerPed.Position, car.Position) > 2.3f)
            {
                Game.DisplayHelp("You need to be ~b~closer~s~ to the vehicle.", 4000);
                return;
            }
            Vector3 directionFromPedToCar = (car.Position - playerPed.Position);
            directionFromPedToCar.Normalize();
            playerPed.Tasks.AchieveHeading(MathHelper.ConvertDirectionToHeading(directionFromPedToCar)).WaitForCompletion(1800);

            string modelName = car.Model.Name.ToLower();
            if (vowels.Contains<string>(modelName[0].ToString()))
            {
                article = "an";
            }
            else { article = "a"; }

            if (numbers.Contains<string>(modelName.Last().ToString()))
            {
                modelName = modelName.Substring(0, modelName.Length - 1);
            }
            modelName = char.ToUpper(modelName[0]) + modelName.Substring(1);


            string licencePlate = car.LicensePlate;

            foreach (char character in licencePlate)
            {
                if (!Char.IsWhiteSpace(character))
                {
                    licencePlateAudioMessage = licencePlateAudioMessage + " " + character;
                    licencePlateNotificationMessage = licencePlateNotificationMessage + character;
                }
            }
            Game.DisplayNotification("~g~Traffic Officer ~b~" + TrafficPolicerHandler.DivisionUnitBeat + " ~s~is reporting an ~r~illegally parked vehicle.");
            Game.DisplayNotification("~b~Processing a parking ticket for " + article + " ~r~" + modelName + "~b~ with licence plate: ~r~" + licencePlateNotificationMessage + ".");
            Game.DisplayNotification("~b~The offending ~r~" + modelName + " ~b~is parked on ~o~" + World.GetStreetName(car.Position) + ".");
            if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
            {
                API.LSPDFRPlusFunctions.AddCountToStatistic(Main.PluginName, "Parking tickets issued");
            }


            playerPed.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_UNARMED"), 0, true);
            Rage.Object notepad = new Rage.Object("prop_notepad_02", playerPed.Position);
            int boneIndex = NativeFunction.Natives.GET_PED_BONE_INDEX<int>(playerPed, (int)PedBoneId.LeftThumb2);
            NativeFunction.Natives.ATTACH_ENTITY_TO_ENTITY(notepad, playerPed, boneIndex, 0f, 0f, 0f, 0f, 0f, 0f, true, false, false, false, 2, 1);
            playerPed.Tasks.PlayAnimation("veh@busted_std", "issue_ticket_cop", 1f, AnimationFlags.Loop | AnimationFlags.UpperBodyOnly).WaitForCompletion(8000);
            notepad.Delete();
            Game.DisplayNotification("~g~Traffic Officer ~b~" + TrafficPolicerHandler.DivisionUnitBeat + " ~s~is reporting an ~r~illegally parked vehicle.");
            Game.DisplayNotification("~b~Processing a parking ticket for " + article + " ~r~" + modelName + "~b~ with licence plate: ~r~" + licencePlateNotificationMessage + ".");
            Game.DisplayNotification("~b~The offending ~r~" + modelName + " ~b~is parked on ~o~" + World.GetStreetName(car.Position) + ".");
            playerPed.Tasks.PlayAnimation("random@arrests", "generic_radio_enter", 0.7f, AnimationFlags.UpperBodyOnly | AnimationFlags.StayInEndFrame).WaitForCompletion(1500);







            Functions.PlayScannerAudioUsingPosition("WE_HAVE_01 ILLEGALLY_PARKED_VEHICLE IN_OR_ON_POSITION INTRO_02 TARGET_VEHICLE_LICENCE_PLATE UHH" + licencePlateAudioMessage + " OUTRO_03 NOISE_SHORT INTRO_01 CODE4_ADAM PROCEED_WITH_PATROL NOISE_SHORT", playerPed.Position);
            TrafficPolicerHandler.vehiclesTicketedForParking.Add(car);
            car.IsPersistent = false;
            GameFiber.Sleep(5900);
            Game.DisplayNotification("~g~Traffic Officer ~b~" + TrafficPolicerHandler.DivisionUnitBeat + " ~s~is reporting an ~r~illegally parked vehicle.");
            Game.DisplayNotification("~b~Processing a parking ticket for " + article + " ~r~" + modelName + "~b~ with licence plate: ~r~" + licencePlateNotificationMessage + ".");
            Game.DisplayNotification("~b~The offending ~r~" + modelName + " ~b~is parked on ~o~" + World.GetStreetName(car.Position) + ".");


            playerPed.Tasks.PlayAnimation("random@arrests", "generic_radio_exit", 1.0f, AnimationFlags.UpperBodyOnly);




        }
        
    }
}
