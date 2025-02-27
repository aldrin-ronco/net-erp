using Caliburn.Micro;
using Common.Extensions;
using Common.Interfaces;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Models.Global;
using NetErp.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NetErp.Global.Smtp.ViewModels
{
    public class SmtpDetailViewModel : Screen, INotifyDataErrorInfo
    {
        IGenericDataAccess<SmtpGraphQLModel> SmtpService = IoC.Get<IGenericDataAccess<SmtpGraphQLModel>>();
        public SmtpViewModel Context { get; set; }
        public bool IsNewRecord => SmtpId == 0;

        Dictionary<string, List<string>> _errors;
        public SmtpDetailViewModel(SmtpViewModel context)
        {
            Context = context;
            _errors = new Dictionary<string, List<string>>();
        }

        protected override void OnViewReady(object view)
        {
            base.OnViewReady(view);
            this.SetFocus(() => SmtpName);
            ValidateProperties();
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


        private int _smtpId;

        public int SmtpId
        {
            get { return _smtpId; }
            set 
            {
                if (_smtpId != value) 
                {
                    _smtpId = value;
                    NotifyOfPropertyChange(nameof(SmtpId));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }


        private string _smtpName = string.Empty;

        public string SmtpName
        {
            get { return _smtpName; }
            set
            {
                if (_smtpName != value)
                {
                    _smtpName = value;
                    NotifyOfPropertyChange(nameof(SmtpName));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(SmtpName), value);
                }
            }
        }

        private string _smtpHost = string.Empty;

        public string SmtpHost
        {
            get { return _smtpHost; }
            set
            {
                if (_smtpHost != value)
                {
                    _smtpHost = value;
                    NotifyOfPropertyChange(nameof(SmtpHost));
                    NotifyOfPropertyChange(nameof(CanSave));
                    ValidateProperty(nameof(SmtpHost), value);
                }
            }
        }

        private int _smtpPort;

        public int SmtpPort
        {
            get { return _smtpPort; }
            set
            {
                if (_smtpPort != value)
                {
                    _smtpPort = value;
                    NotifyOfPropertyChange(nameof(SmtpPort));
                    NotifyOfPropertyChange(nameof(CanSave));
                }
            }
        }


        private ICommand? _goBackCommand;

        public ICommand? GoBackCommand
        {
            get
            {
                if (_goBackCommand is null) _goBackCommand = new AsyncCommand(GoBackAsync);
                return _goBackCommand;
            }
        }

        public async Task GoBackAsync()
        {
            await Context.ActivateMasterView();
        }

        public bool CanSave
        {
            get
            {
                if (string.IsNullOrEmpty(SmtpName) || string.IsNullOrEmpty(SmtpHost) || SmtpPort == 0) return false;
                return true;
            }
        }

        private ICommand? _saveCommand;

        public ICommand? SaveCommand
        {
            get
            {
                if (_saveCommand is null) _saveCommand = new AsyncCommand(SaveAsync);
                return _saveCommand;
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                SmtpGraphQLModel result = await ExecuteSaveAsync();
                if (IsNewRecord)
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new SmtpCreateMessage() { CreatedSmtp = result });
                }
                else
                {
                    await Context.EventAggregator.PublishOnUIThreadAsync(new SmtpUpdateMessage() { UpdatedSmtp = result });
                }
                await Context.ActivateMasterView();
            }
            catch (Exception)
            {

                throw;
            }
            finally 
            {
                IsBusy = false;
            }
        }

        public async Task<SmtpGraphQLModel> ExecuteSaveAsync()
        {
            dynamic variables = new ExpandoObject();
            variables.data = new ExpandoObject();
            if (!IsNewRecord) variables.id = SmtpId;
            variables.data.name = SmtpName;
            variables.data.host = SmtpHost;
            variables.data.port = SmtpPort;            

            string query = IsNewRecord ? @"
            mutation($data: CreateOrEditEmailInput!){
                CreateResponse: createSmtp(data: $data){
                id
                name
                host
                port
                }
            }" :
            @"
            mutation($id: Int!, $data: UpdateSmtpInput! ){
                UpdateResponse: updateSmtp(id: $id, data: $data){
                id
                name
                host
                port
                }
            }";
            var result = IsNewRecord ? await SmtpService.Create(query, variables) : await SmtpService.Update(query, variables);
            return result;
        }

        public void CleanUpControls()
        {
            SmtpId = 0;
            SmtpName = string.Empty;
            SmtpHost = string.Empty;
            SmtpPort = 0;
        }

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName)) return null;
            return _errors[propertyName];
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

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
            if (string.IsNullOrEmpty(value)) value = string.Empty.Trim();
            try
            {
                ClearErrors(propertyName);
                switch (propertyName)
                {
                    case nameof(SmtpName):
                        if (string.IsNullOrEmpty(SmtpName)) AddError(propertyName, "El nombre no puede estar vacío");
                        break;
                    case nameof(SmtpHost):
                        if (string.IsNullOrEmpty(SmtpHost)) AddError(propertyName, "El host no puede estar vacío");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Invoke(() => ThemedMessageBox.Show("Atención !", $"{GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name.Between("<", ">")} \r\n{ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private void ValidateProperties()
        {
            ValidateProperty(nameof(SmtpName), SmtpName);
            ValidateProperty(nameof(SmtpHost), SmtpHost);
        }
    }
}
