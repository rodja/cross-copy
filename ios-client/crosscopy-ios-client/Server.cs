using System;
using System.Net;
using System.Net.Cache;
using System.IO;
using System.Text;
using System.Json;


using CrossCopy.iOSClient.BL;

namespace CrossCopy.Api
{
    public class Server
    {
        public delegate void TransferEventHandler (object sender,TransferEventArgs e);

        public delegate void EventDelegate (object sender,DownloadDataCompletedEventArgs e);

        public delegate void StatusChanged ();

        const string SERVER = @"http://www.cross-copy.net";
        const string API = @"/api/{0}";
        static string DeviceID = string.Format (
                "?device_id={0}",
                Guid.NewGuid ()
            );
        WebClient shareClient = new WebClient ();
        WebClient receiveClient = new WebClient ();
        
        public Server ()
        {
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
                DataItem item = new DataItem (
                    e.Result,
                    DataItemDirection.In,
                    DateTime.Now
                );
                TransferEvent (this, new TransferEventArgs (item));
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

                TransferEvent (
                    this,
                    new TransferEventArgs (new DataItem (
                    JsonObject.Parse (e.Result) ["data"],
                    DataItemDirection.Out,
                    DateTime.Now
                )
                )
                );
            };

        
        }

        public event TransferEventHandler TransferEvent;

        public string Secret{ get; set; }

        public string CurrentPath { get { return "/api/" + Secret; } }

        public void Listen ()
        {
            if (String.IsNullOrEmpty (Secret))
                return;
            Console.Out.WriteLine ("Listen for secret: {0}", Secret);
            receiveClient.CancelAsync ();
            receiveClient.DownloadStringAsync (new Uri (String.Format (
                "{0}/api/{1}{2}",
                SERVER,
                Secret,
                DeviceID
            )
            )
            );
        }

        public void Send (string message)
        {
            if (String.IsNullOrEmpty (Secret))
                return;

            shareClient.UploadStringAsync (
                new Uri (String.Format (
                "{0}/api/{1}.json{2}",
                SERVER,
                Secret,
                DeviceID
            )
            ),
                "PUT",
                message
            );
    
        }

        public void UploadFileAsync (string filePath, byte[] fileByteArray, StatusChanged downloadCompleted)
        {
            if (String.IsNullOrEmpty (Secret))
                return;

          
            string destinationPath = String.Format (
                "/api/{0}/{1}",
                Secret,
                UrlHelper.GetFileName (filePath)
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
                    Send (destinationPath);
                }
            };

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
    }

    public class TransferEventArgs : EventArgs
    {
        public TransferEventArgs (DataItem data)
        {
            Data = data;
        }

        public DataItem Data{ get; set; }
    }
}

