using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Global.Collaborator.DTO;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.Collaborator.ViewModels
{
    public class CollaboratorDetailViewModel(
        IRepository<AccountGraphQLModel> accountService,
        AccessProfileCache accessProfileCache,
        EmailCache emailCache,
        CostCenterCache costCenterCache,
        JoinableTaskFactory joinableTaskFactory) : Screen
    {
        #region Dependencies

        private readonly IRepository<AccountGraphQLModel> _accountService = accountService;
        private readonly AccessProfileCache _accessProfileCache = accessProfileCache;
        private readonly EmailCache _emailCache = emailCache;
        private readonly CostCenterCache _costCenterCache = costCenterCache;
        private readonly JoinableTaskFactory _joinableTaskFactory = joinableTaskFactory;

        #endregion

        #region Properties

        public int AccountId { get; set; }

        public double DialogWidth
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogWidth));
                }
            }
        } = 550;

        public double DialogHeight
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogHeight));
                }
            }
        } = 550;

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Profession { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        public ObservableCollection<SelectableItemDTO> AvailableProfiles { get; set; } = [];
        public ObservableCollection<SelectableItemDTO> AvailableEmails { get; set; } = [];
        public ObservableCollection<SelectableItemDTO> AvailableCostCenters { get; set; } = [];

        private HashSet<int> _originalProfileIds = [];
        private HashSet<int> _originalEmailIds = [];
        private HashSet<int> _originalCostCenterIds = [];

        public bool HasChanges
        {
            get
            {
                HashSet<int> currentProfiles = [.. AvailableProfiles.Where(p => p.IsSelected).Select(p => p.Id)];
                HashSet<int> currentEmails = [.. AvailableEmails.Where(e => e.IsSelected).Select(e => e.Id)];
                HashSet<int> currentCostCenters = [.. AvailableCostCenters.Where(c => c.IsSelected).Select(c => c.Id)];

                return !_originalProfileIds.SetEquals(currentProfiles) ||
                       !_originalEmailIds.SetEquals(currentEmails) ||
                       !_originalCostCenterIds.SetEquals(currentCostCenters);
            }
        }

        public bool CanSave => HasChanges;

        #endregion

        #region Commands

        private ICommand? _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        private ICommand? _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                _cancelCommand ??= new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        #endregion

        #region Load

        public async Task LoadDataAsync(int accountId)
        {
            AccountId = accountId;

            var (_, query) = _loadAccountQuery.Value;

            dynamic variables = new ExpandoObject();
            variables.singleItemResponseId = accountId;

            AccountGraphQLModel account = await _accountService.FindByIdAsync(query, variables);

            FullName = account.FullName;
            Email = account.Email;
            Profession = account.Profession;
            PhotoUrl = account.PhotoUrl;

            HashSet<int> assignedProfileIds = [.. account.AccessProfiles.Select(p => p.Id)];
            HashSet<int> assignedEmailIds = [.. account.Emails.Select(e => e.Id)];
            HashSet<int> assignedCostCenterIds = [.. account.CostCenters.Select(c => c.Id)];

            AvailableProfiles = [.. _accessProfileCache.Items
                .Where(p => p.IsActive && !p.IsSystemAdmin)
                .Select(p => new SelectableItemDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    IsSelected = assignedProfileIds.Contains(p.Id)
                })];

            AvailableEmails = [.. _emailCache.Items
                .Where(e => e.IsActive)
                .Select(e => new SelectableItemDTO
                {
                    Id = e.Id,
                    Name = e.Email,
                    Description = e.Description,
                    IsSelected = assignedEmailIds.Contains(e.Id)
                })];

            AvailableCostCenters = [.. _costCenterCache.Items
                .Select(c => new SelectableItemDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsSelected = assignedCostCenterIds.Contains(c.Id)
                })];

            // Subscribe to changes for CanSave
            foreach (SelectableItemDTO item in AvailableProfiles)
                item.PropertyChanged += (_, _) => NotifyOfPropertyChange(nameof(CanSave));
            foreach (SelectableItemDTO item in AvailableEmails)
                item.PropertyChanged += (_, _) => NotifyOfPropertyChange(nameof(CanSave));
            foreach (SelectableItemDTO item in AvailableCostCenters)
                item.PropertyChanged += (_, _) => NotifyOfPropertyChange(nameof(CanSave));

            _originalProfileIds = assignedProfileIds;
            _originalEmailIds = assignedEmailIds;
            _originalCostCenterIds = assignedCostCenterIds;
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;

                List<int> selectedProfileIds = [.. AvailableProfiles.Where(p => p.IsSelected).Select(p => p.Id)];
                List<int> selectedEmailIds = [.. AvailableEmails.Where(e => e.IsSelected).Select(e => e.Id)];
                List<int> selectedCostCenterIds = [.. AvailableCostCenters.Where(c => c.IsSelected).Select(c => c.Id)];

                // Profiles
                if (!_originalProfileIds.SetEquals(selectedProfileIds.ToHashSet()))
                {
                    var (_, query) = _associateProfilesQuery.Value;
                    dynamic variables = new ExpandoObject();
                    variables.updateResponseAccountId = AccountId;
                    variables.updateResponseAccessProfileIds = selectedProfileIds;
                    await _accountService.UpdateAsync<UpsertResponseType<AccountGraphQLModel>>(query, variables);
                }

                // Emails
                if (!_originalEmailIds.SetEquals(selectedEmailIds.ToHashSet()))
                {
                    var (_, query) = _associateEmailsQuery.Value;
                    dynamic variables = new ExpandoObject();
                    variables.updateResponseAccountId = AccountId;
                    variables.updateResponseEmailIds = selectedEmailIds;
                    await _accountService.UpdateAsync<UpsertResponseType<AccountGraphQLModel>>(query, variables);
                }

                // Cost Centers
                if (!_originalCostCenterIds.SetEquals(selectedCostCenterIds.ToHashSet()))
                {
                    var (_, query) = _associateCostCentersQuery.Value;
                    dynamic variables = new ExpandoObject();
                    variables.updateResponseAccountId = AccountId;
                    variables.updateResponseCostCenterIds = selectedCostCenterIds;
                    await _accountService.UpdateAsync<UpsertResponseType<AccountGraphQLModel>>(query, variables);
                }

                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al guardar cambios.\r\n{GetType().Name}.{nameof(SaveAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadAccountQuery = new(() =>
        {
            var fields = FieldSpec<AccountGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.FullName)
                .Field(e => e.Email)
                .Field(e => e.Profession)
                .Field(e => e.PhotoUrl)
                .SelectList(e => e.AccessProfiles, sq => sq
                    .Field(p => p.Id))
                .SelectList(e => e.Emails, sq => sq
                    .Field(em => em.Id))
                .SelectList(e => e.CostCenters, sq => sq
                    .Field(c => c.Id))
                .Build();

            var fragment = new GraphQLQueryFragment("account",
                [new("id", "ID!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _associateProfilesQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AccountGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "account", nested: sq => sq
                    .Field(e => e.Id))
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("associateAccountAccessProfiles",
                [new("accountId", "ID!"), new("accessProfileIds", "[ID!]!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _associateEmailsQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AccountGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "account", nested: sq => sq
                    .Field(e => e.Id))
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("associateAccountEmails",
                [new("accountId", "ID!"), new("emailIds", "[ID!]!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _associateCostCentersQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<AccountGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "account", nested: sq => sq
                    .Field(e => e.Id))
                .Field(f => f.Success)
                .Field(f => f.Message)
                .Build();

            var fragment = new GraphQLQueryFragment("associateAccountCostCenters",
                [new("accountId", "ID!"), new("costCenterIds", "[ID!]!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        #endregion
    }
}
