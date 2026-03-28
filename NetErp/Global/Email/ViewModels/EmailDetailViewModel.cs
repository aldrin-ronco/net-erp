using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Extensions.Global;
using Microsoft.VisualStudio.Threading;
using Models.Global;
using NetErp.Helpers;
using NetErp.Helpers.Cache;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static Models.Global.EmailGraphQLModel;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Global.Email.ViewModels
{
    public class EmailDetailViewModel : Screen, INotifyDataErrorInfo
    {
        #region Dependencies

        private readonly IRepository<EmailGraphQLModel> _emailService;
        private readonly SmtpCache _smtpCache;
        private readonly StringLengthCache _stringLengthCache;
        private readonly IEventAggregator _eventAggregator;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        #endregion

        #region Constructor

        public EmailDetailViewModel(
            IRepository<EmailGraphQLModel> emailService,
            IEventAggregator eventAggregator,
            SmtpCache smtpCache,
            StringLengthCache stringLengthCache,
            JoinableTaskFactory joinableTaskFactory)
        {
            _emailService = emailService;
            _eventAggregator = eventAggregator;
            _smtpCache = smtpCache;
            _stringLengthCache = stringLengthCache;
            _joinableTaskFactory = joinableTaskFactory;
        }

        public async Task InitializeAsync()
        {
            await _smtpCache.EnsureLoadedAsync();
            EmailSmtp = [.. _smtpCache.Items];
        }

        #endregion

        #region Properties

        public int Id { get; set; }

        public bool IsNewRecord => Id == 0;

        public string PasswordPlaceholder => !IsNewRecord
            ? "Deje vacío para continuar con la antigua contraseña"
            : string.Empty;

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
        } = 500;

        public ObservableCollection<SmtpGraphQLModel> EmailSmtp
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(EmailSmtp));
                }
            }
        } = [];

        [ExpandoPath("smtpId", SerializeAsId = true)]
        public SmtpGraphQLModel? SelectedSmtp
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(SelectedSmtp));
                    this.TrackChange(nameof(SelectedSmtp));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }

        public string Description
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Description));
                    this.TrackChange(nameof(Description));
                    ValidateProperty(nameof(Description), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string Email
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Email));
                    this.TrackChange(nameof(Email));
                    ValidateProperty(nameof(Email), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public string Password
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Password));
                    this.TrackChange(nameof(Password));
                    ValidateProperty(nameof(Password), value);
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = string.Empty;

        public new bool IsActive
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsActive));
                    this.TrackChange(nameof(IsActive));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        } = true;

        public bool IsCorporate { get; } = true;

        public bool IsElectronicInvoiceRecipient { get; } = false;

        public bool IsBusy
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        #endregion

        #region StringLength Properties

        public int DescriptionMaxLength => _stringLengthCache.GetMaxLength<EmailGraphQLModel>(nameof(EmailGraphQLModel.Description));
        public int EmailMaxLength => _stringLengthCache.GetMaxLength<EmailGraphQLModel>(nameof(EmailGraphQLModel.Email));
        public int PasswordMaxLength => _stringLengthCache.GetMaxLength<EmailGraphQLModel>(nameof(EmailGraphQLModel.Password));

        #endregion

        #region Commands

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

        public bool CanSave
        {
            get
            {
                if (string.IsNullOrEmpty(Email)) return false;
                if (string.IsNullOrEmpty(Description)) return false;
                if (SelectedSmtp is null) return false;
                if (IsNewRecord && string.IsNullOrEmpty(Password)) return false;
                if (!this.HasChanges()) return false;
                return _errors.Count <= 0;
            }
        }

        #endregion

        #region Save / Cancel

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                UpsertResponseType<EmailGraphQLModel> result = await ExecuteSaveAsync();

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
                    IsNewRecord
                        ? new EmailCreateMessage { CreatedEmail = result }
                        : new EmailUpdateMessage { UpdatedEmail = result },
                    CancellationToken.None);

                await TryCloseAsync(true);
            }
            catch (Exception ex)
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();
                ThemedMessageBox.Show(
                    title: "Atención!",
                    text: $"Error al realizar operación.\r\n{GetType().Name}.{nameof(SaveAsync)}: {ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<UpsertResponseType<EmailGraphQLModel>> ExecuteSaveAsync()
        {
            if (IsNewRecord)
            {
                var (_, query) = _createQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "createResponseInput");
                return await _emailService.CreateAsync<UpsertResponseType<EmailGraphQLModel>>(query, variables);
            }
            else
            {
                var (_, query) = _updateQuery.Value;
                dynamic variables = ChangeCollector.CollectChanges(this, prefix: "updateResponseData");
                variables.updateResponseId = Id;
                return await _emailService.UpdateAsync<UpsertResponseType<EmailGraphQLModel>>(query, variables);
            }
        }

        public async Task CancelAsync()
        {
            await TryCloseAsync(false);
        }

        #endregion

        #region Lifecycle

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            ValidateProperties();
            this.AcceptChanges();
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion

        #region INotifyDataErrorInfo

        private readonly Dictionary<string, List<string>> _errors = [];

        public bool HasErrors => _errors.Count > 0;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? value)) return Enumerable.Empty<string>();
            return value;
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = [];

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }

        private void ValidateProperty(string propertyName, string value)
        {
            ClearErrors(propertyName);
            switch (propertyName)
            {
                case nameof(Email):
                    if (string.IsNullOrEmpty(Email)) AddError(propertyName, "El campo 'Correo electrónico' no puede estar vacío.");
                    break;
                case nameof(Password):
                    if (IsNewRecord && string.IsNullOrEmpty(Password)) AddError(propertyName, "El campo 'Contraseña' no puede estar vacío.");
                    break;
                case nameof(Description):
                    if (string.IsNullOrEmpty(Description)) AddError(propertyName, "El campo 'Descripción' no puede estar vacío.");
                    break;
                default:
                    break;
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(Email), Email);
            ValidateProperty(nameof(Password), Password);
            ValidateProperty(nameof(Description), Description);
        }

        #endregion

        #region SetForNew / SetForEdit

        public void SetForNew()
        {
            Id = 0;
            Email = string.Empty;
            Description = string.Empty;
            Password = string.Empty;
            SelectedSmtp = null;

            SeedDefaultValues();
        }

        public void SetForEdit()
        {
            SeedCurrentValues();
        }

        private void SeedDefaultValues()
        {
            this.ClearSeeds();
            this.SeedValue(nameof(Email), Email);
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(Password), Password);
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(IsCorporate), IsCorporate);
            this.SeedValue(nameof(IsElectronicInvoiceRecipient), IsElectronicInvoiceRecipient);
            this.AcceptChanges();
        }

        private void SeedCurrentValues()
        {
            this.SeedValue(nameof(Email), Email);
            this.SeedValue(nameof(Description), Description);
            this.SeedValue(nameof(Password), Password);
            if (SelectedSmtp is not null) this.SeedValue(nameof(SelectedSmtp), SelectedSmtp);
            this.SeedValue(nameof(IsActive), IsActive);
            this.SeedValue(nameof(IsCorporate), IsCorporate);
            this.SeedValue(nameof(IsElectronicInvoiceRecipient), IsElectronicInvoiceRecipient);
            this.AcceptChanges();
        }

        #endregion

        #region Load for Edit

        public async Task LoadDataForEditAsync(int id)
        {
            var (_, query) = _loadByIdQuery.Value;

            dynamic variables = new ExpandoObject();
            variables.singleItemResponseId = id;

            EmailGraphQLModel entity = await _emailService.FindByIdAsync(query, variables);

            Id = entity.Id;
            Description = entity.Description;
            Email = entity.Email;
            Password = entity.Password;
            IsActive = entity.IsActive;
            SelectedSmtp = entity.Smtp is null
                ? null
                : EmailSmtp.FirstOrDefault(c => c.Id == entity.Smtp.Id);
        }

        #endregion

        #region GraphQL Queries

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _createQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<EmailGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "email", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(e => e.Email))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("createEmail",
                [new("input", "CreateEmailInput!")],
                fields, "CreateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _updateQuery = new(() =>
        {
            var fields = FieldSpec<UpsertResponseType<EmailGraphQLModel>>
                .Create()
                .Select(selector: f => f.Entity, alias: "entity", overrideName: "email", nested: sq => sq
                    .Field(e => e.Id)
                    .Field(e => e.Description)
                    .Field(f => f.Email))
                .Field(f => f.Message)
                .Field(f => f.Success)
                .SelectList(f => f.Errors, sq => sq
                    .Field(f => f.Fields)
                    .Field(f => f.Message))
                .Build();

            var fragment = new GraphQLQueryFragment("updateEmail",
                [new("data", "UpdateEmailInput!"), new("id", "ID!")],
                fields, "UpdateResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        });

        private static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> _loadByIdQuery = new(() =>
        {
            var fields = FieldSpec<EmailGraphQLModel>
                .Create()
                .Field(e => e.Id)
                .Field(e => e.Description)
                .Field(e => e.Email)
                .Field(e => e.IsActive)
                .Select(e => e.Smtp, cat => cat
                    .Field(c => c!.Id)
                    .Field(c => c!.Name))
                .Build();

            var fragment = new GraphQLQueryFragment("email",
                [new("id", "ID!")],
                fields, "SingleItemResponse");
            return (fragment, new GraphQLQueryBuilder([fragment]).GetQuery());
        });

        #endregion
    }
}
