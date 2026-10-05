using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace BannerlordPlusPlus
{
    public class TradeAgreementBehavior : CampaignBehaviorBase
    {
        private List<TradeAgreement> _activeAgreements = new();

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (_activeAgreements == null)
            {
                _activeAgreements = new List<TradeAgreement>();
            }

            dataStore.SyncData("BPP_TradeAgreements", ref _activeAgreements);
        }
        

        private void OnDailyTick()
        {
            if (_activeAgreements != null)
            {
                _activeAgreements.RemoveAll(a => a == null || a.IsExpired);
            }

            ProcessAITradeAgreements();
        }

        public bool HasTradeAgreement(Kingdom k1, Kingdom k2)
        {
            if (_activeAgreements == null || k1 == null || k2 == null) return false;

            return _activeAgreements.Exists(a => 
                a != null && !a.IsExpired &&
                ((a.Kingdom1Id == k1.StringId && a.Kingdom2Id == k2.StringId) || 
                 (a.Kingdom1Id == k2.StringId && a.Kingdom2Id == k1.StringId)));
        }

        public void AddAgreement(TradeAgreement agreement)
        {
            if (_activeAgreements == null)
            {
                _activeAgreements = new List<TradeAgreement>();
            }

            _activeAgreements.Add(agreement);
        }

        private void ProcessAITradeAgreements()
{
    var kingdoms = Kingdom.All.Where(k => k != null && !k.IsEliminated).ToList();
    
    for (int i = 0; i < kingdoms.Count; i++)
    {
        for (int j = i + 1; j < kingdoms.Count; j++)
        {
            var k1 = kingdoms[i];
            var k2 = kingdoms[j];

            if (k1.IsAtWarWith(k2) || HasTradeAgreement(k1, k2)) continue;

            // Krallıkların lider klanları üzerinden ilişki puanı kontrolü
            Clan leaderClan1 = k1.RulingClan;
            Clan leaderClan2 = k2.RulingClan;

            if (leaderClan1 != null && leaderClan2 != null)
            {
                float relation = leaderClan1.GetRelationWithClan(leaderClan2);

                // Krallık liderlerinin klan ilişkisi +7'nin üzerindeyse %5 şansla ticaret anlaşması yap
                if (relation > 7f && MBRandom.RandomFloat < 0.05f)
                {
                    AddAgreement(new TradeAgreement(k1, k2, 0.20f, 60.0)); // 60 günlük %20 indirim
                }
            }
        }
    }
}
}
}