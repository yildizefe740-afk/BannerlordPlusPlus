using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Actions;

namespace BannerlordPlusPlus
{
    public class SubjectMethods
    {
        public static int GetTax(Kingdom subjectKingdom, float taxRate = 0.10f)
        {
            if(subjectKingdom == null) return 0;

            int baseKingdomTax = KingdomExtensions.TaxAmount(subjectKingdom);

            int subjectTaxAmount = (int)(baseKingdomTax * taxRate);

            return subjectTaxAmount;
        }


        public static void CallToWar(IFaction Senior, Kingdom Junior, IFaction sacrifice, DeclareWarAction.DeclareWarDetail detail)
        {
            if(Senior == null || Junior == null || sacrifice == null) return;

            if (Senior.IsAtWarWith(sacrifice) && !Junior.IsAtWarWith(sacrifice))
            {
                DeclareWarAction.ApplyByDefault(Junior, sacrifice);
            }
        }
    }
}