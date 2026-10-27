using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace BannerlordPlusPlus
{
    [HarmonyPatch(typeof(PeaceOfferCampaignBehavior), "OnPeaceOffered")]
    public class GlobalPeaceOfferOverridePatch
    {
        // Returning false from the prefix prevents TaleWorlds' default tribute offer window from opening.
        public static bool Prefix(IFaction opponentFaction, int tributeAmount, int tributeDuration)
        {
            Kingdom playerKingdom = Clan.PlayerClan.Kingdom;
            Kingdom enemyKingdom = opponentFaction as Kingdom;

            // Use the default behavior if this is not a kingdom-level war (e.g., against bandits).
            if (playerKingdom == null || enemyKingdom == null) 
                return true;

            // --- ARBITRATION / BANNERLORD++ PEACE ENGINE ---
            // Prevent the game's ShowInquiry and tribute windows in OnPeaceOffered from opening.
            // Hand control over to our CustomPeaceBehavior.
            CustomPeaceBehavior.Instance?.ProcessGlobalPeaceRequest(playerKingdom, enemyKingdom, tributeAmount);

            // Return false to completely cancel TaleWorlds' own windows and tribute logic.
            return false; 
        }
    }
}