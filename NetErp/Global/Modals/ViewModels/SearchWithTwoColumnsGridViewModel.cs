using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Xpf;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.CatalogItems.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Global.Modals.ViewModels
{
    public class SearchWithTwoColumnsGridViewModel<XModel>: ViewModelBase
    {
        public readonly IGenericDataAccess<XModel> DynamicService = IoC.Get<IGenericDataAccess<XModel>>();

        public Window DialogWindow { get; set; } = new();

        public string Query { get; set; } = string.Empty;

        public SearchWithTwoColumnsGridMessageToken? MessageToken { get; set; }
        public dynamic? Variables { get; set; }

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

        private XModel? _selectedItem;
        public XModel? SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                SetValue(ref _selectedItem, value);
            }
        }

        private ObservableCollection<XModel> _itemsSource;
        public ObservableCollection<XModel> ItemsSource
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
                    Variables.filter = new ExpandoObject();
                }
                //TODO
                Variables.filter.or = new ExpandoObject[]
                {
                    new(),
                    new()
                };

                Variables.filter.or[0].searchName = new ExpandoObject();
                Variables.filter.or[0].searchName.@operator = "like";
                Variables.filter.or[0].searchName.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                Variables.filter.or[1].identificationNumber = new ExpandoObject();
                Variables.filter.or[1].identificationNumber.@operator = "like";
                Variables.filter.or[1].identificationNumber.value = string.IsNullOrEmpty(FilterSearch) ? "" : FilterSearch.Trim().RemoveExtraSpaces();

                //Pagination
                Variables.filter.Pagination = new ExpandoObject();
                Variables.filter.Pagination.Page = PageIndex;
                Variables.filter.Pagination.PageSize = PageSize;
                var result = await DynamicService.GetPage(Query, Variables);

                TotalCount = result.PageResponse.Count;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ItemsSource = new ObservableCollection<XModel>(result.PageResponse.Rows);
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
            Messenger.Default.Send(message: new ReturnedDataFromModalWithTwoColumnsGridViewMessage<XModel>() { ReturnedData = SelectedItem }, token: MessageToken);
            DialogWindow.Close();
        }

        [Command]
        public void EnterKey()
        {
            Messenger.Default.Send(message: new ReturnedDataFromModalWithTwoColumnsGridViewMessage<XModel>() { ReturnedData = SelectedItem }, token: MessageToken);
            DialogWindow.Close();
        }

        [Command]
        public void GridEscKey()
        {
            SelectedItem = default;
            this.SetFocus(() => FilterSearch);
        }

        public SearchWithTwoColumnsGridViewModel(string query, string fieldHeader1, string fieldHeader2, string fieldData1, string fieldData2, dynamic? variables, SearchWithTwoColumnsGridMessageToken? messageToken)
        {
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
