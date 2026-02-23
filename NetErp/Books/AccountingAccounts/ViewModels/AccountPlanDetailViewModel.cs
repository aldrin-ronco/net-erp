using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Models.Books;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingAccounts.ViewModels
{
    public class AccountPlanDetailViewModel : Screen
    {
        #region "Propiedades"

        // Contiene la lista de las cuentas que clasifican como naturaleza debito

        private List<string> _debitAccounts = new List<string>() { "1", "5", "6", "7" };

        // Prefijos de cuentas que requieren margen
        private static readonly List<string> _marginPrefixes = new List<string>() { "2408", "2365", "2367", "2368" };

        private readonly IRepository<AccountingAccountGraphQLModel> _accountingAccountService;
        private readonly List<AccountingAccountGraphQLModel> _accounts;
        private readonly int _selectedItemId;

        public Dictionary<char, string> AccountNature => Dictionaries.BooksDictionaries.AccountNatureDictionary;
        private decimal _margin;
        public decimal Margin
        {
            get { return _margin; }
            set
            {
                if (_margin != value)
                {
                    _margin = value;
                    NotifyOfPropertyChange(nameof(Margin));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        private int _marginBasis = 100;
        public int MarginBasis
        {
            get { return _marginBasis; }
            set
            {
                if (_marginBasis != value)
                {
                    _marginBasis = value;
                    NotifyOfPropertyChange(nameof(MarginBasis));
                    NotifyOfPropertyChange(nameof(IsMarginBasis100));
                    NotifyOfPropertyChange(nameof(IsMarginBasis1000));
                }
            }
        }

        public bool IsMarginBasis100
        {
            get { return _marginBasis == 100; }
            set { if (value) MarginBasis = 100; }
        }

        public bool IsMarginBasis1000
        {
            get { return _marginBasis == 1000; }
            set { if (value) MarginBasis = 1000; }
        }

        public bool RequiresMargin
        {
            get
            {
                string code = IsNewRecord ? Lv5Code : Code;
                return code.Length >= 8 && _marginPrefixes.Any(p => code.StartsWith(p));
            }
        }

        public Visibility MarginVisibility
        {
            get { return RequiresMargin ? Visibility.Visible : Visibility.Collapsed; }
        }

        private string _selectedAccountNature;

        public string SelectedAccountNature
        {
            get
            {
                return IsNewRecord ? _debitAccounts.Any(x => x.Contains(Lv1Code)) ? "D" : "C" : _selectedAccountNature;
            }
            set
            {
                if (_selectedAccountNature != value)
                {
                    _selectedAccountNature = value;
                    NotifyOfPropertyChange(nameof(SelectedAccountNature));
                }
            }
        }

        private string _code = string.Empty;
        public string Code
        {
            get { return _code; }
            set
            {
                if (_code != value)
                {
                    _code = value;
                    NotifyOfPropertyChange(nameof(Code));
                    OnCodeChanged();
                }
            }
        }

        protected void OnCodeChanged()
        {
            NotifyOfPropertyChange(nameof(IsNewRecord));
            NotifyOfPropertyChange(nameof(Lv1Visibility));
            NotifyOfPropertyChange(nameof(Lv2Visibility));
            NotifyOfPropertyChange(nameof(Lv3Visibility));
            NotifyOfPropertyChange(nameof(Lv4Visibility));
            NotifyOfPropertyChange(nameof(Lv5Visibility));
            NotifyOfPropertyChange(nameof(MarginVisibility));
        }

        // Focus handling
        private bool _lv5CodeIsFocused;
        public bool Lv5CodeIsFocused
        {
            get { return _lv5CodeIsFocused; }
            set
            {
                if (_lv5CodeIsFocused != value)
                {
                    _lv5CodeIsFocused = value;
                    NotifyOfPropertyChange(nameof(Lv5CodeIsFocused));
                }
            }
        }

        //Visibilidad

        private Visibility _lv1Visibility;
        public Visibility Lv1Visibility
        {
            get { return this._code.Length >= 1 || this.IsNewRecord ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                if (_lv1Visibility != value)
                {
                    _lv1Visibility = value;
                    NotifyOfPropertyChange(nameof(Lv1Visibility));
                }
            }
        }


        private Visibility _lv2Visibility;
        public Visibility Lv2Visibility
        {
            get { return this._code.Length >= 2 || this.IsNewRecord ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                if (_lv2Visibility != value)
                {
                    _lv2Visibility = value;
                    NotifyOfPropertyChange(nameof(Lv2Visibility));
                }
            }
        }


        private Visibility _lv3Visibility;
        public Visibility Lv3Visibility
        {
            get { return this._code.Length >= 4 || this.IsNewRecord ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                if (_lv3Visibility != value)
                {
                    _lv3Visibility = value;
                    NotifyOfPropertyChange(nameof(Lv3Visibility));
                }
            }
        }

        private Visibility _lv4Visibility;
        public Visibility Lv4Visibility
        {
            get { return this._code.Length >= 6 || this.IsNewRecord ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                if (_lv4Visibility != value)
                {
                    _lv4Visibility = value;
                    NotifyOfPropertyChange(nameof(Lv4Visibility));
                }
            }
        }


        private Visibility _lv5Visibility;
        public Visibility Lv5Visibility
        {
            get { return this._code.Length >= 8 || this.IsNewRecord ? Visibility.Visible : Visibility.Collapsed; }
            set
            {
                if (_lv5Visibility != value)
                {
                    _lv5Visibility = value;
                    NotifyOfPropertyChange(nameof(Lv5Visibility));
                }
            }
        }


        // Seleccion de Texto para Lv5 (Auxiliar)

        private int _selectionStart;
        public int SelectionStart
        {
            get { return _selectionStart; }
            set
            {
                if (_selectionStart != value)
                {
                    _selectionStart = value;
                    NotifyOfPropertyChange(nameof(SelectionStart));
                }
            }
        }

        private int _selectionLength;
        public int SelectionLength
        {
            get { return _selectionLength; }
            set
            {
                if (_selectionLength != value)
                {
                    _selectionLength = value;
                    NotifyOfPropertyChange(nameof(SelectionLength));
                }
            }
        }

        // Codigos de cuentas

        private string _lv1Code = string.Empty;
        public string Lv1Code
        {
            get { return _lv1Code; }
            set
            {
                if (_lv1Code != value)
                {
                    _lv1Code = value;
                    NotifyOfPropertyChange(nameof(Lv1Code));
                    OnLV1CodeChanged();
                }
            }
        }

        protected void OnLV1CodeChanged()
        {
            NotifyOfPropertyChange(nameof(SelectedAccountNature));
        }

        private string _lv2Code = string.Empty;
        public string Lv2Code
        {
            get { return _lv2Code; }
            set
            {
                if (_lv2Code != value)
                {
                    _lv2Code = value;
                    NotifyOfPropertyChange(nameof(Lv2Code));
                }
            }
        }

        private string _lv3Code = string.Empty;
        public string Lv3Code
        {
            get { return _lv3Code; }
            set
            {
                if (_lv3Code != value)
                {
                    _lv3Code = value;
                    NotifyOfPropertyChange(nameof(Lv3Code));
                }
            }
        }

        private string _lv4Code = string.Empty;
        public string Lv4Code
        {
            get { return _lv4Code; }
            set
            {
                if (_lv4Code != value)
                {
                    _lv4Code = value;
                    NotifyOfPropertyChange(nameof(Lv4Code));
                }
            }
        }

        private string _lv5Code = string.Empty;
        public string Lv5Code
        {
            get { return _lv5Code; }
            set
            {
                if (_lv5Code != value)
                {
                    _lv5Code = value;
                    NotifyOfPropertyChange(nameof(Lv5Code));
                    OnLv5CodeChanged();
                }
            }
        }

        protected void OnLv5CodeChanged()
        {
            NotifyOfPropertyChange(nameof(CanSave));
            NotifyOfPropertyChange(nameof(MarginVisibility));

            if (IsNewRecord)
            {
                if (Lv5Code.Length <= 1)
                {
                    PopulateInfoLv1(Lv5Code);
                    SetReadOnlyState(Lv5Code);
                }

                if (Lv5Code.Length <= 2)
                {
                    PopulateInfoLv2(Lv5Code);
                    SetReadOnlyState(Lv5Code);
                }

                if (Lv5Code.Length <= 4)
                {
                    PopulateInfoLv3(Lv5Code);
                    SetReadOnlyState(Lv5Code);
                }

                if (Lv5Code.Length <= 6)
                {
                    PopulateInfoLv4(Lv5Code);
                    SetReadOnlyState(Lv5Code);
                }


                if (Lv5Code.Length <= 8)
                {
                    PopulateInfoNameLv5(Lv5Code);
                    SetReadOnlyState(Lv5Code);
                    SetTextBoxSelectionForExistingAccountCode(Lv5Code);
                    SetTextBoxNameStyleForExistingAccountCode(Lv5Code);
                    SetNextFocus(Lv5Code);
                }
            }
        }

        // Nombres de las cuentas

        private string _lv1Name = string.Empty;
        public string Lv1Name
        {
            get { return _lv1Name; }
            set
            {
                if (_lv1Name != value)
                {
                    _lv1Name = value;
                    NotifyOfPropertyChange(nameof(Lv1Name));
                    OnAccountingAccountNameChanged();
                }
            }
        }

        private string _lv2Name = string.Empty;
        public string Lv2Name
        {
            get { return _lv2Name; }
            set
            {
                if (_lv2Name != value)
                {
                    _lv2Name = value;
                    NotifyOfPropertyChange(nameof(Lv2Name));
                    OnAccountingAccountNameChanged();
                }
            }
        }

        private string _lv3Name = string.Empty;
        public string Lv3Name
        {
            get { return _lv3Name; }
            set
            {
                if (_lv3Name != value)
                {
                    _lv3Name = value;
                    NotifyOfPropertyChange(nameof(Lv3Name));
                    OnAccountingAccountNameChanged();
                }
            }
        }

        private string _lv4Name = string.Empty;
        public string Lv4Name
        {
            get { return _lv4Name; }
            set
            {
                if (_lv4Name != value)
                {
                    _lv4Name = value;
                    NotifyOfPropertyChange(nameof(Lv4Name));
                    OnAccountingAccountNameChanged();
                }
            }
        }

        private string _lv5Name = string.Empty;
        public string Lv5Name
        {
            get { return _lv5Name; }
            set
            {
                if (_lv5Name != value)
                {
                    _lv5Name = value;
                    NotifyOfPropertyChange(nameof(Lv5Name));
                    OnAccountingAccountNameChanged();
                }
            }
        }
        protected void OnAccountingAccountNameChanged()
        {
            NotifyOfPropertyChange(nameof(CanSave));
        }

        // Focos en codigos de las cuentas

        private bool _isFocusedLv5Code;
        public bool IsFocusedLv5Code
        {
            get { return _isFocusedLv5Code; }
            set
            {
                if (_isFocusedLv5Code != value)
                {
                    _isFocusedLv5Code = value;
                    NotifyOfPropertyChange(nameof(IsFocusedLv5Code));
                }
            }
        }

        private bool _lv5NameIsFocused;

        public bool Lv5NameIsFocused
        {
            get { return _lv5NameIsFocused; }
            set
            {
                if (_lv5NameIsFocused != value)
                {
                    _lv5NameIsFocused = value;
                    NotifyOfPropertyChange(nameof(Lv5NameIsFocused));
                }
            }
        }

        private bool _lv4NameIsFocused;

        public bool Lv4NameIsFocused
        {
            get { return _lv4NameIsFocused; }
            set
            {
                if (_lv4NameIsFocused != value)
                {
                    _lv4NameIsFocused = value;
                    NotifyOfPropertyChange(nameof(Lv4NameIsFocused));
                }
            }
        }

        private bool _lv3NameIsFocused;

        public bool Lv3NameIsFocused
        {
            get { return _lv3NameIsFocused; }
            set
            {
                if (_lv3NameIsFocused != value)
                {
                    _lv3NameIsFocused = value;
                    NotifyOfPropertyChange(nameof(Lv3NameIsFocused));
                }
            }
        }

        private bool _lv2NameIsFocused;

        public bool Lv2NameIsFocused
        {
            get { return _lv2NameIsFocused; }
            set
            {
                if (_lv2NameIsFocused != value)
                {
                    _lv2NameIsFocused = value;
                    NotifyOfPropertyChange(nameof(Lv2NameIsFocused));
                }
            }
        }

        private bool _lv1NameIsFocused;

        public bool Lv1NameIsFocused
        {
            get { return _lv1NameIsFocused; }
            set
            {
                if (_lv1NameIsFocused != value)
                {
                    _lv1NameIsFocused = value;
                    NotifyOfPropertyChange(nameof(Lv1NameIsFocused));
                }
            }
        }
        // IsReadonly Name

        private bool _isReadOnlyLv1Name;
        public bool IsReadOnlyLv1Name
        {
            get { return _isReadOnlyLv1Name; }
            set
            {
                if (_isReadOnlyLv1Name != value)
                {
                    _isReadOnlyLv1Name = value;
                    NotifyOfPropertyChange(nameof(IsReadOnlyLv1Name));
                }
            }
        }

        private bool _isReadOnlyLv2Name;
        public bool IsReadOnlyLv2Name
        {
            get { return _isReadOnlyLv2Name; }
            set
            {
                if (_isReadOnlyLv2Name != value)
                {
                    _isReadOnlyLv2Name = value;
                    NotifyOfPropertyChange(nameof(IsReadOnlyLv2Name));
                }
            }
        }

        private bool _isReadOnlyLv3Name;
        public bool IsReadOnlyLv3Name
        {
            get { return _isReadOnlyLv3Name; }
            set
            {
                if (_isReadOnlyLv3Name != value)
                {
                    _isReadOnlyLv3Name = value;
                    NotifyOfPropertyChange(nameof(IsReadOnlyLv3Name));
                }
            }
        }

        private bool _isReadOnlyLv4Name;
        public bool IsReadOnlyLv4Name
        {
            get { return _isReadOnlyLv4Name; }
            set
            {
                if (_isReadOnlyLv4Name != value)
                {
                    _isReadOnlyLv4Name = value;
                    NotifyOfPropertyChange(nameof(IsReadOnlyLv4Name));
                }
            }
        }


        private bool _isReadOnlyLv5Name;
        public bool IsReadOnlyLv5Name
        {
            get { return _isReadOnlyLv5Name; }
            set
            {
                if (_isReadOnlyLv5Name != value)
                {
                    _isReadOnlyLv5Name = value;
                    NotifyOfPropertyChange(nameof(IsReadOnlyLv5Name));
                }
            }
        }


        // IsReadOnly Code

        private bool _isReadOnlyLv5Code;
        public bool IsReadOnlyLv5Code
        {
            get { return _isReadOnlyLv5Code; }
            set
            {
                if (_isReadOnlyLv5Code != value)
                {
                    _isReadOnlyLv5Code = value;
                    NotifyOfPropertyChange(nameof(IsReadOnlyLv5Code));
                }
            }
        }


        // Es nuevo registro ?

        private bool _isNewRecord;
        public bool IsNewRecord
        {
            get { return string.IsNullOrEmpty(_code); }
            set
            {
                if (_isNewRecord != value)
                {
                    _isNewRecord = value;
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }


        // Styles

        private string _lv5NameStyle = "TextBoxDefault";
        public string Lv5NameStyle
        {
            get { return _lv5NameStyle; }
            set
            {
                if (_lv5NameStyle != value)
                {
                    _lv5NameStyle = value;
                    NotifyOfPropertyChange(nameof(Lv5NameStyle));
                }
            }
        }


        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    OnIsBusyChanged();
                }
            }
        }
        protected void OnIsBusyChanged()
        {
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion


        #region "Constructores"

        public AccountPlanDetailViewModel(
            IRepository<AccountingAccountGraphQLModel> accountingAccountService,
            List<AccountingAccountGraphQLModel> accounts,
            int selectedItemId = 0)
        {
            _accountingAccountService = accountingAccountService;
            _accounts = accounts;
            _selectedItemId = selectedItemId;
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region "Lifecycle"

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            PopulateInfo(Code);
            SetReadOnlyState(Code);
            // Defer focus so the dialog layout is fully rendered
            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                SetLevelFocus(Code);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        #endregion

        #region "Metodos"

        /// <summary>
        /// Establece cual es el control que debe recibir el foco una vez se digita el ultimo numero de la cuenta auxiliar
        /// </summary>
        /// <param name="accountCode">Cuenta auxiliar de 8 digitos</param>
        public void SetNextFocus(string accountCode)
        {
            if (accountCode.Length < 8) return;

            if (AccountCodeExist(accountCode)) return;

            if (string.IsNullOrEmpty(this.Lv1Name))
            {
                this.SetFocus(() => Lv1Name);
            }
            else
            {
                if (string.IsNullOrEmpty(this.Lv2Name))
                {
                    this.SetFocus(() => Lv2Name);
                }
                else
                {
                    if (string.IsNullOrEmpty(this.Lv3Name))
                    {
                        this.SetFocus(() => Lv3Name);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(this.Lv4Name))
                        {
                            this.SetFocus(() => Lv4Name);
                        }
                        else
                        {
                            this.SetFocus(() => Lv5Name);
                        }
                    }
                }
            }

        }

        public string GetUpdateQuery()
        {
            var fields = FieldSpec<UpsertResponseType<AccountingAccountGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "accountingAccount", nested: sq => sq
                    .Field(f => f.Id)
                    .Field(f => f.Name)
                    .Field(f => f.Code)
                    .Field(f => f.Nature)
                    .Field(f => f.Margin)
                    .Field(f => f.MarginBasis)
                    .Field(f => f.InsertedAt)
                    .Field(f => f.UpdatedAt))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameters = new List<GraphQLQueryParameter>
            {
                new("data", "UpdateAccountingAccountInput!"),
                new("id", "ID!")
            };
            var fragment = new GraphQLQueryFragment("updateAccountingAccount", parameters, fields, "UpdateResponse");
            var builder = new GraphQLQueryBuilder([fragment]);
            return builder.GetQuery(GraphQLOperations.MUTATION);
        }

        public async Task<UpsertResponseType<AccountingAccountGraphQLModel>> UpdateAsync()
        {
            try
            {
                AccountingAccountGraphQLModel? model = null;

                if (this.Lv5Visibility == Visibility.Visible)
                {
                    model = new()
                    {
                        Id = _selectedItemId,
                        Code = this.Lv5Code.RemoveExtraSpaces().Trim(),
                        Name = this.Lv5Name.RemoveExtraSpaces().Trim(),
                        Nature = this.SelectedAccountNature,
                        Margin = RequiresMargin ? this.Margin : 0,
                        MarginBasis = RequiresMargin ? this.MarginBasis : 0
                    };
                }
                else
                {
                    if (this.Lv4Visibility == Visibility.Visible)
                    {
                        model = new()
                        {
                            Id = _selectedItemId,
                            Code = this.Lv4Code.RemoveExtraSpaces().Trim(),
                            Name = this.Lv4Name.RemoveExtraSpaces().Trim(),
                            Nature = this.SelectedAccountNature,
                            Margin = 0,
                            MarginBasis = 0
                        };
                    }
                    else
                    {
                        if (this.Lv3Visibility == Visibility.Visible)
                        {
                            model = new()
                            {
                                Id = _selectedItemId,
                                Code = this.Lv3Code.RemoveExtraSpaces().Trim(),
                                Name = this.Lv3Name.RemoveExtraSpaces().Trim(),
                                Nature = this.SelectedAccountNature,
                                Margin = 0,
                                MarginBasis = 0
                            };
                        }
                        else
                        {
                            if (this.Lv2Visibility == Visibility.Visible)
                            {
                                model = new()
                                {
                                    Id = _selectedItemId,
                                    Code = this.Lv2Code.RemoveExtraSpaces().Trim(),
                                    Name = this.Lv2Name.RemoveExtraSpaces().Trim(),
                                    Nature = this.SelectedAccountNature,
                                    Margin = 0,
                                    MarginBasis = 0
                                };
                            }
                            else
                            {
                                model = new()
                                {
                                    Id = _selectedItemId,
                                    Code = this.Lv1Code.RemoveExtraSpaces().Trim(),
                                    Name = this.Lv1Name.RemoveExtraSpaces().Trim(),
                                    Nature = this.SelectedAccountNature,
                                    Margin = 0,
                                    MarginBasis = 0
                                };
                            }
                        }
                    }
                }
                if (model is null) throw new Exception("model no puede ser null");

                string query = GetUpdateQuery();

                object variables = new
                {
                    updateResponseId = model.Id,
                    updateResponseData = new
                    {

                        model.Name,
                        model.Margin,
                        model.MarginBasis
                    }
                };

                UpsertResponseType<AccountingAccountGraphQLModel> updatedAccount = await _accountingAccountService.UpdateAsync<UpsertResponseType<AccountingAccountGraphQLModel>>(query, variables);
                return updatedAccount;
            }
            catch (Exception)
            {
                throw;
            }
        }


        public string GetInsertQuery()
        {
            var fields = FieldSpec<UpsertResponseType<List<AccountingAccountGraphQLModel>>>
                .Create()
                .SelectList(selector: list => list.Entity, alias: "Entity", overrideName: "accountingAccounts", nested: account => account
                    .Field(f => f.Id)
                    .Field(f => f.Code)
                    .Field(f => f.Name)
                    .Field(f => f.Nature)
                    .Field(f => f.Margin)
                    .Field(f => f.MarginBasis)
                    .Field(f => f.InsertedAt)
                    .Field(f => f.UpdatedAt))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var parameter = new GraphQLQueryParameter("input", "[CreateAccountingAccountInput!]!");

            var fragment = new GraphQLQueryFragment("createAccountingAccounts", [parameter], fields, "CreateResponse");

            var builder = new GraphQLQueryBuilder([fragment]);

            return builder.GetQuery(GraphQLOperations.MUTATION);
        }
        /// <summary>
        /// Guarda la nueva cuenta contable o actualiza los cambios
        /// </summary>
        public async Task<UpsertResponseType<List<AccountingAccountGraphQLModel>>> InsertAsync()
        {
            try
            {
                List<AccountingAccountGraphQLModel> models = [];
                // Crear el nivel 1 en caso de que no exista
                if (!this.IsReadOnlyLv1Name)
                {
                    AccountingAccountGraphQLModel model = new()
                    {
                        Code = this.Lv1Code.RemoveExtraSpaces().Trim(),
                        Name = this.Lv1Name.RemoveExtraSpaces().Trim(),
                        Nature = this.SelectedAccountNature,
                        Margin = 0,
                        MarginBasis = 0
                    };
                    models.Add(model);
                }

                // Crear el nivel 2 en caso de que no exista
                if (!this.IsReadOnlyLv2Name)
                {
                    AccountingAccountGraphQLModel model = new()
                    {
                        Code = this.Lv2Code.RemoveExtraSpaces().Trim(),
                        Name = this.Lv2Name.RemoveExtraSpaces().Trim(),
                        Nature = this.SelectedAccountNature,
                        Margin = 0,
                        MarginBasis = 0
                    };
                    models.Add(model);
                }

                // Crear el nivel 3 en caso de no existir
                if (!this.IsReadOnlyLv3Name)
                {
                    AccountingAccountGraphQLModel model = new()
                    {
                        Code = this.Lv3Code.RemoveExtraSpaces().Trim(),
                        Name = this.Lv3Name.RemoveExtraSpaces().Trim(),
                        Nature = this.SelectedAccountNature,
                        Margin = 0,
                        MarginBasis = 0
                    };
                    models.Add(model);
                }

                // Crear el nivel 4 en caso de no existir
                if (!this.IsReadOnlyLv4Name)
                {
                    AccountingAccountGraphQLModel model = new AccountingAccountGraphQLModel()
                    {
                        Code = this.Lv4Code.RemoveExtraSpaces().Trim(),
                        Name = this.Lv4Name.RemoveExtraSpaces().Trim(),
                        Nature = this.SelectedAccountNature,
                        Margin = 0,
                        MarginBasis = 0
                    };
                    models.Add(model);
                }

                // Crear el nivel 5
                AccountingAccountGraphQLModel modelLv5 = new()
                {
                    Code = this.Lv5Code.RemoveExtraSpaces().Trim(),
                    Name = this.Lv5Name.RemoveExtraSpaces().Trim(),
                    Nature = this.SelectedAccountNature,
                    Margin = RequiresMargin ? this.Margin : 0,
                    MarginBasis = RequiresMargin ? this.MarginBasis : 0
                };
                models.Add(modelLv5);

                string query = GetInsertQuery();

                var modelsWithOutIds = from model in models
                                       select new { model.Code, model.Name, model.Nature, model.Margin, model.MarginBasis };

                dynamic variables = new ExpandoObject();
                variables.createResponseInput = new ExpandoObject();
                variables.createResponseInput = modelsWithOutIds;
                UpsertResponseType<List<AccountingAccountGraphQLModel>> response = await _accountingAccountService.CreateAsync<UpsertResponseType<List<AccountingAccountGraphQLModel>>>(query, variables);
                return response;
            }
            catch (Exception ex)
            {
                throw;
            }
        }



        /// <summary>
        /// Obtiene la informacion del primer nivel (Clase) y asigna la cuenta y el nombre
        /// </summary>
        /// <param name="accountCode">Cuenta contable, 1 digito de longitud</param>
        public void PopulateInfoLv1(string accountCode)
        {
            // Lv1
            try
            {
                if (accountCode.Length >= 1)
                {
                    var lv1 = from account
                    in _accounts
                              where account.Code == accountCode.Substring(0, 1)
                              select account;

                    if (lv1 != null && lv1.ToList().Count > 0)
                    {
                        this.Lv1Code = lv1.First().Code;
                        this.Lv1Name = lv1.First().Name;
                        this.SelectedAccountNature = lv1.First().Nature;
                    }
                    else
                    {
                        this.Lv1Code = accountCode.Substring(0, 1);
                    }
                }
                else
                {
                    this.Lv1Code = "";
                    this.Lv1Name = "";
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "PopulateInfoLv1" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public void PopulateInfoLv2(string accountCode)
        {
            // Lv2
            try
            {
                if (accountCode.Length >= 2)
                {
                    var lv2 = from account
                    in _accounts
                              where account.Code == accountCode.Substring(0, 2)
                              select account;

                    if (lv2 != null && lv2.ToList().Count > 0)
                    {
                        this.Lv2Code = lv2.First().Code;
                        this.Lv2Name = lv2.First().Name;
                        this.SelectedAccountNature = lv2.First().Nature;
                    }
                    else
                    {
                        this.Lv2Code = accountCode.Substring(0, 2);
                    }
                }
                else
                {
                    this.Lv2Code = "";
                    this.Lv2Name = "";
                }

            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "PopulateInfoLv2" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public void PopulateInfoLv3(string accountCode)
        {
            // Lv3
            try
            {
                if (accountCode.Length >= 4)
                {
                    var lv3 = from account
                    in _accounts
                              where account.Code == accountCode.Substring(0, 4)
                              select new { account.Code, account.Name, account.Nature };

                    if (lv3 != null && lv3.ToList().Count > 0)
                    {

                        this.Lv3Code = lv3.First().Code;
                        this.Lv3Name = lv3.First().Name;
                        this.SelectedAccountNature = lv3.First().Nature;
                    }
                    else
                    {
                        this.Lv3Code = accountCode.Substring(0, 4);
                    }
                }
                else
                {
                    this.Lv3Code = "";
                    this.Lv3Name = "";
                }

            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "PopulateInfoLv3" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public void PopulateInfoLv4(string accountCode)
        {
            // Lv4
            try
            {
                if (accountCode.Length >= 6)
                {
                    var lv4 = from account
                    in _accounts
                              where account.Code == accountCode.Substring(0, 6)
                              select account;

                    if (lv4 != null && lv4.ToList().Count > 0)
                    {
                        this.Lv4Code = lv4.First().Code;
                        this.Lv4Name = lv4.First().Name;
                        this.SelectedAccountNature = lv4.First().Nature;
                    }
                    else
                    {
                        Lv4Code = accountCode.Substring(0, 6);
                    }
                }
                else
                {
                    this.Lv4Code = "";
                    this.Lv4Name = "";
                }

            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "PopulateInfoLv4" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public void PopulateInfoNameLv5(string accountCode)
        {
            try
            {
                if (accountCode.Length >= 8)
                {
                    var lv5 = from account
                    in _accounts
                              where account.Code == accountCode
                              select account;

                    if (lv5 != null && lv5.ToList().Count > 0)
                    {
                        if (this.IsNewRecord)
                        {
                            this.Lv5Name = "YA EXISTE UNA CUENTA CONTABLE CON ESTE CODIGO";
                        }
                        else
                        {
                            this.Lv5Name = lv5.First().Name;
                        }
                    }
                    else
                    {
                        this.Lv5Name = "";
                    }
                }
                else
                {
                    this.Lv5Name = "";
                }

            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "PopulateInfoNameLv5" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        /// <summary>
        /// LLenar todos los niveles del detalle de la cuenta con la informacion correspondiente
        /// </summary>
        /// <param name="accountCode"></param>
        public void PopulateInfo(string accountCode)
        {
            try
            {
                // Lv1
                if (accountCode.Length >= 1)
                {
                    var lv1 = from account
                    in _accounts
                              where account.Code == accountCode.Substring(0, 1)
                              select account;

                    if (lv1 != null && lv1.ToList().Count > 0)
                    {
                        this.Lv1Code = lv1.First().Code;
                        this.Lv1Name = lv1.First().Name;
                        this.SelectedAccountNature = lv1.First().Nature;
                    }
                    else
                    {
                        this.Lv1Code = accountCode.Substring(0, 1);
                    }
                }
                else
                {
                    this.Lv1Code = "";
                    this.Lv1Name = "";
                }

                // Lv2
                if (accountCode.Length >= 2)
                {
                    var lv2 = from account
                    in _accounts
                              where account.Code == accountCode.Substring(0, 2)
                              select account;

                    if (lv2 != null && lv2.ToList().Count > 0)
                    {
                        this.Lv2Code = lv2.First().Code;
                        this.Lv2Name = lv2.First().Name;
                        this.SelectedAccountNature = lv2.First().Nature;
                    }
                    else
                    {
                        this.Lv2Code = accountCode.Substring(0, 2);
                    }
                }
                else
                {
                    this.Lv2Code = "";
                    this.Lv2Name = "";
                }

                // Lv3
                if (accountCode.Length >= 4)
                {
                    var lv3 = from account
                    in _accounts
                              where account.Code == accountCode.Substring(0, 4)
                              select new { account.Code, account.Name, account.Nature };

                    if (lv3 != null && lv3.ToList().Count > 0)
                    {

                        this.Lv3Code = lv3.First().Code;
                        this.Lv3Name = lv3.First().Name;
                        this.SelectedAccountNature = lv3.First().Nature;
                    }
                    else
                    {
                        this.Lv3Code = accountCode.Substring(0, 4);
                    }
                }
                else
                {
                    this.Lv3Code = "";
                    this.Lv3Name = "";
                }

                // Lv4
                if (accountCode.Length >= 6)
                {
                    var lv4 = from account
                    in _accounts
                              where account.Code == accountCode.Substring(0, 6)
                              select account;

                    if (lv4 != null && lv4.ToList().Count > 0)
                    {
                        this.Lv4Code = lv4.First().Code;
                        this.Lv4Name = lv4.First().Name;
                        this.SelectedAccountNature = lv4.First().Nature;
                    }
                    else
                    {
                        Lv4Code = accountCode.Substring(0, 6);
                    }
                }
                else
                {
                    this.Lv4Code = "";
                    this.Lv4Name = "";
                }

                // Lv5
                if (accountCode.Length >= 8)
                {
                    var lv5 = from account
                    in _accounts
                              where account.Code == accountCode
                              select account;

                    if (lv5 != null && lv5.ToList().Count > 0)
                    {
                        this.Lv5Code = lv5.First().Code;
                        this.Lv5Name = lv5.First().Name;
                        this.SelectedAccountNature = lv5.First().Nature;
                        this.Margin = lv5.First().Margin;
                        this.MarginBasis = lv5.First().MarginBasis;
                    }
                }
                else
                {
                    this.Lv5Code = "";
                    this.Lv5Name = "";
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "PopulateInfo" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        /// <summary>
        /// Establece el foco en el control correspondiente, dependiendo de si estamos editando o creando una nueva cuenta contable
        /// </summary>
        /// <param name="accountCode">Cuenta contable, vacio para nueva</param>

        void SetFocus(Expression<Func<object>> propertyExpression)
        {
            string controlName = propertyExpression.GetMemberInfo().Name;
            Lv5CodeIsFocused = false;
            Lv5NameIsFocused = false;
            Lv4NameIsFocused = false;
            Lv3NameIsFocused = false;
            Lv2NameIsFocused = false;
            Lv1NameIsFocused = false;

            Lv5CodeIsFocused = controlName == nameof(Lv5Code);
            Lv5NameIsFocused = controlName == nameof(Lv5Name);
            Lv4NameIsFocused = controlName == nameof(Lv4Name);
            Lv3NameIsFocused = controlName == nameof(Lv3Name);
            Lv2NameIsFocused = controlName == nameof(Lv2Name);
            Lv1NameIsFocused = controlName == nameof(Lv1Name);
        }

        public void SetLevelFocus(string accountCode)
        {
            try
            {
                //// Cuenta Vacia
                if (IsNewRecord)
                {

                    this.SetFocus(() => Lv5Code);
                    return;
                }

                // Level5
                if (accountCode.Length >= 8)
                {
                    if (IsNewRecord)
                    {
                        this.SetFocus(() => Lv5Code);
                    }
                    else
                    {
                        this.SetFocus(() => Lv5Name);
                    }
                    return;
                }

                // Level4
                if (accountCode.Length == 6)
                {
                    this.SetFocus(() => Lv4Name);
                    return;
                }

                // Level3
                if (accountCode.Length == 4)
                {
                    this.SetFocus(() => Lv3Name);
                    return;
                }

                // Level2
                if (accountCode.Length == 2)
                {
                    this.SetFocus(() => Lv2Name);
                    return;
                }

                // Level1
                if (accountCode.Length == 1)
                {
                    this.SetFocus(() => Lv1Name);
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "SetLevelFocus" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public void SetReadOnlyState(string accountCode)
        {
            try
            {
                if (this.IsNewRecord)
                {
                    IsReadOnlyLv1Name = !string.IsNullOrEmpty(this.Lv1Name.Trim()) || string.IsNullOrEmpty(this.Lv5Code);
                    IsReadOnlyLv2Name = !string.IsNullOrEmpty(this.Lv2Name.Trim()) || string.IsNullOrEmpty(this.Lv5Code);
                    IsReadOnlyLv3Name = !string.IsNullOrEmpty(this.Lv3Name.Trim()) || string.IsNullOrEmpty(this.Lv5Code);
                    IsReadOnlyLv4Name = !string.IsNullOrEmpty(this.Lv4Name.Trim()) || string.IsNullOrEmpty(this.Lv5Code);
                    IsReadOnlyLv5Name = AccountCodeExist(accountCode);
                }
                else
                {
                    IsReadOnlyLv1Name = accountCode.Length == 1 ? false : true;
                    IsReadOnlyLv2Name = accountCode.Length == 2 ? false : true;
                    IsReadOnlyLv3Name = accountCode.Length == 4 ? false : true;
                    IsReadOnlyLv4Name = accountCode.Length == 6 ? false : true;
                    IsReadOnlyLv5Name = accountCode.Length >= 8 ? false : true;
                }

                IsReadOnlyLv5Code = this.IsNewRecord ? false : true;

            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "SetReadOnlyState" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public void SetTextBoxNameStyleForExistingAccountCode(string accountCode)
        {
            // Si la longitud del codigo recibido es menor de 8 regreso sin procesar nada
            try
            {
                if (accountCode.Length < 8)
                {
                    if (this.Lv5NameStyle.ToLower() != "TextBoxDefault".ToLower())
                    {
                        this.Lv5NameStyle = "TextBoxDefault";
                    }
                }
                else
                {
                    if (AccountCodeExist(accountCode))
                    {
                        this.Lv5NameStyle = "TextBoxDanger";
                    }
                    else
                    {
                        this.Lv5NameStyle = "TextBoxDefault";
                    }
                }

            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "SetTextBoxNameStyleForExistingAccountCode" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public void SetTextBoxSelectionForExistingAccountCode(string accountCode)
        {
            try
            {
                // Si la longitud del codigo recibido es menor de 8 regreso sin procesar nada
                if (accountCode.Length < 8) return;

                if (AccountCodeExist(accountCode))
                {
                    this.SelectionStart = 6;
                    this.SelectionLength = 2;
                }

            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "SetTextBoxSelectionForExistingAccountCode" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }

        public bool AccountCodeExist(string accountCode)
        {
            try
            {
                var result = from account in _accounts
                             where account.Code == accountCode
                             select account;

                return (result.ToList().Count > 0);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void CleanUpControls()
        {
            try
            {
                // Codigos de Cuentas
                this.Lv5Code = "";
                this.Lv1Code = "";
                this.Lv2Code = "";
                this.Lv3Code = "";
                this.Lv4Code = "";
                // Nombres de Cuentas
                this.Lv1Name = "";
                this.Lv2Name = "";
                this.Lv3Name = "";
                this.Lv4Name = "";
                this.Lv5Name = "";
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                App.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "CleanUpControls" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
        }
        #endregion

        #region "Commands"

        private ICommand? _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                _saveCommand ??= new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        private ICommand? _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                _cancelCommand ??= new AsyncCommand(CancelAsync);
                return _cancelCommand;
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                this.IsBusy = true;
                if (this.IsNewRecord)
                {
                    UpsertResponseType<List<AccountingAccountGraphQLModel>> result = await Task.Run(() => this.InsertAsync());
                    if (!result.Success)
                    {
                        ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                        return;
                    }
                    Messenger.Default.Send(new AccountingAccountCreateListMessage() { UpsertList = result });
                }
                else
                {
                    if (_selectedItemId <= 0) throw new ArgumentException("No se ha seleccionado una cuenta contable para editar");
                    UpsertResponseType<AccountingAccountGraphQLModel> result = await Task.Run(() => this.UpdateAsync());
                    if (!result.Success)
                    {
                        ThemedMessageBox.Show(text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo", title: $"{result.Message}!", messageBoxButtons: MessageBoxButton.OK, icon: MessageBoxImage.Error);
                        return;
                    }
                    Messenger.Default.Send(new AccountingAccountUpdateMessage() { UpsertAccount = result });
                }
                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                System.Reflection.MethodBase? currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
                Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show(title: "Atención!", text: $"{this.GetType().Name}.{(currentMethod is null ? "Save" : currentMethod.Name.Between("<", ">"))} \r\n{ex.Message}", messageBoxButtons: MessageBoxButton.OK, image: MessageBoxImage.Error));
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        public bool CanSave
        {
            get
            {
                if (this.IsBusy) return false;
                if (RequiresMargin && this.Margin <= 0) return false;
                if (this.IsNewRecord)
                {
                    if (this.Lv5Code.Length < 8) return false;
                    if (string.IsNullOrEmpty(this.Lv1Name.Trim())) return false;
                    if (string.IsNullOrEmpty(this.Lv2Name.Trim())) return false;
                    if (string.IsNullOrEmpty(this.Lv3Name.Trim())) return false;
                    if (string.IsNullOrEmpty(this.Lv4Name.Trim())) return false;
                    if (AccountCodeExist(this.Lv5Code)) return false;
                    if (string.IsNullOrEmpty(this.Lv5Name.Trim())) return false;
                    return true;
                }
                else
                {
                    if (this.Lv5Visibility == Visibility.Visible) // Auxiliar
                    {
                        return this.Lv5Code.Trim().Length >= 8 && !string.IsNullOrEmpty(this.Lv5Name);
                    }
                    else
                    {
                        if (this.Lv4Visibility == Visibility.Visible) // Sub Cuenta
                        {
                            return !string.IsNullOrEmpty(this.Lv4Name);
                        }
                        else
                        {
                            if (this.Lv3Visibility == Visibility.Visible) // Cuenta
                            {
                                return !string.IsNullOrEmpty(this.Lv3Name);
                            }
                            else
                            {
                                if (this.Lv2Visibility == Visibility.Visible) // Grupo
                                {
                                    return !string.IsNullOrEmpty(this.Lv2Name);
                                }
                                else
                                {
                                    return !string.IsNullOrEmpty(this.Lv1Name); // Clase
                                }
                            }
                        }
                    }
                }
            }
        }



        #endregion
    }
}
