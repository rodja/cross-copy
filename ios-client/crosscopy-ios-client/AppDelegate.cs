using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Json;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using CrossCopy.iOSClient.BL;
using CrossCopy.iOSClient.UI;
using CrossCopy.iOSClient.Helpers;
using MonoTouch.MediaPlayer;

namespace CrossCopy.iOSClient
{
    [Register ("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate
    {
        #region Constants
        const string SERVER = @"http://www.cross-copy.net";
        const string API = @"/api/{0}";
        static string DeviceID = string.Format("?device_id={0}", Guid.NewGuid());
        static string BaseDir = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
        static UIImage imgFolder = UIImage.FromFile("Images/folder.png");
        static UIImage imgFile = UIImage.FromFile("Images/file.png");
        static UIImage imgDownload = UIImage.FromFile("Images/download.png");
        static UIImage imgUpload = UIImage.FromFile("Images/upload.png");
        static UIColor backgroundColor = new UIColor (224 / 255.0f, 224 / 255.0f, 224 / 255.0f, 1.0f);
        static UIColor lightTextColor = new UIColor (163 / 255.0f, 163 / 255.0f, 163 / 255.0f, 1.0f);
        static UIColor darkTextColor = new UIColor (102 / 255.0f, 102 / 255.0f, 102 / 255.0f, 1.0f);
        static UIImagePickerController imagePicker;
        static MPMoviePlayerController moviePlayer;
        #endregion
        
        #region Private members
        UIWindow window;
        UINavigationController navigation;
        EntryElement secret, data;
        Section entries;
        
        string secretValue;
        List<DataItem> history;
        
        WebRequest request;
    
        WebClient shareClient = new WebClient();
        WebClient receiveClient = new WebClient();
        
        List<string> selectedFilePathArray;
        #endregion
        
        public delegate void EventDelegate(object sender, DownloadDataCompletedEventArgs e);
        
        #region Methods
        public override bool FinishedLaunching (UIApplication app, NSDictionary options)
        {
            window = new UIWindow (UIScreen.MainScreen.Bounds);
            window.MakeKeyAndVisible ();
            
            if (history == null)
            {
                history = new List<DataItem>();
            }
            
            if (selectedFilePathArray == null)
            {
                selectedFilePathArray = new List<string>();
            }
            
            var root = CreateRootElement();
            var dvc = new StyledDialogViewController(root, null, backgroundColor)
            {
                Autorotate = true, 
                HidesBottomBarWhenPushed = true
            };

            navigation = new UINavigationController();
            navigation.PushViewController(dvc, false);
            navigation.SetNavigationBarHidden(true, false);
            
            window.RootViewController = navigation;
            
            receiveClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
            receiveClient.DownloadStringCompleted += (sender, e) => { 
                if (e.Cancelled) return;
                if (e.Error != null) {
                    Console.Out.WriteLine("Error fetching data: {0}", e.Error.Message);
                    Listen ();
                    return;
                }
                PasteData (e.Result, DataItemDirection.In);
                Listen ();
            };
            
            shareClient.UploadStringCompleted += (sender, e) => {
                if (e.Cancelled || String.IsNullOrWhiteSpace(e.Result)) return;
                if (e.Error != null) {
                    Console.Out.WriteLine("Error sharing data: {0}", e.Error.Message);
                    return;
                }

                PasteData (JsonObject.Parse(e.Result)["data"], DataItemDirection.Out);
            };

            Listen ();
            
            return true;
        }
        
        public override void OnActivated (UIApplication application)
        {
        }
        
        private RootElement CreateRootElement ()
        {
            var captionLabel = UIHelper.CreateLabel ("cross copy", true, 32, UITextAlignment.Center, UIColor.Black);
            var subcaptionLabel = UIHelper.CreateLabel ("... can transfer stuff between devices", false, 18, UITextAlignment.Center, lightTextColor);
            var section1Label = UIHelper.CreateLabel ("  1 open this page on different devices", false, 18, UITextAlignment.Left, darkTextColor);
            var section2Label = UIHelper.CreateLabel ("  2 enter the same secret", false, 18, UITextAlignment.Left, darkTextColor);
            var section3Label = UIHelper.CreateLabel ("  3 pick a file you want to share", false, 18, UITextAlignment.Left, darkTextColor);
            var section4Label = UIHelper.CreateLabel ("  4 wait for appearing cross copies", false, 18, UITextAlignment.Left, darkTextColor);
            
            var root = new RootElement ("CrossCopy") 
            {
                new Section (captionLabel, subcaptionLabel),
                new Section (section1Label),
                new Section (section2Label) 
                {
                    (secret = new AdvancedEntryElement (" ", "secret phrase", "", delegate {
                        Console.Out.WriteLine("Secret changed: {0}", secret.Value);
                        Listen();
                    }))
                },
                new Section (section3Label) 
                {
                    (data = new ImageButtonEntryElement (" ", "or type text", "", "Images/browse.png", delegate {
                        ShowImagePicker();
                    }))
                },
                (entries = new Section(section4Label))
            };
            
            root.UnevenRows = true;
            
            secret.AutocapitalizationType = UITextAutocapitalizationType.None;
            secret.ShouldReturn += delegate 
            {
                UIApplication.SharedApplication.InvokeOnMainThread (delegate {
                    secret.GetContainerTableView ().EndEditing (true);
                });
                
                return true;
            };
            
            data.ShouldReturn += delegate 
            {
                UIApplication.SharedApplication.InvokeOnMainThread (delegate {
                    data.GetContainerTableView ().EndEditing (true);
                });
                
                ShareData(data.Value);
                
                return true;
            };
            
            return root;
        }
        
        private void Listen ()
        {
            secretValue = secret.Value;
            if (secretValue.Length == 0)
                return;
            receiveClient.CancelAsync();
            receiveClient.DownloadStringAsync(new Uri(String.Format("{0}/api/{1}{2}", SERVER, secretValue, DeviceID)));
        }
                
        private void PasteData(string data, DataItemDirection direction)
        {
            UIApplication.SharedApplication.InvokeOnMainThread(delegate
            { 
                DataItem item = new DataItem(data, direction, DateTime.Now);
                
                history.Insert(0, item);
                
                string apiUrl = string.Format(API, secretValue);
                if (data.IndexOf(apiUrl) != -1)
                {
                    DataImageStringElement entry = new DataImageStringElement(Path.GetFileName(data.Substring(apiUrl.Length + 1)), 
                                                                              (item.Direction == DataItemDirection.In) ? imgDownload : imgUpload,
                                                                              data);
                    entry.Tapped += delegate { OpenFile (entry.Caption); };
                    entry.Alignment = (item.Direction == DataItemDirection.In) ? UITextAlignment.Right : UITextAlignment.Left;
                    entries.Insert(0, entry);
                    if (item.Direction == DataItemDirection.In)
                    {
                        entry.Animating = true;
                        DownloadFileAsync(SERVER + entry.Data, Path.Combine(BaseDir, entry.Caption), delegate { entry.Animating = false; });
                    }
                    else
                    {
                        entry.Animating = false;
                    }
                }
                else
                {
                    UITextAlignment alignment = (item.Direction == DataItemDirection.In) ? UITextAlignment.Right : UITextAlignment.Left;
                    var entry = UIHelper.CreateHtmlViewElement(null, item.Data, alignment);
                    entries.Insert(0, entry);
                }
                
            });
        }
        
        private void ShareData(string dataToShare)
        {
            if (String.IsNullOrEmpty(secretValue))
                return;

            shareClient.UploadStringAsync(new Uri(String.Format("{0}/api/{1}.json{2}", SERVER, secretValue, DeviceID)), "PUT", dataToShare);
    
        }
      
        public void UploadFileAsync(string filePath, byte[] fileByteArray)
        {
            if (String.IsNullOrEmpty(secretValue))
                return;

            LoadingView loading = new LoadingView();
            loading.Show("Uploading file, please wait ...");

            string destinationPath = String.Format("/api/{0}/{1}", secretValue, UrlHelper.GetFileName(filePath));
            WebClient client = new WebClient ();
            client.Headers["content-type"] = "application/octet-stream";
            client.Encoding = Encoding.UTF8;
            client.UploadDataCompleted += (sender, e) => {
                loading.Hide();

                if (e.Cancelled) 
                {
                    Console.Out.WriteLine("Upload file cancelled.");
                    return;
                }

                if (e.Error != null) 
                {
                    Console.Out.WriteLine("Error uploading file: {0}", e.Error.Message);
                    return;
                }

                string response = System.Text.Encoding.UTF8.GetString (e.Result);
                if (!String.IsNullOrEmpty(response))
                {
                    ShareData(destinationPath);
                }
            };

            Uri fileUri = new Uri(SERVER + destinationPath);
            client.UploadDataAsync(fileUri, "POST", fileByteArray);
        }

        public static void DownloadFileAsync(string remoteFilePath, string localFilePath, EventDelegate dwnldCompletedDelegate)
        {
            var url = new Uri(remoteFilePath);
            var webClient = new WebClient();
            webClient.DownloadDataCompleted += (s, e) => {
                var bytes = e.Result; 
                File.WriteAllBytes (localFilePath, bytes);  
            };
            webClient.DownloadDataCompleted += new DownloadDataCompletedEventHandler(dwnldCompletedDelegate);
            webClient.DownloadDataAsync(url);
        }
        
        public static int DownloadFile(string remoteFilename, string localFilename)
        {
            int bytesProcessed = 0;
         
            Stream remoteStream  = null;
            Stream localStream   = null;
            WebResponse response = null;
            
            try
            {
                WebRequest request = WebRequest.Create(remoteFilename);
                if (request != null)
                {
                    response = request.GetResponse();
                    if (response != null)
                    {
                        remoteStream = response.GetResponseStream();
            
                        localStream = File.Create(localFilename);
            
                        byte[] buffer = new byte[1024];
                        int bytesRead;
            
                        do
                        {
                            bytesRead = remoteStream.Read (buffer, 0, buffer.Length);
                            localStream.Write (buffer, 0, bytesRead);
                            bytesProcessed += bytesRead;
                        } while (bytesRead > 0);
                    }
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.Message);
            }
            finally
            {
                if (response != null) response.Close();
                if (remoteStream != null) remoteStream.Close();
                if (localStream != null) localStream.Close();
            }
            
            return bytesProcessed;
        }
        
        private void OpenFile(string fileName)
        {
            var sbounds = UIScreen.MainScreen.Bounds;
            string filePath = Path.Combine (BaseDir, fileName);
            string ext = Path.GetExtension (fileName);
            
            if (ext.ToUpper() == ".MOV" || ext.ToUpper() == ".M4V") 
            {
                var movieController = new AdvancedUIViewController();
                moviePlayer = new MPMoviePlayerController(NSUrl.FromFilename(filePath));
                moviePlayer.View.Frame = new RectangleF(sbounds.X, sbounds.Y-20, sbounds.Width, sbounds.Height);
                moviePlayer.ControlStyle = MPMovieControlStyle.Fullscreen;
                moviePlayer.View.AutoresizingMask = (UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight);
                moviePlayer.ShouldAutoplay = true;
                moviePlayer.PrepareToPlay();
                moviePlayer.Play();
                
                var btnClose = UIButton.FromType(UIButtonType.RoundedRect);
                btnClose.Frame = new RectangleF (3, 7, 60, 30);
                btnClose.SetTitle ("Close", UIControlState.Normal);
                btnClose.SetTitleColor (UIColor.Black, UIControlState.Normal);
                btnClose.TouchDown += delegate {
                    movieController.DismissModalViewControllerAnimated (true);
                };
                
                movieController.View.AddSubview(moviePlayer.View);
                movieController.View.AddSubview (btnClose);
                navigation.PresentModalViewController (movieController, true);
            }
            else if (ext.ToUpper() == ".JPG" || ext.ToUpper() == ".PNG") 
            {
                var imageController = new AdvancedUIViewController(); 
                
                var imageView = new UIImageView (UIImage.FromFile (filePath));
                imageView.Frame = sbounds;
                imageView.UserInteractionEnabled = true;
                imageView.ClipsToBounds = true;
                imageView.ContentMode = UIViewContentMode.ScaleAspectFit;
                
                var btnClose = UIButton.FromType(UIButtonType.RoundedRect);
                btnClose.Frame = new RectangleF ((sbounds.Width / 2) - 50, 20, 100, 30);
                btnClose.SetTitle ("Close", UIControlState.Normal);
                btnClose.SetTitleColor (UIColor.Black, UIControlState.Normal);
                btnClose.TouchDown += delegate {
                    imageController.DismissModalViewControllerAnimated (true);
                };
                
                var scrollView = new UIScrollView(sbounds);
                scrollView.ClipsToBounds = true;
                scrollView.ContentSize = sbounds.Size;
                scrollView.BackgroundColor = UIColor.Gray;
                scrollView.MinimumZoomScale = 1.0f;
                scrollView.MaximumZoomScale = 3.0f; 
                scrollView.MultipleTouchEnabled = true;
                scrollView.AutoresizingMask = (UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight);
                scrollView.ViewForZoomingInScrollView = delegate(UIScrollView sv) {
                    return imageView;
                };
                 
                scrollView.AddSubview(imageView);
                imageController.View.AddSubview(scrollView);
                imageController.View.AddSubview (btnClose);
                navigation.PresentModalViewController (imageController, true);
            }
        }
        
        private void ShowImagePicker()
        {
            imagePicker = new UIImagePickerController();
            imagePicker.SourceType = UIImagePickerControllerSourceType.PhotoLibrary;
            imagePicker.MediaTypes = UIImagePickerController.AvailableMediaTypes (UIImagePickerControllerSourceType.PhotoLibrary);
            
            imagePicker.FinishedPickingMedia += (sender, e) => {
                bool isImage = (e.Info[UIImagePickerController.MediaType].ToString() == "public.image");
                NSUrl referenceUrl = e.Info[UIImagePickerController.ReferenceUrl] as NSUrl;
                UIImage image  = null;
                NSUrl mediaUrl = null;
                    
                if (isImage)
                {
                    image = e.Info[UIImagePickerController.OriginalImage] as UIImage;
                }
                else
                {
                    mediaUrl = e.Info[UIImagePickerController.MediaURL] as NSUrl;
                }
                
                UploadMedia (image, referenceUrl, mediaUrl);
                
                imagePicker.DismissModalViewControllerAnimated(true);
            }; 
            
            imagePicker.Canceled += (sender, e) => {
                imagePicker.DismissModalViewControllerAnimated(true);
            }; 
            
            navigation.PresentModalViewController(imagePicker, true);
        }
        
        private void UploadMedia (UIImage image, NSUrl referenceUrl, NSUrl mediaUrl)
        {
            byte[] mediaByteArray;
            if (image != null) 
            {
                ByteHelper.ImageToByteArray (image, out mediaByteArray);
            }
            else if (mediaUrl != null)
            {
                ByteHelper.VideoToByteArray (mediaUrl, out mediaByteArray);
            }
            else
            {
                Console.Out.WriteLine("No media to upload!");
                return;
            }
            
            UploadFileAsync(referenceUrl.AbsoluteString, mediaByteArray);
        }

        #endregion
    }
}

