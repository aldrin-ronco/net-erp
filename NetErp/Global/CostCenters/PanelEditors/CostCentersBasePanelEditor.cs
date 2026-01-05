using Caliburn.Micro;
using Common.Helpers;
using DevExpress.Xpf.Core;
using GraphQL.Client.Http;
using NetErp.Global.CostCenters.DTO;
using NetErp.Global.CostCenters.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NetErp.Global.CostCenters.PanelEditors
{
    /// <summary>
    /// Clase base abstracta para todos los Panel Editors del módulo CostCenters.
    /// Proporciona implementación común de INotifyDataErrorInfo, manejo de errores GraphQL,
    /// y template methods para operaciones CRUD.
    /// Prefijo "CostCenters" para evitar conflictos con otros módulos (ej: Treasury).
    /// </summary>
    /// <typeparam name="TDto">Tipo del DTO que implementa ICostCentersItems.</typeparam>
    /// <typeparam name="TGraphQLModel">Tipo del modelo GraphQL.</typeparam>
    public abstract class CostCentersBasePanelEditor<TDto, TGraphQLModel> : PropertyChangedBase, ICostCentersPanelEditor
        where TDto : class, ICostCentersItems, new()
        where TGraphQLModel : class
    {
        #region Fields

        protected readonly CostCenterMasterViewModel MasterContext;
        protected readonly Dictionary<string, List<string>> Errors = new();
        protected TDto? OriginalDto;

        #endregion

        #region Constructor

        protected CostCentersBasePanelEditor(CostCenterMasterViewModel masterContext)
        {
            MasterContext = masterContext ?? throw new ArgumentNullException(nameof(masterContext));
        }

        #endregion

        #region Estado - ICostCentersPanelEditor

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
                    NotifyOfPropertyChange(nameof(CanSave));
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
            NotifyOfPropertyChange(nameof(CanSave));
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
        protected abstract Task<TGraphQLModel> ExecuteSaveAsync();

        /// <summary>
        /// Publica el mensaje de evento correspondiente después de guardar.
        /// </summary>
        protected abstract Task PublishMessageAsync(TGraphQLModel result);

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

        #region Operaciones - ICostCentersPanelEditor

        /// <summary>
        /// Template method para guardar. Maneja errores y notificaciones.
        /// </summary>
        public async Task SaveAsync()
        {
            try
            {
                IsBusy = true;
                MasterContext.Refresh();

                TGraphQLModel result = await ExecuteSaveAsync();
                await PublishMessageAsync(result);

                IsEditing = false;
                this.AcceptChanges();
            }
            catch (GraphQLHttpRequestException exGraphQL)
            {
                HandleGraphQLError(exGraphQL, nameof(SaveAsync));
            }
            catch (Exception ex)
            {
                HandleGenericError(ex, nameof(SaveAsync));
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

        #region Helpers de Validación

        /// <summary>
        /// Limpia caracteres no numéricos de un teléfono para validación.
        /// </summary>
        protected static string CleanPhoneForValidation(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return new string(value.Where(char.IsDigit).ToArray());
        }

        /// <summary>
        /// Notifica cambio de CanSave (útil después de validaciones).
        /// </summary>
        protected void NotifyCanSaveChanged()
        {
            NotifyOfPropertyChange(nameof(CanSave));
        }

        #endregion
    }
}
