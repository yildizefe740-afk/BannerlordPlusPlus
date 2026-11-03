using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private const float PendingProposalExpirationDays = 14f;

        private static readonly MethodInfo GetAiChoiceMethod =
            typeof(KingdomElection).GetMethod(
                "GetAiChoice",
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(
                typeof(KingdomElection).FullName,
                "GetAiChoice");

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
            CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
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
            if (dataStore.IsLoading)
            {
                foreach (PeaceProposalTerms proposal in _pendingProposals)
                {
                    if (proposal != null)
                        proposal.IsAwaitingPlayerResponse = false;
                }
            }
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

            terms.SubmittedAtDays = (float)CampaignTime.Now.ToDays;
            terms.LastResolutionAttemptDays = terms.SubmittedAtDays;
            _pendingProposals.Add(terms);
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
                proposal.TargetKingdom == decision.Kingdom);
        }

        public float EvaluatePeaceTermsForAi(
            MakePeaceKingdomDecision decision,
            Clan votingClan)
        {
            PeaceProposalTerms proposal = GetPendingProposal(decision);
            if (proposal == null)
                return 0f;

            float supportModifier = 0f;

            if (proposal.SelectedOptions.HasFlag(PeaceDealOptions.WarReparation))
                supportModifier -= 0.12f;

            if (proposal.SelectedOptions.HasFlag(PeaceDealOptions.PlunderTreasury))
            {
                Kingdom treasurySource =
                    proposal.IsOffer ? proposal.ProposerKingdom : proposal.TargetKingdom;
                int treasury = Math.Max(0, TreasuryBehavior.GetTreasury(treasurySource));
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
                supportModifier += subject?.JuniorKingdom == proposal.TargetKingdom
                    ? 0.30f
                    : -0.20f;
            }

            if (proposal.SelectedOptions.HasFlag(PeaceDealOptions.Truce))
                supportModifier += 0.03f;

            if (proposal.IsOffer)
                supportModifier = -supportModifier;

            return supportModifier;
        }

        public bool HasPendingProposal(MakePeaceKingdomDecision decision)
        {
            return GetPendingProposal(decision) != null;
        }

        public bool HasPendingProposal(Kingdom kingdom1, Kingdom kingdom2)
        {
            return kingdom1 != null &&
                   kingdom2 != null &&
                   _pendingProposals != null &&
                   _pendingProposals.Any(proposal =>
                       proposal != null &&
                       proposal.Involves(kingdom1, kingdom2));
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
            bool isOffer = proposal.IsOffer;

            AddTruce(proposer, target, options.HasFlag(PeaceDealOptions.Truce) ? 200.0 : 100.0);

            if (options.HasFlag(PeaceDealOptions.PlunderTreasury))
            {
                Kingdom source = isOffer ? proposer : target;
                Kingdom recipient = isOffer ? target : proposer;
                int amount = Math.Max(0, TreasuryBehavior.GetTreasury(source));
                TreasuryBehavior.AddToTreasury(source, -amount);
                TreasuryBehavior.AddToTreasury(recipient, amount);
            }

            if (options.HasFlag(PeaceDealOptions.WarReparation))
            {
                Kingdom reparationsRecipient = isOffer ? target : proposer;
                Kingdom reparationsPayer = isOffer ? proposer : target;
                _reparationTargets[reparationsRecipient.StringId] = reparationsPayer.StringId;
            }

            if (options.HasFlag(PeaceDealOptions.Subjectize))
            {
                Kingdom senior = isOffer ? target : proposer;
                Kingdom junior = isOffer ? proposer : target;
                SubjectBehavior.Instance?.AddSubject(senior, junior, proposal.TargetSubjectType);
            }

            Kingdom settlementSource = isOffer ? proposer : target;
            Kingdom settlementRecipient = isOffer ? target : proposer;
            if (options.HasFlag(PeaceDealOptions.CededSettlement) &&
                IsSettlementOwnedBy(proposal.CededSettlement, settlementSource))
            {
                ChangeOwnerOfSettlementAction.ApplyByBarter(
                    settlementRecipient.Leader,
                    proposal.CededSettlement);
            }

            Settlement settlementFromTarget = isOffer
                ? proposal.ProposerSettlementToExchange
                : proposal.TargetSettlementToExchange;
            Settlement settlementFromProposer = isOffer
                ? proposal.TargetSettlementToExchange
                : proposal.ProposerSettlementToExchange;
            if (options.HasFlag(PeaceDealOptions.TerritoryExchange) &&
                IsSettlementOwnedBy(settlementFromTarget, target) &&
                IsSettlementOwnedBy(settlementFromProposer, proposer))
            {
                ChangeOwnerOfSettlementAction.ApplyByBarter(
                    isOffer ? target.Leader : proposer.Leader,
                    settlementFromTarget);
                ChangeOwnerOfSettlementAction.ApplyByBarter(
                    isOffer ? proposer.Leader : target.Leader,
                    settlementFromProposer);
            }

            if (options.HasFlag(PeaceDealOptions.ClaimThrone) &&
                (isOffer ? target.RulingClan : proposer.RulingClan) != null)
            {
                Kingdom kingdomLosingItsRuler = isOffer ? proposer : target;
                Clan newRulingClan = isOffer ? target.RulingClan : proposer.RulingClan;
                ChangeRulingClanAction.Apply(kingdomLosingItsRuler, newRulingClan);
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

            Hero executedRuler = isOffer ? proposer.Leader : target.Leader;
            Hero executioner = isOffer ? target.Leader : proposer.Leader;
            if (options.HasFlag(PeaceDealOptions.ExecuteRuler) &&
                executedRuler != null &&
                executedRuler.IsAlive &&
                executioner != null)
            {
                KillCharacterAction.ApplyByExecution(executedRuler, executioner);
            }

            InformationManager.DisplayMessage(new InformationMessage(
                $"Peace accepted by {target.Name}. The {(isOffer ? "offer" : "demand")} terms have taken effect.",
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

        private void OnMakePeace(
            IFaction faction1,
            IFaction faction2,
            MakePeaceAction.MakePeaceDetail detail)
        {
            if (faction1 is Kingdom kingdom1 && faction2 is Kingdom kingdom2)
                AddTruce(kingdom1, kingdom2, 100.0);
        }

        private void OnDailyTick()
        {
            ResolveDuePeaceProposals();
            _activeTruces?.RemoveAll(a => a == null || a.IsExpired);

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

        private void ResolveDuePeaceProposals()
        {
            if (_pendingProposals == null || _pendingProposals.Count == 0)
                return;

            float currentDay = (float)CampaignTime.Now.ToDays;
            foreach (PeaceProposalTerms proposal in _pendingProposals.ToList())
            {
                if (proposal == null)
                {
                    _pendingProposals.Remove(proposal);
                    continue;
                }

                Kingdom proposer = proposal.ProposerKingdom;
                Kingdom target = proposal.TargetKingdom;
                Clan targetRulingClan = target?.RulingClan;
                float proposalAge = currentDay - proposal.SubmittedAtDays;
                if (proposalAge > PendingProposalExpirationDays &&
                    proposer != null &&
                    proposer == Clan.PlayerClan?.Kingdom)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"The peace proposal to {target?.Name} expired without a council decision.",
                        Colors.Yellow));
                }

                if (proposer == null ||
                    target == null ||
                    proposer.IsEliminated ||
                    target.IsEliminated ||
                    !proposer.IsAtWarWith(target) ||
                    proposalAge > PendingProposalExpirationDays ||
                    targetRulingClan == null ||
                    targetRulingClan.Kingdom != target)
                {
                    _pendingProposals.Remove(proposal);
                    continue;
                }

                if (currentDay - proposal.LastResolutionAttemptDays < 2f)
                    continue;

                proposal.LastResolutionAttemptDays = currentDay;
                var decision = new MakePeaceKingdomDecision(
                    targetRulingClan,
                    proposer,
                    proposal.DailyTributeFromTargetToProposer,
                    proposal.DailyTributeDurationDays,
                    applyResults: true,
                    isProposedByOpponent: true);

                if (proposal.IsAiGenerated &&
                    target == Clan.PlayerClan?.Kingdom &&
                    target.RulingClan == Clan.PlayerClan)
                {
                    if (!proposal.IsAwaitingPlayerResponse)
                        ShowPlayerPeaceProposal(proposal, decision);
                    continue;
                }

                var election = new KingdomElection(decision);
                election.StartElection();
                if (_pendingProposals.Contains(proposal))
                {
                    DecisionOutcome outcome = GetAiChoiceMethod.Invoke(
                        election,
                        new object[] { election.PossibleOutcomes }) as DecisionOutcome;
                    if (outcome == null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"The council of {target.Name} could not resolve the peace proposal.",
                            Colors.Red));
                        continue;
                    }

                    decision.ApplyChosenOutcome(outcome);
                }
            }
        }

        private void ShowPlayerPeaceProposal(
            PeaceProposalTerms proposal,
            MakePeaceKingdomDecision decision)
        {
            MakePeaceKingdomDecision.MakePeaceDecisionOutcome acceptedOutcome =
                decision.DetermineInitialCandidates()
                    .OfType<MakePeaceKingdomDecision.MakePeaceDecisionOutcome>()
                    .FirstOrDefault(outcome => outcome.ShouldPeaceBeDeclared);
            MakePeaceKingdomDecision.MakePeaceDecisionOutcome rejectedOutcome =
                decision.DetermineInitialCandidates()
                    .OfType<MakePeaceKingdomDecision.MakePeaceDecisionOutcome>()
                    .FirstOrDefault(outcome => !outcome.ShouldPeaceBeDeclared);

            if (acceptedOutcome == null || rejectedOutcome == null)
            {
                _pendingProposals.Remove(proposal);
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Could not prepare the peace proposal from {proposal.ProposerKingdom.Name}.",
                    Colors.Red));
                return;
            }

            proposal.IsAwaitingPlayerResponse = true;
            string tributeText = proposal.DailyTributeFromTargetToProposer == 0
                ? "No tribute is requested."
                : proposal.DailyTributeFromTargetToProposer > 0
                    ? $"Your kingdom pays {proposal.DailyTributeFromTargetToProposer:N0} gold per day for {proposal.DailyTributeDurationDays} days."
                    : $"They pay your kingdom {-proposal.DailyTributeFromTargetToProposer:N0} gold per day for {proposal.DailyTributeDurationDays} days.";
            string proposalText =
                $"{proposal.ProposerKingdom.Name} proposes peace. {tributeText} " +
                "The agreement includes a 200-day truce if accepted.";

            InformationManager.ShowInquiry(new InquiryData(
                $"Peace proposal from {proposal.ProposerKingdom.Name}",
                proposalText,
                true,
                true,
                "Accept",
                "Reject",
                () => ResolvePlayerPeaceProposal(proposal, decision, acceptedOutcome),
                () => ResolvePlayerPeaceProposal(proposal, decision, rejectedOutcome)), true, true);
        }

        private void ResolvePlayerPeaceProposal(
            PeaceProposalTerms proposal,
            MakePeaceKingdomDecision decision,
            MakePeaceKingdomDecision.MakePeaceDecisionOutcome outcome)
        {
            if (!_pendingProposals.Contains(proposal))
                return;

            Kingdom proposer = proposal.ProposerKingdom;
            Kingdom target = proposal.TargetKingdom;
            if (proposer == null ||
                target == null ||
                proposer.IsEliminated ||
                target.IsEliminated ||
                !proposer.IsAtWarWith(target))
            {
                _pendingProposals.Remove(proposal);
                return;
            }

            proposal.IsAwaitingPlayerResponse = false;
            decision.ApplyChosenOutcome(outcome);
        }
    }
}