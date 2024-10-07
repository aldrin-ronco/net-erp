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
using NetErp.Inventory.CatalogItems.DTO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;

namespace NetErp.Inventory.CatalogItems.ViewModels
{
    public class SearchItemModalViewModel<TModel, XModel> : ViewModelBase
    {
        public readonly IGenericDataAccess<XModel> DynamicService = IoC.Get<IGenericDataAccess<XModel>>();

       public Window DialogWindow { get; set; } = new();

        public string Query { get; set; } = string.Empty;

        public MessageToken? MessageToken { get; set; }
        public dynamic Variables { get; set; }

        private string _fieldHeader1;
        public string FieldHeader1
        {
            get { return _fieldHeader1; }
            set 
            { 
                SetValue(ref _fieldHeader1, value); 
            }
        }

        private string _fieldHeader2;
        public string FieldHeader2
        {
            get { return _fieldHeader2; }
            set 
            { 
                SetValue(ref _fieldHeader2, value); 
            }
        }

        private string _fieldHeader3;
        public string FieldHeader3
        {
            get { return _fieldHeader3; }
            set 
            { 
                SetValue(ref _fieldHeader3, value); 
            }
        }

        private string _fieldData1;
        public string FieldData1
        {
            get { return _fieldData1; }
            set 
            { 
                SetValue(ref _fieldData1, value); 
            }
        }

        private string _fieldData2;
        public string FieldData2
        {
            get { return _fieldData2; }
            set 
            { 
                SetValue(ref _fieldData2, value); 
            }
        }

        private string _fieldData3;
        public string FieldData3
        {
            get { return _fieldData3; }
            set 
            { 
                SetValue(ref _fieldData3, value); 
            }
        }

        private TModel? _selectedItem;
        public TModel? SelectedItem
        {
            get { return _selectedItem; }
            set 
            { 
                SetValue(ref _selectedItem, value); 
            }
        }

        private CatalogViewModel _context;
        public CatalogViewModel Context
        {
            get { return _context; }
            set 
            { 
                SetValue(ref _context, value);
            }
        }

        private ObservableCollection<TModel> _itemsSource;
        public ObservableCollection<TModel> ItemsSource
        {
            get { return _itemsSource; }
            set 
            { 
                SetValue(ref _itemsSource, value); 
            }
        }

        private string _filterSearch = string.Empty;
        public string FilterSearch
        {
            get { return _filterSearch; }
            set
            {
                SetValue(ref _filterSearch, value, changedCallback: OnFilterSearChange);
            }
        }

        private bool _filterSearchFocus;

        public bool FilterSearchFocus
        {
            get { return _filterSearchFocus; }
            set 
            {
                SetValue(ref _filterSearchFocus, value);
            }
        }

        private bool _gridFocus;

        public bool GridFocus
        {
            get { return _gridFocus; }
            set
            {
                SetValue(ref _gridFocus, value);
            }
        }

        private int _pageIndex = 1;
        public int PageIndex
        {
            get { return _pageIndex; }
            set 
            { 
                SetValue(ref _pageIndex, value);
            }
        }

        private int _pageSize = 100;
        public int PageSize
        {
            get { return _pageSize; }
            set 
            { 
                SetValue(ref _pageSize, value); 
            }
        }

        private int _totalCount;
        public int TotalCount
        {
            get { return _totalCount; }
            set 
            { 
                SetValue(ref _totalCount, value); 
            }
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get { return _isBusy; }
            set 
            {
                SetValue(ref _isBusy, value);
            }
        }

        public void OnFilterSearChange()
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
                Variables.filter.name = FilterSearch == "" ? "" : FilterSearch.Trim();
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
        [Command]
        public void FilterSearchEnter()
        {
            FilterSearchFocus = false;
            SelectedItem = ItemsSource.Count > 0 ? ItemsSource[0] : default;
            this.SetFocus(() => GridFocus);
        }

        [Command]
        public void RowDoubleClick(RowClickArgs args)
        {
            Messenger.Default.Send(message: new ReturnedItemFromModalViewMessage() { ReturnedItem = Context.AutoMapper.Map<ItemDTO>(SelectedItem) }, token: MessageToken);
            DialogWindow.Close();
        }

        [Command]
        public void EnterKey()
        {
            Messenger.Default.Send(message: new ReturnedItemFromModalViewMessage() { ReturnedItem = Context.AutoMapper.Map<ItemDTO>(SelectedItem) }, token: MessageToken);
            DialogWindow.Close();
        }

        [Command]
        public void GridEscKey()
        {
            SelectedItem = default;
            this.SetFocus(() => FilterSearch);
        }

        public SearchItemModalViewModel(string query, string fieldHeader1, string fieldHeader2, string fieldHeader3, string fieldData1, string fieldData2, string fieldData3, dynamic variables, MessageToken? messageToken,CatalogViewModel context)
        {
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
