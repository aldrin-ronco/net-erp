using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Global.DynamicControl.ViewModels
{
    public class DynamicControlViewModel : Screen
    {
        private BindableCollection<DynamicControlModel> _controls { get; set; }
        public BindableCollection<DynamicControlModel> Controls
        {
            get { return _controls; }
            set
            {
                if (_controls != value)
                {
                    _controls = value;
                    NotifyOfPropertyChange(nameof(Controls));
                }
            }
        }
    }
}
