using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace BannerlordPlusPlus
{
    public class SubjectMethods
    {
        public int GetTax(Kingdom subjectKingdom, float taxRate = 0.10f)
        {
            if(subjectKingdom == null) return 0;

            int baseKingdomTax = KingdomExtensions.TaxAmount(subjectKingdom);

            int subjectTaxAmount = (int)(baseKingdomTax * taxRate);

            return subjectTaxAmount;
        }


        public static void CallToWar(Kingdom Senior, Kingdom Junior, Kingdom k1)
        {
            if(Senior == null && Junior == null && k1 == null) return;

            
        }


    }
}