using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.KeyVault;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System;

namespace CodeSocketAzureKeyVaultDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            // This is standard code to interact with Blob storage.
            StorageCredentials creds = new StorageCredentials(
                ConfigurationManager.AppSettings["accountName"],
                ConfigurationManager.AppSettings["accountKey"]);
            CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);
            CloudBlobClient client = account.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(ConfigurationManager.AppSettings["container"]);
            container.CreateIfNotExists();

            // The Resolver object is used to interact with Key Vault for Azure Storage.
            // This is where the GetToken method from above is used.
            KeyVaultKeyResolver cloudResolver = new KeyVaultKeyResolver(GetToken);

        // Retrieve the key that you created previously.
        // The IKey that is returned here is an RsaKey.
        // Remember that we used the names contosokeyvault and testrsakey1.

        
            var rsa = cloudResolver.ResolveKeyAsync("https://codesocketkeyvaultprem.vault.azure.net:443/keys/CokeSocketHSMKey/3db2ed41f0c847a49fe8fe2d1b369bfb", CancellationToken.None).GetAwaiter().GetResult();


            // Now you simply use the RSA key to encrypt by setting it in the BlobEncryptionPolicy.
            BlobEncryptionPolicy policy = new BlobEncryptionPolicy(rsa, null);
            BlobRequestOptions options = new BlobRequestOptions() { EncryptionPolicy = policy };

            // Reference a block blob.
            CloudBlockBlob blob = container.GetBlockBlobReference("MyFile2.txt");

            // Upload using the UploadFromStream method.
            using (var stream = System.IO.File.OpenRead(@"C:\data\MyFile2.txt"))
            {
                blob.UploadFromStream(stream, stream.Length, null, options, null);
            }
        }

        private async static Task<string> GetToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(
                ConfigurationManager.AppSettings["clientId"],
                ConfigurationManager.AppSettings["clientSecret"]);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
                throw new InvalidOperationException("Failed to obtain the JWT token");

            return result.AccessToken;
        }
    }
}
