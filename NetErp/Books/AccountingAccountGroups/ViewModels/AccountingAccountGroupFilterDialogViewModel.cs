using AutoMapper;
using Caliburn.Micro;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Books;
using NetErp.Books.AccountingAccountGroups.DTO;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Books.AccountingAccountGroups.ViewModels
{
    public class AccountingAccountGroupFilterDialogViewModel : Screen
    {
        #region Dependencies

        private readonly IMapper _autoMapper;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IRepository<AccountingAccountGroupGraphQLModel> _accountingAccountGroupService;

        #endregion

        #region Properties

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        public AccountingAccountGroupGraphQLModel Group { get; set; } = null!;

        public ObservableCollection<AccountingAccountGroupDTO> AccountingAccounts { get; set; } = [];

        private ObservableCollection<AccountingAccountGroupFilterDTO> _groupFilters = [];
        public ObservableCollection<AccountingAccountGroupFilterDTO> GroupFilters
        {
            get => _groupFilters;
            set
            {
                if (_groupFilters != value)
                {
                    _groupFilters = value;
                    NotifyOfPropertyChange(nameof(GroupFilters));
                }
            }
        }

        private string _selectedFilterAccountCode = string.Empty;
        public string SelectedFilterAccountCode
        {
            get => _selectedFilterAccountCode;
            set
            {
                if (_selectedFilterAccountCode != value)
                {
                    _selectedFilterAccountCode = value;
                    NotifyOfPropertyChange(nameof(SelectedFilterAccountCode));
                    NotifyOfPropertyChange(nameof(CanAttachFilter));
                }
            }
        }

        public bool CanAttachFilter => !string.IsNullOrEmpty(SelectedFilterAccountCode)
            && !GroupFilters.Any(f => f.AccountingAccountCode.Trim() == SelectedFilterAccountCode.Trim());

        private double _dialogWidth = 500;
        public double DialogWidth
        {
            get => _dialogWidth;
            set
            {
                if (_dialogWidth != value)
                {
                    _dialogWidth = value;
                    NotifyOfPropertyChange(nameof(DialogWidth));
                }
            }
        }

        private double _dialogHeight = 385;
        public double DialogHeight
        {
            get => _dialogHeight;
            set
            {
                if (_dialogHeight != value)
                {
                    _dialogHeight = value;
                    NotifyOfPropertyChange(nameof(DialogHeight));
                }
            }
        }

        public bool FiltersChanged { get; private set; }

        #endregion

        #region Commands

        private ICommand? _attachFilterCommand;
        public ICommand AttachFilterCommand
        {
            get
            {
                _attachFilterCommand ??= new AsyncCommand(AttachFilterAsync);
                return _attachFilterCommand;
            }
        }

        private ICommand? _detachFilterCommand;
        public ICommand DetachFilterCommand
        {
            get
            {
                _detachFilterCommand ??= new AsyncCommand<AccountingAccountGroupFilterDTO>(DetachFilterAsync);
                return _detachFilterCommand;
            }
        }

        private ICommand? _closeCommand;
        public ICommand CloseCommand
        {
            get
            {
                _closeCommand ??= new AsyncCommand(CloseAsync);
                return _closeCommand;
            }
        }

        #endregion

        #region Constructor

        public AccountingAccountGroupFilterDialogViewModel(
            IMapper mapper,
            Helpers.Services.INotificationService notificationService,
            Helpers.IDialogService dialogService,
            IRepository<AccountingAccountGroupGraphQLModel> accountingAccountGroupService)
        {
            _autoMapper = mapper;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _accountingAccountGroupService = accountingAccountGroupService;
        }

        #endregion

        #region Methods

        public void Initialize(
            AccountingAccountGroupGraphQLModel group,
            ObservableCollection<AccountingAccountGroupDTO> allAccounts)
        {
            Group = group;
            AccountingAccounts = new ObservableCollection<AccountingAccountGroupDTO>(
                allAccounts.Where(a => a.Code.Trim().Length >= 4 && a.Code.Trim().Length < 8));
            GroupFilters = _autoMapper.Map<ObservableCollection<AccountingAccountGroupFilterDTO>>(group.Filters);
        }

        public async Task AttachFilterAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedFilterAccountCode)) return;

                var account = AccountingAccounts.FirstOrDefault(a => a.Code.Trim() == SelectedFilterAccountCode.Trim());
                if (account == null) return;

                IsBusy = true;

                string query = _attachFilterMutation.Value;
                dynamic variables = new ExpandoObject();
                variables.attachAccountToAccountingAccountGroupFilterAccountingAccountGroupId = Group.Id;
                variables.attachAccountToAccountingAccountGroupFilterAccountingAccountId = account.Id;

                var response = await _accountingAccountGroupService.MutationContextAsync<AttachAccountToGroupFilterResponse>(query, variables);
                var result = response.AttachAccountToAccountingAccountGroupFilter;
                if (result.Success)
                {
                    var newFilter = new AccountingAccountGroupFilterGraphQLModel
                    {
                        AccountingAccount = new AccountingAccountGraphQLModel
                        {
                            Id = account.Id,
                            Code = account.Code,
                            Name = account.Name
                        }
                    };
                    Group.Filters = [.. Group.Filters, newFilter];
                    GroupFilters = _autoMapper.Map<ObservableCollection<AccountingAccountGroupFilterDTO>>(Group.Filters);
                    SelectedFilterAccountCode = string.Empty;
                    FiltersChanged = true;
                    _notificationService.ShowSuccess("Filtro agregado correctamente");
                }
                else
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: result.Message ?? "No se pudo agregar el filtro",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"{GetType().Name}.AttachFilterAsync \r\n{ex.Message}",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DetachFilterAsync(AccountingAccountGroupFilterDTO? filter)
        {
            try
            {
                if (filter is null) return;

                IsBusy = true;

                string query = _detachFilterMutation.Value;
                dynamic variables = new ExpandoObject();
                variables.detachAccountFromAccountingAccountGroupFilterAccountingAccountGroupId = Group.Id;
                variables.detachAccountFromAccountingAccountGroupFilterAccountingAccountId = filter.AccountingAccountId;

                var response = await _accountingAccountGroupService.MutationContextAsync<DetachAccountFromGroupFilterResponse>(query, variables);
                var result = response.DetachAccountFromAccountingAccountGroupFilter;
                if (result.Success)
                {
                    Group.Filters = Group.Filters
                        .Where(f => f.AccountingAccount.Id != filter.AccountingAccountId)
                        .ToList();
                    GroupFilters = _autoMapper.Map<ObservableCollection<AccountingAccountGroupFilterDTO>>(Group.Filters);
                    FiltersChanged = true;
                    _notificationService.ShowSuccess("Filtro eliminado correctamente");
                }
                else
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: result.Message ?? "No se pudo eliminar el filtro",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!",
                        text: $"{GetType().Name}.DetachFilterAsync \r\n{ex.Message}",
                        messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CloseAsync()
        {
            await _dialogService.CloseDialogAsync(this, FiltersChanged);
        }

        #endregion

        #region GraphQL Mutations

        private static readonly Lazy<string> _attachFilterMutation = new(() =>
        {
            var fields = FieldSpec<FilterMutationResult>
                .Create()
                .Field(f => f.Success)
                .Field(f => f.Message)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("accountingAccountGroupId", "ID!"),
                new("accountingAccountId", "ID!")
            };
            var fragment = new GraphQLQueryFragment("attachAccountToAccountingAccountGroupFilter", parameters, fields);
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        private static readonly Lazy<string> _detachFilterMutation = new(() =>
        {
            var fields = FieldSpec<FilterMutationResult>
                .Create()
                .Field(f => f.Success)
                .Field(f => f.Message)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("accountingAccountGroupId", "ID!"),
                new("accountingAccountId", "ID!")
            };
            var fragment = new GraphQLQueryFragment("detachAccountFromAccountingAccountGroupFilter", parameters, fields);
            return new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION);
        });

        #endregion
    }
}
