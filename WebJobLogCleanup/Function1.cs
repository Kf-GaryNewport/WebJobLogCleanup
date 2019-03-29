using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WebJobLogCleanup
{
    public static class Function1
    {
        // Get key from KeyVault
        private static string StorageAccount = System.Environment.GetEnvironmentVariable("StorageAccount");

        [FunctionName("LogCleanup")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            var blobs = BlobBatch(StorageAccount, "azure-webjobs-hosts", "output-logs", -30);

            log.Info($"Blob Count : {blobs.Count()}");

            return req.CreateResponse(HttpStatusCode.OK, "Count [" + blobs.Count() + "]" + " Deleted [" + BlobBatchDelete(blobs) + "]");
        }


        public static IEnumerable<CloudBlockBlob> BlobBatch(string storageConnectionString, string container, string directory, int days)
        {
            // Parse the connection string and return a reference to the storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            // Create the table client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            var cont = blobClient.GetContainerReference(container);

            // Query out all the blobs which created after 30 days
            var blobs = cont.GetDirectoryReference(directory).ListBlobs(true)
                .OfType<CloudBlockBlob>()
                .Where(b => b.Properties.LastModified < new DateTimeOffset(DateTime.Now.AddDays(days)));

            return blobs;
        }

        public static int BlobBatchDelete(IEnumerable<CloudBlockBlob> blobs)
        {
            var count = 0;
            // Delete these blobs
            foreach (var item in blobs)
            {
                item.DeleteIfExists();
                count++;
            }

            return count;
        }

    }
}
