using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Settlements;
using NativeKingdomDiplomacyVM =
    TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy.KingdomDiplomacyVM;

namespace BannerlordPlusPlus
{
    public sealed class DiplomacyCategoryVM : ViewModel
    {
        private readonly string _categoryTitle;

        [DataSourceProperty]
        public string CategoryTitle =>
            $"{_categoryTitle} ({Items.Count + Subcategories.Sum(category => category.Items.Count)})";

        [DataSourceProperty]
        public MBBindingList<DiplomacyItemVM> Items { get; } = new MBBindingList<DiplomacyItemVM>();

        [DataSourceProperty]
        public MBBindingList<DiplomacyCategoryVM> Subcategories { get; } =
            new MBBindingList<DiplomacyCategoryVM>();

        [DataSourceProperty]
        public bool HasItems => Items.Count > 0;

        [DataSourceProperty]
        public bool HasSubcategories => Subcategories.Count > 0;

        public DiplomacyCategoryVM(string title)
        {
            _categoryTitle = title;
        }
    }

    public sealed class DiplomacyItemVM : ViewModel
    {
        private readonly KingdomDiplomacyScreenVM _owner;
        private readonly SubjectAttributes _subject;
        private readonly Kingdom _kingdom;
        private readonly bool _isWarKingdom;
        private readonly string _kingdomName;
        private readonly string _detailsText;

        [DataSourceProperty]
        public string KingdomName => _kingdomName;

        [DataSourceProperty]
        public string DetailsText => _detailsText;

        [DataSourceProperty]
        public bool IsSubject => _subject != null;

        [DataSourceProperty]
        public bool IsWarKingdom => _isWarKingdom;

        [DataSourceProperty]
        public bool IsRegularItem => !IsSubject && !IsWarKingdom;

        public DiplomacyItemVM(
            Kingdom kingdom,
            string details,
            KingdomDiplomacyScreenVM owner = null,
            SubjectAttributes subject = null,
            bool isWarKingdom = false)
        {
            _owner = owner;
            _subject = subject;
            _kingdom = kingdom;
            _isWarKingdom = isWarKingdom;
            _kingdomName = kingdom?.Name?.ToString() ?? "Unknown Kingdom";
            _detailsText = details;
        }

        public void ExecuteSelectSubject()
        {
            if (_subject != null)
                _owner?.SelectSubject(_subject);
        }

        public void ExecuteSelectWarKingdom()
        {
            if (_isWarKingdom)
                _owner?.SelectWarKingdom(_kingdom);
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

    public sealed class PeaceClauseVM : ViewModel
    {
        private readonly KingdomDiplomacyScreenVM _owner;
        private readonly PeaceDealOptions _option;
        private readonly string _name;
        private readonly string _description;
        private bool _isSelected;

        [DataSourceProperty]
        public string Name => _name;

        [DataSourceProperty]
        public string Description => _description;

        public PeaceDealOptions Option => _option;

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

        public PeaceClauseVM(
            KingdomDiplomacyScreenVM owner,
            PeaceDealOptions option,
            string name,
            string description,
            bool isSelected)
        {
            _owner = owner;
            _option = option;
            _name = name;
            _description = description;
            _isSelected = isSelected;
        }

        public void ExecuteToggle()
        {
            _owner.TogglePeaceClause(_option);
        }
    }

    public sealed class PeaceSettlementVM : ViewModel
    {
        private readonly KingdomDiplomacyScreenVM _owner;
        private readonly Settlement _settlement;
        private readonly bool _isTargetSettlement;
        private bool _isSelected;

        [DataSourceProperty]
        public string Name => _settlement.Name.ToString();

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

        public PeaceSettlementVM(
            KingdomDiplomacyScreenVM owner,
            Settlement settlement,
            bool isTargetSettlement,
            bool isSelected)
        {
            _owner = owner;
            _settlement = settlement;
            _isTargetSettlement = isTargetSettlement;
            _isSelected = isSelected;
        }

        public void ExecuteSelect()
        {
            _owner.SelectPeaceSettlement(_settlement, _isTargetSettlement);
        }

        public bool Represents(Settlement settlement)
        {
            return _settlement == settlement;
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
        private readonly MBBindingList<PeaceClauseVM> _peaceClauses =
            new MBBindingList<PeaceClauseVM>();
        private readonly MBBindingList<PeaceSettlementVM> _targetPeaceSettlements =
            new MBBindingList<PeaceSettlementVM>();
        private readonly MBBindingList<PeaceSettlementVM> _proposerPeaceSettlements =
            new MBBindingList<PeaceSettlementVM>();
        private DiplomacyEvaluator.ProposalType _selectedProposalType =
            DiplomacyEvaluator.ProposalType.Alliance;
        private Kingdom _selectedTarget;
        private Kingdom _selectedWarKingdom;
        private SubjectAttributes _selectedSubject;
        private Settlement _selectedCededSettlement;
        private Settlement _selectedTargetSettlementToExchange;
        private Settlement _selectedProposerSettlementToExchange;
        private int _peaceDailyTribute;
        private int _peaceTributeDurationDays;
        private bool _isProposalMode;
        private bool _isPeaceProposalMode;
        private string _statusMessage = string.Empty;

        [DataSourceProperty]
        public MBBindingList<DiplomacyCategoryVM> TreatyCategories => _categories;

        [DataSourceProperty]
        public MBBindingList<DiplomacyProposalKindVM> ProposalTypes => _proposalTypes;

        [DataSourceProperty]
        public MBBindingList<DiplomacyTargetVM> ProposalTargets => _proposalTargets;

        [DataSourceProperty]
        public MBBindingList<PeaceClauseVM> PeaceClauses => _peaceClauses;

        [DataSourceProperty]
        public MBBindingList<PeaceSettlementVM> TargetPeaceSettlements => _targetPeaceSettlements;

        [DataSourceProperty]
        public MBBindingList<PeaceSettlementVM> ProposerPeaceSettlements => _proposerPeaceSettlements;

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
                OnPropertyChanged(nameof(IsDiplomacyScrollAreaVisible));
                OnPropertyChanged(nameof(IsTreatyListVisible));
                OnPropertyChanged(nameof(IsPactButtonVisible));
                OnPropertyChanged(nameof(IsPeaceProposalButtonVisible));
                OnPropertyChanged(nameof(IsPeaceProposalVisible));
                OnPropertyChanged(nameof(IsBackButtonVisible));
            }
        }

        [DataSourceProperty]
        public bool IsPeaceProposalMode
        {
            get => _isPeaceProposalMode;
            private set
            {
                if (_isPeaceProposalMode == value)
                    return;

                _isPeaceProposalMode = value;
                OnPropertyChangedWithValue(value, nameof(IsPeaceProposalMode));
                OnPropertyChanged(nameof(IsTreatyListVisible));
                OnPropertyChanged(nameof(IsPactButtonVisible));
                OnPropertyChanged(nameof(IsPeaceProposalButtonVisible));
                OnPropertyChanged(nameof(IsPeaceProposalVisible));
                OnPropertyChanged(nameof(IsBackButtonVisible));
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
        public bool IsTreatyListVisible => !IsProposalMode && !IsPeaceProposalMode;

        [DataSourceProperty]
        public bool IsDiplomacyScrollAreaVisible => !IsProposalMode;

        [DataSourceProperty]
        public bool IsPactButtonVisible =>
            !IsProposalMode && !IsPeaceProposalMode && _selectedWarKingdom == null;

        [DataSourceProperty]
        public bool IsPeaceProposalButtonVisible =>
            !IsProposalMode && !IsPeaceProposalMode && _selectedWarKingdom != null;

        [DataSourceProperty]
        public bool IsPeaceProposalVisible =>
            IsPeaceProposalMode && _selectedWarKingdom != null;

        [DataSourceProperty]
        public bool IsCededSettlementSelectorVisible =>
            IsPeaceProposalVisible && IsPeaceClauseSelected(PeaceDealOptions.CededSettlement);

        [DataSourceProperty]
        public bool IsTerritoryExchangeSelectorVisible =>
            IsPeaceProposalVisible && IsPeaceClauseSelected(PeaceDealOptions.TerritoryExchange);

        [DataSourceProperty]
        public bool CanSubmitPeaceProposal =>
            _selectedWarKingdom != null &&
            (!IsPeaceClauseSelected(PeaceDealOptions.CededSettlement) ||
             _selectedCededSettlement != null) &&
            (!IsPeaceClauseSelected(PeaceDealOptions.TerritoryExchange) ||
             (_selectedTargetSettlementToExchange != null &&
              _selectedProposerSettlementToExchange != null));

        [DataSourceProperty]
        public bool IsBackButtonVisible =>
            IsProposalMode || IsPeaceProposalMode;

        [DataSourceProperty]
        public string SelectedWarKingdomName =>
            _selectedWarKingdom?.Name?.ToString() ?? string.Empty;

        [DataSourceProperty]
        public int PeaceDailyTribute => _peaceDailyTribute;

        [DataSourceProperty]
        public int PeaceTributeDurationDays => _peaceTributeDurationDays;

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
            InitializePeaceClauses();
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
            _selectedTarget = null;
            IsProposalMode = true;
            RefreshProposalTargets();
        }

        public void ExecuteCloseProposal()
        {
            IsProposalMode = false;
            IsPeaceProposalMode = false;
            _selectedWarKingdom = null;
            StatusMessage = string.Empty;
            RefreshValues();
            OnPropertyChanged(nameof(IsPactButtonVisible));
            OnPropertyChanged(nameof(IsPeaceProposalButtonVisible));
            OnPropertyChanged(nameof(IsPeaceProposalVisible));
            OnPropertyChanged(nameof(IsBackButtonVisible));
            OnPropertyChanged(nameof(SelectedWarKingdomName));
            OnPropertyChanged(nameof(IsCededSettlementSelectorVisible));
            OnPropertyChanged(nameof(IsTerritoryExchangeSelectorVisible));
            OnPropertyChanged(nameof(CanSubmitPeaceProposal));
        }

        public void SelectWarKingdom(Kingdom kingdom)
        {
            if (kingdom == null || _playerKingdom == null || !kingdom.IsAtWarWith(_playerKingdom))
                return;

            SelectKingdom(kingdom);
            _selectedWarKingdom = kingdom;
            IsProposalMode = false;
            IsPeaceProposalMode = false;
            StatusMessage = string.Empty;
            OnPropertyChanged(nameof(IsPactButtonVisible));
            OnPropertyChanged(nameof(IsPeaceProposalButtonVisible));
            OnPropertyChanged(nameof(IsPeaceProposalVisible));
            OnPropertyChanged(nameof(IsBackButtonVisible));
            OnPropertyChanged(nameof(SelectedWarKingdomName));
            OnPropertyChanged(nameof(IsCededSettlementSelectorVisible));
            OnPropertyChanged(nameof(IsTerritoryExchangeSelectorVisible));
            OnPropertyChanged(nameof(CanSubmitPeaceProposal));
            RefreshPeaceSettlementChoices();
        }

        public void ExecuteOpenPeaceProposal()
        {
            Kingdom target = _selectedWarKingdom;
            if (_playerKingdom == null ||
                target == null ||
                target.IsEliminated ||
                !target.IsAtWarWith(_playerKingdom))
            {
                StatusMessage = "Select a kingdom that is currently at war with you.";
                _selectedWarKingdom = null;
                OnPropertyChanged(nameof(IsPeaceProposalButtonVisible));
                OnPropertyChanged(nameof(IsPactButtonVisible));
                OnPropertyChanged(nameof(SelectedWarKingdomName));
                return;
            }

            StatusMessage = string.Empty;
            IsProposalMode = false;
            IsPeaceProposalMode = true;
            RefreshPeaceSettlementChoices();
        }

        public void ExecuteProposePeace()
        {
            Kingdom target = _selectedWarKingdom;
            if (_playerKingdom == null ||
                target == null ||
                target.IsEliminated ||
                !target.IsAtWarWith(_playerKingdom))
            {
                StatusMessage = "That kingdom is no longer at war with you.";
                _selectedWarKingdom = null;
                IsPeaceProposalMode = false;
                OnPropertyChanged(nameof(IsPactButtonVisible));
                OnPropertyChanged(nameof(IsPeaceProposalButtonVisible));
                OnPropertyChanged(nameof(IsPeaceProposalVisible));
                OnPropertyChanged(nameof(IsBackButtonVisible));
                OnPropertyChanged(nameof(SelectedWarKingdomName));
                return;
            }

            if (!CanSubmitPeaceProposal)
            {
                StatusMessage = "Choose the required settlement terms before submitting.";
                return;
            }

            CustomPeaceBehavior peaceBehavior = CustomPeaceBehavior.Instance;
            if (peaceBehavior == null)
            {
                StatusMessage = "The peace decision system is unavailable.";
                return;
            }

            PeaceDealOptions selectedOptions = _peaceClauses
                .Where(clause => clause.IsSelected)
                .Aggregate(
                    PeaceDealOptions.None,
                    (options, clause) => options | clause.Option);

            var terms = new PeaceProposalTerms
            {
                ProposerKingdom = _playerKingdom,
                TargetKingdom = target,
                SelectedOptions = selectedOptions,
                DailyTributeToTarget = _peaceDailyTribute,
                DailyTributeDurationDays = _peaceTributeDurationDays,
                CededSettlement = _selectedCededSettlement,
                TargetSettlementToExchange = _selectedTargetSettlementToExchange,
                ProposerSettlementToExchange = _selectedProposerSettlementToExchange,
                TargetSubjectType = SubjectType.Vassal
            };

            if (!peaceBehavior.SubmitPeaceProposal(terms))
            {
                StatusMessage = $"Could not submit the proposal to {target.Name}'s kingdom council.";
                return;
            }

            StatusMessage =
                $"Peace proposal submitted to {target.Name}'s council. Its lords will evaluate the terms.";
        }

        public void TogglePeaceClause(PeaceDealOptions option)
        {
            PeaceClauseVM clause = _peaceClauses.FirstOrDefault(item => item.Option == option);
            if (clause == null)
                return;

            clause.IsSelected = !clause.IsSelected;
            if (option == PeaceDealOptions.CededSettlement ||
                option == PeaceDealOptions.TerritoryExchange)
            {
                OnPropertyChanged(nameof(IsCededSettlementSelectorVisible));
                OnPropertyChanged(nameof(IsTerritoryExchangeSelectorVisible));
            }

            OnPropertyChanged(nameof(CanSubmitPeaceProposal));
        }

        public void SelectPeaceSettlement(Settlement settlement, bool isTargetSettlement)
        {
            if (settlement == null)
                return;

            if (isTargetSettlement)
            {
                _selectedCededSettlement = settlement;
                _selectedTargetSettlementToExchange = settlement;
                foreach (PeaceSettlementVM item in _targetPeaceSettlements)
                    item.IsSelected = item.Represents(settlement);
            }
            else
            {
                _selectedProposerSettlementToExchange = settlement;
                foreach (PeaceSettlementVM item in _proposerPeaceSettlements)
                    item.IsSelected = item.Represents(settlement);
            }

            OnPropertyChanged(nameof(CanSubmitPeaceProposal));
        }

        public void ExecuteIncreasePeaceTribute()
        {
            if (_peaceDailyTribute == 0)
            {
                _peaceTributeDurationDays = 30;
                OnPropertyChanged(nameof(PeaceTributeDurationDays));
            }

            _peaceDailyTribute = Math.Min(100000, _peaceDailyTribute + 100);
            OnPropertyChanged(nameof(PeaceDailyTribute));
        }

        public void ExecuteDecreasePeaceTribute()
        {
            _peaceDailyTribute = Math.Max(0, _peaceDailyTribute - 100);
            OnPropertyChanged(nameof(PeaceDailyTribute));
            if (_peaceDailyTribute == 0)
            {
                _peaceTributeDurationDays = 0;
                OnPropertyChanged(nameof(PeaceTributeDurationDays));
            }
        }

        public void ExecuteIncreasePeaceTributeDuration()
        {
            if (_peaceDailyTribute == 0)
                return;

            _peaceTributeDurationDays = Math.Min(365, _peaceTributeDurationDays + 30);
            OnPropertyChanged(nameof(PeaceTributeDurationDays));
        }

        public void ExecuteDecreasePeaceTributeDuration()
        {
            _peaceTributeDurationDays = Math.Max(0, _peaceTributeDurationDays - 30);
            OnPropertyChanged(nameof(PeaceTributeDurationDays));
        }

        private bool IsPeaceClauseSelected(PeaceDealOptions option)
        {
            return _peaceClauses.Any(clause =>
                clause.Option == option && clause.IsSelected);
        }

        private void InitializePeaceClauses()
        {
            _peaceClauses.Add(new PeaceClauseVM(
                this,
                PeaceDealOptions.WarReparation,
                "War Reparations",
                "The defeated kingdom pays 10% of its estimated daily income.",
                false));
            _peaceClauses.Add(new PeaceClauseVM(
                this,
                PeaceDealOptions.TerritoryExchange,
                "Exchange Settlements",
                "Each kingdom cedes one selected settlement to the other.",
                false));
            _peaceClauses.Add(new PeaceClauseVM(
                this,
                PeaceDealOptions.PlunderTreasury,
                "Seize Treasury",
                "Transfer the target kingdom's treasury to your kingdom.",
                false));
            _peaceClauses.Add(new PeaceClauseVM(
                this,
                PeaceDealOptions.Subjectize,
                "Make Them a Subject",
                "The target kingdom becomes your vassal.",
                false));
            _peaceClauses.Add(new PeaceClauseVM(
                this,
                PeaceDealOptions.ClaimThrone,
                "Claim the Throne",
                "Your ruling clan takes the target kingdom's throne.",
                false));
            _peaceClauses.Add(new PeaceClauseVM(
                this,
                PeaceDealOptions.ExecuteRuler,
                "Execute Their Ruler",
                "Execute the target kingdom's ruler.",
                false));
            _peaceClauses.Add(new PeaceClauseVM(
                this,
                PeaceDealOptions.CededSettlement,
                "Demand a Settlement",
                "The target cedes one selected settlement to your kingdom.",
                false));
            _peaceClauses.Add(new PeaceClauseVM(
                this,
                PeaceDealOptions.GainIndependence,
                "Grant Independence",
                "End the subject relationship between the two kingdoms.",
                false));
            _peaceClauses.Add(new PeaceClauseVM(
                this,
                PeaceDealOptions.Truce,
                "Extended Truce",
                "Set a 200-day truce instead of the standard 100 days.",
                true));
        }

        private void RefreshPeaceSettlementChoices()
        {
            _targetPeaceSettlements.Clear();
            _proposerPeaceSettlements.Clear();
            _selectedCededSettlement = null;
            _selectedTargetSettlementToExchange = null;
            _selectedProposerSettlementToExchange = null;

            if (_selectedWarKingdom != null)
            {
                foreach (Settlement settlement in _selectedWarKingdom.Settlements
                             .Where(item => item != null)
                             .OrderBy(item => item.Name.ToString()))
                {
                    var settlementVM = new PeaceSettlementVM(this, settlement, true, false);
                    _targetPeaceSettlements.Add(settlementVM);
                    if (_selectedCededSettlement == null)
                    {
                        _selectedCededSettlement = settlement;
                        _selectedTargetSettlementToExchange = settlement;
                        settlementVM.IsSelected = true;
                    }
                }
            }

            if (_playerKingdom != null)
            {
                foreach (Settlement settlement in _playerKingdom.Settlements
                             .Where(item => item != null)
                             .OrderBy(item => item.Name.ToString()))
                {
                    var settlementVM = new PeaceSettlementVM(this, settlement, false, false);
                    _proposerPeaceSettlements.Add(settlementVM);
                    if (_selectedProposerSettlementToExchange == null)
                    {
                        _selectedProposerSettlementToExchange = settlement;
                        settlementVM.IsSelected = true;
                    }
                }
            }

            OnPropertyChanged(nameof(CanSubmitPeaceProposal));
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
                ShowProposalResult("Proposal unavailable", StatusMessage);
                return;
            }

            if (!DiplomacyEvaluator.EvaluateProposal(
                    _playerKingdom,
                    target,
                    _selectedProposalType,
                    out string reason))
            {
                StatusMessage = $"Proposal declined: {reason}";
                ShowProposalResult("Proposal declined", reason);
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
                        ShowProposalResult("Agreement unavailable", StatusMessage);
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
                        ShowProposalResult("Agreement unavailable", StatusMessage);
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
                        ShowProposalResult("Agreement unavailable", StatusMessage);
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
                        ShowProposalResult("Agreement unavailable", StatusMessage);
                        return;
                    }

                    break;
            }

            StatusMessage = $"Proposal accepted. Agreement established with {target.Name}.";
            RefreshTreatyCategories();
            RefreshProposalTargets();
            OnPropertyChanged(nameof(CanSubmitProposal));
            ShowProposalResult("Agreement accepted", StatusMessage);
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
            if (!CanOpenPactNegotiation(kingdom))
                return false;

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

        private bool CanOpenPactNegotiation(Kingdom kingdom)
        {
            return _playerKingdom != null &&
                   kingdom != null &&
                   kingdom != _playerKingdom &&
                   !kingdom.IsEliminated &&
                   !kingdom.IsAtWarWith(_playerKingdom) &&
                   SubjectBehavior.CanCreateDiplomaticPact(_playerKingdom, kingdom);
        }

        private static void ShowProposalResult(string title, string message)
        {
            InformationManager.ShowInquiry(new InquiryData(
                title,
                message,
                true,
                false,
                "OK",
                string.Empty,
                null,
                null), true, true);
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

            var wars = new DiplomacyCategoryVM("Wars");
            foreach (Kingdom kingdom in Kingdom.All
                         .Where(item =>
                             item != null &&
                             item != _playerKingdom &&
                             !item.IsEliminated &&
                             item.IsAtWarWith(_playerKingdom))
                         .OrderBy(item => item.Name.ToString()))
            {
                wars.Items.Add(new DiplomacyItemVM(
                    kingdom,
                    "At war — select, then propose peace.",
                    this,
                    isWarKingdom: true));
            }
            _categories.Add(wars);

            var subjects = new DiplomacyCategoryVM("Subjects");
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
            var category = new DiplomacyCategoryVM(title);

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
