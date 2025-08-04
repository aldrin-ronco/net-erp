using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetErp.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime DateTimeKindUTC(DateTime? dateTime)
        {
            //TODO: Handle null case more gracefully
            if (dateTime == null) throw new Exception("");
            return DateTime.SpecifyKind(dateTime!.Value, DateTimeKind.Utc);
        }
    }
}
