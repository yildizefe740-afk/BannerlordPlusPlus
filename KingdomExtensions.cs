using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerlordPlusPlus
{
    public static class KingdomExtensions
    {
        public static int TaxAmount(this Kingdom kingdom)
        {
            if (kingdom == null) return 0;

            int totalTax = 0;

            foreach (Clan clan in kingdom.Clans)
            {
                if (clan.IsEliminated) continue;

                foreach (Settlement settlement in clan.Settlements)
                {
                    if (settlement.IsTown || settlement.IsCastle)
                    {
                        totalTax += (int)Campaign.Current.Models.SettlementTaxModel.CalculateTownTax(settlement.Town).ResultNumber;
                    }
                }
            }

            return totalTax;
        }
    }
}