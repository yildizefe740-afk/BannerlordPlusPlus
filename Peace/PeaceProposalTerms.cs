using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace BannerlordPlusPlus
{
    public class PeaceProposalTerms
    {
        [SaveableField(1)]
        public Kingdom ProposerKingdom;

        [SaveableField(2)]
        public Kingdom TargetKingdom;

        [SaveableField(3)]
        public PeaceDealOptions SelectedOptions;

        [SaveableField(4)]
        public int DailyTributeFromTargetToProposer;

        [SaveableField(5)]
        public int DailyTributeDurationDays;

        [SaveableField(6)]
        public Settlement CededSettlement;

        [SaveableField(7)]
        public Settlement ProposerSettlementToExchange;

        [SaveableField(8)]
        public Settlement TargetSettlementToExchange;

        [SaveableField(9)]
        public SubjectType TargetSubjectType;

        [SaveableField(10)]
        public float SubmittedAtDays;

        [SaveableField(11)]
        public bool IsOffer;

        [SaveableField(12)]
        public float LastResolutionAttemptDays;

        public PeaceProposalTerms()
        {
        }

        public bool Involves(Kingdom kingdom1, Kingdom kingdom2)
        {
            return (ProposerKingdom == kingdom1 && TargetKingdom == kingdom2) ||
                   (ProposerKingdom == kingdom2 && TargetKingdom == kingdom1);
        }
    }
}
