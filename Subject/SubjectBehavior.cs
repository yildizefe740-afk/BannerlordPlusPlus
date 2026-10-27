using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BannerlordPlusPlus
{
    public class SubjectBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, SubjectAttributes> _activeSubjects = new();
        private static readonly HashSet<string> _revoltNotified = new();
        private const float IndependenceStrengthRatio = 1.2f;

        public static SubjectBehavior Instance { get; private set; }

        public SubjectBehavior()
        {
            Instance = this;
        }

        private string GetSubjectKey(Kingdom senior, Kingdom junior)
        {
            if (senior == null || junior == null) return string.Empty;
            return $"{senior.StringId}_{junior.StringId}";
        }

        public void AddSubject(Kingdom senior, Kingdom junior, SubjectType type = SubjectType.Vassal)
        {
            if (senior == null || junior == null || senior == junior)
            {
                InformationManager.DisplayMessage(new InformationMessage("AddSubject Error: Senior or Junior is null, or they are the same kingdom.", Colors.Red));
                return;
            }
            if (_activeSubjects == null) _activeSubjects = new();

            // Check whether a relationship already exists between them.
            if (GetSubjectBetween(senior, junior) != null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"AddSubject Warning: A subject relation already exists between {senior.Name} and {junior.Name}.", Colors.Yellow));
                return;
            }

            if (IsRestrictedSubjectType(type))
            {
                if (GetControlledSubject(junior) != null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"AddSubject Error: {junior.Name} is already a subject of another kingdom.", Colors.Red));
                    return;
                }
                if (GetControlledSubject(senior) != null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"AddSubject Error: {senior.Name} is already a subject and cannot be an overlord.", Colors.Red));
                    return;
                }
                if (HasRestrictedSubjects(junior))
                {
                    InformationManager.DisplayMessage(new InformationMessage($"AddSubject Error: {junior.Name} already has vassals/subjects and cannot become a subject.", Colors.Red));
                    return;
                }
            }

            string key = GetSubjectKey(senior, junior);
            if (_activeSubjects.ContainsKey(key))
            {
                InformationManager.DisplayMessage(new InformationMessage($"AddSubject Warning: Dictionary already contains key {key}.", Colors.Yellow));
                return;
            }

            SubjectAttributes subject = new SubjectAttributes
            {
                SeniorKingdomId = senior.StringId,
                JuniorKingdomId = junior.StringId,
                Type = type,
                LibertyDesire = 0f,
                IsRebelling = false
            };

            _activeSubjects[key] = subject;
            KingdomsUI.Invalidate();

            BringSubjectIntoCurrentWars(subject);

            InformationManager.DisplayMessage(new InformationMessage(
                $"{junior.Name} is now a subject ({type}) of {senior.Name}.", Colors.Green
            ));
        }

        public override void RegisterEvents()
        {
            CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
            CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("BannerlordPlusPlus_ActiveSubjects", ref _activeSubjects);

            if (dataStore.IsLoading && _activeSubjects == null)
                _activeSubjects = new();
        }

        private void OnDailyTick()
        {
            if (_activeSubjects == null || _activeSubjects.Count == 0) return;

            foreach (var pair in _activeSubjects.ToList())
            {
                SubjectAttributes subject = pair.Value;
                if (subject == null)
                {
                    _activeSubjects.Remove(pair.Key);
                    continue;
                }

                Kingdom senior = FindKingdom(subject.SeniorKingdomId);
                Kingdom junior = FindKingdom(subject.JuniorKingdomId);

                if (senior == null || junior == null || senior.IsEliminated || junior.IsEliminated)
                {
                    _activeSubjects.Remove(pair.Key);
                    _revoltNotified.Remove(pair.Key);
                    KingdomsUI.Invalidate();
                    continue;
                }

                UpdateLibertyDesire(subject, senior, junior);
                TriggerIndependenceWar(subject, senior, junior);
            }
        }

        public void EndSubject(Kingdom senior, Kingdom junior, string reason)
        {
            if (_activeSubjects == null || senior == null || junior == null) return;

            string key = GetSubjectKey(senior, junior);
            if (!_activeSubjects.Remove(key)) return;

            _revoltNotified.Remove(key);
            KingdomsUI.Invalidate();

            InformationManager.DisplayMessage(new InformationMessage(
                $"{junior.Name} gained independence against {senior.Name} ({reason})", Colors.Green
            ));
        }

        public void OnKingdomDestroyed(Kingdom destroyedKingdom)
        {
            if (destroyedKingdom == null || _activeSubjects == null) return;

            List<string> keysToRemove = _activeSubjects
                .Where(p => p.Value != null &&
                            (p.Value.SeniorKingdomId == destroyedKingdom.StringId ||
                             p.Value.JuniorKingdomId == destroyedKingdom.StringId))
                .Select(p => p.Key)
                .ToList();

            foreach (string key in keysToRemove)
            {
                _activeSubjects.Remove(key);
                _revoltNotified.Remove(key);
            }

            if (keysToRemove.Count > 0)
                KingdomsUI.Invalidate();
        }

        public void UpdateLibertyDesire(SubjectAttributes subject, Kingdom senior, Kingdom junior)
        {
            if (subject == null || senior == null || junior == null) return;

            int juniorSettlements = junior.Settlements.Count;
            int seniorSettlements = senior.Settlements.Count;

            float delta = 0f;

            if (juniorSettlements > seniorSettlements) delta += 1f;
            else if (seniorSettlements > juniorSettlements) delta -= 1f;

            int juniorTax = junior.TaxAmount();
            int seniorTax = senior.TaxAmount();
            if (juniorTax > seniorTax) delta += 2f;
            else if (juniorTax < seniorTax) delta -= 2f;

            if (junior.CurrentTotalStrength > senior.CurrentTotalStrength) delta += 5f;
            else if (junior.CurrentTotalStrength < senior.CurrentTotalStrength) delta -= 5f;

            subject.LibertyDesire = MBMath.ClampFloat(subject.LibertyDesire + delta, 0f, 100f);

            if (subject.LibertyDesire < 100f)
                _revoltNotified.Remove(GetSubjectKey(senior, junior));
        }

        public void TriggerIndependenceWar(SubjectAttributes subject, Kingdom senior, Kingdom junior)
        {
            if (subject == null || !IsRestrictedSubjectType(subject.Type)) return;
            if (subject.IsRebelling) return;
            if (senior.IsEliminated || junior.IsEliminated) return;
            if (junior.IsAtWarWith(senior)) return;
            if (subject.LibertyDesire < 100f) return;

            bool isPlayerJunior = Clan.PlayerClan?.Kingdom != null && junior == Clan.PlayerClan.Kingdom;

            if (isPlayerJunior)
            {
                string key = GetSubjectKey(senior, junior);
                if (_revoltNotified.Add(key))
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Your people have had enough of serving {senior.Name}! Declare war on them to begin the War of Independence.",
                        Colors.Green
                    ));
                }
                return;
            }

            if (MBRandom.RandomFloat >= 0.15f) return;

            subject.IsRebelling = true;
            DeclareWarAction.ApplyByDefault(junior, senior);

            if (!junior.IsAtWarWith(senior))
            {
                subject.IsRebelling = false;
                return;
            }

            InformationManager.DisplayMessage(new InformationMessage(
                $"{junior.Name} has revolted against its overlord {senior.Name}!", Colors.Red
            ));
        }

        private void ResolveIndependenceWar(SubjectAttributes subject, Kingdom senior, Kingdom junior)
        {
            if (subject == null || !subject.IsRebelling) return;

            subject.IsRebelling = false;
            subject.LibertyDesire = 0f;

            if (senior == null || junior == null) return;

            bool independent = junior.CurrentTotalStrength > senior.CurrentTotalStrength * IndependenceStrengthRatio;
            if (independent)
            {
                EndSubject(senior, junior, "Independence War");
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"The rebellion of {junior.Name} against {senior.Name} has been crushed.", Colors.Red
                ));
            }
        }

        public static SubjectAttributes GetSubjectBetween(Kingdom k1, Kingdom k2)
        {
            if (Instance == null || Instance._activeSubjects == null || k1 == null || k2 == null) return null;

            string key1 = Instance.GetSubjectKey(k1, k2);
            if (Instance._activeSubjects.TryGetValue(key1, out var subject1)) return subject1;

            string key2 = Instance.GetSubjectKey(k2, k1);
            if (Instance._activeSubjects.TryGetValue(key2, out var subject2)) return subject2;

            return null;
        }

        public static SubjectAttributes GetControlledSubject(Kingdom junior)
        {
            if (Instance == null || Instance._activeSubjects == null || junior == null)
                return null;

            return Instance._activeSubjects.Values.FirstOrDefault(subject =>
                subject != null &&
                subject.JuniorKingdomId == junior.StringId &&
                IsRestrictedSubjectType(subject.Type));
        }

        public static bool IsRestrictedSubjectType(SubjectType type)
        {
            return type == SubjectType.Vassal || type == SubjectType.March;
        }

        private static bool HasRestrictedSubjects(Kingdom senior)
        {
            if (Instance == null || Instance._activeSubjects == null || senior == null)
                return false;

            return Instance._activeSubjects.Values.Any(subject =>
                subject != null &&
                subject.SeniorKingdomId == senior.StringId &&
                IsRestrictedSubjectType(subject.Type));
        }

        private static bool SharesSenior(Kingdom a, Kingdom b)
        {
            SubjectAttributes sa = GetControlledSubject(a);
            SubjectAttributes sb = GetControlledSubject(b);
            return sa != null && sb != null && sa.SeniorKingdomId == sb.SeniorKingdomId;
        }

        public static bool CanCreateDiplomaticPact(Kingdom kingdom1, Kingdom kingdom2)
        {
            return kingdom1 != null &&
                   kingdom2 != null &&
                   GetControlledSubject(kingdom1) == null &&
                   GetControlledSubject(kingdom2) == null;
        }

        public static bool CanCreateTradeAgreement(Kingdom kingdom1, Kingdom kingdom2)
        {
            if (kingdom1 == null || kingdom2 == null)
                return false;

            SubjectAttributes subject1 = GetControlledSubject(kingdom1);
            SubjectAttributes subject2 = GetControlledSubject(kingdom2);

            if (subject1 == null && subject2 == null)
                return true;

            return subject1 != null &&
                   subject2 != null &&
                   subject1.SeniorKingdomId == subject2.SeniorKingdomId;
        }

        public static bool CanCreateDiplomaticPact(string kingdom1Id, string kingdom2Id)
        {
            return CanCreateDiplomaticPact(FindKingdom(kingdom1Id), FindKingdom(kingdom2Id));
        }

        public static bool CanCreateTradeAgreement(string kingdom1Id, string kingdom2Id)
        {
            return CanCreateTradeAgreement(FindKingdom(kingdom1Id), FindKingdom(kingdom2Id));
        }

        private static Kingdom FindKingdom(string kingdomId)
        {
            return string.IsNullOrEmpty(kingdomId)
                ? null
                : Kingdom.All.FirstOrDefault(kingdom => kingdom != null && kingdom.StringId == kingdomId);
        }

        private void OnWarDeclared(
            IFaction faction1,
            IFaction faction2,
            DeclareWarAction.DeclareWarDetail detail)
        {
            if (_activeSubjects == null || faction1 == null || faction2 == null)
                return;

            foreach (SubjectAttributes subject in _activeSubjects.Values.ToList())
            {
                if (subject == null || !IsRestrictedSubjectType(subject.Type))
                    continue;

                Kingdom senior = FindKingdom(subject.SeniorKingdomId);
                Kingdom junior = FindKingdom(subject.JuniorKingdomId);
                if (senior == null || junior == null)
                    continue;

                if (faction1 == junior && faction2 == senior)
                {
                    subject.IsRebelling = true;
                    continue;
                }

                if (subject.IsRebelling)
                    continue;

                if (faction1 == senior && faction2 != junior)
                {
                    SubjectMethods.CallToWar(senior, junior, faction2);
                }
                else if (faction2 == senior && faction1 != junior)
                {
                    SubjectMethods.CallToWar(senior, junior, faction1);
                }
                else if (faction2 == junior && faction1 != senior && !senior.IsAtWarWith(faction1))
                {
                    DeclareWarAction.ApplyByDefault(senior, faction1);
                }
            }
        }

        private void OnMakePeace(
            IFaction faction1,
            IFaction faction2,
            MakePeaceAction.MakePeaceDetail detail)
        {
            if (_activeSubjects == null || faction1 == null || faction2 == null)
                return;

            foreach (SubjectAttributes subject in _activeSubjects.Values.ToList())
            {
                if (subject == null || !IsRestrictedSubjectType(subject.Type))
                    continue;

                Kingdom senior = FindKingdom(subject.SeniorKingdomId);
                Kingdom junior = FindKingdom(subject.JuniorKingdomId);
                if (senior == null || junior == null)
                    continue;

                bool isIndependencePair =
                    (faction1 == junior && faction2 == senior) ||
                    (faction1 == senior && faction2 == junior);

                if (isIndependencePair)
                {
                    ResolveIndependenceWar(subject, senior, junior);
                    continue;
                }

                if (subject.IsRebelling)
                    continue;

                IFaction enemy = faction1 == senior ? faction2 : faction2 == senior ? faction1 : null;
                if (enemy != null && junior.IsAtWarWith(enemy))
                    MakePeaceAction.Apply(junior, enemy);
            }
        }

        private void BringSubjectIntoCurrentWars(SubjectAttributes subject)
        {
            if (subject == null || !IsRestrictedSubjectType(subject.Type))
                return;

            Kingdom senior = FindKingdom(subject.SeniorKingdomId);
            Kingdom junior = FindKingdom(subject.JuniorKingdomId);
            if (senior == null || junior == null)
                return;

            foreach (Kingdom enemy in Kingdom.All.ToList())
            {
                if (enemy != null && !enemy.IsEliminated && enemy != junior && senior.IsAtWarWith(enemy))
                    SubjectMethods.CallToWar(senior, junior, enemy);
            }
        }

        [HarmonyPatch(typeof(DeclareWarAction), "ApplyInternal")]
        public static class PreventIndependentSubjectWarsPatch
        {
            public static bool Prefix(IFaction faction1, IFaction faction2)
            {
                if (!(faction1 is Kingdom aggressor))
                    return true;

                Kingdom defender = faction2 as Kingdom;

                SubjectAttributes subject = GetControlledSubject(aggressor);
                if (subject != null)
                {
                    Kingdom senior = FindKingdom(subject.SeniorKingdomId);
                    if (senior == null)
                        return true;

                    if (faction2 == senior)
                        return subject.IsRebelling || subject.LibertyDesire >= 100f;

                    if (defender != null && SharesSenior(aggressor, defender))
                        return false;

                    return faction2 != null && senior.IsAtWarWith(faction2);
                }

                if (defender != null)
                {
                    SubjectAttributes defenderSubject = GetControlledSubject(defender);
                    if (defenderSubject != null && defenderSubject.SeniorKingdomId == aggressor.StringId)
                        return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(MakePeaceAction), "ApplyInternal")]
        public static class PreventIndependentSubjectPeacePatch
        {
            public static bool Prefix(IFaction faction1, IFaction faction2)
            {
                return CanMakePeace(faction1, faction2) && CanMakePeace(faction2, faction1);
            }

            private static bool CanMakePeace(IFaction faction, IFaction opponent)
            {
                if (!(faction is Kingdom kingdom))
                    return true;

                SubjectAttributes subject = GetControlledSubject(kingdom);
                if (subject == null)
                    return true;

                Kingdom senior = FindKingdom(subject.SeniorKingdomId);
                if (senior == null || subject.IsRebelling || opponent == senior)
                    return true;

                return !senior.IsAtWarWith(opponent);
            }
        }
    }
}