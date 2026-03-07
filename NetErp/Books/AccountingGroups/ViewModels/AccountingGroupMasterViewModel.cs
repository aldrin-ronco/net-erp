using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Books;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using Services.Books.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.Primitives;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingGroups.ViewModels
{
    public class AccountingGroupMasterViewModel : Screen
    {
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AccountingGroupGraphQLModel> _accountingGroupService;
        public AccountingGroupViewModel Context { get; set; }
        public AccountingGroupMasterViewModel(AccountingGroupViewModel context, Helpers.Services.INotificationService notificationService,
            IRepository<AccountingGroupGraphQLModel> accountingGroupService)
        {
            Context = context;
            _notificationService = notificationService;
            _accountingGroupService = accountingGroupService;
        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            IsBusy = true;
            _ = Task.Run(() => LoadAccountingGroupsAsync());
          //  this.SetFocus(() => FilterSearch);
        }
        #region ModelProperties

        #endregion
        #region PropertiesAndCommands
        public ObservableCollection<AccountingGroupGraphQLModel> _accountingGroups;
        public ObservableCollection<AccountingGroupGraphQLModel> AccountingGroups
        {
            get { return _accountingGroups; }
            set
            {
                if (_accountingGroups != value)
                {
                    _accountingGroups = value;
                    NotifyOfPropertyChange(nameof(AccountingGroups));
                }
            }
        }
        private AccountingGroupGraphQLModel? _selectedItem = null;

        public AccountingGroupGraphQLModel? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanDeleteAccountingGroup));
                }
            }
        }
        private string _filterSearch;
        public string FilterSearch
        {
            get { return _filterSearch; }
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = Task.Run(() => LoadAccountingGroupsAsync());
                }
            }
        }
        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }
        public bool CanDeleteAccountingGroup
        {
            get
            {
                if (SelectedItem is null) return false;
                return true;
            }
        }
        private ICommand _deleteAccountingGroupCommand;
        public ICommand DeleteAccountingGroupCommand
        {
            get
            {
                if (_deleteAccountingGroupCommand is null) _deleteAccountingGroupCommand = new AsyncCommand(DeleteAccountingGroup);
                return _deleteAccountingGroupCommand;
            }
        }
        private ICommand _createAccountingGroupCommand;
        public ICommand CreateAccountingGroupCommand
        {
            get
            {
                if (_createAccountingGroupCommand is null) _createAccountingGroupCommand = new AsyncCommand(CreateAccountingGroupAsync);
                return _createAccountingGroupCommand;
            }
        }
        public async Task CreateAccountingGroupAsync()
        {
            await Context.ActivateDetailViewForNewAsync();
        }
        public async Task EditAccountingGroup()
        {
            await Context.ActivateDetailViewForEditAsync(SelectedItem.Id);

        }
        #endregion

        #region ApiMethods
        public string GetLoadAccountingGroupsQuery()
        {
            var accountingGroupFields = FieldSpec<PageType<AccountingGroupGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Name)
                    .Select(e => e.AccountInventory, acc => acc
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                            .Field(c => c.Code)
                            )
                    .Select(e => e.AccountCost, acc => acc
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                            .Field(c => c.Code)
                            )
                    .Select(e => e.AccountIncome, acc => acc
                            .Field(c => c.Id)
                            .Field(c => c.Name)
                            .Field(c => c.Code)
                            )
                    )
                .Build();

            var accountingGroupParameters = new GraphQLQueryParameter("filters", "AccountingGroupFilters");

            var accountingGroupFragment = new GraphQLQueryFragment("accountingGroupsPage", [accountingGroupParameters], accountingGroupFields, "PageResponse");

            var builder = new GraphQLQueryBuilder([accountingGroupFragment]);

            return builder.GetQuery();
        }
        public async Task LoadAccountingGroupsAsync()
        {
            try
            {

                IsBusy = true;

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
               // variables.pageResponseFilters.matching = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                string query = GetLoadAccountingGroupsQuery();

                PageType<AccountingGroupGraphQLModel> result = await _accountingGroupService.GetPageAsync(query, variables);
                this.AccountingGroups = [.. result.Entries];
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }

        }
        public string GetCanDeleteAccountingGroupQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteAccountingGroup", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        public async Task DeleteAccountingGroup()
        {
            try
            {

                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteAccountingGroupQuery();

                object variables = new { canDeleteResponseId = SelectedItem.Id };

                var validation = await _accountingGroupService.CanDeleteAsync(query, variables); 

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    if (ThemedMessageBox.Show("Atención !", "¿Confirma que desea eliminar el registro seleccionado?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;
                }
                else
                {
                    IsBusy = false;
                    ThemedMessageBox.Show("Atención !", "El registro no puede ser eliminado" +
                        (char)13 + (char)13 + validation.Message, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsBusy = true;
                DeleteResponseType deletedAccountingGroup = await Task.Run(() => this.ExecuteDeleteAccountingGroupAsync(SelectedItem.Id));

                if (!deletedAccountingGroup.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedAccountingGroup.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new AccountingGroupDeleteMessage { DeletedAccountingGroup = deletedAccountingGroup });

                NotifyOfPropertyChange(nameof(CanDeleteAccountingGroup));
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{exGraphQL.Message}\r\n{graphQLError.Errors[0].Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{this.GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public string GetDeleteAccountingGroupQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("deleteAccountingGroup", [parameter], fields, alias: "DeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public async Task<DeleteResponseType> ExecuteDeleteAccountingGroupAsync(int id)
        {
            try
            {

                string query = GetDeleteAccountingGroupQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _accountingGroupService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion
    }
}
