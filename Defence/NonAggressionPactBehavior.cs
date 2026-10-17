using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace BannerlordPlusPlus
{
    public class NonAggressionPactBehavior : CampaignBehaviorBase
    {
        private static List<NonAggressionPact> _NApact = [];

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("BannerlordPlusPlus", ref _NApact);
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public void OnDailyTick()
        {
            if (_NApact != null)
            {
                _NApact.RemoveAll(a => a == null || a.IsExpired);
            }
        }

        public static void AddPact(Kingdom k1, Kingdom k2, double durationDays)
        {
            if (!_NApact.Any(p => (p.Kingdom1Id == k1.StringId && p.Kingdom2Id == k2.StringId) || 
                                  (p.Kingdom1Id == k2.StringId && p.Kingdom2Id == k1.StringId)))
            {
                _NApact.Add(new NonAggressionPact(k1, k2, durationDays));
            }
        }

        [HarmonyPatch(typeof(DeclareWarAction), "ApplyNonAggresion")]
        public class ApplyNonAggresionPact
        {
            public static bool Prefix(Kingdom kingdom1, Kingdom kingdom2)
            {
                if (kingdom1 == null || kingdom2 == null)
                    return true;

                bool hasActivePact = _NApact.Any(p => 
                    !p.IsExpired && 
                    ((p.Kingdom1Id == kingdom1.StringId && p.Kingdom2Id == kingdom2.StringId) || 
                     (p.Kingdom1Id == kingdom2.StringId && p.Kingdom2Id == kingdom1.StringId))
                );

                if (hasActivePact)
                {
                    return false; 
                }

                return true;
            }
        }        
    }
}