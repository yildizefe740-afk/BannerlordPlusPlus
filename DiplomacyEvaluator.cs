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
                reason = "Geçersiz krallık bilgisi.";
                return false;
            }

            // 1. Kural: Savaş halindeyken pakt teklif edilemez
            if (proposer.IsAtWarWith(target))
            {
                reason = "Halihazırda savaş halindeyiz!";
                return false;
            }

            // 2. Kural: Taze Ateşkes Kontrolü (Döv-sal / exploit engeli)
            var peaceBehavior = CustomPeaceBehavior.Instance;
            if (peaceBehavior != null && peaceBehavior.HasActiveTruce(proposer, target))
            {
                reason = "Savaştan henüz yeni çıktık. Krallık meclisimiz şu an bir paktı onaylamıyor.";
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
                reason = "Krallığınızı bizimle eşit bir diplomatik muhatap olarak görecek kadar güçlü bulmuyoruz.";
            }
            else if (relation < 0)
            {
                reason = "Klanlarımız arasındaki soğuk ilişkiler bu anlaşmaya engel oluyor.";
            }
            else
            {
                reason = "Krallık konseyimiz bu teklifi mevcut konjonktürde stratejik olarak uygun bulmuyor.";
            }

            return false;
        }
    }
}