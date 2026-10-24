using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BannerlordPlusPlus
{
    /// <summary>
    /// Kurallar:
    ///  - Tributary: sadece harac oder, diplomasisi ve savas/baris kararlari serbesttir.
    ///  - Vassal / March: butun diplomasi kapali. Tek istisna: ayni overlord'un vassal/march'lari
    ///    kendi aralarinda ticaret anlasmasi yapabilir.
    ///  - Bagimsizlik savasi ozel bir savastir: sadece vassal/march, sadece overlord'a karsi,
    ///    sadece LibertyDesire >= 100 iken (veya IsRebelling iken) acilabilir.
    /// </summary>
    public class SubjectBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, SubjectAttributes> _activeSubjects = new();

        // Oyuncuya "isyan edebilirsin" mesajini her gun tekrarlamamak icin (kayda gerek yok)
        private static readonly HashSet<string> _revoltNotified = new();

        // Bagimsizlik savasini kazanmak icin gereken guc orani (CustomPeaceBehavior ile ayni deger)
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
            if (senior == null || junior == null || senior == junior) return;
            if (_activeSubjects == null) _activeSubjects = new();

            // (A,B) veya (B,A) zaten varsa tekrar ekleme
            if (GetSubjectBetween(senior, junior) != null) return;

            if (IsRestrictedSubjectType(type))
            {
                if (GetControlledSubject(junior) != null) return; // zaten baska birinin vassal/march'i
                if (GetControlledSubject(senior) != null) return; // vassal overlord olamaz
                if (HasRestrictedSubjects(junior)) return;        // overlord vassal olamaz
            }

            string key = GetSubjectKey(senior, junior);
            if (_activeSubjects.ContainsKey(key)) return;

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
                Kingdom senior = subject?.SeniorKingdom;
                Kingdom junior = subject?.JuniorKingdom;

                // Yetim kayit (krallik yok olmus / bulunamiyor): temizle
                if (subject == null || senior == null || junior == null ||
                    senior.IsEliminated || junior.IsEliminated)
                {
                    _activeSubjects.Remove(pair.Key);
                    _revoltNotified.Remove(pair.Key);
                    KingdomsUI.Invalidate();
                    continue;
                }

                UpdateLibertyDesire(subject);
                TriggerIndependenceWar(subject);
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

            // Kingdom nesnesi artik cozulemeyebilir; bu yuzden ID uzerinden dogrudan anahtarla sil
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

            if (subject.LibertyDesire < 100f)
                _revoltNotified.Remove(GetSubjectKey(senior, junior));
        }

        /// <summary>
        /// Bagimsizlik savasi sadece vassal ve march icin gecerli. Tributary zaten ozgur oldugu icin isyan etmez.
        /// AI: LibertyDesire 100 iken gunluk %15 sansla savas acar.
        /// Oyuncu: savasi kendisi acar (diplomasi ekranindan overlord'a savas ilani); yama buna izin verir.
        /// </summary>
        public void TriggerIndependenceWar(SubjectAttributes subject)
        {
            if (subject == null) return;
            if (!IsRestrictedSubjectType(subject.Type)) return;
            if (subject.IsRebelling) return;

            Kingdom senior = subject.SeniorKingdom;
            Kingdom junior = subject.JuniorKingdom;

            if (senior == null || junior == null) return;
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

            subject.IsRebelling = true; // yama bu bayrakla savasa izin verir
            DeclareWarAction.ApplyByDefault(junior, senior);

            if (!junior.IsAtWarWith(senior))
            {
                // Savas baska bir kural yuzunden acilmadiysa bayrak takili kalmasin
                subject.IsRebelling = false;
                return;
            }

            InformationManager.DisplayMessage(new InformationMessage(
                $"{junior.Name} has revolted against its overlord {senior.Name}!", Colors.Red
            ));
        }

        /// <summary>
        /// Barisla biten bagimsizlik savasini sonuclandirir (vanilla AI baris yaptiginda).
        /// Oyuncu baris teklifi CustomPeaceBehavior uzerinden gider ve bayragi zaten sifirlar.
        /// </summary>
        private void ResolveIndependenceWar(SubjectAttributes subject)
        {
            if (subject == null || !subject.IsRebelling) return;

            Kingdom senior = subject.SeniorKingdom;
            Kingdom junior = subject.JuniorKingdom;

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

        // ---------------------------------------------------------------------
        // Sorgu yardimcilari
        // ---------------------------------------------------------------------

        public static SubjectAttributes GetSubjectBetween(Kingdom k1, Kingdom k2)
        {
            if (Instance == null || Instance._activeSubjects == null || k1 == null || k2 == null) return null;

            string key1 = Instance.GetSubjectKey(k1, k2);
            if (Instance._activeSubjects.TryGetValue(key1, out var subject1)) return subject1;

            string key2 = Instance.GetSubjectKey(k2, k1);
            if (Instance._activeSubjects.TryGetValue(key2, out var subject2)) return subject2;

            return null;
        }

        /// <summary>Krallik bir vassal/march ise, onu kontrol eden kaydi dondurur. Tributary icin null.</summary>
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

        /// <summary>Iki krallik da vassal/march ve ayni overlord'a bagli mi?</summary>
        private static bool SharesSenior(Kingdom a, Kingdom b)
        {
            SubjectAttributes sa = GetControlledSubject(a);
            SubjectAttributes sb = GetControlledSubject(b);
            return sa != null && sb != null && sa.SeniorKingdomId == sb.SeniorKingdomId;
        }

        // ---------------------------------------------------------------------
        // Diplomasi kapilari. Bu fonksiyonlar HER diplomatik yolda cagrilmali:
        // savunma pakti, NAP, ittifak, evlilik, (varsa) hediye/teklif menuleri ve AI uretimleri.
        // ---------------------------------------------------------------------

        /// <summary>Savunma pakti, NAP, ittifak, evlilik vb. Vassal/March olan taraf varsa false.</summary>
        public static bool CanCreateDiplomaticPact(Kingdom kingdom1, Kingdom kingdom2)
        {
            return kingdom1 != null &&
                   kingdom2 != null &&
                   GetControlledSubject(kingdom1) == null &&
                   GetControlledSubject(kingdom2) == null;
        }

        /// <summary>
        /// Ticaret anlasmasi: iki taraf da ozgurse serbest. Vassal/March ise sadece
        /// ayni overlord'un vassal/march'i ile yapabilir. Diger her durumda false.
        /// </summary>
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

        // ---------------------------------------------------------------------
        // Olaylar
        // ---------------------------------------------------------------------

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

                Kingdom senior = subject.SeniorKingdom;
                Kingdom junior = subject.JuniorKingdom;
                if (senior == null || junior == null)
                    continue;

                // Bagimsizlik savasi basladi (oyuncu ya da AI, overlord'a savas actigi an)
                if (faction1 == junior && faction2 == senior)
                {
                    subject.IsRebelling = true;
                    continue;
                }

                // Isyan eden alt krallik overlord'un savaslarina katilmaz, overlord da onun savaslarina
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

                Kingdom senior = subject.SeniorKingdom;
                Kingdom junior = subject.JuniorKingdom;
                if (senior == null || junior == null)
                    continue;

                // Bagimsizlik savasi baris ile bitti
                bool isIndependencePair =
                    (faction1 == junior && faction2 == senior) ||
                    (faction1 == senior && faction2 == junior);

                if (isIndependencePair)
                {
                    ResolveIndependenceWar(subject);
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

            Kingdom senior = subject.SeniorKingdom;
            Kingdom junior = subject.JuniorKingdom;
            if (senior == null || junior == null)
                return;

            foreach (Kingdom enemy in Kingdom.All.ToList())
            {
                if (enemy != null && !enemy.IsEliminated && enemy != junior && senior.IsAtWarWith(enemy))
                    SubjectMethods.CallToWar(senior, junior, enemy);
            }
        }

        // ---------------------------------------------------------------------
        // Harmony yamalari
        // ---------------------------------------------------------------------

        [HarmonyPatch(typeof(DeclareWarAction), "ApplyInternal")]
        public static class PreventIndependentSubjectWarsPatch
        {
            public static bool Prefix(IFaction faction1, IFaction faction2)
            {
                if (!(faction1 is Kingdom aggressor))
                    return true;

                Kingdom defender = faction2 as Kingdom;

                // Saldiran bir vassal/march ise
                SubjectAttributes subject = GetControlledSubject(aggressor);
                if (subject != null)
                {
                    Kingdom senior = subject.SeniorKingdom;
                    if (senior == null)
                        return true; // yetim kayit, engelleme

                    // Bagimsizlik savasi: sadece overlord'a karsi, sadece LibertyDesire 100 iken veya isyan basladiysa
                    if (faction2 == senior)
                        return subject.IsRebelling || subject.LibertyDesire >= 100f;

                    // Ayni overlord'un diger vassal/march'ina savas acamaz
                    if (defender != null && SharesSenior(aggressor, defender))
                        return false;

                    // Gerisi: sadece overlord zaten savastaysa o dusmana katilabilir
                    return faction2 != null && senior.IsAtWarWith(faction2);
                }

                // Overlord kendi vassal/march'ina savas acamaz
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

                // Isyan eden kendi savasini yonetir; overlord ile baris (bagimsizlik savasi sonu) her zaman serbest
                Kingdom senior = subject.SeniorKingdom;
                if (senior == null || subject.IsRebelling || opponent == senior)
                    return true;

                return !senior.IsAtWarWith(opponent);
            }
        }
    }
}