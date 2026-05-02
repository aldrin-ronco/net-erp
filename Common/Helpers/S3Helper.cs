using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Dictionaries;
using Models.Global;
using System;
using System.Threading.Tasks;

namespace Common.Helpers
{
    public class S3Helper : IDisposable
    {
        public string Bucket { get; }
        public string Directory { get; }

        private readonly AmazonS3Client _client;
        private bool _disposed;

        public S3Helper(string accessKey, string secretKey, RegionEndpoint region, string bucket, string directory)
        {
            ArgumentNullException.ThrowIfNull(accessKey);
            ArgumentNullException.ThrowIfNull(secretKey);
            ArgumentNullException.ThrowIfNull(region);
            Bucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
            Directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _client = new AmazonS3Client(accessKey, secretKey, region);
        }

        public static S3Helper FromStorageLocation(S3StorageLocationGraphQLModel location)
        {
            AwsS3ConfigGraphQLModel awsConfig = location.AwsS3Config ?? SessionInfo.DefaultAwsS3Config
                ?? throw new InvalidOperationException("No se encontró configuración AWS S3. Verifique que la empresa tenga configuración global o que la ubicación de almacenamiento tenga credenciales propias.");

            RegionEndpoint region = GlobalDictionaries.GetAwsRegionEndpoint(awsConfig.Region);

            return new S3Helper(
                awsConfig.AccessKey,
                awsConfig.SecretKey,
                region,
                location.Bucket,
                location.Directory);
        }

        private string BuildKey(string s3FileName)
        {
            return string.IsNullOrEmpty(Directory)
                ? s3FileName.Trim()
                : $"{Directory.Trim()}/{s3FileName.Trim()}";
        }

        public async Task UploadFileAsync(string localFilePath, string s3FileName)
        {
            using TransferUtility utility = new(_client);
            TransferUtilityUploadRequest request = new()
            {
                BucketName = Bucket.Trim(),
                Key = BuildKey(s3FileName),
                FilePath = localFilePath
            };
            await utility.UploadAsync(request);
        }

        public async Task DownloadFileAsync(string localFilePath, string s3FileName)
        {
            using TransferUtility utility = new(_client);
            TransferUtilityDownloadRequest request = new()
            {
                BucketName = Bucket.Trim(),
                Key = BuildKey(s3FileName),
                FilePath = localFilePath
            };
            await utility.DownloadAsync(request);
        }

        public async Task DeleteFileAsync(string s3FileName)
        {
            Amazon.S3.Model.DeleteObjectRequest request = new()
            {
                BucketName = Bucket.Trim(),
                Key = BuildKey(s3FileName)
            };
            await _client.DeleteObjectAsync(request);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _client.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
