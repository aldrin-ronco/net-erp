using Models.Global;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Books
{
    public class AccountingPeriodGraphQLModel
    {
        public int Id { get; set; } = 0;
        public CompanyGraphQLModel Company { get; set; }
        public CostCenterGraphQLModel CostCenter { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }

        public string Status { get; set; }
       




    }

  

}
