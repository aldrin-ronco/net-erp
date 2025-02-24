using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Billing;
using Models.Global;
using NetErp.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Global.Smtp.ViewModels
{
    public class SmtpMasterViewModel : Screen,
        IHandle<SmtpDeleteMessage>,
        IHandle<SmtpUpdateMessage>,
        IHandle<SmtpCreateMessage>
    {
        public IGenericDataAccess<SmtpGraphQLModel> SmtpService { get; set; } = IoC.Get<IGenericDataAccess<SmtpGraphQLModel>>();
        public SmtpViewModel Context { get; set; }

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


        private ObservableCollection<SmtpGraphQLModel> _smtps;

        public ObservableCollection<SmtpGraphQLModel> Smtps
        {
            get { return _smtps; }
            set 
            {
                if (_smtps != value) 
                {
                    _smtps = value;
                    NotifyOfPropertyChange(nameof(Smtps));
                }
            }
        }

        private SmtpGraphQLModel? _selectedItem = null;

        public SmtpGraphQLModel? SelectedItem
        {
            get { return _selectedItem; }
            set 
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyOfPropertyChange(nameof(SelectedItem));
                    NotifyOfPropertyChange(nameof(CanDeleteSmtp));
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
                    if(string.IsNullOrEmpty(value) || value.Length >= 3) _ = Task.Run(() => LoadSmtpsAsync());
                }
            }
        }

        public bool CanDeleteSmtp
        {
            get 
            { 
                if(SelectedItem is null) return false;
                return true;
            }
        }

        /// <summary>
        /// PageIndex
        /// </summary>
        private int _pageIndex = 1; // DevExpress first page is index zero
        public int PageIndex
        {
            get { return _pageIndex; }
            set
            {
                if (_pageIndex != value)
                {
                    _pageIndex = value;
                    NotifyOfPropertyChange(nameof(PageIndex));
                }
            }
        }

        /// <summary>
        /// PageSize
        /// </summary>
        private int _pageSize = 50; // Default PageSize 50
        public int PageSize
        {
            get { return _pageSize; }
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    NotifyOfPropertyChange(nameof(PageSize));
                }
            }

        }


        /// <summary>
        /// TotalCount
        /// </summary>
        private int _totalCount = 0;
        public int TotalCount
        {
            get { return _totalCount; }
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    NotifyOfPropertyChange(nameof(TotalCount));
                }
            }
        }

        // Tiempo de respuesta
        private string _responseTime;
        public string ResponseTime
        {
            get { return _responseTime; }
            set
            {
                if (_responseTime != value)
                {
                    _responseTime = value;
                    NotifyOfPropertyChange(nameof(ResponseTime));
                }
            }
        }

        /// <summary>
        /// PaginationCommand para controlar evento
        /// </summary>
        private ICommand _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) this._paginationCommand = new AsyncCommand(ExecuteChangeIndex, CanExecuteChangeIndex);
                return _paginationCommand;
            }
        }

        private async Task ExecuteChangeIndex()
        {
            await LoadSmtpsAsync();
        }
        private bool CanExecuteChangeIndex()
        {
            return true;
        }

        private ICommand _deleteSmtpCommand;

        public ICommand DeleteSmtpCommand
        {
            get
            {
                if (_deleteSmtpCommand is null) _deleteSmtpCommand = new AsyncCommand(DeleteSmtp);
                return _deleteSmtpCommand;
            }
        }

        public async Task DeleteSmtp()
        {
            try
            {
                IsBusy = true;
                int id = SelectedItem.Id;

                string query = @"query($id:Int!){
                  CanDeleteModel: canDeleteSmtp(id: $id){
                    canDelete
                    message
                  }
                }";

                object variables = new { Id = id };

                var validation = await this.SmtpService.CanDelete(query, variables);

                if (validation.CanDelete)
                {
                    IsBusy = false;
                    MessageBoxResult result = ThemedMessageBox.Show(title: "Confirme...", text: $"¿Confirma que desea eliminar el registro {SelectedItem.Name}?", messageBoxButtons: MessageBoxButton.YesNo, image: MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes) return;
                }
                else
                {
                    IsBusy = false;
                    Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: "El registro no puede ser eliminado" +
                    (char)13 + (char)13 + validation.Message, messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                    return;
                }
                this.IsBusy = true;

                Refresh();

                SmtpGraphQLModel deletedSmtp = await ExecuteDeleteSmtpAsync(id);

                await Context.EventAggregator.PublishOnUIThreadAsync(new SmtpDeleteMessage() { DeletedSmtp = deletedSmtp});

                NotifyOfPropertyChange(nameof(CanDeleteSmtp));
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<SmtpGraphQLModel> ExecuteDeleteSmtpAsync(int id)
        {
            try
            {
                string query = @"mutation($id:Int!){
                  DeleteResponse: deleteSmtp(id: $id){
                    id
                    name
                    host
                    port
                  }
                }";
                dynamic variables = new ExpandoObject();
                variables.id = id;
                var result = await SmtpService.Delete(query, variables);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public SmtpMasterViewModel(SmtpViewModel context)
        {
            Context = context;
            Context.EventAggregator.SubscribeOnPublishedThread(this);
            
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            _ = Task.Run(() => LoadSmtpsAsync());
            this.SetFocus(() => FilterSearch);
        }

        public async Task EditSmtp()
        {
            await Context.ActivateDetailViewForEdit(SelectedItem ?? new());
        }

        public async Task LoadSmtpsAsync()
        {
            try
            {
                IsBusy = true;
                string query;
                query = @"
                query($filter: SmtpFilterInput!){
                  PageResponse: smtpPage(filter: $filter){
                    count
                    rows{
                      id
                      name
                      host
                      port
                    }
                  }
                }";
                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                variables.filter.name = new ExpandoObject();
                variables.filter.name.@operator = "like";
                variables.filter.name.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                //Pagination
                variables.filter.pagination = new ExpandoObject();
                variables.filter.pagination.page = PageIndex;
                variables.filter.pagination.pageSize = PageSize;
                // Iniciar cronometro
                Stopwatch stopwatch = new();
                stopwatch.Start();
                var result = await SmtpService.GetPage(query, variables);
                TotalCount = result.PageResponse.Count;
                Smtps = new ObservableCollection<SmtpGraphQLModel>(result.PageResponse.Rows);
                // Detener cronometro
                stopwatch.Stop();
                ResponseTime = $"{stopwatch.Elapsed:hh\\:mm\\:ss\\.ff}";
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private ICommand _createSmtpCommand;

        public ICommand CreateSmtpCommand
        {
            get 
            {
                if (_createSmtpCommand is null) _createSmtpCommand = new AsyncCommand(CreateSmtpAsync);
                return _createSmtpCommand; 
            }

        }

        public async Task CreateSmtpAsync()
        {
            await Context.ActivateDetailViewForNew();
        }

        public Task HandleAsync(SmtpDeleteMessage message, CancellationToken cancellationToken)
        {
            SmtpGraphQLModel smtpToDelete = Smtps.FirstOrDefault(x => x.Id == message.DeletedSmtp.Id) ?? new SmtpGraphQLModel();
            Smtps.Remove(smtpToDelete);
            SelectedItem = null;
            return Task.CompletedTask;
        }

        public Task HandleAsync(SmtpUpdateMessage message, CancellationToken cancellationToken)
        {
            return LoadSmtpsAsync();
        }

        public Task HandleAsync(SmtpCreateMessage message, CancellationToken cancellationToken)
        {
            return LoadSmtpsAsync();
        }
    }
}
