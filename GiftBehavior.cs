using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace BannerlordPlusPlus
{

    public class GiftBehavior : CampaignBehaviorBase
    {

        public override void RegisterEvents()
        {
            
        }

        public override void SyncData(IDataStore dataStore)
        {
            // there is nothing to save and sync because change is saving by native relation model 
        }
        public int CalculateRelationGainDynamic(int goldAmount)
        {
            if (goldAmount <= 0) return 0;

            double gain = 5.0 * Math.Log(1.0 + (goldAmount / 2000.0));  

            int finalGain = (int)Math.Floor(gain);
    
            return Math.Min(finalGain, 20);
 
        }
        
        public void SendGiftToHero(Hero gifterHero,Hero targetHero, int sendGold)
        {
            if(targetHero == null || gifterHero == null ||sendGold <= 0) return;
            
            int relationChange = CalculateRelationGainDynamic(sendGold);

            if(relationChange > 0)
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(gifterHero, targetHero, relationChange);

                GiveGoldAction.ApplyBetweenCharacters(gifterHero, targetHero, sendGold);
            }
        }
    }
}
