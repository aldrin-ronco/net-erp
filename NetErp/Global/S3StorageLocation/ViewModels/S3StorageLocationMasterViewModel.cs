using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Global;
using NetErp.Global.S3StorageLocation.ViewModels;
using NetErp.Helpers;
using NetErp.Helpers.GraphQLQueryBuilder;
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

namespace NetErp.Global.S3StorageLocation.ViewModels
{
    public class S3StorageLocationMasterViewModel : Screen,
        IHandle<S3StorageLocationDeleteMessage>,
        IHandle<S3StorageLocationUpdateMessage>,
        IHandle<S3StorageLocationCreateMessage>
    {
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly IRepository<S3StorageLocationGraphQLModel> _s3StorageLocationGraphQLModel;
        public S3StorageLocationViewModel Context { get; set; }
        public S3StorageLocationMasterViewModel(S3StorageLocationViewModel context, Helpers.Services.INotificationService notificationService,
            IRepository<S3StorageLocationGraphQLModel> s3StorageLocationGraphQLModel)
        {
            _notificationService = notificationService;
            _s3StorageLocationGraphQLModel = s3StorageLocationGraphQLModel;
            Context = context;
            Context.EventAggregator.SubscribeOnUIThread(this);
        }


        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            IsBusy = true;
            _ = Task.Run(() => LoadS3StorageLocationAsync());
            this.SetFocus(() => FilterSearch);
        }

        #region propertiesAndCommands
        private S3StorageLocationGraphQLModel _selectedItem;
        public S3StorageLocationGraphQLModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanDeleteS3StorageLocation));
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
                    if (string.IsNullOrEmpty(value) || value.Length >= 3) _ = Task.Run(() => LoadS3StorageLocationAsync());
                }
            }
        }

        public ObservableCollection<S3StorageLocationGraphQLModel> _s3StorageLocations;
        public ObservableCollection<S3StorageLocationGraphQLModel> S3StorageLocations
        {
            get { return _s3StorageLocations; }
            set
            {
                if (_s3StorageLocations != value)
                {
                    _s3StorageLocations = value;
                    NotifyOfPropertyChange(nameof(S3StorageLocations));
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
        public bool CanDeleteS3StorageLocation
        {
            get
            {
                if (SelectedItem is null) return false;
                return true;
            }
        }


        private ICommand _deleteS3StorageLocationCommand;
        public ICommand DeleteS3StorageLocationCommand
        {
            get
            {
                if (_deleteS3StorageLocationCommand is null) _deleteS3StorageLocationCommand = new AsyncCommand(DeleteS3StorageLocation);
                return _deleteS3StorageLocationCommand;
            }
        }
        private ICommand _createS3StorageLocationCommand;
        public ICommand CreateS3StorageLocationCommand
        {
            get
            {
                if (_createS3StorageLocationCommand is null) _createS3StorageLocationCommand = new AsyncCommand(CreateS3StorageLocationAsync);
                return _createS3StorageLocationCommand;
            }
        }
        public async Task CreateS3StorageLocationAsync()
        {
            await Context.ActivateDetailViewForNewAsync();
        }
        public async Task EditS3StorageLocation()
        {
            await Context.ActivateDetailViewForEditAsync(SelectedItem.Id);

        }
#endregion
        #region ApiMethods
        public string GetLoadS3StorageLocationQuery()
        {
            var S3StorageLocationFields = FieldSpec<PageType<S3StorageLocationGraphQLModel>>
                .Create()
                .SelectList(it => it.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.Bucket)
                    .Field(e => e.Directory)

                    )
                .Build();

            var S3StorageLocationParameters = new GraphQLQueryParameter("filters", "S3StorageLocationFilters");

            var S3StorageLocationFragment = new GraphQLQueryFragment("s3StorageLocationsPage", [S3StorageLocationParameters], S3StorageLocationFields, "PageResponse");

            var builder = new GraphQLQueryBuilder([S3StorageLocationFragment]);

            return builder.GetQuery();
        }
        public async Task LoadS3StorageLocationAsync()
        {
            try
            {

                IsBusy = true;

                dynamic variables = new ExpandoObject();
                variables.pageResponseFilters = new ExpandoObject();
                variables.pageResponseFilters.matching = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                string query = GetLoadS3StorageLocationQuery();

                PageType<S3StorageLocationGraphQLModel> result = await _s3StorageLocationGraphQLModel.GetPageAsync(query, variables);
                this.S3StorageLocations = [.. result.Entries];
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
        public string GetCanDeleteS3StorageLocationQuery()
        {
            var fields = FieldSpec<CanDeleteType>
                .Create()
                .Field(f => f.CanDelete)
                .Field(f => f.Message)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("canDeleteS3StorageLocation", [parameter], fields, alias: "CanDeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery();
        }

        public async Task DeleteS3StorageLocation()
        {
            try
            {

                this.IsBusy = true;
                this.Refresh();

                string query = GetCanDeleteS3StorageLocationQuery();

                object variables = new { canDeleteResponseId = SelectedItem.Id };

                var validation = await _s3StorageLocationGraphQLModel.CanDeleteAsync(query, variables);

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
                DeleteResponseType deletedS3StorageLocation = await Task.Run(() => this.ExecuteDeleteS3StorageLocationAsync(SelectedItem.Id));

                if (!deletedS3StorageLocation.Success)
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"No pudo ser eliminado el registro \n\n {deletedS3StorageLocation.Message} \n\n Verifica la información e intenta más tarde.");
                    return;
                }

                await Context.EventAggregator.PublishOnUIThreadAsync(new S3StorageLocationDeleteMessage { DeletedS3StorageLocation = deletedS3StorageLocation });

                NotifyOfPropertyChange(nameof(CanDeleteS3StorageLocation));
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

        public string GetDeleteS3StorageLocationQuery()
        {
            var fields = FieldSpec<DeleteResponseType>
                .Create()
                .Field(f => f.DeletedId)
                .Field(f => f.Message)
                .Field(f => f.Success)
                .Build();

            var parameter = new GraphQLQueryParameter("id", "ID!");

            var fragment = new GraphQLQueryFragment("deleteS3StorageLocation", [parameter], fields, alias: "DeleteResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        public async Task<DeleteResponseType> ExecuteDeleteS3StorageLocationAsync(int id)
        {
            try
            {

                string query = GetDeleteS3StorageLocationQuery();

                object variables = new
                {
                    deleteResponseId = id
                };

                // Eliminar registros
                DeleteResponseType deletedRecord = await _s3StorageLocationGraphQLModel.DeleteAsync<DeleteResponseType>(query, variables);
                return deletedRecord;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task HandleAsync(S3StorageLocationDeleteMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _notificationService.ShowSuccess(message.DeletedS3StorageLocation.Message);
                await LoadS3StorageLocationAsync();
               
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

        public async Task HandleAsync(S3StorageLocationUpdateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadS3StorageLocationAsync();
                _notificationService.ShowSuccess(message.UpdatedS3StorageLocation.Message);
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

        public async Task HandleAsync(S3StorageLocationCreateMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await LoadS3StorageLocationAsync();
                _notificationService.ShowSuccess(message.CreatedS3StorageLocation.Message);
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
