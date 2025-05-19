using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Xpf;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Models.Inventory;
using NetErp.Books.AccountingAccounts.ViewModels;
using NetErp.Helpers;
using NetErp.Inventory.CatalogItems.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;

namespace NetErp.Inventory.CatalogItems.ViewModels
{
    public class SearchItemModalViewModel<TModel, XModel> : Screen
    {
        public readonly IGenericDataAccess<XModel> DynamicService = IoC.Get<IGenericDataAccess<XModel>>();


        private readonly Helpers.IDialogService _dialogService;

        public string Query { get; set; } = string.Empty;

        public MessageToken? MessageToken { get; set; }
        public dynamic Variables { get; set; }

        private string _fieldHeader1;
        public string FieldHeader1
        {
            get { return _fieldHeader1; }
            set
            {
                if (_fieldHeader1 != value)
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

        private string _fieldHeader3;
        public string FieldHeader3
        {
            get { return _fieldHeader3; }
            set
            {
                if (_fieldHeader3 != value)
                {
                    _fieldHeader3 = value;
                    NotifyOfPropertyChange(nameof(FieldHeader3));
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

        private string _fieldData3;
        public string FieldData3
        {
            get { return _fieldData3; }
            set
            {
                if (_fieldData3 != value)
                {
                    _fieldData3 = value;
                    NotifyOfPropertyChange(nameof(FieldData3));
                }
            }
        }

        private TModel? _selectedItem;
        public TModel? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                NotifyOfPropertyChange(nameof(SelectedItem));
            }
        }

        private CatalogViewModel _context;
        public CatalogViewModel Context
        {
            get { return _context; }
            set
            {
                if (_context != value)
                {
                    _context = value;
                    NotifyOfPropertyChange(nameof(Context));
                }
            }
        }

        private ObservableCollection<TModel> _itemsSource;
        public ObservableCollection<TModel> ItemsSource
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
                if (_filterSearchFocus != value)
                {
                    _filterSearchFocus = value;
                    NotifyOfPropertyChange(nameof(FilterSearchFocus));
                }
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

        private int _pageIndex = 1;
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

        private int _totalCount;
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
                _ = Task.Run(() => LoadItemsAsync());
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

        public async Task LoadItemsAsync()
        {
            try
            {
                IsBusy = true;
                Variables.filter.and[1].or = new ExpandoObject[]
                {
                    new(),
                    new()
                };

                Variables.filter.and[1].or[0].name = new ExpandoObject();
                Variables.filter.and[1].or[0].name.@operator = "like";
                Variables.filter.and[1].or[0].name.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                Variables.filter.and[1].or[1].reference = new ExpandoObject();
                Variables.filter.and[1].or[1].reference.@operator = "like";
                Variables.filter.and[1].or[1].reference.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                Variables.filter.Pagination = new ExpandoObject();
                Variables.filter.Pagination.Page = PageIndex;
                Variables.filter.Pagination.PageSize = PageSize;

                var result = await DynamicService.GetPage(Query, Variables);

                TotalCount = result.PageResponse.Count;
                ItemsSource = new ObservableCollection<TModel>(Context.AutoMapper.Map<ObservableCollection<TModel>>(result.PageResponse.Rows));
                this.SetFocus(() => FilterSearch);

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
                await App.Current.Dispatcher.InvokeAsync(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "LoadItemsAsync" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
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
                if (_rowDoubleClickCommand == null) _rowDoubleClickCommand = new AsyncCommand(RowDoubleClick);
                return _rowDoubleClickCommand;
            }
        }

        public async Task RowDoubleClick()
        {
            Messenger.Default.Send(message: new ReturnedItemFromModalViewMessage() { ReturnedItem = Context.AutoMapper.Map<ItemDTO>(SelectedItem) }, token: MessageToken);
            await _dialogService.CloseDialogAsync(this, true);
        }

        private ICommand _enterKeyCommand;

        public ICommand EnterKeyCommand
        {
            get
            {
                if (_enterKeyCommand == null) _enterKeyCommand = new AsyncCommand(EnterKey);
                return _enterKeyCommand;
            }
        }

        public async Task EnterKey()
        {
            Messenger.Default.Send(message: new ReturnedItemFromModalViewMessage() { ReturnedItem = Context.AutoMapper.Map<ItemDTO>(SelectedItem) }, token: MessageToken);
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

        public SearchItemModalViewModel(string query, string fieldHeader1, string fieldHeader2, string fieldHeader3, string fieldData1, string fieldData2, string fieldData3, dynamic variables, MessageToken? messageToken, CatalogViewModel context, Helpers.IDialogService dialogService)
        {
            _dialogService = dialogService;
            Query = query;
            FieldHeader1 = fieldHeader1;
            FieldHeader2 = fieldHeader2;
            FieldHeader3 = fieldHeader3;
            FieldData1 = fieldData1;
            FieldData2 = fieldData2;
            FieldData3 = fieldData3;
            Variables = variables;
            Context = context;
            SelectedItem = default;
            MessageToken = messageToken;
            ItemsSource = new ObservableCollection<TModel>();
            _ = Task.Run(() => LoadItemsAsync());
        }
    }

    public enum MessageToken { RelatedProduct, SearchProduct }
}
