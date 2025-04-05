using Common.Interfaces;
using Models.Billing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Billing.DAL.PostgreSQL
{
    public class PriceListService: IGenericDataAccess<PriceListGraphQLModel>
    {
    }

    public class PriceListDetailService: IGenericDataAccess<PriceListDetailGraphQLModel>
    {
    }
}
