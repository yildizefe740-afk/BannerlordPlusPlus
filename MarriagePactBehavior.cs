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
            // Günlük ilişki artışı ve temizlik için
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            
            // Eşlerden biri öldüğünde paktın anında düşmesi için
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("BannerlordPlusPlus_MarriagePacts", ref _marriages);
        }

        public void OnDailyTick()
        {
            if (_marriages is null) return;

            // 1. Ölen, boşanan veya RulingClan statüsünü kaybeden geçersiz paktları listeden temizle
            _marriages.RemoveAll(m => m == null || !m.IsValid);

            // 2. Geçerli paktlar için her gün krallık liderleri arasındaki ilişkiyi artır
            foreach (var pact in _marriages)
            {
                if (pact.Kingdom1 == null || pact.Kingdom2 == null) continue;

                Hero leader1 = pact.Kingdom1.Leader;
                Hero leader2 = pact.Kingdom2.Leader;

                if (leader1 != null && leader2 != null && leader1 != leader2)
                {
                    // Yönetici klanlar arası evlilik sebebiyle liderlerin ilişkisini +1 artırır
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leader1, leader2, 1);
                }
            }
        }

        private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            // Savaşta veya yaşlılıktan ölen karakter yönetici evlilik paktında varsa paktı temizle
            _marriages?.RemoveAll(m => m == null || !m.IsValid);
        }

        // Yeni bir evlilik paktı eklemek için yardımcı metod
        public void AddMarriagePact(MarriagePact pact)
        {
            if (pact != null && pact.IsValid && !_marriages.Contains(pact))
            {
                _marriages.Add(pact);
            }
        }
    }
}