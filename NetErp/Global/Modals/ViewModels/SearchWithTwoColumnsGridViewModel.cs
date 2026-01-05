using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Xpf;
using DevExpress.Xpf.Controls;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using NetErp.Helpers;
using NetErp.Inventory.CatalogItems.DTO;
using Ninject.Activation;
using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.Modals.ViewModels
{
    public class SearchWithTwoColumnsGridViewModel<XModel> : Screen
    {
        public readonly IRepository<XModel> DynamicService = IoC.Get<IRepository<XModel>>();

        private readonly Helpers.IDialogService _dialogService;

        public string Query { get; set; } = string.Empty;

        public SearchWithTwoColumnsGridMessageToken? MessageToken { get; set; }
        public dynamic? Variables { get; set; }

        private string _fieldHeader1;
        public string FieldHeader1
        {
            get { return _fieldHeader1; }
            set
            {
                if(_fieldHeader1 != value)
                {
                    _fieldHeader1 = value;
                    NotifyOfPropertyChange(nameof(FieldHeader1));
                }
            }
        }

        private string _fieldHeader2;
        public string FieldHeader2
        {
            get { return _fieldHeader2; }
            set
            {
                if (_fieldHeader2 != value)
                {
                    _fieldHeader2 = value;
                    NotifyOfPropertyChange(nameof(FieldHeader2));
                }
            }
        }

        private string _fieldData1;
        public string FieldData1
        {
            get { return _fieldData1; }
            set
            {
                if (_fieldData1 != value)
                {
                    _fieldData1 = value;
                    NotifyOfPropertyChange(nameof(FieldData1));
                }
            }
        }

        private string _fieldData2;
        public string FieldData2
        {
            get { return _fieldData2; }
            set
            {
                if (_fieldData2 != value)
                {
                    _fieldData2 = value;
                    NotifyOfPropertyChange(nameof(FieldData2));
                }
            }
        }

        private XModel? _selectedItem;
        public XModel? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                NotifyOfPropertyChange(nameof(SelectedItem));
            }
        }

        private ObservableCollection<XModel> _itemsSource;
        public ObservableCollection<XModel> ItemsSource
        {
            get { return _itemsSource; }
            set
            {
                if (_itemsSource != value)
                {
                    _itemsSource = value;
                    NotifyOfPropertyChange(nameof(ItemsSource));
                }
            }
        }

        private string _filterSearch = string.Empty;
        public string FilterSearch
        {
            get { return _filterSearch; }
            set
            {
                if (_filterSearch != value)
                {
                    _filterSearch = value;
                    NotifyOfPropertyChange(nameof(FilterSearch));
                    OnFilterSearhChange();
                }
            }
        }

        private bool _filterSearchFocus;
        public bool FilterSearchFocus
        {
            get { return _filterSearchFocus; }
            set
            {
                _filterSearchFocus = value;
                NotifyOfPropertyChange(nameof(FilterSearchFocus));
            }
        }

        private bool _gridFocus;
        public bool GridFocus
        {
            get { return _gridFocus; }
            set
            {
                if (_gridFocus != value)
                {
                    _gridFocus = value;
                    NotifyOfPropertyChange(nameof(GridFocus));
                }
            }
        }

        private int _pageNumber = 1;
        public int PageNumber
        {
            get { return _pageNumber; }
            set
            {
                if (_pageNumber != value)
                {
                    _pageNumber = value;
                    NotifyOfPropertyChange(nameof(PageNumber));
                }
            }
        }

        private int _pageSize = 100;
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

        private int _totalEntries;
        public int TotalEntries
        {
            get { return _totalEntries; }
            set
            {
                if (_totalEntries != value)
                {
                    _totalEntries = value;
                    NotifyOfPropertyChange(nameof(TotalEntries));
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

        public void OnFilterSearhChange()
        {
            if (FilterSearch.Length > 2 || FilterSearch.Length == 0)
            {
                _ = Task.Run(() => LoadItemsSourceAsync());
            }
        }

        void SetFocus(Expression<Func<object>> propertyExpression)
        {
            string controlName = propertyExpression.GetMemberInfo().Name;
            FilterSearchFocus = false;
            GridFocus = false;

            FilterSearchFocus = controlName == nameof(FilterSearch);
            GridFocus = controlName == nameof(GridFocus);
        }

        private ICommand _paginationCommand;
        public ICommand PaginationCommand
        {
            get
            {
                if (_paginationCommand == null) _paginationCommand = new AsyncCommand(ExecuteChangeIndex, CanExecuteChangeIndex);
                return _paginationCommand;
            }
        }

        private async Task ExecuteChangeIndex()
        {
            await LoadItemsSourceAsync();
        }

        private bool CanExecuteChangeIndex()
        {
            return true;
        }

        public async Task LoadItemsSourceAsync()
        {
            try
            {
                IsBusy = true;
                if (Variables is null)
                {
                    Variables = new ExpandoObject();
                    Variables.pageResponseFilters = new ExpandoObject();
                }

                Variables.pageResponseFilters.matching = FilterSearch.Trim().RemoveExtraSpaces();

                //Pagination
                Variables.pageResponsePagination = new ExpandoObject();
                Variables.pageResponsePagination.Page = PageNumber;
                Variables.pageResponsePagination.PageSize = PageSize;
                PageType<XModel> result = await DynamicService.GetPageAsync(Query, Variables);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ItemsSource = new ObservableCollection<XModel>(result.Entries);
                    this.SetFocus(() => FilterSearch);
                });

            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                Common.Helpers.GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<Common.Helpers.GraphQLError>(exGraphQL.Content is null ? "" : exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                if (graphQLError != null && currentMethod != null)
                {
                    await App.Current.Dispatcher.InvokeAsync(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod.Name.Between("<", ">"))} \r\n{graphQLError.Errors[0].Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                await App.Current.Dispatcher.InvokeAsync(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadItemsSourceAsync" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private ICommand _filterSearchEnterCommand;
        public ICommand FilterSearchEnterCommand
        {
            get
            {
                if (_filterSearchEnterCommand == null) _filterSearchEnterCommand = new DelegateCommand(FilterSearchEnter);
                return _filterSearchEnterCommand;
            }
        }

        public void FilterSearchEnter()
        {
            FilterSearchFocus = false;
            SelectedItem = ItemsSource.Count > 0 ? ItemsSource[0] : default;
            this.SetFocus(() => GridFocus);
        }

        private ICommand _rowDoubleClickCommand;

        public ICommand RowDoubleClickCommand
        {
            get
            {
                if (_rowDoubleClickCommand == null) _rowDoubleClickCommand = new AsyncCommand(RowDoubleClickAsync);
                return _rowDoubleClickCommand;
            }
        }

        public async Task RowDoubleClickAsync()
        {
            Messenger.Default.Send(message: new ReturnedDataFromModalWithTwoColumnsGridViewMessage<XModel>() { ReturnedData = SelectedItem }, token: MessageToken);
            await _dialogService.CloseDialogAsync(this, true);
        }


        private ICommand _enterKeyCommand;

        public ICommand EnterKeyCommand
        {
            get 
            {
                if (_enterKeyCommand == null) _enterKeyCommand = new AsyncCommand(EnterKeyAsync);
                return _enterKeyCommand; 
            }
        }

        public async Task EnterKeyAsync()
        {
            Messenger.Default.Send(message: new ReturnedDataFromModalWithTwoColumnsGridViewMessage<XModel>() { ReturnedData = SelectedItem }, token: MessageToken);
            await _dialogService.CloseDialogAsync(this, true);
        }

        private ICommand _gridEscKeyCommand;

        public ICommand GridEscKeyCommand
        {
            get 
            {
                if (_gridEscKeyCommand == null) _gridEscKeyCommand = new DelegateCommand(GridEscKey);
                return _gridEscKeyCommand; 
            }
        }

        public void GridEscKey()
        {
            SelectedItem = default;
            this.SetFocus(() => FilterSearch);
        }

        public SearchWithTwoColumnsGridViewModel(string query, string fieldHeader1, string fieldHeader2, string fieldData1, string fieldData2, dynamic? variables, SearchWithTwoColumnsGridMessageToken? messageToken, Helpers.IDialogService dialogService)
        {
            _dialogService = dialogService;
            Query = query;
            _fieldHeader1 = fieldHeader1;
            _fieldHeader2 = fieldHeader2;
            _fieldData1 = fieldData1;
            _fieldData2 = fieldData2;
            Variables = variables;
            SelectedItem = default;
            MessageToken = messageToken;
            _itemsSource = new ObservableCollection<XModel>();
            _ = Task.Run(() => LoadItemsSourceAsync());
        }
    }

    public enum SearchWithTwoColumnsGridMessageToken { CompanyAccountingEntity, BankAccountingEntity }

    public class ReturnedDataFromModalWithTwoColumnsGridViewMessage<XModel>
    {
        public XModel? ReturnedData { get; set; }
    }
}
