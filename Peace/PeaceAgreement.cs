using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace BannerlordPlusPlus
{
    public class PeaceAgreement
    {
        [SaveableProperty(1)]
        public Kingdom WinnerKingdom { get; set; }
        
        [SaveableProperty(2)]
        public Kingdom LooserKingdom { get; set; }
        
        [SaveableProperty(3)]
        public PeaceDealOptions SelectedOptions { get; set; }
        
        [SaveableProperty(6)]
        public int WarReparationAmount { get; set; }
        
        [SaveableProperty(5)]
        public Settlement CededSettlement { get; set; }
        public SubjectType TargetSubjectType;

        // Truce duration tracking.
        
        [SaveableProperty(7)]
        public double TruceDurationDays { get; set; }
        
        [SaveableProperty(8)]
        public double CreationDay { get; set; }

        public PeaceAgreement()
        {
    
        }

        // Three-parameter constructor used by CustomPeaceBehavior.
        public PeaceAgreement(Kingdom k1, Kingdom k2, double truceDurationDays)
        {
            WinnerKingdom = k1;
            LooserKingdom = k2;
            TruceDurationDays = truceDurationDays;
            CreationDay = CampaignTime.Now.ToDays;
        }

        // IsExpired and Involves checks.
        public bool IsExpired => (CampaignTime.Now.ToDays) >= (CreationDay + TruceDurationDays);

        public bool Involves(Kingdom k1, Kingdom k2)
        {
            if (k1 == null || k2 == null) return false;
            return (WinnerKingdom == k1 && LooserKingdom == k2) || (WinnerKingdom == k2 && LooserKingdom == k1);
        }
    }
}