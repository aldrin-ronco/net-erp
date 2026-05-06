using Models.Inventory;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.UserControls.Helpers
{
    internal static class ItemStockByStorageQueries
    {
        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> StockTotalsByItem = new(() =>
        {
            var fields = FieldSpec<PageType<StockTotalGraphQLModel>>
                .Create()
                .Field(p => p.TotalEntries)
                .SelectList(p => p.Entries, e => e
                    .Field(s => s.Dimension)
                    .Field(s => s.Quantity)
                    .Select(s => s.Storage, st => st
                        .Field(g => g.Id)
                        .Field(g => g.Name)))
                .Build();

            var fragment = new GraphQLQueryFragment("stockTotalsPage",
                [new("pagination", "Pagination"), new("filters", "StockTotalFilters")],
                fields, "PageResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery());
        });
    }
}
