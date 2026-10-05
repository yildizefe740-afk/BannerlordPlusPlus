using TaleWorlds.CampaignSystem;

namespace BannerlordPlusPlus
{
    public static class DiplomacyEvaluator
    {
        public enum ProposalType
        {
            TradeAgreement,
            DefensivePact
        }

        public static bool EvaluateProposal(Kingdom proposer, Kingdom target, ProposalType type, out string reason)
        {
            reason = string.Empty;

            if (proposer == null || target == null)
            {
                reason = "Kingdom do not exist.";
                return false;
            }

            // 1. Kural: Savaş halindeyken pakt teklif edilemez
            if (proposer.IsAtWarWith(target))
            {
                reason = "Are you mad we are already fighting!";
                return false;
            }

            // 2. Kural: Taze Ateşkes Kontrolü (Döv-sal / exploit engeli)
            var peaceBehavior = CustomPeaceBehavior.Instance;
            if (peaceBehavior != null && peaceBehavior.HasActiveTruce(proposer, target))
            {
                reason = "We can not make an agreement.";
                return false;
            }

            // --- Skora Dayalı AI Değerlendirmesi ---
            float score = 0f;

            // Klan İlişkisi (-100 ile +100 arası)
            float relation = target.RulingClan.GetRelationWithClan(proposer.RulingClan);
            score += relation * 0.5f; // Maksimum +50 veya -50 puan

            // Güç Oranı (Askeri Güç Denge Hesabı)
            float proposerPower = proposer.CurrentTotalStrength;
            float targetPower = target.CurrentTotalStrength + 1f;
            float powerRatio = proposerPower / targetPower;

            if (powerRatio < 0.3f)
            {
                score -= 35f; // Krallık çok zayıfsa ciddi eksi puan
            }
            else if (powerRatio > 1.2f)
            {
                score += 15f; // Krallık güçlü veya dengeliyse artı puan
            }

            // Teklif Türüne Göre Kabul Eşiği (Threshold)
            float requiredScore = (type == ProposalType.DefensivePact) ? 25f : 10f;

            if (score >= requiredScore)
            {
                reason = "Anlaşma kabul edildi.";
                return true;
            }

            // Reddedilme Gerekçesini Belirleme
            if (powerRatio < 0.3f)
            {
                reason = "Your Kingdom is not a match for my kingdom.";
            }
            else if (relation < 0)
            {
                reason = "Because of Cold relations between our kingdom we can not make a deal.";
            }
            else
            {
                reason = "Our Lords dont want to make a deal.";
            }

            return false;
        }
    }
}