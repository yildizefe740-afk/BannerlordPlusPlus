using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;

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
                _NApact.RemoveAll(pact =>
                    pact == null ||
                    pact.IsExpired ||
                    !SubjectBehavior.CanCreateDiplomaticPact(pact.Kingdom1Id, pact.Kingdom2Id));
            }

            ProcessAINAP();
        }

        public static void AddPact(Kingdom k1, Kingdom k2, double durationDays)
        {
            if (!SubjectBehavior.CanCreateDiplomaticPact(k1, k2))
                return;

            if (_NApact == null)
                _NApact = new List<NonAggressionPact>();

            if (!_NApact.Any(p => (p.Kingdom1Id == k1.StringId && p.Kingdom2Id == k2.StringId) ||
                                  (p.Kingdom1Id == k2.StringId && p.Kingdom2Id == k1.StringId)))
            {
                _NApact.Add(new NonAggressionPact(k1, k2, durationDays));
            }
        }

        public bool HasNonAggressionPact(Kingdom k1, Kingdom k2)
        {
            if (_NApact == null || k1 == null || k2 == null)
                return false;

            return _NApact.Any(p =>
                p != null && !p.IsExpired &&
                ((p.Kingdom1Id == k1.StringId && p.Kingdom2Id == k2.StringId) ||
                 (p.Kingdom1Id == k2.StringId && p.Kingdom2Id == k1.StringId)));
        }

        [HarmonyPatch(typeof(DeclareWarAction), "ApplyNonAggresion")]
        public class ApplyNonAggresionPact
        {
            public static bool Prefix(IFaction faction1, IFaction faction2)
            {
                if (faction1 == null || faction2 == null)
                    return true;

                if (_NApact == null)
                    return true;

                bool hasActivePact = _NApact.Any(p => 
                    !p.IsExpired && 
                    ((p.Kingdom1Id == faction1.StringId && p.Kingdom2Id == faction2.StringId) ||
                     (p.Kingdom1Id == faction2.StringId && p.Kingdom2Id == faction1.StringId))
                );

                if (hasActivePact)
                {
                    return false; 
                }

                return true;
            }
        }  

        public void ProcessAINAP()
        {
            var kingdoms = Kingdom.All.Where(k => k != null && !k.IsEliminated).ToList();
            
            for(int i = 0; i < kingdoms.Count; i++)
            {
                for(int j = i+1 ; j < kingdoms.Count; j++)
                {
                    var k1 = kingdoms[i];
                    var k2 = kingdoms[j];

                    if(k1.IsAtWarWith(k2) ||
                       HasNonAggressionPact(k1, k2) ||
                       !SubjectBehavior.CanCreateDiplomaticPact(k1, k2))
                    {
                        continue;
                    }

                    Clan rulingClan1 = k1.RulingClan;
                    Clan rulingClan2 = k2.RulingClan;

                    if(rulingClan1 != null && rulingClan2 != null)
                    {
                        float relation = rulingClan1.GetRelationWithClan(rulingClan2);

                        if(relation > 15f && MBRandom.RandomFloat < 0.03f)
                        {
                            NonAggressionPactBehavior.AddPact(k1, k2, 60.0);
                        }
                    }
                }
            }
        }    
    }
}