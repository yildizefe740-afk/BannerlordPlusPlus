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
            // Bannerlord API'sinde doğru event dinleyici metodu: AddNonSerializedListener
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Save/Load data synchronization
        }

        private void OnDailyTick()
        {
            // Iterate through all active kingdoms to collect taxes from subjects
            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (kingdom == null || kingdom.IsEliminated) continue;

                // Obtain the senior/suzerain kingdom if this kingdom is a subject
                Kingdom seniorKingdom = GetSeniorKingdom(kingdom);

                if (seniorKingdom != null && !seniorKingdom.IsEliminated)
                {
                    int taxAmount = SubjectMethods.GetTax(kingdom, 0.10f);

                    if (taxAmount > 0)
                    {
                        // Deduct tax from subject treasury and add to senior kingdom treasury
                        AddToTreasury(kingdom, -taxAmount);
                        AddToTreasury(seniorKingdom, taxAmount);
                    }
                }
            }
        }

        private Kingdom GetSeniorKingdom(Kingdom subjectKingdom)
        {
            // TODO: Connect your diplomacy/subject relationship management logic here
            return null;
        }

        // --- TREASURY HELPER METHODS ---
        public static int GetTreasury(Kingdom kingdom)
        {
            if (kingdom == null) return 0;
            return kingdom.KingdomBudgetWallet;
        }

        public static void AddToTreasury(Kingdom kingdom, int amount)
        {
            if (kingdom == null) return;
            kingdom.KingdomBudgetWallet += amount;
            
            // Prevent treasury from dropping below zero
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

                    TextObject taxText = new("{=caravan_tax}Market/Caravan Tax 15%");
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
                return false; 
            }
        }

        public static void ShowTreasuryInfo(Kingdom kingdom)
        {
            if (kingdom == null) return;

            int treasury = GetTreasury(kingdom);

            InformationManager.ShowInquiry(new InquiryData(
                new TextObject("{=treasury_title}Kingdom Treasury").ToString(),
                new TextObject($"{kingdom.Name} Treasury Balance: {treasury:N0} Denars\n\n(15% trade tax collected from market caravans is deposited directly into this vault.)").ToString(),
                true,
                false,
                new TextObject("Confirm").ToString(),
                null,
                null,
                null
            ));
        }
    }
}