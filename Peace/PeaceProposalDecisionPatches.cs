using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
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
            if (behavior == null || !behavior.HasPendingProposal(__instance))
                return;

            float normalizedSupport =
                __result / 200f + behavior.EvaluatePeaceTermsForAi(__instance, clan);
            __result = MBMath.ClampFloat(normalizedSupport, 0f, 1f);
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

    [HarmonyPatch(typeof(DeclareWarAction), "ApplyInternal")]
    internal static class PeaceTruceWarDeclarationPatch
    {
        private static bool Prefix(IFaction faction1, IFaction faction2)
        {
            CustomPeaceBehavior behavior = CustomPeaceBehavior.Instance;
            if (!(faction1 is Kingdom kingdom1) ||
                !(faction2 is Kingdom kingdom2) ||
                behavior == null ||
                !behavior.HasActiveTruce(kingdom1, kingdom2))
            {
                return true;
            }

            Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
            if (playerKingdom == kingdom1 || playerKingdom == kingdom2)
            {
                Kingdom otherKingdom = playerKingdom == kingdom1 ? kingdom2 : kingdom1;
                InformationManager.DisplayMessage(new InformationMessage(
                    $"A truce with {otherKingdom.Name} prevents this war declaration.",
                    Colors.Yellow));
            }

            return false;
        }
    }
}
