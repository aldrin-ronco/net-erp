using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Helpers
{
    public static class S3Helper
    {

        public static string S3Bucket { get; set; } = string.Empty;
        public static string S3Directory { get; set; } = string.Empty;

        public static string LocalFilePath { get; set; } = string.Empty;
        public static string S3FileName {  get; set; } = string.Empty;

        public static string S3AccessKey {  get; set; } = string.Empty;

        public static string S3SecretKey { get; set; } = string.Empty;

        public static RegionEndpoint Region { get; set; } 


        public static void Initialize(string s3Bucket, string s3Directory, string s3AccessKey, string s3SecretKey, RegionEndpoint region)
        {
            S3Bucket = s3Bucket;
            S3Directory = s3Directory;
            S3AccessKey = s3AccessKey;
            S3SecretKey = s3SecretKey;
            Region = region;
        }


        public async static Task DeleteFileFromS3Async()
        {
            try
            {
                IAmazonS3 client = new AmazonS3Client(S3AccessKey, S3SecretKey, Region);
                DeleteObjectRequest request = new();
                request.BucketName = S3Bucket.Trim(); //no subdirectory just bucket name
                request.Key = string.IsNullOrEmpty(S3Directory) ? S3FileName.Trim() : $@"{S3Directory.Trim()}/{S3FileName.Trim()}"; //file name up in S3
                await client.DeleteObjectAsync(request); //commensing the transfer
                client.Dispose();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task DownloadFileFromS3()
        {
            try
            {
                IAmazonS3 client = new AmazonS3Client(S3AccessKey, S3SecretKey, Region);
                TransferUtility utility = new(client);
                TransferUtilityDownloadRequest request = new();
                request.BucketName = S3Bucket.Trim(); //no subdirectory just bucket name
                request.Key = string.IsNullOrEmpty(S3Directory) ? S3FileName.Trim() : $@"{S3Directory.Trim()}/{S3FileName.Trim()}"; //file name up in S3
                request.FilePath = LocalFilePath; //local file name
                await utility.DownloadAsync(request); //commensing the transfer
                utility.Dispose();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async static Task UploadFileToS3Async()
        {
            try
            {
                IAmazonS3 client = new AmazonS3Client(S3AccessKey, S3SecretKey, Region);
                TransferUtility utility = new(client);
                TransferUtilityUploadRequest request = new();
                request.BucketName = S3Bucket.Trim(); //no subdirectory just bucket name
                request.Key = string.IsNullOrEmpty(S3Directory) ? S3FileName.Trim() : $@"{S3Directory.Trim()}/{S3FileName.Trim()}"; //file name up in S3
                request.FilePath = LocalFilePath; //local file name
                await utility.UploadAsync(request); //commensing the transfer
                utility.Dispose();
            }
            catch (Exception)
            {

                throw;
            }
        }

    }

}
