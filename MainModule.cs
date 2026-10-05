using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerlordPlusPlus
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            
            // Harmony Yamalarını Yükle
            var harmony = new Harmony("com.bannerlordplusplus.patch");
            harmony.PatchAll();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            if (gameStarterObject is CampaignGameStarter starter)
            {
                // Bütün Behavior'ları Oyuna Ekle
                starter.AddBehavior(new SubjectBehavior());
                starter.AddBehavior(new TradeAgreementBehavior());
                starter.AddBehavior(new DefensivePactBehavior());
                starter.AddBehavior(new TreasuryBehavior());
                starter.AddBehavior(new CustomPeaceBehavior());
                starter.AddBehavior(new DiplomacyDialogBehavior());
                starter.AddBehavior(new MarriagePactBehavior());
                starter.AddBehavior(new GiftBehavior());
            }
        }
    }
}