using Common.Helpers;
using Common.Interfaces;
using Models.Global;
using NetErp.Helpers.GraphQLQueryBuilder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace NetErp.Helpers.Services
{
    /// <summary>
    /// Resuelve la S3StorageLocation con key <c>product_images</c> y construye el
    /// helper + path local de cache. Reutilizable por cualquier ViewModel que
    /// necesite acceso a S3 para imágenes de productos.
    /// </summary>
    public static class S3ConfigLoader
    {
        public const string ProductImagesKey = "product_images";

        /// <summary>
        /// Carga configuración. Errores se silencian y retornan
        /// <c>(null, "")</c> — la UI debe interpretar S3 como no disponible.
        /// </summary>
        public static async Task<(S3Helper? Helper, string LocalCachePath)> LoadProductImagesAsync(
            IRepository<S3StorageLocationGraphQLModel> service)
        {
            try
            {
                Dictionary<string, object> fields = FieldSpec<S3StorageLocationGraphQLModel>
                    .Create()
                    .Field(f => f.Id)
                    .Field(f => f.Key)
                    .Field(f => f.Bucket)
                    .Field(f => f.Directory)
                    .Select(f => f.AwsS3Config, aws => aws
                        .Field(a => a.Id)
                        .Field(a => a.AccessKey)
                        .Field(a => a.SecretKey)
                        .Field(a => a.Region))
                    .Build();

                GraphQLQueryFragment fragment = new("s3StorageLocationByKey",
                    [new("key", "String!")], fields, "SingleItemResponse");
                string query = new GraphQLQueryBuilder.GraphQLQueryBuilder([fragment]).GetQuery(GraphQLOperations.QUERY);
                S3StorageLocationGraphQLModel? location = await service.GetSingleItemAsync(
                    query, new { singleItemResponseKey = ProductImagesKey });

                if (location is null) return (null, string.Empty);

                string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                string localCachePath = Path.Combine(appDir, "cache", location.Bucket, location.Directory);
                Directory.CreateDirectory(localCachePath);

                if (location.AwsS3Config is null && SessionInfo.DefaultAwsS3Config is null)
                    return (null, localCachePath);

                return (S3Helper.FromStorageLocation(location), localCachePath);
            }
            catch
            {
                return (null, string.Empty);
            }
        }
    }
}
