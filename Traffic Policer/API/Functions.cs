using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using System.Reflection;
using Traffic_Policer.Impairment_Tests;

namespace Traffic_Policer.API
{
    public static class Functions
    {
        /// <summary>
        /// Check whether the vehicle is insured as per the insurance system.
        /// </summary>
        /// <param name="veh"></param>
        /// <returns></returns>
        
        public static bool IsVehicleInsured(Vehicle veh)
        {
            if (veh.Exists())
            {
                
                Traffic_Policer.EVehicleDetailsStatus insurancestatus = VehicleDetails.GetInsuranceStatusForVehicle(veh);
                return insurancestatus == EVehicleDetailsStatus.Valid;
                
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Sets the insurance status for a vehicle. Used when vehicle is checked.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="Insured">If false, sets insurance status to expired/none at random.</param>
        public static void SetVehicleInsuranceStatus(Vehicle vehicle, bool Insured)
        {
            if (Insured)
            {
                VehicleDetails.SetInsuranceStatusForVehicle(vehicle, EVehicleDetailsStatus.Valid);
            }
            else
            {
                if (TrafficPolicerHandler.rnd.Next(5) < 2)
                {
                    VehicleDetails.SetInsuranceStatusForVehicle(vehicle, EVehicleDetailsStatus.None);
                }
                else
                {
                    VehicleDetails.SetInsuranceStatusForVehicle(vehicle, EVehicleDetailsStatus.Expired);
                }
            }
        }

        /// <summary>
        /// Sets the insurance status for a vehicle. Used when vehicle is checked.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="InsuranceStatus"></param>
        public static void SetVehicleInsuranceStatus(Vehicle vehicle, EVehicleDetailsStatus InsuranceStatus)
        {
            VehicleDetails.SetInsuranceStatusForVehicle(vehicle, InsuranceStatus);
        }

        /// <summary>
        /// Sets the registration status for a vehicle. Used when vehicle is checked.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="registrationValid">If false, sets status to expired/none at random </param>
        public static void SetVehicleRegistrationStatus(Vehicle vehicle, bool registrationValid)
        {
            if (registrationValid)
            {
                VehicleDetails.SetRegistrationStatusForVehicle(vehicle, EVehicleDetailsStatus.Valid);
            }
            else
            {
                if (TrafficPolicerHandler.rnd.Next(5) < 2)
                {
                    VehicleDetails.SetRegistrationStatusForVehicle(vehicle, EVehicleDetailsStatus.None);
                }
                else
                {
                    VehicleDetails.SetRegistrationStatusForVehicle(vehicle, EVehicleDetailsStatus.Expired);
                }
            }
        }

        /// <summary>
        /// Sets the registration status for a vehicle. Used when vehicle is checked.
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="RegistrationStatus"></param>
        public static void SetVehicleRegistrationStatus(Vehicle vehicle, EVehicleDetailsStatus RegistrationStatus)
        {
            VehicleDetails.SetRegistrationStatusForVehicle(vehicle, RegistrationStatus);
        }

        /// <summary>
        /// Gets the registration status for a vehicle. Used when vehicle is checked.
        /// </summary>
        /// <param name="veh"></param>
        /// <returns></returns>
        public static EVehicleDetailsStatus GetVehicleRegistrationStatus(Vehicle veh)
        {
            return VehicleDetails.GetRegistrationStatusForVehicle(veh);
        }

        /// <summary>
        /// Gets the insurance status for a vehicle. Used when vehicle is checked.
        /// </summary>
        /// <param name="veh"></param>
        /// <returns></returns>
        public static EVehicleDetailsStatus GetVehicleInsuranceStatus(Vehicle veh)
        {
            return VehicleDetails.GetInsuranceStatusForVehicle(veh);
        }

        /// <summary>
        /// Sets the drug levels for the ped. Used by Traffic Policer's Drugalyzer.
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="Cannabis"></param>
        /// <param name="Cocaine"></param>
        public static void SetPedDrugsLevels(Ped ped, bool Cannabis, bool Cocaine)
        {
            DrugTestKit.SetPedDrugsLevels(ped, Cannabis, Cocaine);
        }
        /// <summary>
        /// Prevents this ped from being taken over by a Traffic Policer ambient event.
        /// </summary>
        /// <param name="ped"></param>
        public static void MakePedImmuneToAmbientEvents(Ped ped)
        {
            if (ped.Exists())
            {
                TrafficPolicerHandler.driversConsidered.Add(ped);
            }
            else
            {
                Game.LogTrivial("Traffic Policer API - MakePedImmuneToAmbientEvents ped doesn't exist.");
            }
        }




        /// <summary>
        /// Use this only if you don't want the vehicle details to appear after typing in a licence plate in a custom window. Remember to reactivate this after you're done fetching the input.
        /// </summary>
        /// <param name="enabled"></param>
        public static void SetAutomaticVehicleDeatilsChecksEnabled(bool enabled)
        {
            Game.LogTrivial("Traffic Policer API: Assembly " + Assembly.GetCallingAssembly().GetName().Name + " setting automatic vehicle details checks to: " + enabled.ToString());
            VehicleDetails.AutomaticDetailsChecksEnabled = enabled;
        }

        /// <summary>
        /// Sets the alcohol level for the ped. Used by Traffic Policer's Breathalyzer. Automatically converts the AlcoholLevel to an appropriate reading depending on the player's personal alcohol limit.
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="AlcoholLevel"></param>
        public static void SetPedAlcoholLevel(Ped ped, AlcoholLevels AlcoholLevel)
        {
            Breathalyzer.SetPedAlcoholLevels(ped, AlcoholLevel);
        }

        /// <summary>
        /// Sets whether the ped is over the alcohol limit or not.
        /// </summary>
        /// <param name="ped"></param>
        /// <param name="OverTheAlcoholLimit">If true, sets ped as over the limit. If false, sets ped as under the limit.</param>
        public static void SetPedAlcoholLevel(Ped ped, bool OverTheAlcoholLimit)
        {
            if (OverTheAlcoholLimit)
            {
                Breathalyzer.SetPedAlcoholLevels(ped, Breathalyzer.GetRandomOverTheLimitAlcoholLevel());
            }
            else
            {
                Breathalyzer.SetPedAlcoholLevels(ped, Breathalyzer.GetRandomUnderTheLimitAlcoholLevel());
            }
        }

        /// <summary>
        /// Returns a random alcohol level that's over the limit. Higher alcohol limits have a lower chance of being returned.
        /// </summary>
        /// <returns></returns>
        public static AlcoholLevels GetRandomOverTheLimitAlcoholLevel()
        {
            return Breathalyzer.GetRandomOverTheLimitAlcoholLevel();
        }

        /// <summary>
        /// Returns a random alcohol level that's under the limit. Higher alcohol limits have a lower chance of being returned.
        /// </summary>
        /// <returns></returns>
        public static AlcoholLevels GetRandomUnderTheLimitAlcoholLevel()
        {
            return Breathalyzer.GetRandomUnderTheLimitAlcoholLevel();
        }

        /// <summary>
        /// Returns true if ped has cocaine or cannabis in their system.
        /// </summary>
        /// <param name="ped"></param>
        /// <returns></returns>
        public static bool DoesPedHaveDrugsInSystem(Ped ped)
        {
            return DrugTestKit.DoesPedHaveDrugsInSystem(ped);
        }
        /// <summary>
        /// Returns true if ped is over the alcohol limit.
        /// </summary>
        /// <param name="ped"></param>
        /// <returns></returns>
        public static bool IsPedOverTheAlcoholLimit(Ped ped)
        {
            return Breathalyzer.IsPedOverTheLimit(ped);
        }

        


        

    }
}
