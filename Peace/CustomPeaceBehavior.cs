using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BannerlordPlusPlus
{
    public class CustomPeaceBehavior : CampaignBehaviorBase
    {
        public static CustomPeaceBehavior Instance { get; private set; }

        private List<PeaceAgreement> _activeTruces = new();
        private Dictionary<string, string> _reparationTargets = new(); // WinnerId -> LoserId
        private List<PeaceProposalTerms> _pendingProposals = new(); // WinnerId -> LoserId

        public CustomPeaceBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("ActiveTruces", ref _activeTruces);
            dataStore.SyncData("ReparationTargets", ref _reparationTargets);
            dataStore.SyncData("PendingPeaceProposals", ref _pendingProposals);

            _activeTruces ??= new List<PeaceAgreement>();
            _reparationTargets ??= new Dictionary<string, string>();
            _pendingProposals ??= new List<PeaceProposalTerms>();
        }

        public bool HasActiveTruce(Kingdom k1, Kingdom k2)
        {
            return k1 != null &&
                   k2 != null &&
                   _activeTruces != null &&
                   _activeTruces.Any(a => a != null && a.Involves(k1, k2) && !a.IsExpired);
        }

        public bool AddTruce(Kingdom k1, Kingdom k2, double durationDays)
        {
            if (k1 == null || k2 == null || k1 == k2 || durationDays <= 0)
                return false;

            _activeTruces ??= new List<PeaceAgreement>();
            _activeTruces.RemoveAll(a => a == null || a.Involves(k1, k2));
            _activeTruces.Add(new PeaceAgreement(k1, k2, durationDays));
            return true;
        }

        public bool SubmitPeaceProposal(PeaceProposalTerms terms)
        {
            if (terms?.ProposerKingdom == null ||
                terms.TargetKingdom == null ||
                terms.ProposerKingdom == terms.TargetKingdom ||
                !terms.ProposerKingdom.IsAtWarWith(terms.TargetKingdom))
            {
                return false;
            }

            Clan targetRulingClan = terms.TargetKingdom.RulingClan;
            if (targetRulingClan == null || targetRulingClan.Kingdom != terms.TargetKingdom)
                return false;

            _pendingProposals ??= new List<PeaceProposalTerms>();
            if (_pendingProposals.Any(proposal =>
                    proposal != null &&
                    proposal.Involves(terms.ProposerKingdom, terms.TargetKingdom)))
            {
                return false;
            }

            _pendingProposals.Add(terms);

            var decision = new MakePeaceKingdomDecision(
                targetRulingClan,
                terms.ProposerKingdom,
                terms.DailyTributeToTarget,
                terms.DailyTributeDurationDays,
                applyResults: true,
                isProposedByOpponent: true);

            terms.TargetKingdom.AddDecision(decision, ignoreInfluenceCost: true);
            return true;
        }

        public PeaceProposalTerms GetPendingProposal(MakePeaceKingdomDecision decision)
        {
            if (decision?.Kingdom == null ||
                !(decision.FactionToMakePeaceWith is Kingdom opponent) ||
                _pendingProposals == null)
            {
                return null;
            }

            return _pendingProposals.FirstOrDefault(proposal =>
                proposal != null &&
                proposal.ProposerKingdom == opponent &&
                proposal.TargetKingdom == decision.Kingdom &&
                proposal.DailyTributeToTarget == decision.DailyTributeToBePaid &&
                proposal.DailyTributeDurationDays == decision.DailyTributeDurationInDays);
        }

        public float EvaluatePeaceTermsForAi(
            MakePeaceKingdomDecision decision,
            Clan votingClan)
        {
            PeaceProposalTerms proposal = GetPendingProposal(decision);
            if (proposal == null)
                return 0f;

            Kingdom recipient = proposal.TargetKingdom;
            float supportModifier = 0f;

            if (proposal.SelectedOptions.HasFlag(PeaceDealOptions.WarReparation))
                supportModifier -= 0.12f;

            if (proposal.SelectedOptions.HasFlag(PeaceDealOptions.PlunderTreasury))
            {
                int treasury = Math.Max(0, TreasuryBehavior.GetTreasury(recipient));
                supportModifier -= Math.Min(0.30f, treasury / 100000f);
            }

            if (proposal.SelectedOptions.HasFlag(PeaceDealOptions.Subjectize))
                supportModifier -= 0.40f;

            if (proposal.SelectedOptions.HasFlag(PeaceDealOptions.ExecuteRuler))
                supportModifier -= 0.60f;

            Settlement cededSettlement = proposal.CededSettlement;
            if (proposal.SelectedOptions.HasFlag(PeaceDealOptions.CededSettlement) &&
                cededSettlement != null)
            {
                supportModifier -= 0.18f;
                if (votingClan != null && cededSettlement.OwnerClan == votingClan)
                    supportModifier -= 0.20f;
            }

            if (proposal.SelectedOptions.HasFlag(PeaceDealOptions.TerritoryExchange))
            {
                supportModifier -= 0.08f;
                if (proposal.TargetSettlementToExchange != null &&
                    votingClan != null &&
                    proposal.TargetSettlementToExchange.OwnerClan == votingClan)
                {
                    supportModifier -= 0.15f;
                }
            }

            if (proposal.SelectedOptions.HasFlag(PeaceDealOptions.ClaimThrone))
                supportModifier -= 0.55f;

            if (proposal.SelectedOptions.HasFlag(PeaceDealOptions.GainIndependence))
            {
                SubjectAttributes subject = SubjectBehavior.GetSubjectBetween(
                    proposal.ProposerKingdom,
                    proposal.TargetKingdom);
                supportModifier += subject?.JuniorKingdom == recipient ? 0.30f : -0.20f;
            }

            if (proposal.SelectedOptions.HasFlag(PeaceDealOptions.Truce))
                supportModifier += 0.03f;

            return supportModifier;
        }

        public void ResolvePeaceProposal(
            MakePeaceKingdomDecision decision,
            bool accepted)
        {
            PeaceProposalTerms proposal = GetPendingProposal(decision);
            if (proposal == null)
                return;

            _pendingProposals.Remove(proposal);
            bool peaceWasMade = !proposal.ProposerKingdom.IsAtWarWith(proposal.TargetKingdom);
            if (!accepted || !peaceWasMade)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{proposal.TargetKingdom.Name} rejected the peace proposal.",
                    Colors.Yellow));
                return;
            }

            ApplyAcceptedPeaceTerms(proposal);
        }

        private void ApplyAcceptedPeaceTerms(PeaceProposalTerms proposal)
        {
            Kingdom proposer = proposal.ProposerKingdom;
            Kingdom target = proposal.TargetKingdom;
            PeaceDealOptions options = proposal.SelectedOptions;

            AddTruce(proposer, target, options.HasFlag(PeaceDealOptions.Truce) ? 200.0 : 100.0);

            if (options.HasFlag(PeaceDealOptions.PlunderTreasury))
            {
                int amount = Math.Max(0, TreasuryBehavior.GetTreasury(target));
                TreasuryBehavior.AddToTreasury(target, -amount);
                TreasuryBehavior.AddToTreasury(proposer, amount);
            }

            if (options.HasFlag(PeaceDealOptions.WarReparation))
                _reparationTargets[proposer.StringId] = target.StringId;

            if (options.HasFlag(PeaceDealOptions.Subjectize))
                SubjectBehavior.Instance?.AddSubject(proposer, target, proposal.TargetSubjectType);

            if (options.HasFlag(PeaceDealOptions.CededSettlement) &&
                IsSettlementOwnedBy(proposal.CededSettlement, target))
            {
                ChangeOwnerOfSettlementAction.ApplyByBarter(
                    proposer.Leader,
                    proposal.CededSettlement);
            }

            if (options.HasFlag(PeaceDealOptions.TerritoryExchange) &&
                IsSettlementOwnedBy(proposal.TargetSettlementToExchange, target) &&
                IsSettlementOwnedBy(proposal.ProposerSettlementToExchange, proposer))
            {
                ChangeOwnerOfSettlementAction.ApplyByBarter(
                    proposer.Leader,
                    proposal.TargetSettlementToExchange);
                ChangeOwnerOfSettlementAction.ApplyByBarter(
                    target.Leader,
                    proposal.ProposerSettlementToExchange);
            }

            if (options.HasFlag(PeaceDealOptions.ClaimThrone) &&
                proposer.RulingClan != null)
            {
                ChangeRulingClanAction.Apply(target, proposer.RulingClan);
            }

            if (options.HasFlag(PeaceDealOptions.GainIndependence))
            {
                SubjectAttributes subject =
                    SubjectBehavior.GetSubjectBetween(proposer, target);
                if (subject != null)
                {
                    SubjectBehavior.Instance?.EndSubject(
                        subject.SeniorKingdom,
                        subject.JuniorKingdom,
                        "Peace agreement");
                }
            }

            if (options.HasFlag(PeaceDealOptions.ExecuteRuler) &&
                target.Leader != null &&
                target.Leader.IsAlive)
            {
                KillCharacterAction.ApplyByExecution(target.Leader, proposer.Leader);
            }

            InformationManager.DisplayMessage(new InformationMessage(
                $"Peace accepted by {target.Name}. The agreed treaty terms have taken effect.",
                Colors.Green));
        }

        private static bool IsSettlementOwnedBy(Settlement settlement, Kingdom kingdom)
        {
            return settlement != null &&
                   kingdom != null &&
                   settlement.OwnerClan?.Kingdom == kingdom;
        }

        private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
        {
            if (faction1 is Kingdom k1 && faction2 is Kingdom k2)
            {
                if (HasActiveTruce(k1, k2))
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"The truce between {k1.Name} and {k2.Name} has not ended yet!", Colors.Red));
                }
            }
        }

        private void OnDailyTick()
        {
            _activeTruces.RemoveAll(a => a == null || a.IsExpired);

            List<string> winnerIds = _reparationTargets.Keys.ToList();
            foreach (var winnerId in winnerIds)
            {
                string loserId = _reparationTargets[winnerId];
                Kingdom winner = Kingdom.All.FirstOrDefault(k => k.StringId == winnerId);
                Kingdom loser = Kingdom.All.FirstOrDefault(k => k.StringId == loserId);

                // Remove the entry if either kingdom no longer exists or is null.
                if (winner == null || loser == null || winner.IsEliminated || loser.IsEliminated)
                {
                    _reparationTargets.Remove(winnerId);
                    continue;
                }

                if (!HasActiveTruce(winner, loser))
                {
                    _reparationTargets.Remove(winnerId);
                    continue;
                }

                int dailyIncome = 0;
                foreach (var settlement in loser.Settlements)
                {
                    if (settlement != null)
                    {
                        dailyIncome += (int)(settlement.Town?.TradeTaxAccumulated ?? 100) + 200;
                    }
                }

                int reparationAmount = (int)(dailyIncome * 0.10f);

                TreasuryBehavior.AddToTreasury(loser, -reparationAmount);
                TreasuryBehavior.AddToTreasury(winner, reparationAmount);
            }
        }
    }
}