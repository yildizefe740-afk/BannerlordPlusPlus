using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace BannerlordPlusPlus
{
    // Kingdom Diplomatic Status Data Model (UI Item)
    public class KingdomDiplomacyItemVM : ViewModel
    {
        private readonly Kingdom _targetKingdom;
        private readonly Kingdom _selfKingdom;

        [DataSourceProperty]
        public string KingdomName => _targetKingdom?.Name?.ToString() ?? "Unknown";

        [DataSourceProperty]
        public string DiplomacyStatus 
        {
            get
            {
                if (_selfKingdom == null || _targetKingdom == null) return "Neutral";

                if (_selfKingdom.IsAtWarWith(_targetKingdom))
                    return "At War";
                
                // Vassal or Suzerain check can be integrated here
                return "Peace / Allied";
            }
        }

        [DataSourceProperty]
        public int RelationValue => _selfKingdom != null && _targetKingdom != null 
            ? (int)_selfKingdom.Leader.GetRelation(_targetKingdom.Leader) 
            : 0;

        public KingdomDiplomacyItemVM(Kingdom selfKingdom, Kingdom targetKingdom)
        {
            _selfKingdom = selfKingdom;
            _targetKingdom = targetKingdom;
        }
    }

    // Main Diplomacy Screen ViewModel Class
    public class DiplomacyUIVM : ViewModel
    {
        private readonly Kingdom _activeKingdom;

        [DataSourceProperty]
        public MBBindingList<KingdomDiplomacyItemVM> DiplomacyList { get; }

        [DataSourceProperty]
        public string ScreenTitle => new TextObject("{=diplomacy_screen_title}Kingdom Diplomacy & Subjects").ToString();

        public DiplomacyUIVM(Kingdom activeKingdom)
        {
            _activeKingdom = activeKingdom;
            DiplomacyList = new MBBindingList<KingdomDiplomacyItemVM>();
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            DiplomacyList.Clear();

            if (_activeKingdom == null) return;

            // Iterate through all other kingdoms on the campaign map to list relations
            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (kingdom == _activeKingdom || kingdom.IsEliminated) continue;

                DiplomacyList.Add(new KingdomDiplomacyItemVM(_activeKingdom, kingdom));
            }
        }
    }
}