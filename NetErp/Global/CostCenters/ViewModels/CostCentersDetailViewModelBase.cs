using Caliburn.Micro;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace NetErp.Global.CostCenters.ViewModels
{
    /// <summary>
    /// Clase base abstracta para los DetailViewModels dialog-based del módulo CostCenters.
    /// Provee implementación común de INotifyDataErrorInfo, SetPropertyErrors atómico,
    /// IsBusy, DialogWidth/Height y helpers de tab tooltip.
    /// Cada VM concreto implementa CanSave, ExecuteSaveAsync y los seeds.
    /// </summary>
    public abstract class CostCentersDetailViewModelBase : Screen, INotifyDataErrorInfo
    {
        protected readonly JoinableTaskFactory _joinableTaskFactory;
        protected readonly IEventAggregator _eventAggregator;
        protected readonly Dictionary<string, List<string>> _errors = [];

        protected CostCentersDetailViewModelBase(
            JoinableTaskFactory joinableTaskFactory,
            IEventAggregator eventAggregator)
        {
            _joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException(nameof(joinableTaskFactory));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        }

        #region Dialog Size

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
        } = 500;

        #endregion

        #region IsBusy

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

        #region Identity

        public int Id
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(Id));
                    NotifyOfPropertyChange(nameof(IsNewRecord));
                }
            }
        }

        public bool IsNewRecord => Id == 0;

        #endregion

        #region CanSave (abstract)

        public abstract bool CanSave { get; }

        #endregion

        #region INotifyDataErrorInfo

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out List<string>? value))
                return Enumerable.Empty<string>();
            return value;
        }

        /// <summary>
        /// Actualiza los errores de una propiedad de forma atómica: primero muta el diccionario,
        /// luego dispara una única notificación. Evita estados intermedios que rompen tab tooltips.
        /// </summary>
        protected void SetPropertyErrors(string propertyName, IReadOnlyList<string> errors)
        {
            bool hadErrors = _errors.ContainsKey(propertyName);

            if (errors.Count > 0)
                _errors[propertyName] = [.. errors];
            else if (hadErrors)
                _errors.Remove(propertyName);

            if (hadErrors || errors.Count > 0)
                RaiseErrorsChanged(propertyName);
        }

        protected virtual void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            NotifyOfPropertyChange(nameof(HasErrors));
            NotifyOfPropertyChange(nameof(CanSave));
        }

        /// <summary>
        /// Helper para computar tooltip agregado de un grupo de campos (usado en indicadores de tab).
        /// </summary>
        protected string? GetTabTooltip(string[] fields)
        {
            List<string> errors = [.. fields
                .Where(f => _errors.ContainsKey(f))
                .SelectMany(f => _errors[f])];
            return errors.Count > 0 ? string.Join("\n", errors) : null;
        }

        #endregion
    }
}
