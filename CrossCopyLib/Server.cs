using System;
using System.Net;
using System.Net.Cache;
using System.IO;
using System.Text;
using System.Json;
using CrossCopy.BL;
using System.Threading;

namespace CrossCopy.Api
{
        public class Server
        {
                #region Public Properties
                public Secret CurrentSecret{ get; set; }
#endregion

                public delegate void TransferEventHandler (DataItem data);
                public event TransferEventHandler TransferEvent;

                public delegate void EventDelegate (object sender,DownloadDataCompletedEventArgs e);

                public delegate void StatusChanged ();
                public delegate void StatusProgressChanged (UploadProgressChangedEventArgs e);

                public static string SERVER = @"http://www.cross-copy.net";
                public string CurrentPath { get { return "/api/" + CurrentSecret; } }

                #region Private Members
                const string API = @"/api/{0}";
                static string DeviceID = string.Format ("?device_id={0}", Guid.NewGuid ());
                WebClient receiveClient = new WebClient ();
#endregion

                public Server ()
                {
                        CurrentSecret = null;
                        receiveClient.CachePolicy = new RequestCachePolicy (RequestCacheLevel.BypassCache);
                        receiveClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler (GotSomethingFromWeb);
                }

                public void Listen ()
                {
                        if (CurrentSecret == null || receiveClient.IsBusy)
                                return;

                        var uri = new Uri (String.Format ("{0}/api/{1}.json{2}&since={3}", 
                                              SERVER, CurrentSecret, DeviceID, CurrentSecret.LatestId));
                        receiveClient.DownloadStringAsync (uri);
                }

                public void Abort ()
                {
                        receiveClient.CancelAsync ();
                        CurrentSecret = null;
                }

                void GotSomethingFromWeb (object sender, DownloadStringCompletedEventArgs e)
                {
                        if (e.Cancelled)
                                return;
                        
                        if (e.Error == null) {
                                foreach (var i in JsonArray.Parse (e.Result)) {
                                        TransferEvent (new DataItem ((JsonValue)i, DataItemDirection.In, DateTime.Now));
                                }
                        } else
                                Console.Out.WriteLine ("Error fetching data: {0}", e.Error.Message);
                                
                        Listen ();
                }

                #region Send Text
                public void Send (string message)
                {
                        if (CurrentSecret == null)
                                return;

                        var t = new Thread (() => {
                                var shareClient = new WebClient ();

                                try {
                                        var result = shareClient.UploadString (
                                                        new Uri (String.Format ("{0}/api/{1}.json{2}", SERVER, CurrentSecret, DeviceID)), 
                                                        "PUT", message);

                                        if (String.IsNullOrWhiteSpace (result))
                                                return;

                                        TransferEvent (new DataItem (JsonObject.Parse (result), DataItemDirection.Out, DateTime.Now));

                                } catch (Exception e) {
                                        Console.Out.WriteLine ("Error sharing data: {0}", e.Message);
                                        return;
                                }
                        }
                        );
                        t.Start ();
                }
#endregion

                #region Upload Files
                public void UploadFileAsync (string filePath, byte[] fileByteArray, StatusChanged uploadCompleted)
                {
                        if (CurrentSecret == null)
                                return;

                        var destinationPath = String.Format ("/api/{0}/{1}", CurrentSecret, UrlHelper.GetFileName (filePath));
                        var client = new WebClient ();
                        client.Headers ["content-type"] = "application/octet-stream";
                        client.Encoding = Encoding.UTF8;

                        client.UploadDataCompleted += (sender, e) => {
                                uploadCompleted ();
                                if (e.Cancelled) {
                                        Console.Out.WriteLine ("Upload file cancelled.");
                                        return;
                                }

                                if (e.Error != null) {
                                        Console.Out.WriteLine ("Error uploading file: {0}", e.Error.Message);
                                        return;
                                }

                                var response = System.Text.Encoding.UTF8.GetString (e.Result);

                                if (!String.IsNullOrEmpty (response)) {
                                }
                        };
                        Send (destinationPath);
                        client.UploadDataAsync (new Uri (SERVER + destinationPath), "POST", fileByteArray);
                }
                public void UploadFileAsync (string filePath, StatusProgressChanged uploadProgressChanged, StatusChanged uploadCompleted)
                {
                        if (CurrentSecret == null)
                                return;
                        
                        var destinationPath = String.Format ("/api/{0}/{1}", CurrentSecret, UrlHelper.GetFileName (filePath));
                        var client = new WebClient ();
                        client.Headers ["content-type"] = "application/octet-stream";
                        client.Encoding = Encoding.UTF8;
                        
                        client.UploadProgressChanged += (sender, e) => {
                                uploadProgressChanged (e); };
                        client.UploadFileCompleted += (sender, e) => {
                                uploadCompleted ();
                                if (e.Cancelled) {
                                        Console.Out.WriteLine ("Upload file cancelled.");
                                        return;
                                }
                                
                                if (e.Error != null) {
                                        Console.Out.WriteLine ("Error uploading file: {0}", e.Error.Message);
                                        return;
                                }
                                
                                var response = System.Text.Encoding.UTF8.GetString (e.Result);
                                
                                if (!String.IsNullOrEmpty (response)) {
                                }
                        };
                        Send (destinationPath);
                        client.UploadFileAsync (new Uri (SERVER + destinationPath), "POST", filePath);
                }
#endregion
  
                #region Download Files
                public static void DownloadFileAsync (string remoteFilePath, string localFilePath, EventDelegate dwnldCompletedDelegate)
                {
                        var url = new Uri (SERVER + remoteFilePath);
                        var webClient = new WebClient ();
                        webClient.DownloadDataCompleted += (s, e) => {
                                File.WriteAllBytes (localFilePath, e.Result); };
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
#endregion
        }
}

