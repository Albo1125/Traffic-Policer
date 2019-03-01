using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSPDFR_;
using Rage;
using LSPD_First_Response.Engine.Scripting.Entities;

namespace Traffic_Policer.API
{
    internal static class LSPDFRPlusFunctions
    {
        public static void CreateCourtCase(Persona defendant, string crime, int GuiltyChance, string verdict)
        {
            LSPDFR_.API.Functions.CreateNewCourtCase(defendant, crime, GuiltyChance, verdict);
        }

        public static string DetermineFineSentence(int MinFine, int MaxFine)
        {
            return LSPDFR_.API.Functions.DetermineFineSentence(MinFine, MaxFine);
        }

        public static string DeterminePrisonSentence(int MinMonths, int MaxMonths, int SuspendedChance)
        {
            return LSPDFR_.API.Functions.DeterminePrisonSentence(MinMonths, MaxMonths, SuspendedChance);
        }

        public static void AddCountToStatistic(string PluginName, string Statistic)
        {
            LSPDFR_.API.ProtectedFunctions.AddCountToStatistic(PluginName, Statistic);
        }

        public static void AddQuestionToTrafficStop(Ped suspect, string Question, string Answer)
        {
            LSPDFR_.API.Functions.AddQuestionToTrafficStop(suspect, Question, Answer);
        }

        public static void AddQuestionToTrafficStop(Ped suspect, string Question, List<string> Answers)
        {
            LSPDFR_.API.Functions.AddQuestionToTrafficStop(suspect, Question, Answers);
        }

        public static void AddQuestionToTrafficStop(Ped suspect, List<string> Questions, List<string> Answers)
        {
            LSPDFR_.API.Functions.AddQuestionToTrafficStop(suspect, Questions, Answers);
        }

        public static void AddQuestionToTrafficStop(Ped suspect, string Question, Func<Ped, string> Callback)
        {
            LSPDFR_.API.Functions.AddQuestionToTrafficStop(suspect, Question, Callback);
        }
        public static void HideStandardQuestions(Ped suspect, bool Hide)
        {
            LSPDFR_.API.Functions.HideStandardTrafficStopQuestions(suspect, Hide);
        }
    }
}
