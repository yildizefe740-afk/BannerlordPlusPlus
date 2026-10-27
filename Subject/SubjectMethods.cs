using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace BannerlordPlusPlus
{
    public class SubjectMethods
    {
        public static int GetTax(Kingdom subjectKingdom, SubjectType subjectType, float taxRate = 0.10f)
        {
            if (subjectKingdom == null ||
                (subjectType != SubjectType.Tributary && subjectType != SubjectType.Vassal) ||
                taxRate <= 0f)
                return 0;

            int baseKingdomTax = KingdomExtensions.TaxAmount(subjectKingdom);
            return Math.Max(0, (int)(baseKingdomTax * taxRate));
        }

        public static void CallToWar(IFaction senior, Kingdom junior, IFaction enemy)
        {
            if (senior == null || junior == null || enemy == null)
                return;

            SubjectAttributes subject = SubjectBehavior.GetControlledSubject(junior);
            if (subject == null || subject.SeniorKingdom != senior)
                return;

            if (senior.IsAtWarWith(enemy) && !junior.IsAtWarWith(enemy))
            {
                DeclareWarAction.ApplyByDefault(junior, enemy);
            }
        }
    }
}