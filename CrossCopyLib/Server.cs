using System;
using System.Net;
using System.Net.Cache;
using System.IO;
using System.Text;
using System.Json;
using CrossCopy.BL;

namespace CrossCopy.Api
{
    public class Server
    {
        public delegate void TransferEventHandler (DataItem data);

        public delegate void EventDelegate (object sender,DownloadDataCompletedEventArgs e);

        public delegate void StatusChanged ();

		public static string SERVER = @"http://www.cross-copy.net";
        const string API = @"/api/{0}";
        static string DeviceID = string.Format (
                "?device_id={0}",
                Guid.NewGuid ()
            );
        WebClient shareClient = new WebClient ();
        WebClient receiveClient = new WebClient ();

        public Server ()
        {
            CurrentSecret = null;
            receiveClient.CachePolicy = new RequestCachePolicy (RequestCacheLevel.BypassCache);
            receiveClient.DownloadStringCompleted += (sender, e) => { 
                if (e.Cancelled)
                    return;
                if (e.Error != null) {
                    Console.Out.WriteLine (
                        "Error fetching data: {0}",
                        e.Error.Message
                    );
                    Listen ();
                    return;
                }

                JsonValue items = JsonArray.Parse (e.Result);
                foreach (JsonValue i in items) {
                    DataItem item = new DataItem (i,
                        DataItemDirection.In, DateTime.Now);
                    TransferEvent (item);
                }
                Listen ();
            };

            shareClient.UploadStringCompleted += (sender, e) => {
                if (e.Cancelled || String.IsNullOrWhiteSpace (e.Result))
                    return;
                if (e.Error != null) {
                    Console.Out.WriteLine (
                        "Error sharing data: {0}",
                        e.Error.Message
                    );
                    return;
                }

                DataItem item = new DataItem (JsonObject.Parse (e.Result),
                    DataItemDirection.Out, DateTime.Now);
                TransferEvent (item);
            };
        }

        public event TransferEventHandler TransferEvent;
       
        public Secret CurrentSecret{ get; set; }

        public string CurrentPath { get { return "/api/" + CurrentSecret; } }

        public void Listen ()
        {
            if (CurrentSecret == null)
                return;
            Uri uri = new Uri (String.Format ("{0}/api/{1}.json{2}&since={3}", 
                                              SERVER, CurrentSecret, DeviceID, CurrentSecret.LatestId)
            );
            receiveClient.DownloadStringAsync (uri);
        }

        public void Abort ()
        {
            receiveClient.CancelAsync ();
            CurrentSecret = null;
        }

        public void Send (string message)
        {
            if (CurrentSecret == null)
                return;

			shareClient.CancelAsync();
            shareClient.UploadStringAsync (
                new Uri (String.Format ("{0}/api/{1}.json{2}",
                SERVER, CurrentSecret, DeviceID)
            ), "PUT", message);
    
        }

        public void UploadFileAsync (string filePath, byte[] fileByteArray, StatusChanged downloadCompleted)
        {
            if (CurrentSecret == null)
                return;

          
            string destinationPath = String.Format (
                "/api/{0}/{1}", CurrentSecret, UrlHelper.GetFileName (filePath)
            );
            WebClient client = new WebClient ();
            client.Headers ["content-type"] = "application/octet-stream";
            client.Encoding = Encoding.UTF8;
            client.UploadDataCompleted += (sender, e) => {
                downloadCompleted ();

                if (e.Cancelled) {
                    Console.Out.WriteLine ("Upload file cancelled.");
                    return;
                }

                if (e.Error != null) {
                    Console.Out.WriteLine (
                        "Error uploading file: {0}",
                        e.Error.Message
                    );
                    return;
                }

                string response = System.Text.Encoding.UTF8.GetString (e.Result);

                if (!String.IsNullOrEmpty (response)) {
                }
            };
            Send (destinationPath);
            Uri fileUri = new Uri (SERVER + destinationPath);
            client.UploadDataAsync (fileUri, "POST", fileByteArray);
        }

        public static void DownloadFileAsync (string remoteFilePath, string localFilePath, EventDelegate dwnldCompletedDelegate)
        {
            var url = new Uri (SERVER + remoteFilePath);
            var webClient = new WebClient ();
            webClient.DownloadDataCompleted += (s, e) => {
                var bytes = e.Result; 
                File.WriteAllBytes (localFilePath, bytes);  
            };
            webClient.DownloadDataCompleted += new DownloadDataCompletedEventHandler (dwnldCompletedDelegate);
            webClient.DownloadDataAsync (url);
        }

        public static void DownloadFileAsync (string remoteFilePath, EventDelegate dwnldCompletedDelegate)
        {
            var url = new Uri (SERVER + remoteFilePath);
            var webClient = new WebClient ();
            webClient.DownloadDataCompleted += new DownloadDataCompletedEventHandler (dwnldCompletedDelegate);
            webClient.DownloadDataAsync (url);
        }
    }
}

