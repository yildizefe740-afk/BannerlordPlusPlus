using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BannerlordPlusPlus
{
    public static class PactsUI
    {
        public static void OpenDiplomacyMenu()
        {
            var playerKingdom = Clan.PlayerClan?.Kingdom;
            if (playerKingdom == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("You must belong to a kingdom to offer proposals.", Colors.Red));
                return;
            }

            List<InquiryElement> elements = new List<InquiryElement>
            {
                new InquiryElement("alliance", "Propose Full Alliance", null, true, "Propose a military alliance bound by Trust."),
                new InquiryElement("trade", "Propose Trade Agreement", null, true, "Propose a 60-day economic trade agreement."),
                new InquiryElement("defensive", "Propose Defensive Pact", null, true, "Propose a 60-day mutual defensive pact."),
                new InquiryElement("nonaggression", "Propose Non-Aggression Pact", null, true, "Propose a 60-day non-aggression pact.")
            };

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Propose Diplomatic Action",
                "Select a proposal to present to foreign powers:",
                elements,
                true, 1, 1,
                "Select Target Kingdom", "Cancel",
                selected =>
                {
                    var choice = selected.FirstOrDefault()?.Identifier as string;
                    switch (choice)
                    {
                        case "alliance":
                            OpenAllianceInquiry();
                            break;
                        case "trade":
                            OpenTradeAgreementInquiry();
                            break;
                        case "defensive":
                            OpenDefensivePactInquiry();
                            break;
                        case "nonaggression":
                            OpenNonAggressionPactInquiry();
                            break;
                    }
                }, null));
        }

        public static void OpenAllianceInquiry()
        {
            var playerKingdom = Clan.PlayerClan?.Kingdom;
            if (playerKingdom == null) return;

            var allianceBehavior = Campaign.Current.GetCampaignBehavior<AllianceBehavior>();
            var targetKingdoms = Kingdom.All.Where(k =>
                k != playerKingdom &&
                !k.IsEliminated &&
                !k.IsAtWarWith(playerKingdom) &&
                SubjectBehavior.CanCreateDiplomaticPact(playerKingdom, k)).ToList();

            if (!targetKingdoms.Any())
            {
                InformationManager.DisplayMessage(new InformationMessage("There are no available kingdoms for an alliance."));
                return;
            }

            List<InquiryElement> elements = new List<InquiryElement>();
            foreach (var kingdom in targetKingdoms)
            {
                bool hasAlliance = allianceBehavior != null && allianceBehavior.HasAlliance(playerKingdom, kingdom);
                string text = hasAlliance ? $"{kingdom.Name} (Active Alliance)" : kingdom.Name.ToString();
                elements.Add(new InquiryElement(kingdom, text, null, !hasAlliance, ""));
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Military Alliance Proposal",
                "Select a kingdom to send an alliance proposal:",
                elements,
                true, 1, 1,
                "Propose", "Cancel",
                selected =>
                {
                    var selectedKingdom = selected.FirstOrDefault()?.Identifier as Kingdom;
                    if (selectedKingdom != null)
                    {
                        OnOfferAlliance(selectedKingdom);
                    }
                }, null));
        }

        private static void OnOfferAlliance(Kingdom selectedKingdom)
        {
            Kingdom playerKingdom = Clan.PlayerClan.Kingdom;
            var allianceBehavior = Campaign.Current.GetCampaignBehavior<AllianceBehavior>();

            if (allianceBehavior != null)
            {
                var newAlliance = new Alliance(playerKingdom, selectedKingdom);
                allianceBehavior.AddAlliance(newAlliance);

                ShowResultPopup("Alliance Established", $"An alliance has been formed with {selectedKingdom.Name}!");
            }
        }

        public static void OpenTradeAgreementInquiry()
        {
            var playerKingdom = Hero.MainHero.MapFaction as Kingdom;
            if (playerKingdom == null) return;

            var tradeBehavior = Campaign.Current.GetCampaignBehavior<TradeAgreementBehavior>();
            var targetKingdoms = Kingdom.All.Where(k =>
                k != playerKingdom &&
                !k.IsEliminated &&
                !k.IsAtWarWith(playerKingdom) &&
                SubjectBehavior.CanCreateTradeAgreement(playerKingdom, k)).ToList();

            if (!targetKingdoms.Any())
            {
                InformationManager.DisplayMessage(new InformationMessage("No eligible kingdoms for trade."));
                return;
            }

            List<InquiryElement> elements = new List<InquiryElement>();
            foreach (var kingdom in targetKingdoms)
            {
                bool hasAgreement = tradeBehavior != null && tradeBehavior.HasTradeAgreement(playerKingdom, kingdom);
                string text = hasAgreement ? $"{kingdom.Name} (Active Agreement)" : kingdom.Name.ToString();
                elements.Add(new InquiryElement(kingdom, text, null, !hasAgreement, ""));
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Trade Agreement Proposal",
                "Choose a kingdom to propose trade:",
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

        public static void OpenDefensivePactInquiry()
        {
            var playerKingdom = Hero.MainHero.MapFaction as Kingdom;
            if (playerKingdom == null) return;

            var pactBehavior = Campaign.Current.GetCampaignBehavior<DefensivePactBehavior>();
            var targetKingdoms = Kingdom.All.Where(k =>
                k != playerKingdom &&
                !k.IsEliminated &&
                !k.IsAtWarWith(playerKingdom) &&
                SubjectBehavior.CanCreateDiplomaticPact(playerKingdom, k)).ToList();

            if (!targetKingdoms.Any())
            {
                InformationManager.DisplayMessage(new InformationMessage("No eligible kingdoms for defensive pacts."));
                return;
            }

            List<InquiryElement> elements = new List<InquiryElement>();
            foreach (var kingdom in targetKingdoms)
            {
                bool hasPact = pactBehavior != null && pactBehavior.HasDefensivePact(playerKingdom, kingdom);
                string text = hasPact ? $"{kingdom.Name} (Active Pact)" : kingdom.Name.ToString();
                elements.Add(new InquiryElement(kingdom, text, null, !hasPact, ""));
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Defensive Pact Proposal",
                "Choose a kingdom to propose a defensive pact:",
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

        public static void OpenNonAggressionPactInquiry()
        {
            var playerKingdom = Hero.MainHero.MapFaction as Kingdom;
            if (playerKingdom == null) return;

            var pactBehavior = Campaign.Current.GetCampaignBehavior<NonAggressionPactBehavior>();
            var targetKingdoms = Kingdom.All.Where(k =>
                k != playerKingdom &&
                !k.IsEliminated &&
                !k.IsAtWarWith(playerKingdom) &&
                SubjectBehavior.CanCreateDiplomaticPact(playerKingdom, k)).ToList();

            if (!targetKingdoms.Any())
            {
                InformationManager.DisplayMessage(new InformationMessage("No eligible kingdoms for non-aggression pacts."));
                return;
            }

            List<InquiryElement> elements = new List<InquiryElement>();
            foreach (var kingdom in targetKingdoms)
            {
                bool hasPact = pactBehavior != null && pactBehavior.HasNonAggressionPact(playerKingdom, kingdom);
                string text = hasPact ? $"{kingdom.Name} (Active Pact)" : kingdom.Name.ToString();
                elements.Add(new InquiryElement(kingdom, text, null, !hasPact, ""));
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Non-Aggression Pact Proposal",
                "Choose a kingdom to propose a non-aggression pact:",
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

        public static void OnOfferAgreement(Kingdom selectedKingdom, DiplomacyEvaluator.ProposalType proposalType)
        {
            Kingdom playerKingdom = Clan.PlayerClan.Kingdom;
            bool isAccepted = DiplomacyEvaluator.EvaluateProposal(playerKingdom, selectedKingdom, proposalType, out string reason);

            if (isAccepted)
            {
                switch (proposalType)
                {
                    case DiplomacyEvaluator.ProposalType.TradeAgreement:
                        var tradeAgreement = new TradeAgreement(playerKingdom, selectedKingdom, 0.20f, 60.0);
                        Campaign.Current.GetCampaignBehavior<TradeAgreementBehavior>()?.AddAgreement(tradeAgreement);
                        ShowResultPopup("Proposal Accepted", $"{selectedKingdom.Name} accepted the trade agreement!");
                        break;

                    case DiplomacyEvaluator.ProposalType.DefensivePact:
                        var defensivePact = new DefensivePact(playerKingdom, selectedKingdom, 60.0);
                        Campaign.Current.GetCampaignBehavior<DefensivePactBehavior>()?.AddPact(defensivePact);
                        ShowResultPopup("Proposal Accepted", $"{selectedKingdom.Name} signed the defensive pact!");
                        break;

                    case DiplomacyEvaluator.ProposalType.NonAggressionPact:
                        if (NonAggressionPactBehavior.AddPact(playerKingdom, selectedKingdom, 60.0))
                        {
                            ShowResultPopup("Proposal Accepted", $"{selectedKingdom.Name} signed the non-aggression pact!");
                        }
                        else
                        {
                            ShowResultPopup("Pact Not Established", $"The non-aggression pact with {selectedKingdom.Name} could not be established.");
                        }
                        break;
                }
            }
            else
            {
                ShowResultPopup("Proposal Rejected", $"{selectedKingdom.Name} rejected your proposal.\nReason: {reason}");
            }
        }

        public static void ShowResultPopup(string title, string text)
        {
            InformationManager.ShowInquiry(new InquiryData(title, text, true, false, "Okay", "", null, null));
        }

        public static void ShowNonAggressionPactWarning(Kingdom otherKingdom)
        {
            string kingdomName = otherKingdom?.Name?.ToString() ?? "this kingdom";
            InformationManager.DisplayMessage(new InformationMessage(
                $"You already have a non-aggression pact with {kingdomName}. You cannot declare war while it is active.",
                Colors.Red));
        }
    }
}