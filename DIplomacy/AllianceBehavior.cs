using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;

namespace BannerlordPlusPlus
{
    public class AllianceBehavior : CampaignBehaviorBase
    {
        // Decision Thresholds
        private const float HighTrustThreshold = 50f;      // Below this value is considered "Low Trust"
        private const float LowTrustAcceptChance = 0.20f;  // 20% chance to join when trust is low
        private const float DesperateAcceptChance = 0.15f; // 15% "honor over strength" chance when outpowered

        private List<Alliance> _alliances = new();

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("BannerlordPlusPlus_ActiveAlliances", ref _alliances);
            if (dataStore.IsLoading && _alliances == null)
                _alliances = new List<Alliance>();
        }

        public override void RegisterEvents()
        {
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnDefensiveWarDeclared);
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnOffensiveWarDeclared);
            CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
        }

        // ---------------------------------------------------------------------
        // Data Operations
        // ---------------------------------------------------------------------

        public void AddAlliance(Alliance alliance)
        {
            if (alliance == null ||
                string.IsNullOrEmpty(alliance.kingdom1ID) ||
                string.IsNullOrEmpty(alliance.kingdom2ID) ||
                alliance.kingdom1ID == alliance.kingdom2ID ||
                !SubjectBehavior.CanCreateDiplomaticPact(alliance.kingdom1ID, alliance.kingdom2ID))
                return;

            if (_alliances == null)
                _alliances = new List<Alliance>();

            // Prevent duplicate alliance entries for the same kingdom pair
            if (_alliances.Any(a => a != null && Matches(a, alliance.kingdom1ID, alliance.kingdom2ID)))
                return;

            _alliances.Add(alliance);
        }

        public bool HasAlliance(Kingdom k1, Kingdom k2)
        {
            return GetAllianceBetween(k1, k2) != null;
        }

        public Alliance GetAllianceBetween(Kingdom k1, Kingdom k2)
        {
            if (k1 == null || k2 == null || _alliances == null) return null;

            return _alliances.FirstOrDefault(a => a != null && Matches(a, k1.StringId, k2.StringId));
        }

        /// <summary>Returns true if the alliance matches the pair (A, B) or (B, A).</summary>
        private static bool Matches(Alliance a, string id1, string id2)
        {
            return (a.kingdom1ID == id1 && a.kingdom2ID == id2) ||
                   (a.kingdom1ID == id2 && a.kingdom2ID == id1);
        }

        private static bool Involves(Alliance a, Kingdom k)
        {
            return a != null && k != null && (a.kingdom1ID == k.StringId || a.kingdom2ID == k.StringId);
        }

        /// <summary>Call from SubjectBehavior.PurgeDiplomaticTies to break alliances when becoming a vassal/march.</summary>
        public int RemoveAllFor(string kingdomId)
        {
            if (_alliances == null) return 0;
            return _alliances.RemoveAll(a => a != null && (a.kingdom1ID == kingdomId || a.kingdom2ID == kingdomId));
        }

        private void OnKingdomDestroyed(Kingdom destroyedKingdom)
        {
            if (destroyedKingdom == null) return;
            RemoveAllFor(destroyedKingdom.StringId);
        }

        private static Kingdom FindKingdom(string kingdomId)
        {
            return string.IsNullOrEmpty(kingdomId)
                ? null
                : Kingdom.All.FirstOrDefault(k => k != null && k.StringId == kingdomId);
        }

        // ---------------------------------------------------------------------
        // Decision Evaluation (Pure Function)
        // ---------------------------------------------------------------------

        /// <param name="ally">The kingdom evaluating the call (called ally)</param>
        /// <param name="defender">The allied kingdom being attacked</param>
        /// <param name="aggressor">The attacking kingdom</param>
        /// <param name="rng">Random float between 0..1 (generated once per evaluation)</param>
        /// <returns>true if the ally rejects the call to war</returns>
        public bool IsRejected(Kingdom ally, Kingdom defender, Kingdom aggressor, float rng)
        {
            Alliance alliance = GetAllianceBetween(ally, defender);
            if (alliance == null || ally == null || defender == null || aggressor == null) return true;

            // Low trust evaluation: High chance to betray/refuse
            if (alliance.Trust < HighTrustThreshold)
                return rng >= LowTrustAcceptChance;

            // High trust evaluation: Check combined total strength against aggressor
            float combinedPower = ally.CurrentTotalStrength + defender.CurrentTotalStrength;
            if (combinedPower >= aggressor.CurrentTotalStrength)
                return false;

            // Outpowered evaluation: 15% chance to stand by the ally despite losing odds
            return rng >= DesperateAcceptChance;
        }

        // ---------------------------------------------------------------------
        // Defensive Call Logic
        // ---------------------------------------------------------------------

        private void OnDefensiveWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
        {
            if (_alliances == null || _alliances.Count == 0) return;
            if (!(faction1 is Kingdom attacker) || !(faction2 is Kingdom defender)) return;

            Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;

            // Iterate over a copy to prevent modification exceptions during nested war declarations
            foreach (Alliance alliance in _alliances.ToList())
            {
                if (!Involves(alliance, defender))
                    continue;

                string allyId = alliance.kingdom1ID == defender.StringId ? alliance.kingdom2ID : alliance.kingdom1ID;
                Kingdom ally = FindKingdom(allyId);

                if (ally == null || ally.IsEliminated) continue;
                if (ally == attacker) continue;              // Do not call an ally if they are the attacker
                if (ally.IsAtWarWith(attacker)) continue;    // Already at war, skip calling
                if (HasTruceBetween(ally, attacker) || HasTruceBetween(attacker, defender))
                    continue;

                CallToDefensiveWar(alliance, ally, defender, attacker);
            }
        }

        private void OnOffensiveWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
        {
            if (_alliances == null || _alliances.Count == 0) return;
            if (!(faction1 is Kingdom attacker) || !(faction2 is Kingdom defender)) return;

            Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
            if (attacker == playerKingdom)
                return;

            foreach (Alliance alliance in _alliances.ToList())
            {
                if (!Involves(alliance, attacker))
                    continue;

                string allyId = alliance.kingdom1ID == attacker.StringId ? alliance.kingdom2ID : alliance.kingdom1ID;
                Kingdom ally = FindKingdom(allyId);

                if (ally == null || ally.IsEliminated)
                    continue;
                if (ally.IsAtWarWith(defender))
                    continue;
                if (HasTruceBetween(ally, defender) || HasTruceBetween(attacker, defender))
                    continue;

                CallToOffensiveWar(alliance, ally, attacker, defender);
            }
        }

        private void CallToDefensiveWar(Alliance alliance, Kingdom ally, Kingdom defender, Kingdom attacker)
        {
            if (alliance == null || ally == null || defender == null || attacker == null)
                return;

            Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
            if (ally == playerKingdom)
            {
                PupUpsUI.ShowCallToWarPopup(
                    defender,
                    attacker,
                    true,
                    () =>
                    {
                        if (!HasTruceBetween(ally, attacker) &&
                            !HasTruceBetween(attacker, defender) &&
                            !ally.IsAtWarWith(attacker))
                            DeclareWarAction.ApplyByDefault(ally, attacker);
                    },
                    () => RejectDefensiveWarCall(alliance, ally, defender));
                return;
            }

            float rng = MBRandom.RandomFloat;

            if (IsRejected(ally, defender, attacker, rng))
            {
                RejectDefensiveWarCall(alliance, ally, defender);
                return;
            }

            DeclareWarAction.ApplyByDefault(ally, attacker);
        }

        private void RejectDefensiveWarCall(Alliance alliance, Kingdom ally, Kingdom defender)
        {
            if (alliance == null || ally == null || defender == null)
                return;

            _alliances?.Remove(alliance);
            alliance.Trust /= 3f;

            var peaceBehavior = Campaign.Current?.GetCampaignBehavior<CustomPeaceBehavior>();
            peaceBehavior?.AddTruce(ally, defender, 60.0);
        }

        private void CallToOffensiveWar(Alliance alliance, Kingdom ally, Kingdom offensiveAlly, Kingdom target)
        {
            if (alliance == null || ally == null || offensiveAlly == null || target == null)
                return;

            if (HasTruceBetween(ally, target) || HasTruceBetween(offensiveAlly, target))
                return;

            Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
            if (ally == playerKingdom)
            {
                PupUpsUI.ShowCallToWarPopup(
                    offensiveAlly,
                    target,
                    false,
                    () =>
                    {
                        if (!HasTruceBetween(ally, target) && !ally.IsAtWarWith(target))
                            DeclareWarAction.ApplyByDefault(ally, target);
                    },
                    () => RejectOffensiveWarCall(alliance));
                return;
            }

            float rng = MBRandom.RandomFloat;
            if (IsRejected(ally, offensiveAlly, target, rng))
            {
                RejectOffensiveWarCall(alliance);
                return;
            }

            if (!ally.IsAtWarWith(target))
                DeclareWarAction.ApplyByDefault(ally, target);
        }

        private static void RejectOffensiveWarCall(Alliance alliance)
        {
            if (alliance == null)
                return;

            alliance.Trust = System.Math.Max(0f, alliance.Trust - 10f);
        }

        private static bool HasTruceBetween(Kingdom kingdom1, Kingdom kingdom2)
        {
            return CustomPeaceBehavior.Instance?.HasActiveTruce(kingdom1, kingdom2) == true;
        }
    }
}