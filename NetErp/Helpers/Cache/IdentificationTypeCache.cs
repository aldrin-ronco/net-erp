using Caliburn.Micro;
using Common.Interfaces;
using Models.Books;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;

namespace NetErp.Helpers.Cache
{
    public class IdentificationTypeCache : IEntityCache<IdentificationTypeGraphQLModel>,
        IHandle<IdentificationTypeCreateMessage>,
        IHandle<IdentificationTypeUpdateMessage>,
        IHandle<IdentificationTypeDeleteMessage>
    {
        private readonly IRepository<IdentificationTypeGraphQLModel> _service;
        private readonly object _lock = new();

        public ObservableCollection<IdentificationTypeGraphQLModel> Items { get; } = [];
        public bool IsInitialized { get; private set; }

        public IdentificationTypeCache(
            IRepository<IdentificationTypeGraphQLModel> service,
            IEventAggregator eventAggregator)
        {
            _service = service;
            eventAggregator.SubscribeOnUIThread(this);
        }

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            var query = BuildQuery();
            dynamic variables = new ExpandoObject();
            variables.identificationTypesPagePagination = new ExpandoObject();
            variables.identificationTypesPagePagination.pageSize = -1;

            var result = await _service.GetPageAsync(query, variables);

            lock (_lock)
            {
                Items.Clear();
                foreach (var item in result.Entries)
                {
                    Items.Add(item);
                }
                IsInitialized = true;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                Items.Clear();
                IsInitialized = false;
            }
        }

        public void Add(IdentificationTypeGraphQLModel item)
        {
            lock (_lock)
            {
                if (!Items.Any(x => x.Id == item.Id))
                    Items.Add(item);
            }
        }

        public void Update(IdentificationTypeGraphQLModel item)
        {
            lock (_lock)
            {
                var existing = Items.FirstOrDefault(x => x.Id == item.Id);
                if (existing != null)
                {
                    var index = Items.IndexOf(existing);
                    Items[index] = item;
                }
            }
        }

        public void Remove(int id)
        {
            lock (_lock)
            {
                var item = Items.FirstOrDefault(x => x.Id == id);
                if (item != null)
                    Items.Remove(item);
            }
        }

        public Task HandleAsync(IdentificationTypeCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedIdentificationType?.Entity != null)
            {
                Add(message.CreatedIdentificationType.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(IdentificationTypeUpdateMessage message, CancellationToken cancellationToken)
        {
            var entity = message.UpdatedIdentificationType?.Entity;
            if (entity != null)
            {
                var existing = Items.FirstOrDefault(x => x.Id == entity.Id);
                if (existing != null)
                    Update(entity);
                else
                    Add(entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(IdentificationTypeDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedIdentificationType?.DeletedId > 0)
            {
                Remove(message.DeletedIdentificationType.DeletedId.Value);
            }
            return Task.CompletedTask;
        }

        #region Query Builder

        private string BuildQuery()
        {
            var fields = FieldSpec<PageType<IdentificationTypeGraphQLModel>>
                .Create()
                .SelectList(x => x.Entries, entries => entries
                    .Field(x => x.Id)
                    .Field(x => x.Name)
                    .Field(x => x.Code)
                    .Field(x => x.HasVerificationDigit)
                    .Field(x => x.AllowsLetters)
                    .Field(x => x.MinimumDocumentLength)
                )
                .Build();

            var parameter = new GraphQLQueryParameter("pagination", "Pagination");
            var fragment = new GraphQLQueryFragment("identificationTypesPage", [parameter], fields, "PageResponse");
            var builder = new QueryBuilder([fragment]);

            return builder.GetQuery();
        }

        #endregion
    }
}
