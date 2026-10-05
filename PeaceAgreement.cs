using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerlordPlusPlus
{
    public class PeaceAgreement
    {
        public Kingdom WinnerKingdom { get; set; }
        public Kingdom LooserKingdom { get; set; }
        public PeaceDealOptions SelectedOptions { get; set; }
        public int WarReparationAmount { get; set; }
        public Settlement CededSettlement { get; set; }
        public SubjectType TargetSubjectType;

        // Ateşkes süre takibi
        public double TruceDurationDays { get; set; }
        public double CreationDay { get; set; }

        public PeaceAgreement()
        {
            CreationDay = CampaignTime.Now.ToDays;
        }

        // CustomPeaceBehavior'ın aradığı 3 parametreli constructor
        public PeaceAgreement(Kingdom k1, Kingdom k2, double truceDurationDays)
        {
            WinnerKingdom = k1;
            LooserKingdom = k2;
            TruceDurationDays = truceDurationDays;
            CreationDay = CampaignTime.Now.ToDays;
        }

        // IsExpired ve Involves kontrol metodlari
        public bool IsExpired => (CampaignTime.Now.ToDays) >= (CreationDay + TruceDurationDays);

        public bool Involves(Kingdom k1, Kingdom k2)
        {
            if (k1 == null || k2 == null) return false;
            return (WinnerKingdom == k1 && LooserKingdom == k2) || (WinnerKingdom == k2 && LooserKingdom == k1);
        }
    }
}