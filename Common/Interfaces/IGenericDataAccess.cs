﻿using Common.Helpers;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interfaces
{
    public interface IGenericDataAccess<TModel>
    {
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
                    //throw new Exception(result.Errors[0].Message);
                    if (result.Errors[0].Extensions != null)
                    {
                        throw new Exception(result.Errors[0].Extensions["message"].ToString());
                    }
                    else
                    {
                        throw new Exception(result.Errors[0].Message);
                    }
                }
                return result.Data.CreateResponse;
            }
            catch (Exception)
            {
                throw;
            }
        }

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
                    if (result.Errors[0].Extensions != null)
                    {
                        throw new Exception(result.Errors[0].Extensions.Values.ToString());
                    }
                    else
                    {
                        throw new Exception(result.Errors[0].Message);
                    }
                }
                return result.Data.UpdateResponse;
            }
            catch (Exception)
            {
                throw;
            }
        }
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
                    throw new Exception(result.Errors[0].Message);
                }
                return result.Data.DeleteResponse;
            }
            catch (Exception)
            {

                throw;
            }
        }

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
                    throw new Exception(result.Errors[0].Message);
                }
                return result.Data.ListResponse;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<TModel> GetDataContext<TModel>(string query, object variables)
        {
            try
            {
                GraphQLHttpClient client = new(ConnectionConfig.GraphQLAPIUrl, new NewtonsoftJsonSerializer());
                client.HttpClient.DefaultRequestHeaders.Add("DatabaseId", ConnectionConfig.DatabaseId);
                GraphQLResponse<TModel> result = await client.SendQueryAsync<TModel>(new GraphQLRequest()
                {
                    Query = query,
                    Variables = variables
                });
                if (result.Errors != null)
                {
                    throw new Exception(result.Errors[0].Message);
                }
                return result.Data;
            }
            catch (Exception)
            {

                throw;
            }
        }

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
                    throw new Exception(result.Errors[0].Message);
                }
                return result.Data.ListResponse;
            }
            catch (Exception)
            {
                throw;
            }
        }

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
                    throw new Exception(result.Errors[0].Message);
                }
                return result.Data;
            }
            catch (Exception)
            {
                throw;
            }
        }

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
                    throw new Exception(result.Errors[0].Message);
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
        }

        public class ListItemResponseType
        {
            public ObservableCollection<TModel> ListResponse { get; set; }
        }
    }
}
