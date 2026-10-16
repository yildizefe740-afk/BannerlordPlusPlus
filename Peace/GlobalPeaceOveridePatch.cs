using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace BannerlordPlusPlus
{
    [HarmonyPatch(typeof(PeaceOfferCampaignBehavior), "OnPeaceOffered")]
    public class GlobalPeaceOfferOverridePatch
    {
        // Prefix 'false' döndüğünde TaleWorlds'ün varsayılan haraçlı teklif penceresi HİÇ AÇILMAZ!
        public static bool Prefix(IFaction opponentFaction, int tributeAmount, int tributeDuration)
        {
            Kingdom playerKingdom = Clan.PlayerClan.Kingdom;
            Kingdom enemyKingdom = opponentFaction as Kingdom;

            // Krallık seviyesinde bir savaş değilse (örneğin haydutlar) varsayılan mantık çalışsın
            if (playerKingdom == null || enemyKingdom == null) 
                return true;

            // --- TAHKİM / BANNERLORD++ BARIŞ MOTORU ---
            // Oyunun kendi OnPeaceOffered içindeki ShowInquiry / haraç pencerelerini tamamen engelle
            // Kontrolü bizim CustomPeaceBehavior'a devret
            CustomPeaceBehavior.Instance?.ProcessGlobalPeaceRequest(playerKingdom, enemyKingdom, tributeAmount);

            // 'false' döndürerek TaleWorlds'ün kendi pencerelerini ve haraç kodunu TAMAMEN İPTAL ET
            return false; 
        }
    }
}