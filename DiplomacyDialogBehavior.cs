using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace BannerlordPlusPlus
{
    public class DiplomacyDialogBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No extra data to save
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddDiplomacyDialogs(starter);
            AddDiplomacyTownMenu(starter);
        }

        // --- 1. NPC DIALOGUE SYSTEM ---
        private void AddDiplomacyDialogs(CampaignGameStarter starter)
        {
            starter.AddPlayerLine("diplomacy_main_option", "hero_main_options", "diplomacy_talk_response", 
                "I want to learn the diplomatic situation of our kingdom.", 
                IsDiplomacyAvailable, null);

            starter.AddDialogLine("diplomacy_response", "diplomacy_talk_response", "diplomacy_options_list", 
                "As you wish, your Grace. What would you like to know?", 
                null, null);

            // Option 1: Kingdom Treasury
            starter.AddPlayerLine("diplomacy_check_treasury", "diplomacy_options_list", "diplomacy_talk_response", 
                "Treasury", 
                null, 
                () => {
                    if (Hero.MainHero.MapFaction is Kingdom playerKingdom)
                    {
                        TreasuryBehavior.ShowTreasuryInfo(playerKingdom);
                    }
                });

            // Option 2: Propose Trade Agreement
            starter.AddPlayerLine("diplomacy_propose_trade", "diplomacy_options_list", "diplomacy_talk_response", 
                "I want to propose a trade agreement to a kingdom.", 
                null, 
                OpenTradeAgreementInquiry);

            // Option 3: Propose Defensive Pact
            starter.AddPlayerLine("diplomacy_propose_pact", "diplomacy_options_list", "diplomacy_talk_response", 
                "I want to propose a defensive pact to a kingdom.", 
                null, 
                OpenDefensivePactInquiry);

            // Back
            starter.AddPlayerLine("diplomacy_back", "diplomacy_options_list", "hero_main_options", 
                "That's all, thank you.", null, null);
        }

        // --- 2. TOWN / FORTIFICATION MENU ---
        private void AddDiplomacyTownMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("town", "town_diplomacy_option", "Kingdom Action",
                (MenuCallbackArgs args) =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    return IsDiplomacyAvailable();
                },
                (MenuCallbackArgs args) =>
                {
                    GameMenu.SwitchToMenu("diplomacy_town_menu");
                }, false, 4);

            starter.AddGameMenu("diplomacy_town_menu", "Diplomacy",
                (MenuCallbackArgs args) =>
                {
                    args.MenuContext.SetBackgroundMeshName("town_wait");
                });

            starter.AddGameMenuOption("diplomacy_town_menu", "menu_treasury", "Visit Treasury",
                (MenuCallbackArgs args) => true,
                (MenuCallbackArgs args) =>
                {
                    if (Hero.MainHero.MapFaction is Kingdom playerKingdom)
                        TreasuryBehavior.ShowTreasuryInfo(playerKingdom);
                });

            starter.AddGameMenuOption("diplomacy_town_menu", "menu_trade_pact", "Propose a trade agreement",
                (MenuCallbackArgs args) => true,
                (MenuCallbackArgs args) => OpenTradeAgreementInquiry());

            starter.AddGameMenuOption("diplomacy_town_menu", "menu_defensive_pact", "Propose a defensive pact",
                (MenuCallbackArgs args) => true,
                (MenuCallbackArgs args) => OpenDefensivePactInquiry());

            starter.AddGameMenuOption("diplomacy_town_menu", "menu_back", "Return",
                (MenuCallbackArgs args) => true,
                (MenuCallbackArgs args) => GameMenu.SwitchToMenu("town"));
        }

        private bool IsDiplomacyAvailable()
        {
            return Hero.MainHero.MapFaction is Kingdom kingdom && !kingdom.IsEliminated;
        }

        // --- TRADE AGREEMENT INQUIRY ---
        private void OpenTradeAgreementInquiry()
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

            List<InquiryElement> elements = new();
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
                (List<InquiryElement> selected) =>
                {
                    var selectedKingdom = selected.FirstOrDefault()?.Identifier as Kingdom;
                    if (selectedKingdom != null)
                    {
                        OnOfferAgreement(selectedKingdom, DiplomacyEvaluator.ProposalType.TradeAgreement);
                    }
                }, null));
        }

        // --- DEFENSIVE PACT INQUIRY ---
        private void OpenDefensivePactInquiry()
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

            List<InquiryElement> elements = new();
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
                (List<InquiryElement> selected) =>
                {
                    var selectedKingdom = selected.FirstOrDefault()?.Identifier as Kingdom;
                    if (selectedKingdom != null)
                    {
                        OnOfferAgreement(selectedKingdom, DiplomacyEvaluator.ProposalType.DefensivePact);
                    }
                }, null));
        }

        // --- EVALUATION & RESULT PROCESSOR ---
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

        private static void ShowResultPopup(string title, string text)
        {
            InformationManager.ShowInquiry(new InquiryData(
                title, text, true, false, "Okay", "", null, null));
        }
    }
}