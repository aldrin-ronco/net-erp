using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Xpf.Core;
using Models.Global;
using Models.Treasury;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DevExpress.Drawing.Printing.Internal.DXPageSizeInfo;

namespace NetErp.Treasury.Concept.ViewModels
{
    public class ConceptMasterViewModel: Screen
    {
        public IGenericDataAccess<ConceptGraphQLModel> ConceptService { get; set; } = IoC.Get<IGenericDataAccess<ConceptGraphQLModel>>();
        public ConceptViewModel Context { get; set; }
        public ConceptMasterViewModel(ConceptViewModel context)
        {
            Context = context;
            Task.Run(() => LoadConceptsAsync());
        }

        private ObservableCollection<ConceptGraphQLModel> _concepts;

        public ObservableCollection<ConceptGraphQLModel> Concepts
        {
            get
            {
                return _concepts;
            }
            set
            {
                if (_concepts != value)
                {
                    _concepts = value;
                    NotifyOfPropertyChange(nameof(Concepts));
                }
            }
        }

        //private string _selectedConceptType = "Todos";

        //public string SelectedConceptType
        //{
        //    get { return _selectedConceptType;}
        //    set
        //    {
        //        if (_selectedConceptType != value)
        //        {
        //            _selectedConceptType = value;
        //            NotifyOfPropertyChange(nameof(SelectedConceptType));
        //            _ = Task.Run(() => LoadConceptsAsync());
        //        }
        //    }
        //}

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

        public async Task LoadConceptsAsync()
        {
            try
            {
                //IsBusy = true; 

                string query = @"
               query($filter: ConceptFilterInput!){
                    PageResponse: conceptPage(filter: $filter){
                    count
                    rows{
                        id
                        name
                        type
                        margin
                        allowMargin
                        marginBasis
                        accountingAccountId
                    }
                    }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();
                //variables.filter.type = new ExpandoObject();
                //variables.filter.type.@operator = "=";
                //variables.filter.type.value = "I";

                variables.filter.pagination = new ExpandoObject();
                variables.filter.pagination.page = PageIndex;
                variables.filter.pagination.pageSize = PageSize;

                var result = await ConceptService.GetPage(query, variables);                
                Concepts = new ObservableCollection<ConceptGraphQLModel>(result.PageResponse.Rows ?? new List<ConceptGraphQLModel>());

            }
            catch (Exception ex)
            {
                await Execute.OnUIThreadAsync(() =>
                {
                    ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{GetCurrentMethodName.Get()} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                //IsBusy = false; 
            }
        }


    }
}
