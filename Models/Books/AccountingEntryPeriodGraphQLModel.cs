using Models.Billing;
using Models.Global;
using System;
using System.Collections.Generic;
using System.Text;
using static Models.Global.GraphQLResponseTypes;

namespace Models.Books
{
    public class AccountingEntryPeriodGraphQLModel
    {
        public int Id { get; set; } = 0;
        public CostCenterGraphQLModel CostCenter { get; set; }

        public int Month { get; set; }
        public int Year { get; set; }
        public AccountingPeriodGraphQLModel? AccountingPeriod { get; set; }
    }
    public class ZoneCreateMessage
    {
        public UpsertResponseType<ZoneGraphQLModel> CreatedZone { get; set; } = new();
    }
    public class SelectedCostCentersMessage
    {
        public IList<CostCenterGraphQLModel> SelectedCostCenters { get; set; } = new List<CostCenterGraphQLModel>();
    }
}
