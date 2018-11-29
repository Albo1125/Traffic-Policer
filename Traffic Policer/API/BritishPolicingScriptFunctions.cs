using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using British_Policing_Script;

namespace Traffic_Policer.API
{
    internal static class BritishPolicingScriptFunctions
    {

        public static void CreateNewCourtCase(Ped ped, string Crime, bool PleadGuilty, string CourtVerdict)
        {

            British_Policing_Script.API.Functions.CreateNewCourtCase(British_Policing_Script.API.Functions.GetBritishPersona(ped), Crime, 100, CourtVerdict);
        }

        
        
    }
}
