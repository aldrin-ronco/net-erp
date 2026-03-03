using Caliburn.Micro;
using Common.Helpers;
using DevExpress.Xpf.Core;
using Extensions.Global;
using GraphQL.Client.Http;
using NetErp.Inventory.CatalogItems.DTO;
using NetErp.Inventory.CatalogItems.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Inventory.CatalogItems.PanelEditors
{
    public abstract class CatalogItemsBasePanelEditor<TDto, TGraphQLModel> : PropertyChangedBase, ICatalogItemsPanelEditor
        where TDto : class, ICatalogItem, new()
        where TGraphQLModel : class
    {
        #region Fields

        protected readonly CatalogRootMasterViewModel MasterContext;
        protected readonly Dictionary<string, List<string>> Errors = new();
        protected TDto? OriginalDto;

        #endregion

        #region Constructor

        protected CatalogItemsBasePanelEditor(CatalogRootMasterViewModel masterContext)
        {
            MasterContext = masterContext ?? throw new ArgumentNullException(nameof(masterContext));
            SubscribeToMasterPropertyChanged();
        }

        #endregion

        #region Estado

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    NotifyOfPropertyChange(nameof(IsEditing));
                    MasterContext.RefreshCanSave();
                }
            }
        }

        public bool IsNewRecord => GetId() == 0;

        public abstract bool CanSave { get; }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    NotifyOfPropertyChange(nameof(IsBusy));
                }
            }
        }

        #endregion

        #region INotifyDataErrorInfo

        public bool HasErrors => Errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !Errors.ContainsKey(propertyName))
                return Enumerable.Empty<string>();
            return Errors[propertyName];
        }

        protected void AddError(string propertyName, string error)
        {
            if (!Errors.ContainsKey(propertyName))
                Errors[propertyName] = new List<string>();

            if (!Errors[propertyName].Contains(error))
            {
                Errors[propertyName].Add(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        protected void ClearErrors(string propertyName)
        {
            if (Errors.ContainsKey(propertyName))
            {
                Errors.Remove(propertyName);
                RaiseErrorsChanged(propertyName);
            }
        }

        public void ClearAllErrors()
        {
            var propertyNames = Errors.Keys.ToList();
            Errors.Clear();
            foreach (var propertyName in propertyNames)
            {
                RaiseErrorsChanged(propertyName);
            }
        }

        protected void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            NotifyOfPropertyChange(nameof(HasErrors));
            MasterContext.RefreshCanSave();
        }

        #endregion

        #region Abstract Methods

        protected abstract int GetId();
        protected abstract string GetCreateQuery();
        protected abstract string GetUpdateQuery();
        protected abstract Task<UpsertResponseType<TGraphQLModel>> ExecuteSaveAsync();
        protected abstract Task PublishMessageAsync(UpsertResponseType<TGraphQLModel> result);
        public abstract void SetForNew(object context);
        public abstract void SetForEdit(object dto);
        public abstract void ValidateAll();

        #endregion

        #region Operations

        public virtual async Task<bool> SaveAsync()
        {
            try
            {
                IsBusy = true;
                MasterContext.Refresh();

                UpsertResponseType<TGraphQLModel> result = await ExecuteSaveAsync();

                if (!result.Success)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        ThemedMessageBox.Show(
                            title: $"{result.Message}!",
                            text: $"El guardado no ha sido exitoso \n\n {result.Errors.ToUserMessage()} \n\n Verifique los datos y vuelva a intentarlo",
                            messageBoxButtons: MessageBoxButton.OK,
                            image: MessageBoxImage.Error));
                    return false;
                }

                await PublishMessageAsync(result);

                IsEditing = false;
                this.AcceptChanges();
                return true;
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                HandleGraphQLError(exGraphQL, nameof(SaveAsync));
                return false;
            }
            catch (Exception ex)
            {
                HandleGenericError(ex, nameof(SaveAsync));
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public virtual void Undo()
        {
            if (IsNewRecord)
            {
                MasterContext.SelectedItem = null;
            }
            else if (OriginalDto != null)
            {
                SetForEdit(OriginalDto);
            }

            IsEditing = false;
            this.AcceptChanges();
        }

        #endregion

        #region Error Handling

        protected void HandleGraphQLError(GraphQLHttpRequestException exGraphQL, string methodName)
        {
            GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(
                exGraphQL.Content ?? "");

            if (graphQLError != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    ThemedMessageBox.Show(
                        title: "Atenci\u00f3n!",
                        text: $"{GetType().Name}.{methodName}\r\n{graphQLError.Errors[0].Message}",
                        messageBoxButtons: MessageBoxButton.OK,
                        image: MessageBoxImage.Error));
            }
            else
            {
                throw exGraphQL;
            }
        }

        protected void HandleGenericError(Exception ex, string methodName)
        {
            Application.Current.Dispatcher.Invoke(() =>
                ThemedMessageBox.Show(
                    title: "Atenci\u00f3n!",
                    text: $"{GetType().Name}.{methodName}\r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
        }

        #endregion

        #region Master Delegation Properties

        public ICommand EditCommand => MasterContext.EditCommand;
        public ICommand UndoCommand => MasterContext.UndoCommand;
        public ICommand SaveCommand => MasterContext.SaveCommand;
        public bool MasterCanEdit => MasterContext.CanEdit;
        public bool MasterCanUndo => MasterContext.CanUndo;
        public bool MasterCanSave => MasterContext.CanSave;

        public int MasterSelectedIndex
        {
            get => MasterContext.SelectedIndex;
            set => MasterContext.SelectedIndex = value;
        }

        private bool _masterPropertyChangedSubscribed;

        protected void SubscribeToMasterPropertyChanged()
        {
            if (_masterPropertyChangedSubscribed) return;
            MasterContext.PropertyChanged += OnMasterPropertyChanged;
            _masterPropertyChangedSubscribed = true;
        }

        private void OnMasterPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MasterContext.CanEdit):
                    NotifyOfPropertyChange(nameof(MasterCanEdit));
                    break;
                case nameof(MasterContext.CanUndo):
                    NotifyOfPropertyChange(nameof(MasterCanUndo));
                    break;
                case nameof(MasterContext.CanSave):
                    NotifyOfPropertyChange(nameof(MasterCanSave));
                    break;
                case nameof(MasterContext.SelectedIndex):
                    NotifyOfPropertyChange(nameof(MasterSelectedIndex));
                    break;
                case nameof(MasterContext.IsEditing):
                    NotifyOfPropertyChange(nameof(IsEditing));
                    break;
            }
        }

        #endregion
    }
}
