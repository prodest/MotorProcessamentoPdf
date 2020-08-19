using Infrastructure.Repositories.IRepositories;
using Minio;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ArquivoRepository : IArquivoRepository
    {
        private readonly MinioClient MinioClient;
        private const string DocumentoBucketName = "documento-eletronico";

        public ArquivoRepository()
        {
            MinioClient = new MinioClient("localhost:9000", "minioadmin", "minioadmin");
        }

        public async Task<byte[]> GetDocumentoCapturadoAsync(string objectName)
        {
            return await GetContentFromMinioAsync(DocumentoBucketName, objectName);
        }

        private async Task<byte[]> GetContentFromMinioAsync(string bucketName, string objectName)
        {
            byte[] buffer = null;

            try
            {
                await MinioClient.GetObjectAsync(bucketName, objectName, (stream) =>
                {
                    using (stream)
                    {
                        using (MemoryStream content = new MemoryStream())
                        {
                            stream.CopyTo(content);
                            buffer = content.ToArray();
                        }
                    }
                });
            }
            catch (Exception e)
            {
                throw new Exception($"erro ao obter arquivo {objectName} do bucket {bucketName}", e);
            }

            return buffer;
        }
    }
}
