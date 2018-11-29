using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using Rage.Native;
using Traffic_Policer.Impairment_Tests;

namespace Traffic_Policer.Extensions
{
    
    internal static class ExtensionMethods
    {
        public static string ToColouredString(this DrugsLevels e)
        {
            switch (e)
            {
                case DrugsLevels.NEGATIVE:
                    return "~g~NEGATIVE";
                case DrugsLevels.POSITIVE:
                    return "~r~POSITIVE";
            }
            return "UNDETECTABLE";
        }
        public static string ToColouredString(this EVehicleDetailsStatus e)
        {
            switch (e)
            {
                case EVehicleDetailsStatus.Valid:
                    return "~g~Valid";
                case EVehicleDetailsStatus.Expired:
                    return "~o~Expired";
                case EVehicleDetailsStatus.None:
                    return "~r~None";
            }
            return "UNDETECTABLE";
        }
        

        
    }
}
