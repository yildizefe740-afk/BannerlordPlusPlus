using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;

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
            // Kaydedilecek ekstra veri yok
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddDiplomacyDialogs(starter);
            AddDiplomacyTownMenu(starter);
        }

        // --- 1. NPC DİYALOG SİSTEMİ ---
        private void AddDiplomacyDialogs(CampaignGameStarter starter)
        {
            // Kale/Şehir yetkilisi (Chamberlain / Fortification Guard / Lord) ile konuşurken çıkan diplomasi seçeneği
            starter.AddPlayerLine("diplomacy_main_option", "hero_main_options", "diplomacy_talk_response", 
                "Krallığımızın diplomasi ve ticaret durumunu görüşmek istiyorum.", 
                IsDiplomacyAvailable, null);

            starter.AddDialogLine("diplomacy_response", "diplomacy_talk_response", "diplomacy_options_list", 
                "Emredin efendim, hangi diplomasi konusunu değerlendirmek istersiniz?", 
                null, null);

            // Seçenek 1: Krallık Kasası
            starter.AddPlayerLine("diplomacy_check_treasury", "diplomacy_options_list", "diplomacy_talk_response", 
                "Krallık kasasında ne kadar dinarımız var?", 
                null, 
                () => {
                    if (Hero.MainHero.MapFaction is Kingdom playerKingdom)
                    {
                        TreasuryBehavior.ShowTreasuryInfo(playerKingdom);
                    }
                });

            // Seçenek 2: Ticaret Anlaşması Öner
            starter.AddPlayerLine("diplomacy_propose_trade", "diplomacy_options_list", "diplomacy_talk_response", 
                "Başka bir krallığa ticaret anlaşması teklif etmek istiyorum.", 
                null, 
                OpenTradeAgreementInquiry);

            // Seçenek 3: Savunma Paktı Öner
            starter.AddPlayerLine("diplomacy_propose_pact", "diplomacy_options_list", "diplomacy_talk_response", 
                "Başka bir krallığa savunma paktı teklif etmek istiyorum.", 
                null, 
                OpenDefensivePactInquiry);

            // Geri Dönüş
            starter.AddPlayerLine("diplomacy_back", "diplomacy_options_list", "hero_main_options", 
                "Şimdilik bu kadar, sağ olasın.", null, null);
        }

        // --- 2. ŞEHİR / KALE MENÜSÜ SEÇENEĞİ ---
        private void AddDiplomacyTownMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("town", "town_diplomacy_option", "Diplomasi İşlerini Yönet",
                (MenuCallbackArgs args) =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    return IsDiplomacyAvailable();
                },
                (MenuCallbackArgs args) =>
                {
                    GameMenu.SwitchToMenu("diplomacy_town_menu");
                }, false, 4);

            // Alt Menü (GameOverlays parametresi kaldırıldı, tertemiz geçiyor)
            starter.AddGameMenu("diplomacy_town_menu", "Diplomasi ve Dış İlişkiler Masası",
                (MenuCallbackArgs args) =>
                {
                    args.MenuContext.SetBackgroundMeshName("town_wait");
                });

            starter.AddGameMenuOption("diplomacy_town_menu", "menu_treasury", "Krallık Kasasını İncele",
                (MenuCallbackArgs args) => true,
                (MenuCallbackArgs args) =>
                {
                    if (Hero.MainHero.MapFaction is Kingdom playerKingdom)
                        TreasuryBehavior.ShowTreasuryInfo(playerKingdom);
                });

            starter.AddGameMenuOption("diplomacy_town_menu", "menu_trade_pact", "Ticaret Anlaşması Teklif Et",
                (MenuCallbackArgs args) => true,
                (MenuCallbackArgs args) => OpenTradeAgreementInquiry());

            starter.AddGameMenuOption("diplomacy_town_menu", "menu_defensive_pact", "Savunma Paktı Teklif Et",
                (MenuCallbackArgs args) => true,
                (MenuCallbackArgs args) => OpenDefensivePactInquiry());

            starter.AddGameMenuOption("diplomacy_town_menu", "menu_back", "Geri Dön",
                (MenuCallbackArgs args) => true,
                (MenuCallbackArgs args) => GameMenu.SwitchToMenu("town"));
        }

        private bool IsDiplomacyAvailable()
        {
            return Hero.MainHero.MapFaction is Kingdom kingdom && !kingdom.IsEliminated;
        }

        // --- TİCARET ANLAŞMASI SEÇİM POPUP'I ---
        private void OpenTradeAgreementInquiry()
        {
            var playerKingdom = Hero.MainHero.MapFaction as Kingdom;
            if (playerKingdom == null) return;

            var tradeBehavior = Campaign.Current.GetCampaignBehavior<TradeAgreementBehavior>();
            var targetKingdoms = Kingdom.All.Where(k => k != playerKingdom && !k.IsEliminated && !k.IsAtWarWith(playerKingdom)).ToList();

            if (!targetKingdoms.Any())
            {
                InformationManager.DisplayMessage(new InformationMessage("Ticaret anlaşması yapabileceğiniz barışçıl bir krallık bulunmuyor."));
                return;
            }

            List<InquiryElement> elements = new();
            foreach (var kingdom in targetKingdoms)
            {
                bool hasPact = tradeBehavior != null && tradeBehavior.HasTradeAgreement(playerKingdom, kingdom);
                string text = hasPact ? $"{kingdom.Name} (Zaten Aktif Anlaşma Var)" : kingdom.Name.ToString();
                elements.Add(new InquiryElement(kingdom, text, null, !hasPact, ""));
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Ticaret Anlaşması",
                "Ticaret anlaşması teklif etmek istediğiniz krallığı seçin (60 Günlük - %20 İndirim):",
                elements,
                true, 1, 1,
                "Teklif Et", "İptal",
                (List<InquiryElement> selected) =>
                {
                    var selectedKingdom = selected.FirstOrDefault()?.Identifier as Kingdom;
                    if (selectedKingdom != null && tradeBehavior != null)
                    {
                        tradeBehavior.AddAgreement(new TradeAgreement(playerKingdom, selectedKingdom, 0.20f, 60.0));
                        InformationManager.DisplayMessage(new InformationMessage($"{selectedKingdom.Name} ile Ticaret Anlaşması imzalandı!"));
                    }
                }, null));
        }

        // --- SAVUNMA PAKTI SEÇİM POPUP'I ---
        private void OpenDefensivePactInquiry()
        {
            var playerKingdom = Hero.MainHero.MapFaction as Kingdom;
            if (playerKingdom == null) return;

            var pactBehavior = Campaign.Current.GetCampaignBehavior<DefensivePactBehavior>();
            var targetKingdoms = Kingdom.All.Where(k => k != playerKingdom && !k.IsEliminated && !k.IsAtWarWith(playerKingdom)).ToList();

            if (!targetKingdoms.Any())
            {
                InformationManager.DisplayMessage(new InformationMessage("Savunma Paktı teklif edebileceğiniz uygun bir krallık bulunmuyor."));
                return;
            }

            List<InquiryElement> elements = new();
            foreach (var kingdom in targetKingdoms)
            {
                bool hasPact = pactBehavior != null && pactBehavior.HasDefensivePact(playerKingdom, kingdom);
                string text = hasPact ? $"{kingdom.Name} (Aktif Pakt Var)" : kingdom.Name.ToString();
                elements.Add(new InquiryElement(kingdom, text, null, !hasPact, ""));
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Savunma Paktı",
                "Savunma paktı imzalamak istediğiniz krallığı seçin (60 Günlük):",
                elements,
                true, 1, 1,
                "Pakt Yap", "İptal",
                (List<InquiryElement> selected) =>
                {
                    var selectedKingdom = selected.FirstOrDefault()?.Identifier as Kingdom;
                    if (selectedKingdom != null && pactBehavior != null)
                    {
                        pactBehavior.AddPact(new DefensivePact(playerKingdom, selectedKingdom, 60.0));
                        InformationManager.DisplayMessage(new InformationMessage($"{selectedKingdom.Name} ile Savunma Paktı imzalandı!"));
                    }
                }, null));
        }
    }
}