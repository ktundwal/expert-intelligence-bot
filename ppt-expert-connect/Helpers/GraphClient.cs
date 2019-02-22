using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using DelegateAuthenticationProvider = Microsoft.Graph.DelegateAuthenticationProvider;
using DriveItem = Microsoft.Graph.DriveItem;
using GraphServiceClient = Microsoft.Graph.GraphServiceClient;

namespace Microsoft.ExpertConnect.Helpers
{
    public static class GraphClient
    {
        public const string OneDriveFolderName = "expert-connect";

        // Get information about the user.
        public static async Task<Microsoft.Graph.User> GetMeAsync(GraphServiceClient graphClient)
        {
            return await graphClient.Me.Request().GetAsync();
        }

        public static GraphServiceClient GetAuthenticatedClient(string token)
        {
            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    requestMessage =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);

                        // Get event times in the current time zone.
                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + TimeZoneInfo.Local.Id + "\"");

                        return Task.CompletedTask;
                    }));
            return graphClient;
        }

        public static async Task<DriveItem> UploadFileToDriveAsync(GraphServiceClient graphClient, string filePath)
        {
            using (FileStream fileStream = new FileStream(filePath,
                FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true))
            {
                DriveItem uploadedFile = null;

                uploadedFile = await graphClient.Me.Drive.Root.ItemWithPath(fileStream.Name).Content.Request()
                    .PutAsync<DriveItem>(fileStream);
                return (uploadedFile);
            }
        }

        public static async Task<DriveItem> GetOrCreateFolder(GraphServiceClient graphClient, string path)
        {
            try
            {
                var req = graphClient.Me.Drive.Root.ItemWithPath("/" + path).Request();
                DriveItem folder = await req.GetAsync();
                return folder;
            }
            catch (ServiceException exception)
            {
                if (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return await CreateFolder(graphClient, path);
                }

                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private static async Task<DriveItem> CreateFolder(GraphServiceClient graphClient, string folderName)
        {
            try
            {
                DriveItem folder;

                folder = await graphClient.Me.Drive.Root.Children.Request().AddAsync(new DriveItem
                {
                    Name = folderName,
                    Folder = new Folder()
                });

                return folder;
            }
            catch (Exception exception)
            {
                throw;
            }
        }

        private static async Task LoadFolderFromId(GraphServiceClient graphClient, string id)
        {
            if (null == graphClient) return;

            try
            {
                var expandString = false /*this.clientType == ClientType.Consumer*/
                    ? "thumbnails,children($expand=thumbnails)"
                    : "thumbnails,children";

                var folder =
                    await graphClient.Drive.Items[id].Request().Expand(expandString).GetAsync();
            }
            catch (Exception exception)
            {
                throw;
            }
        }

        // https://github.com/microsoftgraph/msgraph-sdk-dotnet/blob/2d565c10969689344bbdaa58f4ab74df06303063/tests/Microsoft.Graph.Test/Requests/Functional/OneDriveTests.cs#L39
        public static async Task<DriveItem> UploadFileAsync(GraphServiceClient graphClient, DriveItem folder,
            string fileName, MemoryStream ms)
        {
            var utcNow = System.DateTimeOffset.UtcNow;
            var props = new DriveItemUploadableProperties();
            props.Name = fileName;
            props.FileSystemInfo = new Microsoft.Graph.FileSystemInfo();
            props.FileSystemInfo.CreatedDateTime = utcNow;
            props.FileSystemInfo.LastModifiedDateTime = utcNow;

            // Get the provider. 
            // POST /v1.0/drive/items/01KGPRHTV6Y2GOVW7725BZO354PWSELRRZ:/_hamiltion.png:/microsoft.graph.createUploadSession
            // The CreateUploadSesssion action doesn't seem to support the options stated in the metadata.
            var uploadSession = await graphClient.Me.Drive.Items[folder.Id].ItemWithPath(fileName).CreateUploadSession()
                .Request().PostAsync();

            var maxChunkSize = 320 * 1024; // 320 KB - Change this to your chunk size. 5MB is the default.
            var provider = new ChunkedUploadProvider(uploadSession, graphClient, ms, maxChunkSize);

            // Setup the chunk request necessities
            var chunkRequests = provider.GetUploadChunkRequests();
            var readBuffer = new byte[maxChunkSize];
            var trackedExceptions = new List<Exception>();
            DriveItem itemResult = null;

            //upload the chunks
            foreach (var request in chunkRequests)
            {
                // Do your updates here: update progress bar, etc.
                // ...
                // Send chunk request
                var result = await provider.GetChunkRequestResponseAsync(request, readBuffer, trackedExceptions);

                if (result.UploadSucceeded)
                {
                    itemResult = result.ItemResponse;
                }
            }

            // Check that upload succeeded
            if (itemResult == null)
            {
                // Retry the upload
                // ...

                throw new Exception("Upload failed");
            }

            return itemResult;
        }

        public static async Task<string> ShareFileAsync(
            GraphServiceClient graphClient,
            DriveItem fileToShare,
            string emailToShareWith,
            string inviteMessage,
            ILogger logger)
        {
            var recipients = new List<DriveRecipient>()
            {
                new DriveRecipient()
                {
                    Email = emailToShareWith,
                },
            };

            var roles = new List<string>()
            {
                "write",
            };

            var inviteCollection = await graphClient.Me.Drive.Items[fileToShare.Id]
                .Invite(recipients, true, roles, true, inviteMessage)
                .Request()
                .PostAsync();

            string id = "not_available";
            try
            {
                id = inviteCollection[0].GrantedTo.User.Id;
            }
            catch (Exception e)
            {
                logger.LogError($"Error extracting id from invited collection", e);
            }

            return id;
        }

        public static DriveItem UploadPowerPointFileToDrive(GraphServiceClient graphClient, DriveItem folder, string pptLink, string projectId)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WebClient c = new WebClient();
                var responseData = pptLink != string.Empty
                    ? c.DownloadData(pptLink)
                    : c.DownloadData("https://query.prod.cms.rt.microsoft.com/cms/api/am/binary/RE2lrfa");

                ms.Write(responseData, 0, (int) responseData.Length);

                string todayDate = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
                string projectIdentifier = $"ppt-project-{projectId}-{todayDate}";
                return UploadFileAsync(graphClient, folder, $"{projectIdentifier}/{projectIdentifier}.pptx", ms).Result;
            }
        }
    }
}
