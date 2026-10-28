using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BannerlordPlusPlus
{
    public class NonAggressionPactBehavior : CampaignBehaviorBase
    {
        // Keep this static for existing callers, but reset it for each new behavior instance.
        private static List<NonAggressionPact> _NApact = new List<NonAggressionPact>();

        public NonAggressionPactBehavior()
        {
            // Prevent pacts from a previous session from leaking into a new game or save.
            // When loading a save, SyncData replaces this list with the saved data.
            _NApact = new List<NonAggressionPact>();
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("BannerlordPlusPlus_NonAggressionPacts", ref _NApact);

            if (dataStore.IsLoading && _NApact == null)
                _NApact = new List<NonAggressionPact>();
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

        public static bool AddPact(Kingdom k1, Kingdom k2, double durationDays)
        {
            if (k1 == null || k2 == null || k1 == k2)
                return false;

            if (!SubjectBehavior.CanCreateDiplomaticPact(k1, k2))
                return false;

            if (_NApact == null)
                _NApact = new List<NonAggressionPact>();

            if (IsPactActive(k1.StringId, k2.StringId))
                return false;

            _NApact.RemoveAll(p => p == null || p.IsExpired);
            _NApact.Add(new NonAggressionPact(k1, k2, durationDays));
            return true;
        }

        /// <summary>Removes pacts when a kingdom becomes a vassal/march or is destroyed.</summary>
        public static int RemoveAllFor(string kingdomId)
        {
            if (_NApact == null || string.IsNullOrEmpty(kingdomId))
                return 0;

            return _NApact.RemoveAll(p => p != null && (p.Kingdom1Id == kingdomId || p.Kingdom2Id == kingdomId));
        }

        public static bool IsPactActive(string id1, string id2)
        {
            if (_NApact == null || string.IsNullOrEmpty(id1) || string.IsNullOrEmpty(id2))
                return false;

            return _NApact.Any(p =>
                p != null && !p.IsExpired &&
                ((p.Kingdom1Id == id1 && p.Kingdom2Id == id2) ||
                 (p.Kingdom1Id == id2 && p.Kingdom2Id == id1)));
        }

        public bool HasNonAggressionPact(Kingdom k1, Kingdom k2)
        {
            if (k1 == null || k2 == null)
                return false;

            return IsPactActive(k1.StringId, k2.StringId);
        }

        // ApplyInternal handles war declarations from decisions, defaults, rebellions, and other sources.
        [HarmonyPatch(typeof(DeclareWarAction), "ApplyInternal")]
        public static class ApplyNonAggressionPact
        {
            public static bool Prefix(IFaction faction1, IFaction faction2)
            {
                if (faction1 == null || faction2 == null)
                    return true;

                if (!IsPactActive(faction1.StringId, faction2.StringId))
                    return true;

                // Explain the block to the player without spamming messages for AI attempts.
                Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
                if (playerKingdom != null && faction1 == playerKingdom)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"A non-aggression pact with {faction2.Name} prevents you from declaring war.",
                        Colors.Yellow));
                }

                return false;
            }
        }

        public void ProcessAINAP()
        {
            Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;

            var kingdoms = Kingdom.All
                .Where(k => k != null && !k.IsEliminated && k != playerKingdom) // Do not create pacts on behalf of the player.
                .ToList();

            for (int i = 0; i < kingdoms.Count; i++)
            {
                for (int j = i + 1; j < kingdoms.Count; j++)
                {
                    var k1 = kingdoms[i];
                    var k2 = kingdoms[j];

                    if (k1.IsAtWarWith(k2) ||
                        HasNonAggressionPact(k1, k2) ||
                        !SubjectBehavior.CanCreateDiplomaticPact(k1, k2))
                    {
                        continue;
                    }

                    Clan rulingClan1 = k1.RulingClan;
                    Clan rulingClan2 = k2.RulingClan;

                    if (rulingClan1 != null && rulingClan2 != null)
                    {
                        float relation = rulingClan1.GetRelationWithClan(rulingClan2);

                        if (relation > 15f && MBRandom.RandomFloat < 0.03f)
                        {
                            AddPact(k1, k2, 60.0);
                        }
                    }
                }
            }
        }
    }
}