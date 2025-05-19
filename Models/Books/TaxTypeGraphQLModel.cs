using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Books
{
    public class TaxTypeGraphQLModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool GeneratedTaxAccountIsRequired { get; set; }
        public bool GeneratedTaxRefundAccountIsRequired { get; set; }
        public bool DeductibleTaxAccountIsRequired { get; set; }
        public bool DeductibleTaxRefundAccountIsRequired { get; set; }
        public string Prefix { get; set; } = string.Empty;
    }
}
