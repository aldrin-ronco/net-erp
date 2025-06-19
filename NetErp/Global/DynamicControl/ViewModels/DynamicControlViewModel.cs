using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Dynamic;
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

        public List<dynamic> GetDataControls()
        {
            List<dynamic> _controls = [];
            foreach(DynamicControlModel control in Controls)
            {
                dynamic c = new ExpandoObject();
                c.id = control.Id;
                c.dataTypeId = control.Datatype.Id;
                c.jsonValue = control.Value;
                _controls.Add(c);
            }
            return _controls;
        }
    }
}
