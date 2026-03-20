using Caliburn.Micro;
using Common.Helpers;
using Common.Interfaces;
using Models.Billing;
using Models.Books;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Models.Global.GraphQLResponseTypes;
using QueryBuilder = NetErp.Helpers.GraphQLQueryBuilder.GraphQLQueryBuilder;

namespace NetErp.Helpers.Cache
{
    public class AwsS3ConfigCache : IEntityCache<AwsS3ConfigGraphQLModel>,
        IHandle<AwsS3ConfigCreateMessage>,
        IHandle<AwsS3ConfigUpdateMessage>,
        IHandle<AwsS3ConfigDeleteMessage>
    {
        private readonly IRepository<AwsS3ConfigGraphQLModel> _service;
        private readonly object _lock = new();
        public ObservableCollection<AwsS3ConfigGraphQLModel> Items  { get; } = [];

        public bool IsInitialized { get; private set; }
        public AwsS3ConfigCache(IRepository<AwsS3ConfigGraphQLModel> service, IEventAggregator eventAggregator)
        {
            this._service = service;
            eventAggregator.SubscribeOnUIThread(this);
        }
        public void Add(AwsS3ConfigGraphQLModel item)
        {
            lock (_lock)
            {
                if (!Items.Any(x => x.Id == item.Id))
                    Items.Add(item);
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

        public async Task EnsureLoadedAsync()
        {
            if (IsInitialized) return;

            try
            {
                var query = BuildQuery();
                dynamic variables = new ExpandoObject();


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
            catch (Exception ex)
            {
                throw new AsyncException(innerException: ex);
            }
        }

        public Task HandleAsync(AwsS3ConfigCreateMessage message, CancellationToken cancellationToken)
        {
            if (message.CreatedAwsS3Config != null
              )
            {
                Add(message.CreatedAwsS3Config.Entity);
            }
            return Task.CompletedTask;
        }

        public Task HandleAsync(AwsS3ConfigUpdateMessage message, CancellationToken cancellationToken)
        {
            var entity = message.UpdatedAwsS3Config?.Entity;

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

        public Task HandleAsync(AwsS3ConfigDeleteMessage message, CancellationToken cancellationToken)
        {
            if (message.DeletedAwsS3Config?.DeletedId > 0)
            {
                Remove(message.DeletedAwsS3Config.DeletedId.Value);
            }

            return Task.CompletedTask;
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

        public void Update(AwsS3ConfigGraphQLModel item)
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

        private string BuildQuery()
        {
            var fields = FieldSpec<PageType<AwsS3ConfigGraphQLModel>>
              .Create()
              .SelectList(it => it.Entries, entries => entries
                  .Field(e => e.Id)
                  .Field(e => e.Description)
                  .Field(e => e.AccessKey)
                  .Field(e => e.Region)
              )
              .Field(o => o.PageNumber)
              .Field(o => o.PageSize)
              .Field(o => o.TotalPages)
              .Field(o => o.TotalEntries)
              .Build();


            var paginationParam = new GraphQLQueryParameter("pagination", "Pagination");
            var filtersParam = new GraphQLQueryParameter("filters", "AwsS3ConfigFilters");
            var fragment = new GraphQLQueryFragment("awsS3ConfigsPage", [paginationParam, filtersParam], fields, "PageResponse");
            var builder = new QueryBuilder([fragment]);

            return builder.GetQuery();
        }
    }

}
