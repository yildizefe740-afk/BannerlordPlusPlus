using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;

namespace BannerlordPlusPlus
{
    public class DefensivePactBehavior : CampaignBehaviorBase
    {
        private List<DefensivePact> _pacts = new();

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPeaceAction);
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("BannerlordPlusPlus_DefensivePacts", ref _pacts);
        }

        public void OnDailyTick()
        {
            if (_pacts != null)
            {
                _pacts.RemoveAll(a => a == null || a.IsExpired);
            }

            ProcessAIDefensivePacts();
        }

        public void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
        {
            if (faction1 is not Kingdom aggressor || faction2 is not Kingdom defender || _pacts == null)
                return;

            foreach (var pact in _pacts)
            {
                if (pact == null || !pact.CanCall)
                    continue;

                if (pact.Kingdom1Id == defender.StringId || pact.Kingdom2Id == defender.StringId)
                {
                    string allyId = (pact.Kingdom1Id == defender.StringId) ? pact.Kingdom2Id : pact.Kingdom1Id;
                    Kingdom ally = Kingdom.All.FirstOrDefault(k => k != null && k.StringId == allyId);

                    if (ally != null && !ally.IsAtWarWith(aggressor))
                    {
                        DeclareWarAction.ApplyByDefault(ally, aggressor);
                        pact.IsCalled = true;
                    }
                }
            }
        }

        public void OnPeaceAction(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
        {
            if (faction1 is not Kingdom k1 || faction2 is not Kingdom k2 || _pacts == null) 
                return;

            foreach (var pact in _pacts)
            {
                if (pact == null) continue;

                bool isPactMemberInvolved = (pact.Kingdom1Id == k1.StringId || pact.Kingdom1Id == k2.StringId) ||
                                            (pact.Kingdom2Id == k1.StringId || pact.Kingdom2Id == k2.StringId);

                if (isPactMemberInvolved && pact.IsCalled)
                {
                    pact.IsCalled = false;
                }
            }
        }

        public bool HasDefensivePact(Kingdom k1, Kingdom k2)
        {
            if (_pacts == null || k1 == null || k2 == null) return false;

            return _pacts.Exists(p => 
                p != null && !p.IsExpired &&
                ((p.Kingdom1Id == k1.StringId && p.Kingdom2Id == k2.StringId) || 
                 (p.Kingdom1Id == k2.StringId && p.Kingdom2Id == k1.StringId)));
        }

        public void AddPact(DefensivePact pact)
        {
            if (_pacts == null)
            {
                _pacts = new List<DefensivePact>();
            }

            _pacts.Add(pact);
        }

        private void ProcessAIDefensivePacts()
        {
            var kingdoms = Kingdom.All.Where(k => k != null && !k.IsEliminated).ToList();

            for (int i = 0; i < kingdoms.Count; i++)
            {
                for (int j = i + 1; j < kingdoms.Count; j++)
                {
                    var k1 = kingdoms[i];
                    var k2 = kingdoms[j];

                    if (k1.IsAtWarWith(k2) || HasDefensivePact(k1, k2)) continue;

                    Clan rulingClan1 = k1.RulingClan;
                    Clan rulingClan2 = k2.RulingClan;

                    if (rulingClan1 != null && rulingClan2 != null)
                    {
                        float relation = rulingClan1.GetRelationWithClan(rulingClan2);

                        if (relation > 15f && MBRandom.RandomFloat < 0.03f)
                        {
                            AddPact(new DefensivePact(k1, k2, 60.0));
                        }
                    }
                }
            }
        }
    }
}