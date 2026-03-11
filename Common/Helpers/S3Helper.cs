using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Dictionaries;
using Models.Global;
using System;
using System.Threading.Tasks;

namespace Common.Helpers
{
    public class S3Helper
    {
        public string Bucket { get; }
        public string Directory { get; }

        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly RegionEndpoint _region;

        public S3Helper(string accessKey, string secretKey, RegionEndpoint region, string bucket, string directory)
        {
            _accessKey = accessKey ?? throw new ArgumentNullException(nameof(accessKey));
            _secretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));
            _region = region ?? throw new ArgumentNullException(nameof(region));
            Bucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
            Directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        public static S3Helper FromStorageLocation(S3StorageLocationGraphQLModel location)
        {
            var awsConfig = location.AwsS3Config ?? SessionInfo.DefaultAwsS3Config
                ?? throw new InvalidOperationException("No se encontró configuración AWS S3. Verifique que la empresa tenga configuración global o que la ubicación de almacenamiento tenga credenciales propias.");

            var region = GlobalDictionaries.AwsSesRegionDictionary[awsConfig.Region];

            return new S3Helper(
                awsConfig.AccessKey,
                awsConfig.SecretKey,
                region,
                location.Bucket,
                location.Directory);
        }

        public async Task UploadFileAsync(string localFilePath, string s3FileName)
        {
            using var client = new AmazonS3Client(_accessKey, _secretKey, _region);
            using var utility = new TransferUtility(client);
            var request = new TransferUtilityUploadRequest
            {
                BucketName = Bucket.Trim(),
                Key = string.IsNullOrEmpty(Directory) ? s3FileName.Trim() : $"{Directory.Trim()}/{s3FileName.Trim()}",
                FilePath = localFilePath
            };
            await utility.UploadAsync(request);
        }

        public async Task DownloadFileAsync(string localFilePath, string s3FileName)
        {
            using var client = new AmazonS3Client(_accessKey, _secretKey, _region);
            using var utility = new TransferUtility(client);
            var request = new TransferUtilityDownloadRequest
            {
                BucketName = Bucket.Trim(),
                Key = string.IsNullOrEmpty(Directory) ? s3FileName.Trim() : $"{Directory.Trim()}/{s3FileName.Trim()}",
                FilePath = localFilePath
            };
            await utility.DownloadAsync(request);
        }

        public async Task DeleteFileAsync(string s3FileName)
        {
            using var client = new AmazonS3Client(_accessKey, _secretKey, _region);
            var request = new Amazon.S3.Model.DeleteObjectRequest
            {
                BucketName = Bucket.Trim(),
                Key = string.IsNullOrEmpty(Directory) ? s3FileName.Trim() : $"{Directory.Trim()}/{s3FileName.Trim()}"
            };
            await client.DeleteObjectAsync(request);
        }
    }
}
