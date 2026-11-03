using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace BannerlordPlusPlus
{
    public sealed class AiPeaceProposalBehavior : CampaignBehaviorBase
    {
        private const float OfferStrengthRatio = 0.72f;
        private const float DemandStrengthRatio = 1.38f;
        private const double ProposalCooldownDays = 30.0;

        private Dictionary<string, double> _nextProposalDays = new();

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("BannerlordPlusPlus_AiPeaceProposalCooldowns", ref _nextProposalDays);
            _nextProposalDays ??= new Dictionary<string, double>();
        }

        private void OnDailyTick()
        {
            CustomPeaceBehavior peaceBehavior = CustomPeaceBehavior.Instance;
            if (peaceBehavior == null || Campaign.Current == null)
                return;

            Kingdom[] kingdoms = Kingdom.All
                .Where(kingdom =>
                    kingdom != null &&
                    !kingdom.IsEliminated &&
                    kingdom.RulingClan != null)
                .OrderBy(kingdom => kingdom.StringId, StringComparer.Ordinal)
                .ToArray();
            Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
            double currentDay = CampaignTime.Now.ToDays;

            for (int firstIndex = 0; firstIndex < kingdoms.Length; firstIndex++)
            {
                Kingdom first = kingdoms[firstIndex];
                for (int secondIndex = firstIndex + 1; secondIndex < kingdoms.Length; secondIndex++)
                {
                    Kingdom second = kingdoms[secondIndex];
                    if (!first.IsAtWarWith(second) ||
                        peaceBehavior.HasPendingProposal(first, second))
                    {
                        continue;
                    }

                    string pairKey = GetPairKey(first, second);
                    if (_nextProposalDays.TryGetValue(pairKey, out double nextProposalDay) &&
                        currentDay < nextProposalDay)
                    {
                        continue;
                    }

                    if (!TryBuildProposal(first, second, playerKingdom, out PeaceProposalTerms terms))
                        continue;

                    if (!peaceBehavior.SubmitPeaceProposal(terms))
                        continue;

                    _nextProposalDays[pairKey] = currentDay + ProposalCooldownDays;
                    return;
                }
            }
        }

        private static bool TryBuildProposal(
            Kingdom first,
            Kingdom second,
            Kingdom playerKingdom,
            out PeaceProposalTerms terms)
        {
            terms = null;
            float firstStrength = Math.Max(1f, first.CurrentTotalStrength);
            float secondStrength = Math.Max(1f, second.CurrentTotalStrength);
            float ratio = firstStrength / secondStrength;

            Kingdom weakerKingdom;
            Kingdom strongerKingdom;
            if (ratio <= OfferStrengthRatio)
            {
                weakerKingdom = first;
                strongerKingdom = second;
            }
            else if (ratio >= DemandStrengthRatio)
            {
                weakerKingdom = second;
                strongerKingdom = first;
            }
            else
            {
                return false;
            }

            Kingdom proposer;
            Kingdom target;
            bool isOffer;
            if (weakerKingdom != playerKingdom)
            {
                proposer = weakerKingdom;
                target = strongerKingdom;
                isOffer = true;
            }
            else if (strongerKingdom != playerKingdom)
            {
                proposer = strongerKingdom;
                target = weakerKingdom;
                isOffer = false;
            }
            else
            {
                return false;
            }

            if (proposer.RulingClan == null || target.RulingClan == null)
                return false;

            Kingdom payingKingdom = isOffer ? proposer : target;
            int dailyTribute = Math.Min(
                300,
                Math.Max(0, TreasuryBehavior.GetTreasury(payingKingdom) / 30));

            terms = new PeaceProposalTerms
            {
                ProposerKingdom = proposer,
                TargetKingdom = target,
                SelectedOptions = PeaceDealOptions.Truce,
                DailyTributeFromTargetToProposer = isOffer ? -dailyTribute : dailyTribute,
                DailyTributeDurationDays = 30,
                IsOffer = isOffer,
                IsAiGenerated = true
            };
            return true;
        }

        private static string GetPairKey(Kingdom kingdom1, Kingdom kingdom2)
        {
            return string.CompareOrdinal(kingdom1.StringId, kingdom2.StringId) < 0
                ? $"{kingdom1.StringId}:{kingdom2.StringId}"
                : $"{kingdom2.StringId}:{kingdom1.StringId}";
        }
    }
}
