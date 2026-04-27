using Models.Books;
using Models.Global;
using Models.Inventory;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.Inventory.StockMovementsIn.Helpers
{
    /// <summary>
    /// Lazy GraphQL queries y mutations para el módulo Stock Movements In.
    /// Centraliza la construcción de queries para reutilizar entre Master, NewDialog y Detail.
    /// </summary>
    internal static class StockMovementInQueries
    {
        // -------- Master listing --------
        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> StockMovementsPage = new(() =>
        {
            var fields = FieldSpec<PageType<StockMovementGraphQLModel>>
                .Create()
                .Field(p => p.PageNumber)
                .Field(p => p.PageSize)
                .Field(p => p.TotalPages)
                .Field(p => p.TotalEntries)
                .SelectList(p => p.Entries, e => e
                    .Field(s => s.Id)
                    .Field(s => s.DocumentNumber)
                    .Field(s => s.Status)
                    .Field(s => s.CancelledWith)
                    .Field(s => s.Note)
                    .Field(s => s.PostedAt)
                    .Field(s => s.CancelledAt)
                    .Field(s => s.InsertedAt)
                    .Select(s => s.AccountingSource, asrc => asrc
                        .Field(a => a.Id)
                        .Field(a => a.Name)
                        .Field(a => a.Code))
                    .Select(s => s.CostCenter, cc => cc
                        .Field(c => c.Id)
                        .Field(c => c.Name))
                    .Select(s => s.Storage, st => st
                        .Field(g => g.Id)
                        .Field(g => g.Name)))
                .Build();

            var fragment = new GraphQLQueryFragment("stockMovementsPage",
                [new("pagination", "Pagination"), new("filters", "StockMovementFilters")],
                fields, "PageResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery());
        });

        // -------- AccountingSource (filtro kardexFlow=I + annulment=false) --------
        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> InboundAccountingSources = new(() =>
        {
            var fields = FieldSpec<PageType<AccountingSourceGraphQLModel>>
                .Create()
                .SelectList(p => p.Entries, e => e
                    .Field(a => a.Id)
                    .Field(a => a.Code)
                    .Field(a => a.Name)
                    .Field(a => a.KardexFlow))
                .Build();

            var fragment = new GraphQLQueryFragment("accountingSourcesPage",
                [new("pagination", "Pagination"), new("filters", "AccountingSourceFilters")],
                fields, "PageResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery());
        });

        // -------- Storages --------
        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> Storages = new(() =>
        {
            var fields = FieldSpec<PageType<StorageGraphQLModel>>
                .Create()
                .SelectList(p => p.Entries, e => e
                    .Field(s => s.Id)
                    .Field(s => s.Name)
                    .Field(s => s.Status))
                .Build();

            var fragment = new GraphQLQueryFragment("storagesPage",
                [new("pagination", "Pagination"), new("filters", "StorageFilters")],
                fields, "PageResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery());
        });

        // -------- Detail by id --------
        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> StockMovementById = new(() =>
        {
            var fields = FieldSpec<StockMovementGraphQLModel>
                .Create()
                .Field(s => s.Id)
                .Field(s => s.DocumentNumber)
                .Field(s => s.Status)
                .Field(s => s.CancelledWith)
                .Field(s => s.Note)
                .Field(s => s.PostedAt)
                .Field(s => s.CancelledAt)
                .Field(s => s.InsertedAt)
                .Select(s => s.AccountingSource, asrc => asrc
                    .Field(a => a.Id)
                    .Field(a => a.Code)
                    .Field(a => a.Name)
                    .Field(a => a.KardexFlow))
                .Select(s => s.CostCenter, cc => cc
                    .Field(c => c.Id)
                    .Field(c => c.Name))
                .Select(s => s.Storage, st => st
                    .Field(g => g.Id)
                    .Field(g => g.Name))
                .SelectList(s => s.Lines, ln => ln
                    .Field(l => l.Id)
                    .Field(l => l.Quantity)
                    .Field(l => l.UnitCost)
                    .Field(l => l.DisplayOrder)
                    .Select(l => l.Item, it => it
                        .Field(i => i.Id)
                        .Field(i => i.Code)
                        .Field(i => i.Reference)
                        .Field(i => i.Name)
                        .Field(i => i.IsLotTracked)
                        .Field(i => i.IsSerialTracked)
                        .Select(i => i.MeasurementUnit, mu => mu
                            .Field(m => m.Id)
                            .Field(m => m.Abbreviation))
                        .Select(i => i.SizeCategory, sc => sc
                            .Field(c => c.Id)
                            .Field(c => c.Name)))
                    .SelectList(l => l.LotPreselections, lp => lp
                        .Field(lt => lt.Id)
                        .Field(lt => lt.LotNumber)
                        .Field(lt => lt.ExpirationDate)
                        .Field(lt => lt.Quantity)
                        .Field(lt => lt.UnitCost)
                        .Select(lt => lt.Lot, lo => lo
                            .Field(l2 => l2.Id)
                            .Field(l2 => l2.LotNumber)))
                    .SelectList(l => l.SerialPreselections, sp => sp
                        .Field(sl => sl.Id)
                        .Field(sl => sl.SerialNumber)
                        .Field(sl => sl.UnitCost)
                        .Select(sl => sl.Serial, sr => sr
                            .Field(s2 => s2.Id)
                            .Field(s2 => s2.SerialNumber)))
                    .SelectList(l => l.SizePreselections, zp => zp
                        .Field(sz => sz.Id)
                        .Field(sz => sz.Quantity)
                        .Field(sz => sz.UnitCost)
                        .Select(sz => sz.Size, sv => sv
                            .Field(s2 => s2.Id)
                            .Field(s2 => s2.Name))))
                .Build();

            var fragment = new GraphQLQueryFragment("stockMovement",
                [new("id", "ID!")], fields, "SingleItemResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery());
        });

        // -------- Items search --------
        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> ItemsPage = new(() =>
        {
            var fields = FieldSpec<PageType<ItemGraphQLModel>>
                .Create()
                .Field(p => p.TotalEntries)
                .SelectList(p => p.Entries, e => e
                    .Field(i => i.Id)
                    .Field(i => i.Code)
                    .Field(i => i.Name)
                    .Field(i => i.Reference)
                    .Field(i => i.IsActive)
                    .Field(i => i.IsLotTracked)
                    .Field(i => i.IsSerialTracked)
                    .Select(i => i.MeasurementUnit, mu => mu
                        .Field(m => m.Id)
                        .Field(m => m.Abbreviation))
                    .Select(i => i.SizeCategory, sc => sc
                        .Field(c => c.Id)
                        .Field(c => c.Name)
                        .SelectList(c => c.ItemSizeValues, sv => sv
                            .Field(v => v.Id)
                            .Field(v => v.Name)
                            .Field(v => v.DisplayOrder))))
                .Build();

            var fragment = new GraphQLQueryFragment("itemsPage",
                [new("pagination", "Pagination"), new("filters", "ItemFilters")],
                fields, "PageResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery());
        });

        // -------- Mutations --------
        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> CreateDraft = new(() =>
            BuildMutation("createStockMovementDraft", "CreateStockMovementDraftInput!", "createResponse"));

        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> UpdateDraft = new(() =>
            BuildIdDataMutation("updateStockMovementDraft", "UpdateStockMovementDraftInput!", "updateResponse"));

        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> DeleteDraft = new(() =>
            BuildMutation("deleteStockMovementDraft", "ID!", "deleteResponse", paramName: "id"));

        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> PostMovement = new(() =>
            BuildMutation("postStockMovement", "ID!", "updateResponse", paramName: "id"));

        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> CancelMovement = new(() =>
            BuildMutation("cancelStockMovement", "CancelStockMovementInput!", "updateResponse"));

        // Líneas
        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> AddDraftLine = new(() =>
            BuildLineMutation("addStockMovementDraftLine", "AddStockMovementDraftLineInput!", "createResponse"));

        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> UpdateDraftLine = new(() =>
            BuildLineMutation("updateStockMovementDraftLine", "UpdateStockMovementDraftLineInput!", "updateResponse"));

        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> DeleteDraftLine = new(() =>
            BuildLineMutation("deleteStockMovementDraftLine", "ID!", "deleteResponse", paramName: "id"));

        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> SetLineLots = new(() =>
            BuildLotsMutation());

        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> SetLineSerials = new(() =>
            BuildSerialsMutation());

        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> SetLineSizes = new(() =>
            BuildSizesMutation());

        // -------- builders --------
        // Para mutations con firma `(id: ID!, data: XInput!)` — dos parámetros separados.
        private static (GraphQLQueryFragment, string) BuildIdDataMutation(string opName, string dataType, string responseAlias)
        {
            var fields = FieldSpec<StockMovementMutationPayload>
                .Create()
                .Field(p => p.Success)
                .Field(p => p.Message)
                .SelectList(p => p.Errors, err => err.Field(e => e.Fields).Field(e => e.Message))
                .Select(p => p.StockMovement, s => s
                    .Field(x => x.Id)
                    .Field(x => x.DocumentNumber)
                    .Field(x => x.Status)
                    .Field(x => x.Note)
                    .Field(x => x.PostedAt)
                    .Field(x => x.CancelledWith)
                    .Field(x => x.CancelledAt))
                .Build();

            var fragment = new GraphQLQueryFragment(opName,
                [new("id", "ID!"), new("data", dataType)], fields, responseAlias);
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        }

        private static (GraphQLQueryFragment, string) BuildMutation(string opName, string inputType, string responseAlias, string paramName = "input")
        {
            var fields = FieldSpec<StockMovementMutationPayload>
                .Create()
                .Field(p => p.Success)
                .Field(p => p.Message)
                .SelectList(p => p.Errors, err => err.Field(e => e.Fields).Field(e => e.Message))
                .Select(p => p.StockMovement, s => s
                    .Field(x => x.Id)
                    .Field(x => x.DocumentNumber)
                    .Field(x => x.Status)
                    .Field(x => x.Note)
                    .Field(x => x.PostedAt)
                    .Field(x => x.CancelledWith)
                    .Field(x => x.CancelledAt))
                .Build();

            var fragment = new GraphQLQueryFragment(opName,
                [new(paramName, inputType)], fields, responseAlias);
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        }

        private static (GraphQLQueryFragment, string) BuildLineMutation(string opName, string inputType, string responseAlias, string paramName = "input")
        {
            var fields = FieldSpec<StockMovementLineMutationPayload>
                .Create()
                .Field(p => p.Success)
                .Field(p => p.Message)
                .SelectList(p => p.Errors, err => err.Field(e => e.Fields).Field(e => e.Message))
                .Select(p => p.StockMovementLine, l => l
                    .Field(x => x.Id)
                    .Field(x => x.Quantity)
                    .Field(x => x.UnitCost)
                    .Field(x => x.DisplayOrder)
                    .Select(x => x.Item, it => it
                        .Field(i => i.Id)
                        .Field(i => i.Code)
                        .Field(i => i.Name)))
                .Build();

            var fragment = new GraphQLQueryFragment(opName,
                [new(paramName, inputType)], fields, responseAlias);
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        }

        private static (GraphQLQueryFragment, string) BuildLotsMutation()
        {
            var fields = FieldSpec<StockMovementLineLotsMutationPayload>
                .Create()
                .Field(p => p.Success)
                .Field(p => p.Message)
                .SelectList(p => p.Errors, err => err.Field(e => e.Fields).Field(e => e.Message))
                .SelectList(p => p.StockMovementLineLots, l => l
                    .Field(x => x.Id)
                    .Field(x => x.LotNumber)
                    .Field(x => x.ExpirationDate)
                    .Field(x => x.Quantity)
                    .Field(x => x.UnitCost))
                .Build();

            var fragment = new GraphQLQueryFragment("setStockMovementLineLots",
                [new("input", "SetStockMovementLineLotsInput!")], fields, "updateResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        }

        private static (GraphQLQueryFragment, string) BuildSerialsMutation()
        {
            var fields = FieldSpec<StockMovementLineSerialsMutationPayload>
                .Create()
                .Field(p => p.Success)
                .Field(p => p.Message)
                .SelectList(p => p.Errors, err => err.Field(e => e.Fields).Field(e => e.Message))
                .SelectList(p => p.StockMovementLineSerials, l => l
                    .Field(x => x.Id)
                    .Field(x => x.SerialNumber)
                    .Field(x => x.UnitCost))
                .Build();

            var fragment = new GraphQLQueryFragment("setStockMovementLineSerials",
                [new("input", "SetStockMovementLineSerialsInput!")], fields, "updateResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        }

        private static (GraphQLQueryFragment, string) BuildSizesMutation()
        {
            var fields = FieldSpec<StockMovementLineSizesMutationPayload>
                .Create()
                .Field(p => p.Success)
                .Field(p => p.Message)
                .SelectList(p => p.Errors, err => err.Field(e => e.Fields).Field(e => e.Message))
                .SelectList(p => p.StockMovementLineSizes, l => l
                    .Field(x => x.Id)
                    .Field(x => x.Quantity)
                    .Field(x => x.UnitCost))
                .Build();

            var fragment = new GraphQLQueryFragment("setStockMovementLineSizes",
                [new("input", "SetStockMovementLineSizesInput!")], fields, "updateResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery(GraphQLOperations.MUTATION));
        }

        // -------- StockBy* (disponibilidad para salidas — no se usa en SM-In, queda disponible) --------
        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> StockByLotsPage = new(() =>
        {
            var fields = FieldSpec<PageType<StockByLotGraphQLModel>>
                .Create()
                .SelectList(p => p.Entries, e => e
                    .Field(s => s.Quantity)
                    .Field(s => s.Cost)
                    .Select(s => s.Lot, lt => lt
                        .Field(l => l.Id)
                        .Field(l => l.LotNumber)
                        .Field(l => l.ExpirationDate)))
                .Build();
            var fragment = new GraphQLQueryFragment("stockByLotsPage",
                [new("pagination", "Pagination"), new("filters", "StockByLotFilters")],
                fields, "PageResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery());
        });

        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> StockBySerialsPage = new(() =>
        {
            var fields = FieldSpec<PageType<StockBySerialGraphQLModel>>
                .Create()
                .SelectList(p => p.Entries, e => e
                    .Field(s => s.Cost)
                    .Select(s => s.Serial, sr => sr
                        .Field(r => r.Id)
                        .Field(r => r.SerialNumber)))
                .Build();
            var fragment = new GraphQLQueryFragment("stockBySerialsPage",
                [new("pagination", "Pagination"), new("filters", "StockBySerialFilters")],
                fields, "PageResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery());
        });

        public static readonly Lazy<(GraphQLQueryFragment Fragment, string Query)> StockBySizesPage = new(() =>
        {
            var fields = FieldSpec<PageType<StockBySizeGraphQLModel>>
                .Create()
                .SelectList(p => p.Entries, e => e
                    .Field(s => s.Quantity)
                    .Field(s => s.Cost)
                    .Select(s => s.Size, sv => sv
                        .Field(z => z.Id)
                        .Field(z => z.Name)))
                .Build();
            var fragment = new GraphQLQueryFragment("stockBySizesPage",
                [new("pagination", "Pagination"), new("filters", "StockBySizeFilters")],
                fields, "PageResponse");
            return (fragment, new QueryBuilder([fragment]).GetQuery());
        });
    }
}
