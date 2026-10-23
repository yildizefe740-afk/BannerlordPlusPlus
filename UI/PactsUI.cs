using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BannerlordPlusPlus
{
    // Handles all pact, agreement selection, and evaluation UI popups
    public static class PactsUI
    {
        // --- TRADE AGREEMENT INQUIRY UI ---
        public static void OpenTradeAgreementInquiry()
        {
            var playerKingdom = Hero.MainHero.MapFaction as Kingdom;
            if (playerKingdom == null) return;

            var tradeBehavior = Campaign.Current.GetCampaignBehavior<TradeAgreementBehavior>();
            var targetKingdoms = Kingdom.All.Where(k => k != playerKingdom && !k.IsEliminated && !k.IsAtWarWith(playerKingdom)).ToList();

            if (!targetKingdoms.Any())
            {
                InformationManager.DisplayMessage(new InformationMessage("There are no available kingdoms for a trade agreement."));
                return;
            }

            List<InquiryElement> elements = new List<InquiryElement>();
            foreach (var kingdom in targetKingdoms)
            {
                bool hasAgreement = tradeBehavior != null && tradeBehavior.HasTradeAgreement(playerKingdom, kingdom);
                string text = hasAgreement ? $"{kingdom.Name} (Active Agreement Exists)" : kingdom.Name.ToString();
                elements.Add(new InquiryElement(kingdom, text, null, !hasAgreement, ""));
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Trade Agreement",
                "Choose a kingdom to propose a trade agreement:",
                elements,
                true, 1, 1,
                "Propose", "Cancel",
                selected =>
                {
                    var selectedKingdom = selected.FirstOrDefault()?.Identifier as Kingdom;
                    if (selectedKingdom != null)
                    {
                        OnOfferAgreement(selectedKingdom, DiplomacyEvaluator.ProposalType.TradeAgreement);
                    }
                }, null));
        }

        // --- DEFENSIVE PACT INQUIRY UI ---
        public static void OpenDefensivePactInquiry()
        {
            var playerKingdom = Hero.MainHero.MapFaction as Kingdom;
            if (playerKingdom == null) return;

            var pactBehavior = Campaign.Current.GetCampaignBehavior<DefensivePactBehavior>();
            var targetKingdoms = Kingdom.All.Where(k => k != playerKingdom && !k.IsEliminated && !k.IsAtWarWith(playerKingdom)).ToList();

            if (!targetKingdoms.Any())
            {
                InformationManager.DisplayMessage(new InformationMessage("There are no available kingdoms for a defensive pact."));
                return;
            }

            List<InquiryElement> elements = new List<InquiryElement>();
            foreach (var kingdom in targetKingdoms)
            {
                bool hasPact = pactBehavior != null && pactBehavior.HasDefensivePact(playerKingdom, kingdom);
                string text = hasPact ? $"{kingdom.Name} (Active Pact Exists)" : kingdom.Name.ToString();
                elements.Add(new InquiryElement(kingdom, text, null, !hasPact, ""));
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Defensive Pact",
                "Choose a kingdom to propose a defensive pact (60 Days):",
                elements,
                true, 1, 1,
                "Propose", "Cancel",
                selected =>
                {
                    var selectedKingdom = selected.FirstOrDefault()?.Identifier as Kingdom;
                    if (selectedKingdom != null)
                    {
                        OnOfferAgreement(selectedKingdom, DiplomacyEvaluator.ProposalType.DefensivePact);
                    }
                }, null));
        }

        // --- NON-AGGRESSION PACT INQUIRY UI ---
        public static void OpenNonAggressionPactInquiry()
        {
            var playerKingdom = Hero.MainHero.MapFaction as Kingdom;
            if (playerKingdom == null) return;

            var pactBehavior = Campaign.Current.GetCampaignBehavior<NonAggressionPactBehavior>();
            var targetKingdoms = Kingdom.All
                .Where(k => k != playerKingdom && !k.IsEliminated && !k.IsAtWarWith(playerKingdom))
                .ToList();

            if (!targetKingdoms.Any())
            {
                InformationManager.DisplayMessage(new InformationMessage("There are no available kingdoms for a non-aggression pact."));
                return;
            }

            List<InquiryElement> elements = new List<InquiryElement>();
            foreach (var kingdom in targetKingdoms)
            {
                bool hasPact = pactBehavior != null && pactBehavior.HasNonAggressionPact(playerKingdom, kingdom);
                string text = hasPact ? $"{kingdom.Name} (Active Pact Exists)" : kingdom.Name.ToString();
                elements.Add(new InquiryElement(kingdom, text, null, !hasPact, ""));
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Non-Aggression Pact",
                "Choose a kingdom to propose a non-aggression pact (60 Days):",
                elements,
                true, 1, 1,
                "Propose", "Cancel",
                selected =>
                {
                    var selectedKingdom = selected.FirstOrDefault()?.Identifier as Kingdom;
                    if (selectedKingdom != null)
                    {
                        OnOfferAgreement(selectedKingdom, DiplomacyEvaluator.ProposalType.NonAggressionPact);
                    }
                }, null));
        }

        // --- EVALUATION & RESULT PROCESSOR UI ---
        public static void OnOfferAgreement(Kingdom selectedKingdom, DiplomacyEvaluator.ProposalType proposalType)
        {
            Kingdom playerKingdom = Clan.PlayerClan.Kingdom;

            // Evaluate proposal via DiplomacyEvaluator AI logic
            bool isAccepted = DiplomacyEvaluator.EvaluateProposal(playerKingdom, selectedKingdom, proposalType, out string reason);

            if (isAccepted)
            {
                switch (proposalType)
                {
                    case DiplomacyEvaluator.ProposalType.TradeAgreement:
                        var tradeAgreement = new TradeAgreement(playerKingdom, selectedKingdom, 0.20f, 60.0);
                        Campaign.Current.GetCampaignBehavior<TradeAgreementBehavior>()?.AddAgreement(tradeAgreement);
                        
                        ShowResultPopup(
                            "Proposal Accepted",
                            $"{selectedKingdom.Name} has accepted your proposal! The trade agreement will remain active for 60 days."
                        );
                        break;

                    case DiplomacyEvaluator.ProposalType.DefensivePact:
                        var defensivePact = new DefensivePact(playerKingdom, selectedKingdom, 60.0);
                        Campaign.Current.GetCampaignBehavior<DefensivePactBehavior>()?.AddPact(defensivePact);
                        
                        ShowResultPopup(
                            "Proposal Accepted",
                            $"{selectedKingdom.Name} has agreed to sign a Defensive Pact with us!"
                        );
                        break;

                    case DiplomacyEvaluator.ProposalType.NonAggressionPact:
                        NonAggressionPactBehavior.AddPact(playerKingdom, selectedKingdom, 60.0);

                        ShowResultPopup(
                            "Proposal Accepted",
                            $"{selectedKingdom.Name} has agreed to sign a Non-Aggression Pact with us for 60 days!"
                        );
                        break;
                }
            }
            else
            {
                ShowResultPopup(
                    "Proposal Rejected",
                    $"{selectedKingdom.Name} rejected your proposal.\n\nReason: {reason}"
                );
            }
        }

        public static void ShowResultPopup(string title, string text)
        {
            InformationManager.ShowInquiry(new InquiryData(
                title, text, true, false, "Okay", "", null, null));
        }

    }
}