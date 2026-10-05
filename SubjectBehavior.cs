using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BannerlordPlusPlus
{
    public class SubjectBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, SubjectAttributes> _activeSubjects = new();

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

        // --- EKSİK OLAN ADD SUBJECT METODU ---
        public void AddSubject(Kingdom senior, Kingdom junior, SubjectType type = SubjectType.Vassal)
        {
            if (senior == null || junior == null || senior == junior) return;

            string key = GetSubjectKey(senior, junior);
            if (!_activeSubjects.ContainsKey(key))
            {
                SubjectAttributes subject = new SubjectAttributes
                {
                    SeniorKingdomId = senior.StringId,
                    JuniorKingdomId = junior.StringId,
                    Type = type,
                    LibertyDesire = 0f,
                    IsRebelling = false
                };

                _activeSubjects[key] = subject;

                InformationManager.DisplayMessage(new InformationMessage(
                    $"{junior.Name} is now a subject ({type}) of {senior.Name}.", Colors.Green
                ));
            }
        }

        public override void RegisterEvents()
        {
            CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("BannerlordPlusPlus_ActiveSubjects", ref _activeSubjects);      
        }

        private void OnDailyTick()
        {
            if (_activeSubjects == null || _activeSubjects.Count == 0) return;

            foreach (var pair in _activeSubjects.Values.ToList())
            {
                UpdateLibertyDesire(pair);
                TriggerIndependenceWar(pair);
            }
        }

        public void EndSubject(Kingdom senior, Kingdom junior, string reason)
        {
            string key = GetSubjectKey(senior, junior);

            if (_activeSubjects.ContainsKey(key))
            {
                _activeSubjects.Remove(key);

                InformationManager.DisplayMessage(new InformationMessage(
                    $"{junior.Name} gained independence against {senior.Name} ({reason})", Colors.Green
                ));
            }
        }

        public void OnKingdomDestroyed(Kingdom destroyedKingdom)
        {
            List<string> keysToRemove = new();

            foreach (var pair in _activeSubjects)
            {
                if (pair.Value.SeniorKingdomId == destroyedKingdom.StringId || 
                    pair.Value.JuniorKingdomId == destroyedKingdom.StringId)
                {
                    keysToRemove.Add(pair.Key);
                }
            }

            foreach (string key in keysToRemove)
            {
                var subject = _activeSubjects[key];
                EndSubject(subject.SeniorKingdom, subject.JuniorKingdom, "Kingdom Destroyed");
            }
        }

        public void UpdateLibertyDesire(SubjectAttributes subject)
        {   
            if (subject == null) return;

            Kingdom senior = subject.SeniorKingdom;
            Kingdom junior = subject.JuniorKingdom;

            if (senior == null || junior == null) return;

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
        }
    
        public void TriggerIndependenceWar(SubjectAttributes subject)
        {
            if (subject == null) return;
            
            Kingdom senior = subject.SeniorKingdom;
            Kingdom junior = subject.JuniorKingdom;

            if (senior == null || junior == null) return;
            if (junior.IsAtWarWith(senior)) return;

            if (subject.LibertyDesire >= 100f)
            {
                bool isPlayerJunior = Clan.PlayerClan.Kingdom != null && junior == Clan.PlayerClan.Kingdom;

                if (!isPlayerJunior)
                {
                    if (MBRandom.RandomFloat < 0.15f)
                    {
                        subject.IsRebelling = true;
                        DeclareWarAction.ApplyByDefault(junior, senior);

                        InformationManager.DisplayMessage(new InformationMessage(
                            $"{junior.Name} has revolted against its overlord {senior.Name}!", Colors.Red
                        ));
                    }
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "You and your people had enough of this bond! Time to REVOLT!", Colors.Green
                    ));
                }
            }
        }

        public static SubjectAttributes GetSubjectBetween(Kingdom k1, Kingdom k2)
        {
            if (Instance == null || k1 == null || k2 == null) return null;

            string key1 = Instance.GetSubjectKey(k1, k2);
            if (Instance._activeSubjects.TryGetValue(key1, out var subject1)) return subject1;

            string key2 = Instance.GetSubjectKey(k2, k1);
            if (Instance._activeSubjects.TryGetValue(key2, out var subject2)) return subject2;

            return null;
        }
    }
}