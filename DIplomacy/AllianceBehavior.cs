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
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
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
            if (alliance == null) return true;

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

        private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
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

                // TODO: Display UI confirmation prompt if ally is player's kingdom
                if (ally == playerKingdom) continue;

                CallToDefensiveWar(alliance, ally, defender, attacker);
            }
        }

        private void CallToDefensiveWar(Alliance alliance, Kingdom ally, Kingdom defender, Kingdom attacker)
        {
            float rng = MBRandom.RandomFloat;

            if (IsRejected(ally, defender, attacker, rng))
            {
                alliance.Trust /= 3f;
                // TODO: Dissolve alliance + apply truce / feud penalty if Trust falls below critical threshold
                return;
            }

            // TODO: Check CustomPeaceBehavior.HasActiveTruce(ally, attacker) before applying war to prevent unintended truce breaks
            DeclareWarAction.ApplyByDefault(ally, attacker);
        }
    }
}