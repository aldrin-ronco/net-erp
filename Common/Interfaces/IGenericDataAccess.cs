using Common.Helpers;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Common.Interfaces
{
    /// <summary>
    /// DEPRECATED: Legacy data access interface that is being phased out.
    /// Use IRepository&lt;TModel&gt; for all new implementations.
    /// This interface will be removed in a future version.
    /// </summary>
    /// <typeparam name="TModel">The GraphQL model type for the entity being managed</typeparam>
    /// <remarks>
    /// This interface is deprecated and should not be used for new implementations.
    /// Migrate existing code to use IRepository&lt;TModel&gt; which provides:
    /// - Better async/await patterns with CancellationToken support
    /// - Improved error handling
    /// - More consistent method naming
    /// - Enhanced type safety
    /// </remarks>
    [System.Obsolete("IGenericDataAccess is deprecated. Use IRepository<TModel> instead. This interface will be removed in a future version.", false)]
    public interface IGenericDataAccess<TModel>
    {
        /// <summary>
        /// DEPRECATED: Creates a new entity. Use IRepository&lt;TModel&gt;.CreateAsync instead.
        /// </summary>
        [System.Obsolete("Use IRepository<TModel>.CreateAsync instead.", false)]
        public async Task<TModel> Create(string query, object variables)
        {
            try
            {
                GraphQLHttpClient client = new(ConnectionConfig.GraphQLAPIUrl, new NewtonsoftJsonSerializer());
                client.HttpClient.DefaultRequestHeaders.Add("DatabaseId", ConnectionConfig.DatabaseId);
                GraphQLResponse<SingleItemResponseType> result = await client.SendMutationAsync<SingleItemResponseType>(new GraphQLRequest()
                {
                    Query = query,
                    Variables = variables
                });
                if (result.Errors != null)
                {
                    GraphQL.GraphQLError error = result.Errors[0];
                    Map? extensions = error.Extensions;
                    if (extensions != null && extensions.TryGetValue("message", out object? value))
                    {
                        throw new Exception(value.ToString());
                    }
                    throw new Exception(error.Message);
                }
                return result.Data.CreateResponse;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// DEPRECATED: Sends mutation for multiple entities. Use IRepository&lt;TModel&gt;.SendMutationListAsync instead.
        /// </summary>
        [System.Obsolete("Use IRepository<TModel>.SendMutationListAsync instead.", false)]
        public async Task<IEnumerable<TModel>> SendMutationList(string query, object variables)
        {
            try
            {
                GraphQLHttpClient client = new(ConnectionConfig.GraphQLAPIUrl, new NewtonsoftJsonSerializer());
                client.HttpClient.DefaultRequestHeaders.Add("DatabaseId", ConnectionConfig.DatabaseId);
                GraphQLResponse<ListItemResponseType> result = await client.SendMutationAsync<ListItemResponseType>(new GraphQLRequest()
                {
                    Query = query,
                    Variables = variables
                });
                if (result.Errors != null)
                {
                    GraphQL.GraphQLError error = result.Errors[0];
                    Map? extensions = error.Extensions;
                    if (extensions != null && extensions.TryGetValue("message", out object? value))
                    {
                        throw new Exception(value.ToString());
                    }
                    throw new Exception(error.Message);
                }
                return result.Data.ListResponse;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// DEPRECATED: Updates an existing entity. Use IRepository&lt;TModel&gt;.UpdateAsync instead.
        /// </summary>
        [System.Obsolete("Use IRepository<TModel>.UpdateAsync instead.", false)]
        public async Task<TModel> Update(string query, object variables)
        {
            try
            {
                GraphQLHttpClient client = new(ConnectionConfig.GraphQLAPIUrl, new NewtonsoftJsonSerializer());
                client.HttpClient.DefaultRequestHeaders.Add("DatabaseId", ConnectionConfig.DatabaseId);
                GraphQLResponse<SingleItemResponseType> result = await client.SendMutationAsync<SingleItemResponseType>(new GraphQLRequest()
                {
                    Query = query,
                    Variables = variables
                });
                if (result.Errors != null)
                {
                    GraphQL.GraphQLError error = result.Errors[0];
                    Map? extensions = error.Extensions;
                    if (extensions != null && extensions.TryGetValue("message", out object? value))
                    {
                        throw new Exception(value.ToString());
                    }
                    throw new Exception(error.Message);
                }
                return result.Data.UpdateResponse;
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// DEPRECATED: Deletes an entity. Use IRepository&lt;TModel&gt;.DeleteAsync instead.
        /// </summary>
        [System.Obsolete("Use IRepository<TModel>.DeleteAsync instead.", false)]
        public async Task<TModel> Delete(string query, object variables)
        {
            try
            {
                GraphQLHttpClient client = new(ConnectionConfig.GraphQLAPIUrl, new NewtonsoftJsonSerializer());
                client.HttpClient.DefaultRequestHeaders.Add("DatabaseId", ConnectionConfig.DatabaseId);
                GraphQLResponse<SingleItemResponseType> result = await client.SendMutationAsync<SingleItemResponseType>(new GraphQLRequest()
                {
                    Query = query,
                    Variables = variables
                });
                if (result.Errors != null)
                {
                    GraphQL.GraphQLError error = result.Errors[0];
                    Map? extensions = error.Extensions;
                    if (extensions != null && extensions.TryGetValue("message", out object? value))
                    {
                        throw new Exception(value.ToString());
                    }
                    throw new Exception(error.Message);
                }
                return result.Data.DeleteResponse;
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// DEPRECATED: Gets list of entities. Use IRepository&lt;TModel&gt;.GetListAsync instead.
        /// </summary>
        [System.Obsolete("Use IRepository<TModel>.GetListAsync instead.", false)]
        public async Task<IEnumerable<TModel>> GetList(string query, object variables)
        {
            try
            {
                GraphQLHttpClient client = new(ConnectionConfig.GraphQLAPIUrl, new NewtonsoftJsonSerializer());
                client.HttpClient.DefaultRequestHeaders.Add("DatabaseId", ConnectionConfig.DatabaseId);
                GraphQLResponse<ListItemResponseType> result = await client.SendQueryAsync<ListItemResponseType>(new GraphQLRequest()
                {
                    Query = query,
                    Variables = variables
                });
                if (result.Errors != null)
                {
                    GraphQL.GraphQLError error = result.Errors[0];
                    Map? extensions = error.Extensions;
                    if (extensions != null && extensions.TryGetValue("message", out object? value))
                    {
                        throw new Exception(value.ToString());
                    }
                    throw new Exception(error.Message);
                }
                return result.Data.ListResponse;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// DEPRECATED: Executes custom mutation. Use IRepository&lt;TModel&gt;.MutationContextAsync instead.
        /// </summary>
        [System.Obsolete("Use IRepository<TModel>.MutationContextAsync instead.", false)]
        public async Task<XModel> MutationContext<XModel>(string query, object variables)
        {
            try
            {
                GraphQLHttpClient client = new(ConnectionConfig.GraphQLAPIUrl, new NewtonsoftJsonSerializer());
                client.HttpClient.DefaultRequestHeaders.Add("DatabaseId", ConnectionConfig.DatabaseId);
                GraphQLResponse<XModel> result = await client.SendMutationAsync<XModel>(new GraphQLRequest()
                {
                    Query = query,
                    Variables = variables
                });

                if (result.Errors != null)
                {
                    GraphQL.GraphQLError error = result.Errors[0];
                    Map? extensions = error.Extensions;
                    if (extensions != null && extensions.TryGetValue("message", out object? value))
                    {
                        throw new Exception(value.ToString());
                    }
                    throw new Exception(error.Message);
                }
                return result.Data;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// DEPRECATED: Executes custom query. Use IRepository&lt;TModel&gt;.GetDataContextAsync instead.
        /// </summary>
        [System.Obsolete("Use IRepository<TModel>.GetDataContextAsync instead.", false)]
        public async Task<XModel> GetDataContext<XModel>(string query, object variables)
        {
            try
            {
                GraphQLHttpClient client = new(ConnectionConfig.GraphQLAPIUrl, new NewtonsoftJsonSerializer());
                client.HttpClient.DefaultRequestHeaders.Add("DatabaseId", ConnectionConfig.DatabaseId);
                GraphQLResponse<XModel> result = await client.SendQueryAsync<XModel>(new GraphQLRequest()
                {
                    Query = query,
                    Variables = variables
                });
                if (result.Errors != null)
                {
                    GraphQL.GraphQLError error = result.Errors[0];
                    Map? extensions = error.Extensions;
                    if (extensions != null && extensions.TryGetValue("message", out object? value))
                    {
                        throw new Exception(value.ToString());
                    }
                    throw new Exception(error.Message);
                }
                return result.Data;
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// DEPRECATED: Finds entity by ID. Use IRepository&lt;TModel&gt;.FindByIdAsync instead.
        /// </summary>
        [System.Obsolete("Use IRepository<TModel>.FindByIdAsync instead.", false)]
        public async Task<TModel> FindById (string query, object variables)
        {
            try
            {
                GraphQLHttpClient client = new(ConnectionConfig.GraphQLAPIUrl, new NewtonsoftJsonSerializer());
                client.HttpClient.DefaultRequestHeaders.Add("DatabaseId", ConnectionConfig.DatabaseId);
                GraphQLResponse<SingleItemResponseType> result = await client.SendQueryAsync<SingleItemResponseType>(new GraphQLRequest()
                {
                    Query = query,
                    Variables = variables
                });
                if (result.Errors != null)
                {
                    GraphQL.GraphQLError error = result.Errors[0];
                    Map? extensions = error.Extensions;
                    if (extensions != null && extensions.TryGetValue("message", out object? value))
                    {
                        throw new Exception(value.ToString());
                    }
                    throw new Exception(error.Message);
                }
                return result.Data.SingleItemResponse;
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// DEPRECATED: Creates multiple entities. Use IRepository&lt;TModel&gt;.CreateListAsync instead.
        /// </summary>
        [System.Obsolete("Use IRepository<TModel>.CreateListAsync instead.", false)]
        public async Task<IEnumerable<TModel>> CreateList(string query, object variables)
        {
            try
            {
                GraphQLHttpClient client = new(ConnectionConfig.GraphQLAPIUrl, new NewtonsoftJsonSerializer());
                client.HttpClient.DefaultRequestHeaders.Add("DatabaseId", ConnectionConfig.DatabaseId);
                ObservableCollection<TModel> models = new();
                models.ToList();
                var result = await client.SendMutationAsync<ListItemResponseType>(new GraphQLRequest()
                {
                    Query = query,
                    Variables = variables
                });
                if (result.Errors != null)
                {
                    GraphQL.GraphQLError error = result.Errors[0];
                    Map? extensions = error.Extensions;
                    if (extensions != null && extensions.TryGetValue("message", out object? value))
                    {
                        throw new Exception(value.ToString());
                    }
                    throw new Exception(error.Message);
                }
                return result.Data.ListResponse;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// DEPRECATED: Gets paginated results. Use IRepository&lt;TModel&gt;.GetPageAsync instead.
        /// </summary>
        [System.Obsolete("Use IRepository<TModel>.GetPageAsync instead.", false)]
        public async Task<PageResponseType> GetPage(string query, object variables)
        {
            try
            {
                GraphQLHttpClient client = new(ConnectionConfig.GraphQLAPIUrl, new NewtonsoftJsonSerializer());
                client.HttpClient.DefaultRequestHeaders.Add("DatabaseId", ConnectionConfig.DatabaseId);
                GraphQLResponse<PageResponseType> result = await client.SendQueryAsync<PageResponseType>(new GraphQLRequest()
                {
                    Query = query,
                    Variables = variables
                });
                if (result.Errors != null)
                {
                    GraphQL.GraphQLError error = result.Errors[0];
                    Map? extensions = error.Extensions;
                    if (extensions != null && extensions.TryGetValue("message", out object? value))
                    {
                        throw new Exception(value.ToString());
                    }
                    throw new Exception(error.Message);
                }

                return result.Data;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// DEPRECATED: Checks if entity can be deleted. Use IRepository&lt;TModel&gt;.CanDeleteAsync instead.
        /// </summary>
        [System.Obsolete("Use IRepository<TModel>.CanDeleteAsync instead.", false)]
        public async Task<CanDeleteModel> CanDelete(string query, object variables)
        {
            try
            {
                GraphQLHttpClient client = new(ConnectionConfig.GraphQLAPIUrl, new NewtonsoftJsonSerializer());
                client.HttpClient.DefaultRequestHeaders.Add("DatabaseId", ConnectionConfig.DatabaseId);
                GraphQLResponse<CanDeleteResponseType> result = await client.SendQueryAsync<CanDeleteResponseType>(new GraphQLRequest()
                {
                    Query = query,
                    Variables = variables
                });
                if (result.Errors != null)
                {
                    GraphQL.GraphQLError error = result.Errors[0];
                    Map? extensions = error.Extensions;
                    if (extensions != null && extensions.TryGetValue("message", out object? value))
                    {
                        throw new Exception(value.ToString());
                    }
                    throw new Exception(error.Message);
                }
                return result.Data.CanDeleteModel;
            }
            catch (Exception)
            {

                throw;
            }
        }

        class PageResponseType
        {
            public PageType PageResponse { get; set; }
        }

        class CanDeleteResponseType
        {
            public CanDeleteModel CanDeleteModel { get; set; }
        }

        class CanDeleteModel
        {
            public bool CanDelete { get; set; } = false;
            public string Message { get; set; } = string.Empty;
        }

        class PageType
        {
            public int Count { get; set; }
            public ObservableCollection<TModel> Rows { get; set; }
        }

        class SingleItemResponseType
        {
            public TModel CreateResponse { get; set; }
            public TModel UpdateResponse { get; set; }
            public TModel DeleteResponse { get; set; }
            public TModel SingleItemResponse { get; set; }
        }

        public class ListItemResponseType
        {
            public ObservableCollection<TModel> ListResponse { get; set; }
        }
    }
}
