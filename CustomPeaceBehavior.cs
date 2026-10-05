using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BannerlordPlusPlus
{
    public class CustomPeaceBehavior : CampaignBehaviorBase
    {
        public static CustomPeaceBehavior Instance { get; private set; }
        public static bool IsExecutingCustomPeace = false;

        private List<PeaceAgreement> _activeTruces = new();
        private Dictionary<string, string> _reparationTargets = new(); // WinnerId -> LoserId

        public CustomPeaceBehavior()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("ActiveTruces", ref _activeTruces);
            dataStore.SyncData("ReparationTargets", ref _reparationTargets);

            _activeTruces ??= new List<PeaceAgreement>();
            _reparationTargets ??= new Dictionary<string, string>();
        }

        public bool HasActiveTruce(Kingdom k1, Kingdom k2)
        {
            return _activeTruces.Any(a => a.Involves(k1, k2) && !a.IsExpired);
        }

        public void ProcessGlobalPeaceRequest(Kingdom k1, Kingdom k2, int requestedTribute, PeaceDealOptions options = PeaceDealOptions.None)
        {
            SubjectAttributes subject = SubjectBehavior.GetSubjectBetween(k1, k2);
            if (subject != null && subject.IsRebelling)
            {
                ExecuteIndependenceWarPeace(subject, k1, k2);
                return;
            }

            ExecuteStandardGlobalPeace(k1, k2, requestedTribute, options);
        }

        private void ExecuteStandardGlobalPeace(Kingdom k1, Kingdom k2, int requestedTribute, PeaceDealOptions options)
        {
            IsExecutingCustomPeace = true;

            // 1. Oyunun varsayılan barış aksiyonu
            MakePeaceAction.Apply(k1, k2);

            // 2. CK3 Tarzı Uzun Soluklu Truce (Minimum 100 Gün, Truce seçildiyse 200 Gün)
            double truceDuration = options.HasFlag(PeaceDealOptions.Truce) ? 200.0 : 100.0;
            _activeTruces.RemoveAll(a => a.Involves(k1, k2));
            _activeTruces.Add(new PeaceAgreement(k1, k2, truceDuration));

            // 3. PlunderTreasury: Krallık hazinesini BÜTÜNÜYLE yağmalama
            if (options.HasFlag(PeaceDealOptions.PlunderTreasury))
            {
                int loserTreasury = TreasuryBehavior.GetTreasury(k2);
                if (loserTreasury > 0)
                {
                    TreasuryBehavior.AddToTreasury(k2, -loserTreasury);
                    TreasuryBehavior.AddToTreasury(k1, loserTreasury);
                    
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{k1.Name}, {k2.Name} krallığının hazinesindeki {loserTreasury:N0} Dinarı tamamen yağmaladı!", Colors.Yellow));
                }
            }

            // 4. WarReparation: Ateşkes süresince ödenmek üzere tazminat bağlama
            if (options.HasFlag(PeaceDealOptions.WarReparation))
            {
                _reparationTargets[k1.StringId] = k2.StringId;
            }

            // 5. Subjectize / ExecuteRuler Diğer Opsiyonlar
            if (options.HasFlag(PeaceDealOptions.Subjectize))
            {
                SubjectBehavior.Instance?.AddSubject(k1, k2);
            }

            if (options.HasFlag(PeaceDealOptions.ExecuteRuler) && k2.Leader != null && k2.Leader.IsAlive)
            {
                KillCharacterAction.ApplyByExecution(k2.Leader, k1.Leader);
            }

            InformationManager.DisplayMessage(new InformationMessage(
                $"A diplomatic peace treaty has been established between {k1.Name} and {k2.Name} ({truceDuration} days truce).", 
                Colors.White));

            IsExecutingCustomPeace = false;
        }

        private void ExecuteIndependenceWarPeace(SubjectAttributes subject, Kingdom k1, Kingdom k2)
        {
            IsExecutingCustomPeace = true;

            Kingdom senior = subject.SeniorKingdom;
            Kingdom junior = subject.JuniorKingdom;

            if (junior.CurrentTotalStrength > senior.CurrentTotalStrength * 1.2f)
            {
                subject.IsRebelling = false;
                SubjectBehavior.Instance?.EndSubject(senior, junior, "Won Independence War");
                MakePeaceAction.Apply(senior, junior);
            }
            else
            {
                subject.IsRebelling = false;
                subject.LibertyDesire = 0f;
                MakePeaceAction.Apply(senior, junior);
            }

            // İsyan savaşlarında 150 günlük uzun ateşkes süresi
            _activeTruces.RemoveAll(a => a.Involves(senior, junior));
            _activeTruces.Add(new PeaceAgreement(senior, junior, 150.0));

            IsExecutingCustomPeace = false;
        }

        private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
        {
            if (faction1 is Kingdom k1 && faction2 is Kingdom k2)
            {
                if (HasActiveTruce(k1, k2))
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{k1.Name} ile {k2.Name} arasındaki ateşkes anlaşması henüz sona ermedi!", Colors.Red));
                }
            }
        }

        private void OnDailyTick()
        {
            // Süresi dolan Truce'ları temizle
            _activeTruces.RemoveAll(a => a.IsExpired);

            // WarReparation: ATEŞKES SÜRDÜĞÜ SÜRECE her gün %10 Krallık GELİRİ transferi
            List<string> winnerIds = _reparationTargets.Keys.ToList();
            foreach (var winnerId in winnerIds)
            {
                string loserId = _reparationTargets[winnerId];
                Kingdom winner = Kingdom.All.FirstOrDefault(k => k.StringId == winnerId);
                Kingdom loser = Kingdom.All.FirstOrDefault(k => k.StringId == loserId);

                if (winner != null && loser != null)
                {
                    // Ateşkes bittiyse tazminat da otomatik kesilir
                    if (!HasActiveTruce(winner, loser))
                    {
                        _reparationTargets.Remove(winnerId);
                        continue;
                    }

                    // Günlük Krallık Gelirinin %10'u hesaplanır
                    int dailyIncome = 0;
                    foreach (var settlement in loser.Settlements)
                    {
                        dailyIncome += (int)(settlement.Town?.TradeTaxAccumulated ?? 100) + 200;
                    }

                    int reparationAmount = (int)(dailyIncome * 0.10f);

                    TreasuryBehavior.AddToTreasury(loser, -reparationAmount);
                    TreasuryBehavior.AddToTreasury(winner, reparationAmount);
                }
            }
        }
    }
}