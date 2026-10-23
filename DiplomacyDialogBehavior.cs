using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;

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
                PactsUI.OpenTradeAgreementInquiry);

            // Option 3: Propose Defensive Pact
            starter.AddPlayerLine("diplomacy_propose_pact", "diplomacy_options_list", "diplomacy_talk_response", 
                "I want to propose a defensive pact to a kingdom.", 
                null, 
                PactsUI.OpenDefensivePactInquiry);

            // Option 4: Propose Non-Aggression Pact
            starter.AddPlayerLine("diplomacy_propose_non_aggression_pact", "diplomacy_options_list", "diplomacy_talk_response",
                "I want to propose a non-aggression pact to a kingdom.",
                null,
                PactsUI.OpenNonAggressionPactInquiry);

            // Back
            starter.AddPlayerLine("diplomacy_back", "diplomacy_options_list", "hero_main_options", 
                "That's all, thank you.", null, null);
        }

        // --- 2. TOWN / FORTIFICATION MENU ---
        private void AddDiplomacyTownMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("town", "town_diplomacy_option", "Kingdom Action",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    return IsDiplomacyAvailable();
                },
                args => GameMenu.SwitchToMenu("diplomacy_town_menu"), false, 4);

            starter.AddGameMenu("diplomacy_town_menu", "Diplomacy",
                args => args.MenuContext.SetBackgroundMeshName("town_wait"));

            starter.AddGameMenuOption("diplomacy_town_menu", "menu_treasury", "Visit Treasury",
                args => true,
                args =>
                {
                    if (Hero.MainHero.MapFaction is Kingdom playerKingdom)
                        TreasuryBehavior.ShowTreasuryInfo(playerKingdom);
                });

            starter.AddGameMenuOption("diplomacy_town_menu", "menu_trade_pact", "Propose a trade agreement",
                args => true,
                args => PactsUI.OpenTradeAgreementInquiry());

            starter.AddGameMenuOption("diplomacy_town_menu", "menu_defensive_pact", "Propose a defensive pact",
                args => true,
                args => PactsUI.OpenDefensivePactInquiry());

            starter.AddGameMenuOption("diplomacy_town_menu", "menu_non_aggression_pact", "Propose a non-aggression pact",
                args => true,
                args => PactsUI.OpenNonAggressionPactInquiry());

            starter.AddGameMenuOption("diplomacy_town_menu", "menu_back", "Return",
                args => true,
                args => GameMenu.SwitchToMenu("town"));
        }

        private bool IsDiplomacyAvailable()
        {
            return Hero.MainHero.MapFaction is Kingdom kingdom && !kingdom.IsEliminated;
        }
    }
}