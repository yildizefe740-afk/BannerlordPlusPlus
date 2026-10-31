using System;
using System.Globalization;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.Core;
using TaleWorlds.Library;
using NativeKingdomDiplomacyVM =
    TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy.KingdomDiplomacyVM;

namespace BannerlordPlusPlus
{
    public sealed class DiplomacyCategoryVM : ViewModel
    {
        private readonly string _categoryTitle;
        private readonly string _emptyMessage;
        private bool _isExpanded = true;

        [DataSourceProperty]
        public string CategoryTitle => _categoryTitle;

        [DataSourceProperty]
        public string EmptyMessage => _emptyMessage;

        [DataSourceProperty]
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value)
                    return;

                _isExpanded = value;
                OnPropertyChangedWithValue(value, nameof(IsExpanded));
            }
        }

        [DataSourceProperty]
        public MBBindingList<DiplomacyItemVM> Items { get; } = new MBBindingList<DiplomacyItemVM>();

        [DataSourceProperty]
        public MBBindingList<DiplomacyCategoryVM> Subcategories { get; } =
            new MBBindingList<DiplomacyCategoryVM>();

        [DataSourceProperty]
        public bool HasItems => Items.Count > 0;

        [DataSourceProperty]
        public bool HasSubcategories => Subcategories.Count > 0;

        [DataSourceProperty]
        public bool IsEmpty => !HasItems && !HasSubcategories;

        public DiplomacyCategoryVM(string title, string emptyMessage = "No active agreements.")
        {
            _categoryTitle = title;
            _emptyMessage = emptyMessage;
        }

        public void ExecuteToggleExpand()
        {
            IsExpanded = !IsExpanded;
        }
    }

    public sealed class DiplomacyItemVM : ViewModel
    {
        private readonly KingdomDiplomacyScreenVM _owner;
        private readonly SubjectAttributes _subject;
        private readonly string _kingdomName;
        private readonly string _detailsText;

        [DataSourceProperty]
        public string KingdomName => _kingdomName;

        [DataSourceProperty]
        public string DetailsText => _detailsText;

        [DataSourceProperty]
        public bool IsSubject => _subject != null;

        [DataSourceProperty]
        public bool IsRegularItem => !IsSubject;

        public DiplomacyItemVM(
            Kingdom kingdom,
            string details,
            KingdomDiplomacyScreenVM owner = null,
            SubjectAttributes subject = null)
        {
            _owner = owner;
            _subject = subject;
            _kingdomName = kingdom?.Name?.ToString() ?? "Unknown Kingdom";
            _detailsText = details;
        }

        public void ExecuteSelectSubject()
        {
            if (_subject != null)
                _owner?.SelectSubject(_subject);
        }
    }

    public sealed class DiplomacyProposalKindVM : ViewModel
    {
        private readonly KingdomDiplomacyScreenVM _owner;
        private readonly DiplomacyEvaluator.ProposalType _proposalType;
        private readonly string _name;
        private readonly string _description;
        private bool _isSelected;

        [DataSourceProperty]
        public string Name => _name;

        [DataSourceProperty]
        public string Description => _description;

        public DiplomacyEvaluator.ProposalType ProposalType => _proposalType;

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;
                OnPropertyChangedWithValue(value, nameof(IsSelected));
            }
        }

        public DiplomacyProposalKindVM(
            KingdomDiplomacyScreenVM owner,
            DiplomacyEvaluator.ProposalType proposalType,
            string name,
            string description)
        {
            _owner = owner;
            _proposalType = proposalType;
            _name = name;
            _description = description;
        }

        public void ExecuteSelect()
        {
            _owner.SelectProposalType(_proposalType);
        }
    }

    public sealed class DiplomacyTargetVM : ViewModel
    {
        private readonly KingdomDiplomacyScreenVM _owner;
        private readonly Kingdom _kingdom;
        private bool _isSelected;

        [DataSourceProperty]
        public string KingdomName => _kingdom.Name.ToString();

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;
                OnPropertyChangedWithValue(value, nameof(IsSelected));
            }
        }

        public DiplomacyTargetVM(KingdomDiplomacyScreenVM owner, Kingdom kingdom)
        {
            _owner = owner;
            _kingdom = kingdom;
        }

        public void ExecuteSelect()
        {
            _owner.SelectTarget(_kingdom);
        }

        public bool Represents(Kingdom kingdom)
        {
            return _kingdom == kingdom;
        }
    }

    public sealed class KingdomDiplomacyScreenVM : NativeKingdomDiplomacyVM
    {
        private readonly Kingdom _playerKingdom;
        private readonly MBBindingList<DiplomacyCategoryVM> _categories =
            new MBBindingList<DiplomacyCategoryVM>();
        private readonly MBBindingList<DiplomacyProposalKindVM> _proposalTypes =
            new MBBindingList<DiplomacyProposalKindVM>();
        private readonly MBBindingList<DiplomacyTargetVM> _proposalTargets =
            new MBBindingList<DiplomacyTargetVM>();
        private DiplomacyEvaluator.ProposalType _selectedProposalType =
            DiplomacyEvaluator.ProposalType.Alliance;
        private Kingdom _selectedTarget;
        private SubjectAttributes _selectedSubject;
        private bool _isProposalMode;
        private string _statusMessage = string.Empty;

        [DataSourceProperty]
        public MBBindingList<DiplomacyCategoryVM> TreatyCategories => _categories;

        [DataSourceProperty]
        public MBBindingList<DiplomacyProposalKindVM> ProposalTypes => _proposalTypes;

        [DataSourceProperty]
        public MBBindingList<DiplomacyTargetVM> ProposalTargets => _proposalTargets;

        [DataSourceProperty]
        public bool IsProposalMode
        {
            get => _isProposalMode;
            private set
            {
                if (_isProposalMode == value)
                    return;

                _isProposalMode = value;
                OnPropertyChangedWithValue(value, nameof(IsProposalMode));
            }
        }

        [DataSourceProperty]
        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (_statusMessage == value)
                    return;

                _statusMessage = value;
                OnPropertyChangedWithValue(value, nameof(StatusMessage));
            }
        }

        [DataSourceProperty]
        public bool CanSubmitProposal => _selectedTarget != null;

        [DataSourceProperty]
        public bool IsTreatyListVisible => !IsProposalMode;

        [DataSourceProperty]
        public string SelectedKingdomText =>
            _selectedTarget == null
                ? "Select a kingdom"
                : $"Selected: {_selectedTarget.Name}";

        [DataSourceProperty]
        public bool IsSelectedSubjectVisible => _selectedSubject != null;

        [DataSourceProperty]
        public bool IsSelectedSubjectPanelVisible =>
            IsSelectedSubjectVisible && IsTreatyListVisible;

        [DataSourceProperty]
        public bool IsSelectedSubjectLibertyDesireVisible =>
            _selectedSubject != null &&
            SubjectBehavior.IsRestrictedSubjectType(_selectedSubject.Type);

        [DataSourceProperty]
        public string SelectedSubjectName =>
            _selectedSubject?.JuniorKingdom?.Name?.ToString() ?? string.Empty;

        [DataSourceProperty]
        public string SelectedSubjectType =>
            _selectedSubject == null
                ? string.Empty
                : _selectedSubject.Type.ToString();

        [DataSourceProperty]
        public string SelectedSubjectLibertyDesire =>
            _selectedSubject == null
                ? string.Empty
                : $"Liberty Desire: {_selectedSubject.LibertyDesire.ToString("0", CultureInfo.InvariantCulture)}%";

        public KingdomDiplomacyScreenVM(Action<KingdomDecision> forceDecision)
            : base(forceDecision)
        {
            _playerKingdom = Clan.PlayerClan?.Kingdom;
            InitializeProposalTypes();
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            RefreshTreatyCategories();
            RefreshProposalTargets();
        }

        public void ExecuteOpenProposal()
        {
            StatusMessage = string.Empty;
            IsProposalMode = true;
            RefreshProposalTargets();
        }

        public void ExecuteCloseProposal()
        {
            IsProposalMode = false;
            StatusMessage = string.Empty;
            RefreshValues();
        }

        public void SelectSubject(SubjectAttributes subject)
        {
            _selectedSubject = subject;
            OnPropertyChanged(nameof(IsSelectedSubjectVisible));
            OnPropertyChanged(nameof(IsSelectedSubjectPanelVisible));
            OnPropertyChanged(nameof(IsSelectedSubjectLibertyDesireVisible));
            OnPropertyChanged(nameof(SelectedSubjectName));
            OnPropertyChanged(nameof(SelectedSubjectType));
            OnPropertyChanged(nameof(SelectedSubjectLibertyDesire));
        }

        public void ExecuteSubmitProposal()
        {
            Kingdom target = _selectedTarget;
            if (_playerKingdom == null || target == null || !IsEligibleTarget(target))
            {
                StatusMessage = "That kingdom is no longer available for this proposal.";
                RefreshProposalTargets();
                return;
            }

            if (!DiplomacyEvaluator.EvaluateProposal(
                    _playerKingdom,
                    target,
                    _selectedProposalType,
                    out string reason))
            {
                StatusMessage = $"Proposal declined: {reason}";
                return;
            }

            switch (_selectedProposalType)
            {
                case DiplomacyEvaluator.ProposalType.Alliance:
                    AllianceBehavior allianceBehavior =
                        Campaign.Current.GetCampaignBehavior<AllianceBehavior>();
                    if (allianceBehavior == null || allianceBehavior.HasAlliance(_playerKingdom, target))
                    {
                        StatusMessage = "An alliance with that kingdom already exists or could not be created.";
                        return;
                    }

                    allianceBehavior.AddAlliance(new Alliance(_playerKingdom, target));
                    break;

                case DiplomacyEvaluator.ProposalType.TradeAgreement:
                    TradeAgreementBehavior tradeBehavior =
                        Campaign.Current.GetCampaignBehavior<TradeAgreementBehavior>();
                    if (tradeBehavior == null || tradeBehavior.HasTradeAgreement(_playerKingdom, target))
                    {
                        StatusMessage = "A trade agreement with that kingdom already exists or could not be created.";
                        return;
                    }

                    tradeBehavior.AddAgreement(new TradeAgreement(_playerKingdom, target, 0.20f, 60.0));
                    break;

                case DiplomacyEvaluator.ProposalType.DefensivePact:
                    DefensivePactBehavior defensiveBehavior =
                        Campaign.Current.GetCampaignBehavior<DefensivePactBehavior>();
                    if (defensiveBehavior == null ||
                        defensiveBehavior.HasDefensivePact(_playerKingdom, target))
                    {
                        StatusMessage = "A defensive pact with that kingdom already exists or could not be created.";
                        return;
                    }

                    defensiveBehavior.AddPact(new DefensivePact(_playerKingdom, target, 60.0));
                    break;

                case DiplomacyEvaluator.ProposalType.NonAggressionPact:
                    if (NonAggressionPactBehavior.IsPactActive(
                            _playerKingdom.StringId,
                            target.StringId) ||
                        !NonAggressionPactBehavior.AddPact(_playerKingdom, target, 60.0))
                    {
                        StatusMessage =
                            "A non-aggression pact with that kingdom already exists or could not be created.";
                        return;
                    }

                    break;
            }

            StatusMessage = $"Proposal accepted. Agreement established with {target.Name}.";
            RefreshTreatyCategories();
            RefreshProposalTargets();
            OnPropertyChanged(nameof(CanSubmitProposal));
        }

        public void SelectProposalType(DiplomacyEvaluator.ProposalType proposalType)
        {
            _selectedProposalType = proposalType;
            foreach (DiplomacyProposalKindVM proposal in _proposalTypes)
                proposal.IsSelected = proposalType == proposal.ProposalType;

            _selectedTarget = null;
            RefreshProposalTargets();
            OnPropertyChanged(nameof(SelectedKingdomText));
            OnPropertyChanged(nameof(CanSubmitProposal));
        }

        public void SelectTarget(Kingdom kingdom)
        {
            _selectedTarget = kingdom;
            foreach (DiplomacyTargetVM target in _proposalTargets)
                target.IsSelected = target.Represents(kingdom);

            OnPropertyChanged(nameof(SelectedKingdomText));
            OnPropertyChanged(nameof(CanSubmitProposal));
        }

        private void InitializeProposalTypes()
        {
            _proposalTypes.Add(new DiplomacyProposalKindVM(
                this,
                DiplomacyEvaluator.ProposalType.Alliance,
                "Military Alliance",
                "Mutual military support and shared trust."));
            _proposalTypes.Add(new DiplomacyProposalKindVM(
                this,
                DiplomacyEvaluator.ProposalType.TradeAgreement,
                "Trade Agreement",
                "A 20% trade discount for 60 days."));
            _proposalTypes.Add(new DiplomacyProposalKindVM(
                this,
                DiplomacyEvaluator.ProposalType.DefensivePact,
                "Defensive Pact",
                "Join each other's defensive wars for 60 days."));
            _proposalTypes.Add(new DiplomacyProposalKindVM(
                this,
                DiplomacyEvaluator.ProposalType.NonAggressionPact,
                "Non-Aggression Pact",
                "Neither kingdom may declare war for 60 days."));
            SelectProposalType(_selectedProposalType);
        }

        private void RefreshProposalTargets()
        {
            Kingdom previousTarget = _selectedTarget;
            _proposalTargets.Clear();

            if (_playerKingdom != null)
            {
                foreach (Kingdom kingdom in Kingdom.All
                             .Where(IsEligibleTarget)
                             .OrderBy(item => item.Name.ToString()))
                {
                    var target = new DiplomacyTargetVM(this, kingdom)
                    {
                        IsSelected = kingdom == previousTarget
                    };
                    _proposalTargets.Add(target);
                }
            }

            if (previousTarget != null &&
                _proposalTargets.All(item => !item.Represents(previousTarget)))
            {
                _selectedTarget = null;
            }

            OnPropertyChanged(nameof(SelectedKingdomText));
            OnPropertyChanged(nameof(CanSubmitProposal));
        }

        private bool IsEligibleTarget(Kingdom kingdom)
        {
            if (_playerKingdom == null ||
                kingdom == null ||
                kingdom == _playerKingdom ||
                kingdom.IsEliminated ||
                kingdom.IsAtWarWith(_playerKingdom) ||
                !SubjectBehavior.CanCreateDiplomaticPact(_playerKingdom, kingdom))
            {
                return false;
            }

            switch (_selectedProposalType)
            {
                case DiplomacyEvaluator.ProposalType.Alliance:
                    AllianceBehavior alliance =
                        Campaign.Current.GetCampaignBehavior<AllianceBehavior>();
                    return alliance == null || !alliance.HasAlliance(_playerKingdom, kingdom);
                case DiplomacyEvaluator.ProposalType.TradeAgreement:
                    TradeAgreementBehavior trade =
                        Campaign.Current.GetCampaignBehavior<TradeAgreementBehavior>();
                    return trade == null || !trade.HasTradeAgreement(_playerKingdom, kingdom);
                case DiplomacyEvaluator.ProposalType.DefensivePact:
                    DefensivePactBehavior defensive =
                        Campaign.Current.GetCampaignBehavior<DefensivePactBehavior>();
                    return defensive == null || !defensive.HasDefensivePact(_playerKingdom, kingdom);
                case DiplomacyEvaluator.ProposalType.NonAggressionPact:
                    return !NonAggressionPactBehavior.IsPactActive(
                        _playerKingdom.StringId,
                        kingdom.StringId);
                default:
                    return false;
            }
        }

        private void RefreshTreatyCategories()
        {
            _categories.Clear();
            if (_playerKingdom == null || Campaign.Current == null)
                return;

            AllianceBehavior allianceBehavior =
                Campaign.Current.GetCampaignBehavior<AllianceBehavior>();
            var alliances = new DiplomacyCategoryVM("Military Alliances");
            if (allianceBehavior != null)
            {
                foreach (Alliance alliance in allianceBehavior.GetActiveAlliancesFor(_playerKingdom))
                {
                    Kingdom partner = FindPartner(alliance.kingdom1ID, alliance.kingdom2ID);
                    if (partner != null)
                        alliances.Items.Add(new DiplomacyItemVM(
                            partner,
                            $"Trust: {alliance.Trust.ToString("0", CultureInfo.InvariantCulture)} / 100"));
                }
            }
            _categories.Add(alliances);

            TradeAgreementBehavior tradeBehavior =
                Campaign.Current.GetCampaignBehavior<TradeAgreementBehavior>();
            var tradeAgreements = new DiplomacyCategoryVM("Trade Agreements");
            if (tradeBehavior != null)
            {
                foreach (TradeAgreement agreement in tradeBehavior.GetActiveAgreementsFor(_playerKingdom))
                {
                    Kingdom partner = FindPartner(agreement.Kingdom1Id, agreement.Kingdom2Id);
                    if (partner != null)
                    {
                        tradeAgreements.Items.Add(new DiplomacyItemVM(
                            partner,
                            $"{agreement.DiscountRate:P0} trade discount; " +
                            $"{DaysRemaining(agreement.ExpirationDateDays)}"));
                    }
                }
            }
            _categories.Add(tradeAgreements);

            DefensivePactBehavior defensiveBehavior =
                Campaign.Current.GetCampaignBehavior<DefensivePactBehavior>();
            var defensivePacts = new DiplomacyCategoryVM("Defensive Pacts");
            if (defensiveBehavior != null)
            {
                foreach (DefensivePact pact in defensiveBehavior.GetActivePactsFor(_playerKingdom))
                {
                    Kingdom partner = FindPartner(pact.Kingdom1Id, pact.Kingdom2Id);
                    if (partner != null)
                    {
                        string status = pact.IsCalled ? "Called to war; " : string.Empty;
                        defensivePacts.Items.Add(new DiplomacyItemVM(
                            partner,
                            $"{status}{DaysRemaining(pact.ExpirationDateDays)}"));
                    }
                }
            }
            _categories.Add(defensivePacts);

            var nonAggressionPacts = new DiplomacyCategoryVM("Non-Aggression Pacts");
            foreach (NonAggressionPact pact in
                     NonAggressionPactBehavior.GetActivePactsFor(_playerKingdom))
            {
                Kingdom partner = FindPartner(pact.Kingdom1Id, pact.Kingdom2Id);
                if (partner != null)
                {
                    nonAggressionPacts.Items.Add(new DiplomacyItemVM(
                        partner,
                        $"War declarations prohibited; {DaysRemaining(pact.ExpirationDateDays)}"));
                }
            }
            _categories.Add(nonAggressionPacts);

            var subjects = new DiplomacyCategoryVM(
                "Subjects",
                "No subject kingdoms.");
            AddSubjectCategory(subjects, SubjectType.Vassal, "Vassals");
            AddSubjectCategory(subjects, SubjectType.March, "Marches");
            AddSubjectCategory(subjects, SubjectType.Tributary, "Tributaries");
            _categories.Add(subjects);
        }

        private void AddSubjectCategory(
            DiplomacyCategoryVM parent,
            SubjectType subjectType,
            string title)
        {
            var category = new DiplomacyCategoryVM(
                title,
                subjectType == SubjectType.March
                    ? "No marches."
                    : $"No {title.ToLowerInvariant()}.");

            foreach (SubjectAttributes subject in SubjectBehavior.GetSubjectsOf(_playerKingdom)
                         .Where(item => item.Type == subjectType))
            {
                Kingdom junior = subject.JuniorKingdom;
                if (junior == null)
                    continue;

                int tax = SubjectMethods.GetTax(junior, subjectType, 0.10f);
                string details = subjectType == SubjectType.March
                    ? "Military support; no treasury tax."
                    : $"+{tax.ToString("N0", CultureInfo.InvariantCulture)} gold/day in tribute.";

                category.Items.Add(new DiplomacyItemVM(junior, details, this, subject));
            }

            parent.Subcategories.Add(category);
        }

        private Kingdom FindPartner(string kingdom1Id, string kingdom2Id)
        {
            string partnerId = kingdom1Id == _playerKingdom.StringId
                ? kingdom2Id
                : kingdom1Id;

            return Kingdom.All.FirstOrDefault(kingdom =>
                kingdom != null &&
                kingdom.StringId == partnerId &&
                !kingdom.IsEliminated);
        }

        private static string DaysRemaining(double expirationDate)
        {
            int days = Math.Max(0, (int)Math.Ceiling(expirationDate - CampaignTime.Now.ToDays));
            return $"{days} days remaining";
        }
    }
}
