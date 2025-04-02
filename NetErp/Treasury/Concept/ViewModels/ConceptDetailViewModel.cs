using Amazon.Util.Internal;
using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using Models.Global;
using Models.Treasury;
using Ninject.Activation;
using Services.Global.DAL.PostgreSQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xceed.Wpf.Toolkit.Primitives;
using static Models.Treasury.ConceptGraphQLModel;

namespace NetErp.Treasury.Concept.ViewModels
{
    public class ConceptDetailViewModel : Screen
    {
        public ConceptViewModel Context { get; set; }
        public ConceptDetailViewModel(ConceptViewModel context)
        {
            Context = context;
            var joinable = new JoinableTaskFactory(new JoinableTaskContext());
            joinable.Run(async () => await LoadCodeAccountingAccounts());
            _errors = new Dictionary<string, List<string>>();

        }
        Dictionary<string, List<string>> _errors;

        public IGenericDataAccess<ConceptGraphQLModel> ConceptService = IoC.Get<IGenericDataAccess<ConceptGraphQLModel>>();
        public IGenericDataAccess<AccountingAccountGraphQLModel> AccountingAccountService = IoC.Get<IGenericDataAccess<AccountingAccountGraphQLModel>>();

        public async Task GoBack()
        {
            await Context.ActivateMasterView();
        }

        private ICommand _goBackCommand;

        public ICommand GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new AsyncCommand(GoBack);
                return _goBackCommand;
            }
        }

        private string _nameConcept;

        public string NameConcept
        {
            get
            {
                return _nameConcept;
            }
            set
            {
                if (_nameConcept != value)
                {
                    {
                        _nameConcept = value;
                        NotifyOfPropertyChange(nameof(NameConcept));
                    }
                }
            }
        }

        private AccountingAccountGraphQLModel _selectedAccoutingAccount;
        public AccountingAccountGraphQLModel SelectedAccoutingAccount
        {
            get { return _selectedAccoutingAccount; }
            set
            {
                if (_selectedAccoutingAccount != value)
                {
                    _selectedAccoutingAccount = value;
                    NotifyOfPropertyChange(nameof(SelectedAccoutingAccount));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private ObservableCollection<AccountingAccountGraphQLModel> _accoutingAccount;
        public ObservableCollection<AccountingAccountGraphQLModel> AccoutingAccount
        {
            get { return _accoutingAccount; }
            set
            {
                if (_accoutingAccount != value)
                {
                    _accoutingAccount = value;
                    NotifyOfPropertyChange(nameof(AccoutingAccount));
                }
            }
        }

        public async Task LoadCodeAccountingAccounts()
        {
            try
            {
                string query = @"query($filter: AccountingAccountFilterInput){                     
                    ListResponse: accountingAccounts(filter: $filter){
                        id
                        code
                        name
                        nature
                        margin
                        marginBasis
                      }
                }";

                dynamic variables = new ExpandoObject();
                variables.filter = new ExpandoObject();

                var result = await AccountingAccountService.GetList(query, new { });
                AccoutingAccount = new ObservableCollection<AccountingAccountGraphQLModel>(result);
                AccoutingAccount.Insert(0, new() { Id = 0, Name = "<< SELECCIONE UNA CUENTA >>" });
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener el código de cuentas", ex);
            }
        }

        private string _selectedType;
        public string SelectedType
        {
            get { return _selectedType; }
            set
            {
                if (_selectedType != value)
                {
                    _selectedType = value;
                    NotifyOfPropertyChange(nameof(SelectedType));
                    NotifyOfPropertyChange(nameof(IsPercentageSectionVisible));
                    NotifyOfPropertyChange(nameof(IsPercentageOptionsVisible));

                    // Asegurar que al seleccionar "Ingreso", la casilla se oculta
                    if (_selectedType == "I")
                    {
                        IsApplyPercentage = false;
                        NotifyOfPropertyChange(nameof(IsApplyPercentage));
                    }
                }
            }
        }

        public bool IsTypeD => SelectedType == "D";
        public bool IsTypeI => SelectedType == "I";
        public bool IsTypeE => SelectedType == "E";

        public Visibility IsPercentageSectionVisible
        {
            get => (SelectedType == "D" || SelectedType == "E") ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool _isApplyPercentage;
        public bool IsApplyPercentage
        {
            get => _isApplyPercentage;
            set
            {
                if (_isApplyPercentage != value)
                {
                    _isApplyPercentage = value;
                    NotifyOfPropertyChange(nameof(IsApplyPercentage));
                    NotifyOfPropertyChange(nameof(IsPercentageOptionsVisible));
                    if (_isApplyPercentage)
                    {
                        PercentageValue = 0.000m; // Reinicia el valor cuando se activa
                    }
                }
            }
        }

        private ICommand _changeTypeCommand;
        public ICommand ChangeTypeCommand
        {
            get
            {
                return _changeTypeCommand ??= new AsyncCommand<object>(async param =>
                {
                    if (param is string type)
                    {
                        SelectedType = type;
                    }
                });
            }
        }

        public Visibility IsPercentageOptionsVisible
        {
            get => (IsApplyPercentage && (SelectedType == "D" || SelectedType == "E"))
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private decimal _percentageValue = 0.000m;
        public decimal PercentageValue
        {
            get { return _percentageValue; }
            set
            {
                if (_percentageValue != value)
                {
                    _percentageValue = value;
                    NotifyOfPropertyChange(nameof(PercentageValue));
                }
            }
        }

        public bool IsNewRecord { get; set; }
        public  bool CanSave
        {
            get
            {
                if (string.IsNullOrEmpty(NameConcept) || string.IsNullOrEmpty(SelectedType)) return false;
                {
                    return true;
                }
            }
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }
        private int _conceptId;

        public int ConceptId
        {
            get { return _conceptId; }
            set
            {
                if (_conceptId != value)
                {
                    _conceptId = value;
                    NotifyOfPropertyChange(nameof(ConceptId));
                    //NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                //IsBusy                
                ConceptGraphQLModel result = await ExecuteSaveAsync();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new TreasuryConceptCreateMessage() { CreatedTreasuryConcept = result });

                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new TreasuryConceptUpdateMessage() { UpdatedTreasuryConcept = result });
                }
                await Context.ActivateMasterView();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                GraphQLError graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(exGraphQL.Content.ToString());
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{exGraphQL.Message}.\r\n{graphQLError.Errors[0].Extensions.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{currentMethod.Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            finally
            {
                //IsBusy = false;
            }
        }

        private bool _isBase100;
        

        public bool IsBase100
        {
            get { return _isBase100; }
            set
            {
                if (_isBase100 != value)  // Evita ejecutar código innecesario si el valor no cambia
                {
                    _isBase100 = value;
                    _isBase1000 = !value; // Modifica la variable interna en lugar de llamar al setter
                    NotifyOfPropertyChange(nameof(IsBase100));
                    NotifyOfPropertyChange(nameof(IsBase1000));
                }
            }
        }
        private bool _isBase1000;
        public bool IsBase1000
        {
            get { return _isBase1000; }
            set
            {
                if (_isBase1000 != value)
                {
                    _isBase1000 = value;
                    _isBase100 = !value; // Modifica la variable interna en lugar de llamar al setter
                    NotifyOfPropertyChange(nameof(IsBase1000));
                    NotifyOfPropertyChange(nameof(IsBase100));
                }
            }
        }


        public async Task<ConceptGraphQLModel> ExecuteSaveAsync()
        {
            dynamic variables = new ExpandoObject();
            variables.data = new ExpandoObject();
            if (!IsNewRecord) variables.id = ConceptId;
            variables.data.name = NameConcept;
            variables.data.accountingAccountId = SelectedAccoutingAccount.Id;
            variables.data.allowMargin = IsApplyPercentage;
            variables.data.margin = IsApplyPercentage ? PercentageValue : 0;
            variables.data.marginBasis = IsBase100 ? 100 : 1000;
            variables.data.type = SelectedType;

            string query = IsNewRecord ? @"
            mutation ($data: CreateConceptInput!) {
              CreateResponse: createConcept(data: $data) {
                id
                name
                type
                margin
                allowMargin    
                marginBasis
                accountingAccountId    
              }
            }" :
            @"
            mutation($data:UpdateConceptInput!, $id: Int!) {
                UpdateResponse: updateConcept(data: $data, id: $id) {
                id
                name
                }
            }";
            var result = IsNewRecord ? await ConceptService.Create(query, variables) : await ConceptService.Update(query, variables);
            return result;
        }
    }
}

