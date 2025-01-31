using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Billing
{
    public class CreditLimitGraphQLModel
    {
        public int Id { get; set; }
        public CustomerGraphQLModel Customer { get; set; }
        public decimal Limit { get; set; }
        public decimal Used { get; set; }
        public decimal Available { get; set; }
        public decimal OriginalLimit { get; set; }
    }

    public class CreditLimitManagerMessage
    {
        public IEnumerable<CreditLimitGraphQLModel> ManagedCreditLimits { get; set; }
    }
}
