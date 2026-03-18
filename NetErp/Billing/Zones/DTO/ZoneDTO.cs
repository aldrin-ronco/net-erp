using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Billing.Zones.DTO
{
   public class ZoneDTO : Screen
    {
        public bool IsSelected
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    NotifyOfPropertyChange(nameof(IsSelected));
                }
            }
        }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public new bool IsActive { get; set; }
    }
}
