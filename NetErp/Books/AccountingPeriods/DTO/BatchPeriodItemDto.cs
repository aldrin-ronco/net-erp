using System;
using System.Collections.Generic;
using System.Text;

namespace NetErp.Books.AccountingPeriods.DTO
{
    public class BatchPeriodItemDto
    {
        public int Month { get; set; }
        public int CostCenterId { get; set; }
        public string Status { get; set; }
        public int Year { get; set; }
    }
}
