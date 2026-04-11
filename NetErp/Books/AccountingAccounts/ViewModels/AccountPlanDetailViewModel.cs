using Caliburn.Micro;
using Common.Extensions;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Books;
using NetErp.Books.AccountingAccounts.Validators;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Books.AccountingAccounts.ViewModels
{
    public class AccountPlanDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region "Propiedades"

        // Contiene la lista de las cuentas que clasifican como naturaleza debito

        private List<string> _debitAccounts = new List<string>() { "1", "5", "6", "7" };

        private readonly IRepository<AccountingAccountGraphQLModel> _accountingAccountService;
        private readonly IEventAggregator _eventAggregator;
        private readonly StringLengthCache _stringLengthCache;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly AccountPlanValidator _validator;
        private readonly List<AccountingAccountGraphQLModel> _accounts;
        private readonly int _selectedItemId;
        private HashSet<string>? _existingCodesSet;
        private readonly Dictionary<string, List<string>> _errors = [];

        public Dictionary<char, string> AccountNature => Dictionaries.BooksDictionaries.AccountNatureDictionary;
        public decimal Margin
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Margin));
                    ValidateProperty(nameof(Margin), value);
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
                return AccountPlanValidator.RequiresMarginFor(code);
            }
        }

        public Visibility MarginVisibility
        {
            get { return RequiresMargin ? Visibility.Visible : Visibility.Collapsed; }
        }

        private string? _selectedAccountNature;

        public string SelectedAccountNature
        {
            get
            {
                return IsNewRecord ? _debitAccounts.Any(x => x.Contains(Lv1Code)) ? "D" : "C" : _selectedAccountNature ?? "";
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
        public bool Lv5CodeIsFocused
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Lv5CodeIsFocused));
                }
            }
        }

        //Visibilidad

        public Visibility Lv1Visibility => _code.Length >= 1 || IsNewRecord ? Visibility.Visible : Visibility.Collapsed;
        public Visibility Lv2Visibility => _code.Length >= 2 || IsNewRecord ? Visibility.Visible : Visibility.Collapsed;
        public Visibility Lv3Visibility => _code.Length >= 4 || IsNewRecord ? Visibility.Visible : Visibility.Collapsed;
        public Visibility Lv4Visibility => _code.Length >= 6 || IsNewRecord ? Visibility.Visible : Visibility.Collapsed;
        public Visibility Lv5Visibility => _code.Length >= 8 || IsNewRecord ? Visibility.Visible : Visibility.Collapsed;


        // Seleccion de Texto para Lv5 (Auxiliar)

        public int SelectionStart
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectionStart));
                }
            }
        }

        public int SelectionLength
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectionLength));
                }
            }
        }

        // Codigos de cuentas

        public string Lv1Code
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Lv1Code));
                    OnLV1CodeChanged();
                }
            }
        } = string.Empty;

        protected void OnLV1CodeChanged()
        {
            NotifyOfPropertyChange(nameof(SelectedAccountNature));
        }

        public string Lv2Code { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(Lv2Code)); } } } = string.Empty;
        public string Lv3Code { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(Lv3Code)); } } } = string.Empty;
        public string Lv4Code { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(Lv4Code)); } } } = string.Empty;

        public string Lv5Code
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Lv5Code));
                    NotifyOfPropertyChange(nameof(Lv5CodeIsTabStop));
                    ValidateProperty(nameof(Lv5Code), value);
                    OnLv5CodeChanged();
                }
            }
        } = string.Empty;

        protected void OnLv5CodeChanged()
        {
            NotifyOfPropertyChange(nameof(CanSave));
            NotifyOfPropertyChange(nameof(MarginVisibility));
            // Re-evaluate Margin validation: RequiresMargin depends on the code prefix,
            // so it may have just flipped. If it flipped on and Margin is still 0, the
            // field needs to show its error indicator so the user knows why Save is blocked.
            ValidateProperty(nameof(Margin), Margin);

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
                    NotifyOfPropertyChange(nameof(AccountCodeExists));
                    SetNextFocus(Lv5Code);
                }
            }
        }

        // Nombres de las cuentas

        public string Lv1Name { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(Lv1Name)); ValidateProperty(nameof(Lv1Name), value); OnAccountingAccountNameChanged(); } } } = string.Empty;
        public string Lv2Name { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(Lv2Name)); ValidateProperty(nameof(Lv2Name), value); OnAccountingAccountNameChanged(); } } } = string.Empty;
        public string Lv3Name { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(Lv3Name)); ValidateProperty(nameof(Lv3Name), value); OnAccountingAccountNameChanged(); } } } = string.Empty;
        public string Lv4Name { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(Lv4Name)); ValidateProperty(nameof(Lv4Name), value); OnAccountingAccountNameChanged(); } } } = string.Empty;
        public string Lv5Name { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(Lv5Name)); ValidateProperty(nameof(Lv5Name), value); OnAccountingAccountNameChanged(); } } } = string.Empty;
        protected void OnAccountingAccountNameChanged()
        {
            NotifyOfPropertyChange(nameof(CanSave));
        }

        // Focos en codigos de las cuentas

        public bool IsFocusedLv5Code { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(IsFocusedLv5Code)); } } }
        public bool Lv5NameIsFocused { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(Lv5NameIsFocused)); } } }
        public bool Lv4NameIsFocused { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(Lv4NameIsFocused)); } } }
        public bool Lv3NameIsFocused { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(Lv3NameIsFocused)); } } }
        public bool Lv2NameIsFocused { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(Lv2NameIsFocused)); } } }
        public bool Lv1NameIsFocused { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(Lv1NameIsFocused)); } } }
        // IsReadonly Name

        public bool IsReadOnlyLv1Name { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(IsReadOnlyLv1Name)); } } }
        public bool IsReadOnlyLv2Name { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(IsReadOnlyLv2Name)); } } }
        public bool IsReadOnlyLv3Name { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(IsReadOnlyLv3Name)); } } }
        public bool IsReadOnlyLv4Name { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(IsReadOnlyLv4Name)); } } }
        public bool IsReadOnlyLv5Name { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(IsReadOnlyLv5Name)); } } }
        public bool IsReadOnlyLv5Code { get; set { if (field != value) { field = value; NotifyOfPropertyChange(nameof(IsReadOnlyLv5Code)); NotifyOfPropertyChange(nameof(Lv5CodeIsTabStop)); } } }

        /// <summary>
        /// Lv5Code only belongs in the tab order while the user is still typing the
        /// auxiliary code (length &lt; 8) in new mode. Once the 8-digit code is fully
        /// entered, SetNextFocus programmatically moves focus to the first empty Name
        /// field; returning to Lv5Code via tab/enter after that point only risks the
        /// user accidentally overtyping a valid code. Edit mode is always false since
        /// the field is read-only there.
        /// </summary>
        public bool Lv5CodeIsTabStop => !IsReadOnlyLv5Code && Lv5Code.Length < 8;


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

        public bool AccountCodeExists => IsNewRecord && Lv5Code.Length >= 8 && AccountCodeExist(Lv5Code);

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        #endregion


        #region "Constructores"

        public AccountPlanDetailViewModel(
            IRepository<AccountingAccountGraphQLModel> accountingAccountService,
            IEventAggregator eventAggregator,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory,
            AccountPlanValidator validator,
            List<AccountingAccountGraphQLModel> accounts,
            int selectedItemId = 0)
        {
            _accountingAccountService = accountingAccountService;
            _eventAggregator = eventAggregator;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
            _validator = validator;
            _accounts = accounts;
            _selectedItemId = selectedItemId;
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #region MaxLength (from StringLengthCache)

        public int CodeMaxLength => _stringLengthCache.GetMaxLength<AccountingAccountGraphQLModel>(nameof(AccountingAccountGraphQLModel.Code));
        public int NameMaxLength => _stringLengthCache.GetMaxLength<AccountingAccountGraphQLModel>(nameof(AccountingAccountGraphQLModel.Name));

        /// <summary>
        /// RegEx mask for the Lv5Code TextEdit. Restricts input to digits only with a
        /// maximum length driven by the StringLengthCache. This prevents the user from
        /// typing letters — account codes are strictly numeric, and the cascading
        /// populate logic in OnLv5CodeChanged assumes digit input.
        /// </summary>
        public string Lv5CodeMask => $"[0-9]{{0,{CodeMaxLength}}}";

        #endregion

        #region Validation (INotifyDataErrorInfo + validator delegation)

        /// <summary>
        /// Cached set of codes currently in the loaded plan. Built lazily from
        /// <c>_accounts</c> so the duplicate check in the validator is O(1).
        /// </summary>
        private HashSet<string> ExistingCodesSet => _existingCodesSet ??= [.. _accounts.Select(a => a.Code)];

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? value))
                return Enumerable.Empty<string>();
            return value;
        }

        private void SetPropertyErrors(string propertyName, IReadOnlyList<string> errors)
        {
            bool hadErrors = _errors.ContainsKey(propertyName);

            if (errors.Count > 0)
                _errors[propertyName] = [.. errors];
            else if (hadErrors)
                _errors.Remove(propertyName);

            if (hadErrors || errors.Count > 0)
                RaiseErrorsChanged(propertyName);
        }

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            NotifyOfPropertyChange(nameof(HasErrors));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        private AccountPlanValidationContext BuildValidationContext() => new()
        {
            IsNewRecord = IsNewRecord,
            CodeLength = _code.Length,
            Lv5Code = Lv5Code,
            Lv1Name = Lv1Name,
            Lv2Name = Lv2Name,
            Lv3Name = Lv3Name,
            Lv4Name = Lv4Name,
            Lv5Name = Lv5Name,
            Margin = Margin,
            RequiresMargin = RequiresMargin,
            ExistingCodes = ExistingCodesSet
        };

        private AccountPlanCanSaveContext BuildCanSaveContext() => new()
        {
            IsBusy = IsBusy,
            IsNewRecord = IsNewRecord,
            CodeLength = _code.Length,
            Lv5Code = Lv5Code,
            Lv1Name = Lv1Name,
            Lv2Name = Lv2Name,
            Lv3Name = Lv3Name,
            Lv4Name = Lv4Name,
            Lv5Name = Lv5Name,
            Margin = Margin,
            RequiresMargin = RequiresMargin,
            ExistingCodes = ExistingCodesSet
        };

        private void ValidateProperty(string propertyName, object? value)
        {
            IReadOnlyList<string> errors = _validator.Validate(propertyName, value, BuildValidationContext());
            SetPropertyErrors(propertyName, errors);
        }

        /// <summary>
        /// Re-runs validation for all fields in the current context. Useful after bulk
        /// state changes (mode switches, cascading populate from Lv5Code) where a single
        /// property notification would miss cross-field dependencies.
        /// </summary>
        private void ValidateProperties()
        {
            AccountPlanValidationContext context = BuildValidationContext();
            Dictionary<string, IReadOnlyList<string>> allErrors = _validator.ValidateAll(context);

            string[] allProps =
            [
                nameof(AccountPlanValidationContext.Lv5Code),
                nameof(AccountPlanValidationContext.Lv1Name),
                nameof(AccountPlanValidationContext.Lv2Name),
                nameof(AccountPlanValidationContext.Lv3Name),
                nameof(AccountPlanValidationContext.Lv4Name),
                nameof(AccountPlanValidationContext.Lv5Name),
                nameof(AccountPlanValidationContext.Margin)
            ];

            foreach (string prop in allProps)
            {
                IReadOnlyList<string> errs = allErrors.TryGetValue(prop, out IReadOnlyList<string>? list)
                    ? list
                    : [];
                SetPropertyErrors(prop, errs);
            }
        }

        #endregion

        public double DialogWidth
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogWidth));
                }
            }
        } = 600;

        public double DialogHeight
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(DialogHeight));
                }
            }
        } = 550;

        #endregion

        #region "Lifecycle"

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            PopulateInfo(Code);
            SetReadOnlyState(Code);
            ValidateProperties();
            NotifyOfPropertyChange(nameof(CanSave));
            if (view is FrameworkElement fe)
            {
                fe.Dispatcher.BeginInvoke(
                    new System.Action(() => SetLevelFocus(Code)),
                    System.Windows.Threading.DispatcherPriority.Loaded);
            }
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

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
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

            var fragment = new GraphQLQueryFragment("updateAccountingAccount",
                [new("data", "UpdateAccountingAccountInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

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

                (GraphQLQueryFragment fragment, string query) = _updateQuery.Value;

                dynamic variables = new GraphQLVariables()
                    .For(fragment, "id", model.Id)
                    .For(fragment, "data", new { model.Name, model.Margin, model.MarginBasis })
                    .Build();

                UpsertResponseType<AccountingAccountGraphQLModel> updatedAccount = await _accountingAccountService.UpdateAsync<UpsertResponseType<AccountingAccountGraphQLModel>>(query, variables);
                return updatedAccount;
            }
            catch (Exception)
            {
                throw;
            }
        }


        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _insertQuery = new(() =>
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

            var fragment = new GraphQLQueryFragment("createAccountingAccounts",
                [new("input", "[CreateAccountingAccountInput!]!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });
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
                    AccountingAccountGraphQLModel model = new()
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

                (GraphQLQueryFragment fragment, string query) = _insertQuery.Value;

                var modelsWithOutIds = from model in models
                                       select new { model.Code, model.Name, model.Nature, model.Margin, model.MarginBasis };

                dynamic variables = new GraphQLVariables()
                    .For(fragment, "input", modelsWithOutIds)
                    .Build();
                UpsertResponseType<List<AccountingAccountGraphQLModel>> response = await _accountingAccountService.CreateAsync<UpsertResponseType<List<AccountingAccountGraphQLModel>>>(query, variables);
                return response;
            }
            catch (Exception)
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
                    IEnumerable<AccountingAccountGraphQLModel> lv1 = from account
                    in _accounts
                              where account.Code == accountCode[..1]
                              select account;

                    if (lv1 != null && lv1.ToList().Count > 0)
                    {
                        this.Lv1Code = lv1.First().Code;
                        this.Lv1Name = lv1.First().Name;
                        this.SelectedAccountNature = lv1.First().Nature;
                    }
                    else
                    {
                        this.Lv1Code = accountCode[..1];
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
                ThemedMessageBox.Show("Atención!", $"{GetType().Name}.{nameof(PopulateInfoLv1)}: {ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PopulateInfoLv2(string accountCode)
        {
            // Lv2
            try
            {
                if (accountCode.Length >= 2)
                {
                    IEnumerable<AccountingAccountGraphQLModel> lv2 = from account
                    in _accounts
                              where account.Code == accountCode[..2]
                              select account;

                    if (lv2 != null && lv2.ToList().Count > 0)
                    {
                        this.Lv2Code = lv2.First().Code;
                        this.Lv2Name = lv2.First().Name;
                        this.SelectedAccountNature = lv2.First().Nature;
                    }
                    else
                    {
                        this.Lv2Code = accountCode[..2];
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
                ThemedMessageBox.Show("Atención!", $"{GetType().Name}.{nameof(PopulateInfoLv2)}: {ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
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
                              where account.Code == accountCode[..4]
                              select new { account.Code, account.Name, account.Nature };

                    if (lv3 != null && lv3.ToList().Count > 0)
                    {

                        this.Lv3Code = lv3.First().Code;
                        this.Lv3Name = lv3.First().Name;
                        this.SelectedAccountNature = lv3.First().Nature;
                    }
                    else
                    {
                        this.Lv3Code = accountCode[..4];
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
                ThemedMessageBox.Show("Atención!", $"{GetType().Name}.{nameof(PopulateInfoLv3)}: {ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PopulateInfoLv4(string accountCode)
        {
            // Lv4
            try
            {
                if (accountCode.Length >= 6)
                {
                    IEnumerable<AccountingAccountGraphQLModel> lv4 = from account
                    in _accounts
                              where account.Code == accountCode[..6]
                              select account;

                    if (lv4 != null && lv4.ToList().Count > 0)
                    {
                        this.Lv4Code = lv4.First().Code;
                        this.Lv4Name = lv4.First().Name;
                        this.SelectedAccountNature = lv4.First().Nature;
                    }
                    else
                    {
                        Lv4Code = accountCode[..6];
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
                ThemedMessageBox.Show("Atención!", $"{GetType().Name}.{nameof(PopulateInfoLv4)}: {ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PopulateInfoNameLv5(string accountCode)
        {
            try
            {
                if (accountCode.Length >= 8)
                {
                    IEnumerable<AccountingAccountGraphQLModel> lv5 = from account
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
                ThemedMessageBox.Show("Atención!", $"{GetType().Name}.{nameof(PopulateInfoNameLv5)}: {ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    IEnumerable<AccountingAccountGraphQLModel> lv1 = from account
                    in _accounts
                              where account.Code == accountCode[..1]
                              select account;

                    if (lv1 != null && lv1.ToList().Count > 0)
                    {
                        this.Lv1Code = lv1.First().Code;
                        this.Lv1Name = lv1.First().Name;
                        this.SelectedAccountNature = lv1.First().Nature;
                    }
                    else
                    {
                        this.Lv1Code = accountCode[..1];
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
                    IEnumerable<AccountingAccountGraphQLModel> lv2 = from account
                    in _accounts
                              where account.Code == accountCode[..2]
                              select account;

                    if (lv2 != null && lv2.ToList().Count > 0)
                    {
                        this.Lv2Code = lv2.First().Code;
                        this.Lv2Name = lv2.First().Name;
                        this.SelectedAccountNature = lv2.First().Nature;
                    }
                    else
                    {
                        this.Lv2Code = accountCode[..2];
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
                              where account.Code == accountCode[..4]
                              select new { account.Code, account.Name, account.Nature };

                    if (lv3 != null && lv3.ToList().Count > 0)
                    {

                        this.Lv3Code = lv3.First().Code;
                        this.Lv3Name = lv3.First().Name;
                        this.SelectedAccountNature = lv3.First().Nature;
                    }
                    else
                    {
                        this.Lv3Code = accountCode[..4];
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
                    IEnumerable<AccountingAccountGraphQLModel> lv4 = from account
                    in _accounts
                              where account.Code == accountCode[..6]
                              select account;

                    if (lv4 != null && lv4.ToList().Count > 0)
                    {
                        this.Lv4Code = lv4.First().Code;
                        this.Lv4Name = lv4.First().Name;
                        this.SelectedAccountNature = lv4.First().Nature;
                    }
                    else
                    {
                        Lv4Code = accountCode[..6];
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
                    IEnumerable<AccountingAccountGraphQLModel> lv5 = from account
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
                ThemedMessageBox.Show("Atención!", $"{GetType().Name}.{nameof(PopulateInfo)}: {ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
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
                ThemedMessageBox.Show("Atención!", $"{GetType().Name}.{nameof(SetLevelFocus)}: {ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    IsReadOnlyLv1Name = accountCode.Length != 1;
                    IsReadOnlyLv2Name = accountCode.Length != 2;
                    IsReadOnlyLv3Name = accountCode.Length != 4;
                    IsReadOnlyLv4Name = accountCode.Length != 6;
                    IsReadOnlyLv5Name = accountCode.Length < 8;
                }

                IsReadOnlyLv5Code = this.IsNewRecord ? false : true;

            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show("Atención!", $"{GetType().Name}.{nameof(SetReadOnlyState)}: {ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
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
                ThemedMessageBox.Show("Atención!", $"{GetType().Name}.{nameof(SetTextBoxSelectionForExistingAccountCode)}: {ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool AccountCodeExist(string accountCode)
        {
            try
            {
                return _accounts.Any(account => account.Code == accountCode);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void CleanUpControls()
        {
            Lv5Code = "";
            Lv1Code = "";
            Lv2Code = "";
            Lv3Code = "";
            Lv4Code = "";
            Lv1Name = "";
            Lv2Name = "";
            Lv3Name = "";
            Lv4Name = "";
            Lv5Name = "";
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
                IsBusy = true;
                if (IsNewRecord)
                {
                    UpsertResponseType<List<AccountingAccountGraphQLModel>> result = await InsertAsync();
                    if (!result.Success)
                    {
                        ThemedMessageBox.Show(
                            text: $"El guardado no ha sido exitoso\r\n\r\n{result.Errors.ToUserMessage()}\r\n\r\nVerifique los datos y vuelva a intentarlo",
                            title: $"{result.Message}!",
                            messageBoxButtons: MessageBoxButton.OK,
                            icon: MessageBoxImage.Error);
                        return;
                    }
                    await _eventAggregator.PublishOnCurrentThreadAsync(
                        new AccountingAccountCreateListMessage { UpsertList = result },
                        CancellationToken.None);
                }
                else
                {
                    if (_selectedItemId <= 0) throw new ArgumentException("No se ha seleccionado una cuenta contable para editar");
                    UpsertResponseType<AccountingAccountGraphQLModel> result = await UpdateAsync();
                    if (!result.Success)
                    {
                        ThemedMessageBox.Show(
                            text: $"El guardado no ha sido exitoso\r\n\r\n{result.Errors.ToUserMessage()}\r\n\r\nVerifique los datos y vuelva a intentarlo",
                            title: $"{result.Message}!",
                            messageBoxButtons: MessageBoxButton.OK,
                            icon: MessageBoxImage.Error);
                        return;
                    }
                    await _eventAggregator.PublishOnCurrentThreadAsync(
                        new AccountingAccountUpdateMessage { UpsertAccount = result },
                        CancellationToken.None);
                }
                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"{GetType().Name}.{nameof(SaveAsync)}: {ex.GetErrorMessage()}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        public bool CanSave => _validator.CanSave(BuildCanSaveContext());



        #endregion
    }
}
