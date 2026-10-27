using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace BannerlordPlusPlus
{
    public class MarriagePactBehavior : CampaignBehaviorBase
    {
        private List<MarriagePact> _marriages = new();

        public override void RegisterEvents()
        {
            // For daily relation increases and cleanup.
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            
            // Remove the pact immediately when either spouse dies.
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("BannerlordPlusPlus_MarriagePacts", ref _marriages);
        }

        public void OnDailyTick()
        {
            if (_marriages is null) return;

            // 1. Remove invalid pacts if a spouse died, divorced, or lost RulingClan status.
            _marriages.RemoveAll(m => m == null || !m.IsValid);

            // 2. Increase the relation between kingdom leaders daily for valid pacts.
            foreach (var pact in _marriages)
            {
                if (pact.Kingdom1 == null || pact.Kingdom2 == null) continue;

                Hero leader1 = pact.Kingdom1.Leader;
                Hero leader2 = pact.Kingdom2.Leader;

                if (leader1 != null && leader2 != null && leader1 != leader2)
                {
                    // Increase leader relations by 1 due to the marriage between ruling clans.
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leader1, leader2, 1);
                }
            }
        }

        private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            // Remove the pact if a character in a ruling marriage pact dies in battle or of old age.
            _marriages?.RemoveAll(m => m == null || !m.IsValid);
        }

        // Helper method to add a marriage pact.
        public void AddMarriagePact(MarriagePact pact)
        {
            if (pact != null && pact.IsValid && !_marriages.Contains(pact))
            {
                _marriages.Add(pact);
            }
        }
    }
}