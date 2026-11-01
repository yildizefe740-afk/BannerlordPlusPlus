using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Library;

namespace BannerlordPlusPlus
{
    [HarmonyPatch(typeof(MakePeaceKingdomDecision), "DetermineSupport")]
    internal static class PeaceProposalSupportPatch
    {
        private static void Postfix(
            MakePeaceKingdomDecision __instance,
            Clan clan,
            DecisionOutcome possibleOutcome,
            ref float __result)
        {
            if (!(possibleOutcome is MakePeaceKingdomDecision.MakePeaceDecisionOutcome outcome) ||
                !outcome.ShouldPeaceBeDeclared)
            {
                return;
            }

            CustomPeaceBehavior behavior = CustomPeaceBehavior.Instance;
            if (behavior == null)
                return;

            float adjustedSupport =
                __result + behavior.EvaluatePeaceTermsForAi(__instance, clan);
            __result = MBMath.ClampFloat(adjustedSupport, 0f, 1f);
        }
    }

    [HarmonyPatch(typeof(MakePeaceKingdomDecision), "ApplyChosenOutcome")]
    internal static class PeaceProposalResolutionPatch
    {
        private static void Postfix(
            MakePeaceKingdomDecision __instance,
            DecisionOutcome chosenOutcome)
        {
            if (!(chosenOutcome is MakePeaceKingdomDecision.MakePeaceDecisionOutcome outcome))
                return;

            CustomPeaceBehavior.Instance?.ResolvePeaceProposal(
                __instance,
                outcome.ShouldPeaceBeDeclared);
        }
    }
}
