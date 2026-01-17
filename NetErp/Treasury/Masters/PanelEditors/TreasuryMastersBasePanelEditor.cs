using Caliburn.Micro;
using Common.Helpers;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using NetErp.Treasury.Masters.DTO;
using NetErp.Treasury.Masters.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Treasury.Masters.PanelEditors
{
    /// <summary>
    /// Clase base abstracta para todos los Panel Editors del módulo Treasury Masters.
    /// Proporciona implementación común de INotifyDataErrorInfo, manejo de errores GraphQL,
    /// y template methods para operaciones CRUD.
    /// Prefijo "TreasuryMasters" para evitar conflictos con otros módulos.
    /// </summary>
    /// <typeparam name="TDto">Tipo del DTO que implementa ITreasuryTreeMasterSelectedItem.</typeparam>
    /// <typeparam name="TGraphQLModel">Tipo del modelo GraphQL.</typeparam>
    public abstract class TreasuryMastersBasePanelEditor<TDto, TGraphQLModel> : PropertyChangedBase, ITreasuryMastersPanelEditor
        where TDto : class, ITreasuryTreeMasterSelectedItem, new()
        where TGraphQLModel : class
    {
        #region Fields

        protected readonly TreasuryRootMasterViewModel MasterContext;
        protected readonly Dictionary<string, List<string>> Errors = new();
        protected TDto? OriginalDto;

        #endregion

        #region Constructor

        protected TreasuryMastersBasePanelEditor(TreasuryRootMasterViewModel masterContext)
        {
            MasterContext = masterContext ?? throw new ArgumentNullException(nameof(masterContext));
        }

        #endregion

        #region Estado - ITreasuryMastersPanelEditor

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

        #region Métodos Abstractos

        /// <summary>
        /// Obtiene el ID del registro actual.
        /// </summary>
        protected abstract int GetId();

        /// <summary>
        /// Obtiene el query GraphQL para crear un nuevo registro.
        /// </summary>
        protected abstract string GetCreateQuery();

        /// <summary>
        /// Obtiene el query GraphQL para actualizar un registro existente.
        /// </summary>
        protected abstract string GetUpdateQuery();

        /// <summary>
        /// Ejecuta la operación de guardado (Create o Update).
        /// </summary>
        protected abstract Task<UpsertResponseType<TGraphQLModel>> ExecuteSaveAsync();

        /// <summary>
        /// Publica el mensaje de evento correspondiente después de guardar.
        /// </summary>
        protected abstract Task PublishMessageAsync(UpsertResponseType<TGraphQLModel> result);

        /// <summary>
        /// Configura el editor para un nuevo registro.
        /// </summary>
        public abstract void SetForNew(object context);

        /// <summary>
        /// Configura el editor para editar un registro existente.
        /// </summary>
        public abstract void SetForEdit(object dto);

        /// <summary>
        /// Ejecuta todas las validaciones del editor.
        /// </summary>
        public abstract void ValidateAll();

        #endregion

        #region Operaciones - ITreasuryMastersPanelEditor

        /// <summary>
        /// Template method para guardar. Maneja errores y notificaciones.
        /// </summary>
        /// <returns>True si el guardado fue exitoso, False en caso contrario.</returns>
        public async Task<bool> SaveAsync()
        {
            try
            {
                IsBusy = true;
                MasterContext.Refresh();

                UpsertResponseType<TGraphQLModel> result = await ExecuteSaveAsync();
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

        /// <summary>
        /// Deshace los cambios y restaura el estado original.
        /// Solo maneja la lógica de datos del panel.
        /// MasterViewModel se encarga de CanEdit/CanUndo del Ribbon.
        /// </summary>
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

        #region Manejo de Errores

        protected void HandleGraphQLError(GraphQLHttpRequestException exGraphQL, string methodName)
        {
            GraphQLError? graphQLError = Newtonsoft.Json.JsonConvert.DeserializeObject<GraphQLError>(
                exGraphQL.Content ?? "");

            if (graphQLError != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    ThemedMessageBox.Show(
                        title: "Atención!",
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
                    title: "Atención!",
                    text: $"{GetType().Name}.{methodName}\r\n{ex.Message}",
                    messageBoxButtons: MessageBoxButton.OK,
                    image: MessageBoxImage.Error));
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Notifica cambio de CanSave (útil después de validaciones).
        /// </summary>
        protected void NotifyCanSaveChanged()
        {
            MasterContext.RefreshCanSave();
        }

        #endregion
    }
}
