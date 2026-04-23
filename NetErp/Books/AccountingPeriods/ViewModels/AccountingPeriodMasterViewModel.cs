using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Dictionaries;
using Microsoft.VisualStudio.Threading;
using Models.Billing;
using Models.Books;
using Models.Global;
using NetErp.Billing.Zones.ViewModels;
using NetErp.Books.AccountingPeriods.DTO;
using NetErp.Global.CostCenters.DTO;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using NetErp.Helpers.Services;
using System;
using Extensions.Global;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Dictionaries.BooksDictionaries;
using static Models.Global.GraphQLResponseTypes;
using static Stimulsoft.Report.WpfDesign.WCFService.StiDatabaseBuildHelper;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingPeriods.ViewModels
{
    public class AccountingPeriodMasterViewModel : Screen,
        IHandle<SelectedCostCentersMessage>
    {

        private readonly CostCenterCache _costCenterCache;
        private readonly Helpers.Services.INotificationService _notificationService;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly IRepository<AccountingPeriodGraphQLModel> _accountingPeriodService;
        private readonly IRepository<AccountingEntryPeriodGraphQLModel> _accountingEntryPeriodService;
        private readonly Helpers.IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;

        public AccountingPeriodMasterViewModel(
            CostCenterCache costCenterCache,
            Helpers.Services.INotificationService notificationService,
            JoinableTaskFactory joinableTaskFactory,
            IRepository<AccountingPeriodGraphQLModel> accountingPeriodService,
            IRepository<AccountingEntryPeriodGraphQLModel> accountingEntryPeriodService,
            Helpers.IDialogService dialogService,
            IEventAggregator eventAggregator
            )
        {
            _costCenterCache = costCenterCache;
            _notificationService = notificationService;
            _joinableTaskFactory = joinableTaskFactory;
            _accountingPeriodService = accountingPeriodService;
            _accountingEntryPeriodService = accountingEntryPeriodService;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            
            _eventAggregator.SubscribeOnUIThread(this);
            
           
        }
   
        private int _totalCount = 0;
        public int TotalCount
        {
            get { return _totalCount; }
            set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    NotifyOfPropertyChange(() => TotalCount);
                }
            }
        }
        private bool _isBusy;
        public bool CanSave => !IsBusy && this.HasChanges();
        
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        public ObservableCollection<MonthItemDto> Months
        {
            get => field;
            set
            {
                if (field != value)
                {
                    if (field != null)
                    {
                        field.CollectionChanged -= Items_CollectionChanged;

                        // 🔴 Desuscribirse de los items viejos
                        foreach (var item in field)
                            item.PropertyChanged -= Item_PropertyChanged;
                    }

                    field = value;

                    if (field != null)
                    {
                        field.CollectionChanged += Items_CollectionChanged;

                        // 🔥 AQUÍ ESTÁ LA CLAVE
                        foreach (var item in field)
                            item.PropertyChanged += Item_PropertyChanged;
                    }

                    NotifyOfPropertyChange(nameof(Months));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }
        protected override async void OnViewReady(object view)
        {
            base.OnViewReady(view);

            try
            {
               
                await _costCenterCache.EnsureLoadedAsync();

                CostCenters = [.. _costCenterCache.Items];

            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al inicializar el módulo.\r\n{GetType().Name}.{nameof(OnViewReady)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
                await TryCloseAsync();
                return;
            }
        }


        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                NotifyOfPropertyChange(() => SelectedYear);
                _ = LoadAccountingPeriodsAsync();
            }
        }
        public int? SelectedCostCenterId
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedCostCenterId));
                    _ = LoadAccountingPeriodsAsync();
                }
            }
        }
        private ICommand? _chooseCentersCommand;
        public ICommand ChooseCentersCommand
        {
            get
            {
                _chooseCentersCommand ??= new AsyncCommand(ChooseCentersAsync);
                return _chooseCentersCommand;
            }
        }

        private ICommand? _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }
        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
     
                await ExecuteSaveAsync(GetBatchPeriodItems([SelectedCostCenterId.Value]));

                
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(SaveAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task<BatchResultGraphQLModel> ExecuteSaveAsync(ObservableCollection<BatchPeriodItemDto> items)
        {
            try
            {
                var query = _batchUpsertAccountingPeriodMutation.Value;
                dynamic variables = new ExpandoObject();
                dynamic input = new ExpandoObject();
                input.Items = items;
                variables.singleItemResponseInput = input;
                BatchResultGraphQLModel result = await _accountingPeriodService.BatchAsync<BatchResultGraphQLModel>(query, variables);
                if (!result.Success)
                {
                    ThemedMessageBox.Show(
                        text: $"El guardado no ha sido exitoso\r\n\r\n{result.Errors.ToUserMessage()}\r\n\r\nVerifique los datos y vuelva a intentarlo",
                        title: $"{result.Message}!",
                        messageBoxButtons: MessageBoxButton.OK,
                        icon: MessageBoxImage.Error);
                    return null;
                }
                ThemedMessageBox.Show(
                       text: $"Configuración aplicada",
                       title: $"{result.Message}!",
                       messageBoxButtons: MessageBoxButton.OK,
                       icon: MessageBoxImage.Information);
                return result;
            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        public async Task ChooseCentersAsync()
        {
            try
            {
                IsBusy = true;
                var detail = new AccountingPeriodCostCenterChoiceViewModel(_costCenterCache, _eventAggregator, SelectedCostCenterId.Value);
               
               

                if (this.GetView() is System.Windows.FrameworkElement parentView)
                {
                    detail.DialogWidth = parentView.ActualWidth * 0.40;
                    detail.DialogHeight = parentView.ActualHeight * 0.30;
                }

                await _dialogService.ShowDialogAsync(detail, "Seleccione Centros de Costo");
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(ChooseCentersAsync)} \r\n{ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
       
        public ObservableCollection<BatchPeriodItemDto> GetBatchPeriodItems(List<int> costCenterIds)
        {
             ObservableCollection<BatchPeriodItemDto> data = [];

            foreach (int costCenterId in costCenterIds) {
                var items = Months.Select(m => new BatchPeriodItemDto
                {
                    Month = m.Id,
                    Year = SelectedYear,
                    CostCenterId = costCenterId,
                    Status = m.Status
                }).ToList();
                foreach (var item in items)
                {
                    data.Add(item);
                }
            }
            
            return data;
        }
    public ObservableCollection<CostCenterGraphQLModel> CostCenters
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(CostCenters));
                }
            }
        } = [];
        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (MonthItemDto item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (MonthItemDto item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }
            this.TrackChange(nameof(Months));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var item = sender as MonthItemDto;
            this.TrackChange(nameof(Months));
            NotifyOfPropertyChange(nameof(CanSave));
            // Aquí haces tracking del cambio
            Console.WriteLine($"Propiedad {e.PropertyName} cambió en item {item}");
        }
        public async Task LoadAccountingPeriodsAsync()
        {
            Months = [];
            if (SelectedYear == null || SelectedCostCenterId == null)
            {
                this.AcceptChanges();
                return;
                
            }
             
            try
            {
                IsBusy = true;

                Stopwatch stopwatch = Stopwatch.StartNew();

                var (fragment, query) = _loadAccountingEntryPeriodQuery.Value;

                dynamic filters = new ExpandoObject();
                filters.year = SelectedYear;
                filters.costCenterId = SelectedCostCenterId   ;

                var variables = new GraphQLVariables()
                    .For(fragment, "pagination", new { Page = 1, PageSize = 12 })
                    .For(fragment, "filters", filters)
                    .Build();

                PageType<AccountingEntryPeriodGraphQLModel> result = await _accountingEntryPeriodService.GetPageAsync(query, variables);

                TotalCount = result.TotalEntries;
                var monthsPeriod = new ObservableCollection<AccountingEntryPeriodGraphQLModel>(result.Entries);

                //TODO evaluar comportamiento
                var baseMonths = GetMonthItemDtos();
                ObservableCollection<MonthItemDto> months = new ObservableCollection<MonthItemDto>();
                foreach (var month in monthsPeriod)

                    {
                    var m = baseMonths.FirstOrDefault(m => m.Id == month.Month);
                    if (m != null)
                    {
                        m.Status = month.AccountingPeriod?.Status ==  BooksDictionaries.AccountingPeriodStatusDictionary["CLOSED"]? BooksDictionaries.AccountingPeriodStatusDictionary["CLOSED"] : BooksDictionaries.AccountingPeriodStatusDictionary["OPEN"];
                    }
                    months.Add(m);

                }
                Months = new ObservableCollection<MonthItemDto>(months);
               this.AcceptChanges();

                stopwatch.Stop();

            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show("Atención!",
                    $"{GetType().Name}.{nameof(LoadAccountingPeriodsAsync)}: {ex.Message}",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
        public BatchOperationInfo GetBatchInfo()
        {


            return new BatchOperationInfo
            {


                BatchQuery = _batchUpsertAccountingPeriodMutation.Value,

                ExtractBatchItem = (variables) =>
                {
                    return variables.GetType().GetProperty("item")!.GetValue(variables)!;
                },

                BuildBatchVariables = (batchItems) =>
                {
                    return new
                    {
                        singleItemResponseInput = new
                        {
                            items = batchItems
                        }
                    };
                },

                ExecuteBatchAsync = async (query, variables, cancellationToken) =>
                {
                    return await _accountingPeriodService.BatchAsync<BatchResultGraphQLModel>(query, variables, cancellationToken);
                }
            };
        }
        private static readonly Lazy<string> _batchUpsertAccountingPeriodMutation = new(() =>
        {
            var fields = FieldSpec<BatchResultGraphQLModel>
                .Create()
                .Field(f => f.Success)
               .Field(f => f.Message)
               .Field(f => f.TotalAffected)
               .Field(f => f.AffectedIds)
               .SelectList(f => f.Errors, sq => sq
                   .Field(e => e.Message))
               .Build();


            var parameters = new List<GraphQLQueryParameter>
            {
                new("input", "BatchUpsertAccountingPeriodsInput!")
            };
            var fragment = new GraphQLQueryFragment("batchUpsertAccountingPeriods", parameters, fields, "SingleItemResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);




        });
       
        private ObservableCollection<MonthItemDto> GetMonthItemDtos()
        {
            return new ObservableCollection<MonthItemDto>
    {
        new MonthItemDto { Id = 1, Name = "Enero", Status = BooksDictionaries.AccountingPeriodStatusDictionary["OPEN"]  },
        new MonthItemDto { Id = 2, Name = "Febrero", Status =BooksDictionaries.AccountingPeriodStatusDictionary["OPEN"]},
        new MonthItemDto { Id = 3, Name = "Marzo", Status = BooksDictionaries.AccountingPeriodStatusDictionary["OPEN"] },
        new MonthItemDto { Id = 4, Name = "Abril", Status = BooksDictionaries.AccountingPeriodStatusDictionary["OPEN"] },
        new MonthItemDto { Id = 5, Name = "Mayo", Status = BooksDictionaries.AccountingPeriodStatusDictionary["OPEN"] },
        new MonthItemDto { Id = 6, Name = "Junio", Status = BooksDictionaries.AccountingPeriodStatusDictionary["OPEN"] },
        new MonthItemDto { Id = 7, Name = "Julio", Status = BooksDictionaries.AccountingPeriodStatusDictionary["OPEN"] },
        new MonthItemDto { Id = 8, Name = "Agosto", Status = BooksDictionaries.AccountingPeriodStatusDictionary["OPEN"] },
        new MonthItemDto { Id = 9, Name = "Septiembre", Status = BooksDictionaries.AccountingPeriodStatusDictionary["OPEN"] },
        new MonthItemDto { Id = 10, Name = "Octubre", Status = BooksDictionaries.AccountingPeriodStatusDictionary["OPEN"] },
        new MonthItemDto { Id = 11, Name = "Noviembre", Status = BooksDictionaries.AccountingPeriodStatusDictionary["OPEN"] },
        new MonthItemDto { Id = 12, Name = "Diciembre", Status = BooksDictionaries.AccountingPeriodStatusDictionary["OPEN"] }
    };
        }

        public async Task HandleAsync(SelectedCostCentersMessage message, CancellationToken cancellationToken)
        {
           
            try
            {
                var selectedIds = message.SelectedCostCenters.Select(s => s.Id).ToList();

               await  ExecuteSaveAsync(GetBatchPeriodItems(selectedIds));


            }
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }
        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadAccountingEntryPeriodQuery = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingEntryPeriodGraphQLModel>>
                .Create()
                .Field(f => f.TotalEntries)
                .SelectList(f => f.Entries, entries => entries
                    .Field(e => e.Id)
                    .Field(e => e.Month)
                    
                    .Field(en => en.Year)
                    .Select(selector: e => e.CostCenter, nested: entity => entity
                        .Field(en => en.Id)
                        .Field(en => en.Name)
                       
                    )
                      .Select(selector: e => e.AccountingPeriod, nested: entity => entity
                        .Field(en => en.Id)
                        .Field(en => en.Status)

                    )
                    )
                .Build();

            var fragment = new GraphQLQueryFragment("accountingEntryPeriodsPage",
                [new("filters", "AccountingEntryPeriodFilters"), new("pagination", "Pagination")],
                fields, "PageResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion
    }

    
   
    
   
}
