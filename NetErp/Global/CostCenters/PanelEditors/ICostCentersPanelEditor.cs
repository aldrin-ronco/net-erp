using System;
using System.Collections;
using System.ComponentModel;
using System.Threading.Tasks;

namespace NetErp.Global.CostCenters.PanelEditors
{
    /// <summary>
    /// Interface base para todos los Panel Editors del módulo CostCenters.
    /// Define el contrato común para edición inline de entidades.
    /// Prefijo "CostCenters" para evitar conflictos con otros módulos (ej: Treasury).
    /// </summary>
    public interface ICostCentersPanelEditor : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        #region Estado

        /// <summary>
        /// Indica si el editor está en modo de edición.
        /// Controla si los campos del panel están habilitados para edición.
        /// </summary>
        bool IsEditing { get; set; }

        /// <summary>
        /// Indica si es un registro nuevo (Id == 0).
        /// </summary>
        bool IsNewRecord { get; }

        /// <summary>
        /// Indica si se puede guardar (validaciones pasadas y hay cambios).
        /// </summary>
        bool CanSave { get; }

        /// <summary>
        /// Indica si hay una operación en progreso.
        /// </summary>
        bool IsBusy { get; set; }

        #endregion

        #region Operaciones

        /// <summary>
        /// Configura el editor para crear un nuevo registro.
        /// </summary>
        /// <param name="context">Contexto necesario (ej: CompanyLocationId para CostCenter).</param>
        void SetForNew(object context);

        /// <summary>
        /// Configura el editor para editar un registro existente.
        /// </summary>
        /// <param name="dto">El DTO con los datos a editar.</param>
        void SetForEdit(object dto);

        /// <summary>
        /// Guarda los cambios (Create o Update según IsNewRecord).
        /// </summary>
        Task SaveAsync();

        /// <summary>
        /// Deshace los cambios y sale del modo edición.
        /// </summary>
        void Undo();

        #endregion

        #region Validación

        /// <summary>
        /// Ejecuta todas las validaciones del editor.
        /// </summary>
        void ValidateAll();

        /// <summary>
        /// Limpia todos los errores de validación.
        /// </summary>
        void ClearAllErrors();

        #endregion
    }
}
