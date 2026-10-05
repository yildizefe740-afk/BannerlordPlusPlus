using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;  
using TaleWorlds.Library;
using HarmonyLib;

namespace BannerlordPlusPlus
{
    public class TreasuryBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            // Event listener register
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Save/Load sync
        }

        // --- HAZİNE HELPER METODLARI ---
        public static int GetTreasury(Kingdom kingdom)
        {
            if (kingdom == null) return 0;
            return kingdom.KingdomBudgetWallet;
        }

        public static void AddToTreasury(Kingdom kingdom, int amount)
        {
            if (kingdom == null) return;
            kingdom.KingdomBudgetWallet += amount;
            
            // Sıfırın altına düşmesini engelle
            if (kingdom.KingdomBudgetWallet < 0)
            {
                kingdom.KingdomBudgetWallet = 0;
            }
        }

        // --- CARAVAN TAX PATCH ---
        [HarmonyPatch(typeof(DefaultClanFinanceModel), "AddIncomeFromParty")]
        public class CaravanIncomeTaxPatch
        {
            public static void Postfix(MobileParty party, Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals, ref int __result)
            {
                if (party == null || !party.IsCaravan || clan?.Kingdom == null)
                    return;

                int dailyProfit = __result;

                if (dailyProfit > 0)
                {
                    int taxAmount = (int)(dailyProfit * 0.15f);
                    __result -= taxAmount;

                    TextObject taxText = new("{=caravan_tax}Market/Caravan Tax %15");
                    goldChange.Add(-taxAmount, taxText);

                    if (applyWithdrawals && clan.Kingdom != null)
                    {
                        AddToTreasury(clan.Kingdom, taxAmount);
                    }
                }
            }
        }

        // --- BLOCK DEFAULT BUDGET PATCH ---
        [HarmonyPatch(typeof(DefaultClanFinanceModel), "CalculateClanIncome")]
        public class BlockKingdomBudgetDistributionPatch
        {
            public static bool Prefix()
            {
                return true; 
            }
        }

        public static void ShowTreasuryInfo(Kingdom kingdom)
        {
            if (kingdom == null) return;

            int treasury = GetTreasury(kingdom);

            InformationManager.ShowInquiry(new InquiryData(
                new TextObject("{=treasury_title}Krallık Hazinesi").ToString(),
                new TextObject($"{kingdom.Name} Krallık Kasası: {treasury:N0} Dinar\n\n(Kervanlardan alınan %15 vergi bu cüzdana aktarılmaktadır.)").ToString(),
                true,
                false,
                new TextObject("Tamam").ToString(),
                null,
                null,
                null
            ));
        }
    }
}