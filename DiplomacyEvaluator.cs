using TaleWorlds.CampaignSystem;

namespace BannerlordPlusPlus
{
    public static class DiplomacyEvaluator
    {
        public enum ProposalType
        {
            TradeAgreement,
            DefensivePact,
            NonAggressionPact
        }

        public static bool EvaluateProposal(Kingdom proposer, Kingdom target, ProposalType type, out string reason)
        {
            reason = string.Empty;

            if (proposer == null || target == null)
            {
                reason = "Kingdom does not exist.";
                return false;
            }

            // Rule 1: Cannot propose pacts while at war
            if (proposer.IsAtWarWith(target))
            {
                reason = "Are you out of your mind? We are already at war!";
                return false;
            }

            // Rule 2: Active Truce Check (Exploit prevention)
            var peaceBehavior = CustomPeaceBehavior.Instance;
            if (peaceBehavior != null && peaceBehavior.HasActiveTruce(proposer, target))
            {
                reason = "We are bound by an active truce and cannot make an agreement right now.";
                return false;
            }

            // --- Score-Based AI Evaluation ---
            float score = 0f;

            // Clan Relations (-100 to +100)
            float relation = target.RulingClan.GetRelationWithClan(proposer.RulingClan);
            score += relation * 0.5f; // Max +50 or -50 points

            // Power Ratio Balance
            float proposerPower = proposer.CurrentTotalStrength;
            float targetPower = target.CurrentTotalStrength + 1f;
            float powerRatio = proposerPower / targetPower;

            if (powerRatio < 0.3f)
            {
                score -= 35f; // Severe penalty if kingdom is too weak
            }
            else if (powerRatio > 1.2f)
            {
                score += 15f; // Bonus if kingdom is strong or well-matched
            }

            // Threshold based on proposal type
            float requiredScore = (type == ProposalType.DefensivePact) ? 25f : 10f;

            if (score >= requiredScore)
            {
                reason = "Proposal accepted.";
                return true;
            }

            // Rejection Reasons
            if (powerRatio < 0.3f)
            {
                reason = "Your kingdom is far too weak to be considered a worthy partner.";
            }
            else if (relation < 0)
            {
                reason = "Due to our strained relations, our lords refuse to entertain this deal.";
            }
            else
            {
                reason = "Our council does not find this agreement advantageous at this time.";
            }

            return false;
        }
    }
}