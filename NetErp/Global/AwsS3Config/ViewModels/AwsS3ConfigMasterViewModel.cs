using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Billing;
using Models.Books;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
using Ninject.Activation;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.Primitives;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.AwsS3Config.ViewModels
{
    public class AwsS3ConfigMasterViewModel : Screen,
         IHandle<AwsS3ConfigCreateMessage>,
        IHandle<AwsS3ConfigUpdateMessage>,
        IHandle<AwsS3ConfigDeleteMessage>
    {

        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<AwsS3ConfigGraphQLModel> _awsS3ConfigService;
        public AwsS3ConfigViewModel Context { get; set; }

        public AwsS3ConfigMasterViewModel(AwsS3ConfigViewModel context, Helpers.Services.INotificationService notificationService,
            IRepository<AwsS3ConfigGraphQLModel> awsS3ConfigService)
        {
            Context = context;
            _notificationService = notificationService;
            _awsS3ConfigService = awsS3ConfigService;
            Context.EventAggregator.SubscribeOnUIThread(this);

        }
        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            IsBusy = true;
            _ = Task.Run(() => LoadAwsS3ConfigsAsync());
             this.SetFocus(() => FilterSearch);
        }

        #region propertiesAndCommands
        private AwsS3ConfigGraphQLModel _selectedItem;
        public AwsS3ConfigGraphQLModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanDeleteAwsS3Config));
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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = Task.Run(() => LoadAwsS3ConfigsAsync());
                }
            }
        }

        public ObservableCollection<AwsS3ConfigGraphQLModel> _awsS3Configs;
        public ObservableCollection<AwsS3ConfigGraphQLModel> AwsS3Configs
        {
            get { return _awsS3Configs; }
            set
            {
                if (_awsS3Configs != value)
                {
                    _awsS3Configs = value;
                    NotifyOfPropertyChange(nameof(AwsS3Configs));
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
        public bool CanDeleteAwsS3Config
        {
            get
            {
                if (SelectedItem is null) return false;
                return true;
            }
        }
        private ICommand _deleteAwsS3ConfigCommand;
        public ICommand DeleteAwsS3ConfigCommand
        {
            get
            {
                if (_deleteAwsS3ConfigCommand is null) _deleteAwsS3ConfigCommand = new AsyncCommand(DeleteAwsS3Config);
                return _deleteAwsS3ConfigCommand;
            }
        }
        private ICommand _createAwsS3ConfigCommand;
        public ICommand CreateAwsS3ConfigCommand
        {
            get
            {
                if (_createAwsS3ConfigCommand is null) _createAwsS3ConfigCommand = new AsyncCommand(CreateAwsS3ConfigAsync);
                return _createAwsS3ConfigCommand;
            }
        }
        public async Task CreateAwsS3ConfigAsync()
        {
            await Context.ActivateDetailViewForNewAsync();
        }
        public async Task EditAwsS3Config()
        {
            await Context.ActivateDetailViewForEditAsync(SelectedItem.Id);

        }
        #endregion
        #region ApiMethods
        public string GetLoadAwsS3ConfigsQuery()
        {
            var AwsS3ConfigFields = FieldSpec<PageType<AwsS3ConfigGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    
                    )
                .Build();

            var AwsS3ConfigParameters = new GraphQLQueryParameter("filters", "AwsS3ConfigFilters");

            var AwsS3ConfigFragment = new GraphQLQueryFragment("awsS3ConfigsPage", [AwsS3ConfigParameters], AwsS3ConfigFields, "PageResponse");

            var builder = new GraphQLQueryBuilder([AwsS3ConfigFragment]);

            return builder.GetQuery();
        }
        public async Task LoadAwsS3ConfigsAsync()
        {
            try
            {

                IsBusy = true;

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.matching = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                string query = GetLoadAwsS3ConfigsQuery();

                PageType<AwsS3ConfigGraphQLModel> result = await _awsS3ConfigService.GetPageAsync(query, variables);
                this.AwsS3Configs = [.. result.Entries];
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
        public string GetCanDeleteAwsS3ConfigQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteAwsS3Config", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        public async Task DeleteAwsS3Config()
        {
            try
            {

                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteAwsS3ConfigQuery();

                object variables = new { canDeleteResponseId = SelectedItem.Id };

                var validation = await _awsS3ConfigService.CanDeleteAsync(query, variables);

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
                DeleteResponseType deletedAwsS3Config = await Task.Run(() => this.ExecuteDeleteAwsS3ConfigAsync(SelectedItem.Id));

                if (!deletedAwsS3Config.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedAwsS3Config.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new AwsS3ConfigDeleteMessage { DeletedAwsS3Config = deletedAwsS3Config });

                NotifyOfPropertyChange(nameof(CanDeleteAwsS3Config));
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

        public string GetDeleteAwsS3ConfigQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("deleteAwsS3Config", [parameter], fields, alias: "DeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public async Task<DeleteResponseType> ExecuteDeleteAwsS3ConfigAsync(int id)
        {
            try
            {

                string query = GetDeleteAwsS3ConfigQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _awsS3ConfigService.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task HandleAsync(AwsS3ConfigDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAwsS3ConfigsAsync();
                _notificationService.ShowSuccess(message.DeletedAwsS3Config.Message);
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        public async Task HandleAsync(AwsS3ConfigUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAwsS3ConfigsAsync();
                _notificationService.ShowSuccess(message.UpdatedAwsS3Config.Message);
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }

        public async Task HandleAsync(AwsS3ConfigCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadAwsS3ConfigsAsync();
                _notificationService.ShowSuccess(message.CreatedAwsS3Config.Message);
            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
        }
        #endregion

    }
}
