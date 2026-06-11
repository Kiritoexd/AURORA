using Amazon.S3;
using Amazon.S3.Model;
using System.IO;
using System.Threading.Tasks;

namespace AURORA.Servicios
{
    public class BackblazeFileRepository : IFileRepository
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public BackblazeFileRepository(IAmazonS3 s3Client, IConfiguration config)
        {
            _s3Client = s3Client;
            _bucketName = config.GetSection("BackblazeB2")["BucketName"];
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                InputStream = fileStream
            };

            await _s3Client.PutObjectAsync(request);
            return GetFileUrl(fileName);
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            var baseUrl = $"{_s3Client.Config.ServiceURL}/{_bucketName}/";

            string key;
            if (fileUrl.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
                key = fileUrl.Substring(baseUrl.Length);
            else
                key = fileUrl;

            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(request);
        }

        public string GetFileUrl(string fileName)
        {
            return $"{_s3Client.Config.ServiceURL}/{_bucketName}/{fileName}";
        }
    }
}