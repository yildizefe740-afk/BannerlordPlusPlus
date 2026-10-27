using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace BannerlordPlusPlus
{
    // Each category on the left (e.g., ALLIANCES, SUBJECTS, DEFENSIVE PACTS).
    public class DiplomacyCategoryVM : ViewModel
    {
        private string _categoryTitle;
        private bool _isExpanded = true;

        [DataSourceProperty]
        public string CategoryTitle
        {
            get => _categoryTitle;
            set
            {
                if (_categoryTitle != value)
                {
                    _categoryTitle = value;
                    OnPropertyChangedWithValue(value, "CategoryTitle");
                }
            }
        }

        [DataSourceProperty]
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChangedWithValue(value, "IsExpanded");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<DiplomacyItemVM> Items { get; } = new();

        public DiplomacyCategoryVM(string title)
        {
            _categoryTitle = title;
        }

        public void ExecuteToggleExpand()
        {
            IsExpanded = !IsExpanded;
        }
    }

    // Each kingdom entry within a category.
    public class DiplomacyItemVM : ViewModel
    {
        private readonly Kingdom _targetKingdom;

        [DataSourceProperty]
        public string KingdomName => _targetKingdom?.Name?.ToString() ?? "Unknown";

        [DataSourceProperty]
        public string DetailsText { get; set; }

        public Kingdom TargetKingdom => _targetKingdom;

        public DiplomacyItemVM(Kingdom targetKingdom, string details)
        {
            _targetKingdom = targetKingdom;
            DetailsText = details;
        }
    }

    // ViewModel for the main Diplomacy tab.
    public class KingdomDiplomacyVM : ViewModel
    {
        private readonly Kingdom _playerKingdom;

        [DataSourceProperty]
        public MBBindingList<DiplomacyCategoryVM> Categories { get; } = new();

        public KingdomDiplomacyVM()
        {
            _playerKingdom = Clan.PlayerClan?.Kingdom;
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            Categories.Clear();

            if (_playerKingdom == null) return;

            var allianceBehavior = Campaign.Current.GetCampaignBehavior<AllianceBehavior>();
            var tradeBehavior = Campaign.Current.GetCampaignBehavior<TradeAgreementBehavior>();
            var pactBehavior = Campaign.Current.GetCampaignBehavior<DefensivePactBehavior>();

            // 1. ALLIANCES CATEGORY
            var allianceCategory = new DiplomacyCategoryVM("ALLIANCES");
            foreach (var k in Kingdom.All.Where(k => k != _playerKingdom && !k.IsEliminated))
            {
                if (allianceBehavior != null && allianceBehavior.HasAlliance(_playerKingdom, k))
                {
                    var alliance = allianceBehavior.GetAllianceBetween(_playerKingdom, k);
                    float trust = alliance != null ? alliance.Trust : 0f;
                    allianceCategory.Items.Add(new DiplomacyItemVM(k, $"Trust: {trust:0.0}"));
                }
            }
            Categories.Add(allianceCategory);

            // 2. DEFENSIVE & TRADE PACTS CATEGORY
            var pactCategory = new DiplomacyCategoryVM("ACTIVE PACTS");
            foreach (var k in Kingdom.All.Where(k => k != _playerKingdom && !k.IsEliminated))
            {
                bool trade = tradeBehavior != null && tradeBehavior.HasTradeAgreement(_playerKingdom, k);
                bool defensive = pactBehavior != null && pactBehavior.HasDefensivePact(_playerKingdom, k);

                if (trade || defensive)
                {
                    string status = (trade ? "Trade " : "") + (defensive ? "Defensive" : "");
                    pactCategory.Items.Add(new DiplomacyItemVM(k, status));
                }
            }
            Categories.Add(pactCategory);

            // 3. SUBJECTS / VASSALS CATEGORY
            var subjectCategory = new DiplomacyCategoryVM("SUBJECTS & MARCHES");
            // Add subject kingdoms from SubjectBehavior here.
            Categories.Add(subjectCategory);
        }

        // Handler for the "Propose New Pact / Alliance" button in the lower-right corner.
        public void ExecuteProposeNewPact()
        {
            PactsUI.OpenDiplomacyMenu();
        }
    }
}