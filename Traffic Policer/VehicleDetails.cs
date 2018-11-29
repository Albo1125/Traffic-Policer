using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using Rage.Native;
using Traffic_Policer.Extensions;
using LSPD_First_Response.Mod.API;

namespace Traffic_Policer
{
    public enum EVehicleDetailsStatus { None, Expired, Valid };
    
    internal class VehicleDetails
    {
        public EVehicleDetailsStatus InsuranceStatus;
        public EVehicleDetailsStatus RegistrationStatus;
        public Vehicle veh;
        public VehicleDetails(Vehicle _veh, EVehicleDetailsStatus _InsuranceStatus, EVehicleDetailsStatus _RegistrationStatus)
        {
            this.veh = _veh;
            this.InsuranceStatus = _InsuranceStatus;
            this.RegistrationStatus = _RegistrationStatus;
        }


        public static bool AutomaticDetailsChecksEnabledBaseSetting = true;
        public static bool AutomaticDetailsChecksEnabled = true;
        private static Dictionary<PoolHandle, VehicleDetails> VehiclesWithDetails = new Dictionary<PoolHandle, VehicleDetails>();

        public static bool UserTyping = false;
        internal static void CheckForTextEntry()
        {
            if (AutomaticDetailsChecksEnabledBaseSetting && AutomaticDetailsChecksEnabled)
            {
                if (Functions.IsPoliceComputerActive() && NativeFunction.Natives.UPDATE_ONSCREEN_KEYBOARD<int>() == 0)
                {
                    UserTyping = true;
                }
                if (UserTyping && NativeFunction.Natives.UPDATE_ONSCREEN_KEYBOARD<int>() == 1)
                {
                    string plate = NativeFunction.Natives.GET_ONSCREEN_KEYBOARD_RESULT<string>();
                    DisplayVehicleDetailsNotification(GetVehicleDetailsForLicencePlate(plate));
                    UserTyping = false;
                }
            }
            else if (!AutomaticDetailsChecksEnabled) { UserTyping = false; }
        }
        public static EVehicleDetailsStatus GetInsuranceStatusForVehicle(Vehicle veh)
        {
            if (veh.Exists())
            {
                if (!VehiclesWithDetails.ContainsKey(veh.Handle))
                {
                    AddVehicleToDetailsDatabase(veh);
                }
                return VehiclesWithDetails[veh.Handle].InsuranceStatus;
            }
            else
            {
                return EVehicleDetailsStatus.Valid;
            }
        }

        public static EVehicleDetailsStatus GetRegistrationStatusForVehicle(Vehicle veh)
        {
            if (veh.Exists())
            {
                if (!VehiclesWithDetails.ContainsKey(veh.Handle))
                {

                    AddVehicleToDetailsDatabase(veh);
                }
                return VehiclesWithDetails[veh.Handle].RegistrationStatus;
            }
            return EVehicleDetailsStatus.Valid;
        }

        public static void AddVehicleToDetailsDatabase(Vehicle veh, int InsuranceChance =7, int RegistrationChance = 7)
        {
            if (!VehiclesWithDetails.ContainsKey(veh.Handle))
            {
                EVehicleDetailsStatus InsuranceStatusToAdd;
                
                if (TrafficPolicerHandler.rnd.Next(InsuranceChance) == 0 && !veh.HasSiren)
                {
                    if (TrafficPolicerHandler.rnd.Next(5) < 2) { InsuranceStatusToAdd = EVehicleDetailsStatus.Expired; }
                    else { InsuranceStatusToAdd = EVehicleDetailsStatus.None; }
                    
                }
                else
                {

                    InsuranceStatusToAdd = EVehicleDetailsStatus.Valid;
                }

                EVehicleDetailsStatus RegistrationStatusToAdd;
                if (TrafficPolicerHandler.rnd.Next(RegistrationChance) == 0 && !veh.HasSiren)
                {
                    if (TrafficPolicerHandler.rnd.Next(5) < 2) { RegistrationStatusToAdd = EVehicleDetailsStatus.Expired; }
                    else { RegistrationStatusToAdd = EVehicleDetailsStatus.None; }

                }
                else
                {

                    RegistrationStatusToAdd = EVehicleDetailsStatus.Valid;
                }

                VehiclesWithDetails.Add(veh.Handle, new VehicleDetails(veh, InsuranceStatusToAdd, RegistrationStatusToAdd));
            }
        }

        public static bool IsVehicleInDetailsDatabase(Vehicle veh)
        {
            return VehiclesWithDetails.ContainsKey(veh.Handle);
        }

        public static void SetInsuranceStatusForVehicle(Vehicle veh, EVehicleDetailsStatus status)
        {
            if (veh.Exists())
            {
                if (!IsVehicleInDetailsDatabase(veh))
                {
                    AddVehicleToDetailsDatabase(veh);
                }

                VehiclesWithDetails[veh.Handle].InsuranceStatus = status;
            }

        }

        public static void SetRegistrationStatusForVehicle(Vehicle veh, EVehicleDetailsStatus status)
        {
            if (veh.Exists())
            {
                if (!IsVehicleInDetailsDatabase(veh))
                {
                    AddVehicleToDetailsDatabase(veh);
                }

                VehiclesWithDetails[veh.Handle].RegistrationStatus = status;
            }

        }

        private static VehicleDetails GetVehicleDetailsForLicencePlate(string LicencePlate)
        {
            LicencePlate = LicencePlate.ToUpper();
            //Game.LogTrivial(LicencePlate);
            Vehicle[] allvehssorted = (from x in World.GetAllVehicles() orderby Game.LocalPlayer.Character.DistanceTo(x) select x).ToArray();
            foreach (Vehicle veh in allvehssorted)
            {
                if (veh.Exists())
                {
                    if (veh.LicensePlate == LicencePlate)
                    {
                        if (!IsVehicleInDetailsDatabase(veh))
                        {
                            AddVehicleToDetailsDatabase(veh);
                        }
                        return VehiclesWithDetails[veh.Handle];
                    }
                }
            }
            return null; //should never happen
        }
        
        public static void DisplayVehicleDetailsNotification(VehicleDetails vehdetails)
        {
            if (vehdetails == null) { return; }
            string subtitle;
            if (vehdetails.veh.Exists()) { subtitle = vehdetails.veh.LicensePlate; }
            else { subtitle = ""; }
            GameFiber.StartNew(delegate
            {
                GameFiber.Wait(3000);
                Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "Vehicle Details", subtitle, "Insurance: " + vehdetails.InsuranceStatus.ToColouredString() + "~n~~s~Registration: " + vehdetails.RegistrationStatus.ToColouredString());
            });
                
            
        }
    }
}
