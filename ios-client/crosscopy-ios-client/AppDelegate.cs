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

using CrossCopy.Api;

namespace CrossCopy.iOSClient
{
    [Register ("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate
    {
        #region Constants
        const string SERVER = @"http://www.cross-copy.net";
        const string API = @"/api/{0}";
        static string DeviceID = string.Format (
                "?device_id={0}",
                Guid.NewGuid ()
            );
        static string BaseDir = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
        static UIImage imgDownload = UIImage.FromFile ("Images/download.png");
        static UIImage imgUpload = UIImage.FromFile ("Images/upload.png");
        static UIColor backgroundColor = new UIColor (
                224 / 255.0f,
                224 / 255.0f,
                224 / 255.0f,
                1.0f
            );
        static UIColor lightTextColor = new UIColor (
                115 / 255.0f,
                115 / 255.0f,
                115 / 255.0f,
                1.0f
            );
        static UIColor darkTextColor = new UIColor (
                80 / 255.0f,
                80 / 255.0f,
                80 / 255.0f,
                1.0f
            );
        static UIImagePickerController imagePicker;
        static MPMoviePlayerController moviePlayer;
        #endregion
        
        #region Private members
        UIWindow window;
        UINavigationController navigation;
        EntryElement secretEntry, dataEntry;
        StyledStringElement pickPhoto;
        Section secretsSection, entriesSection;
        Secret currentSecret;
        string secretValue = string.Empty;
        WebClient receiveClient = new WebClient ();
        Server server = new Server ();
        List<string> selectedFilePathArray;
        #endregion
        
        #region Public props
        public static History HistoryData { get; set; }
        #endregion

        #region Methods
        public override bool FinishedLaunching (UIApplication app, NSDictionary options)
        {
            StoreHelper.Load ();

            window = new UIWindow (UIScreen.MainScreen.Bounds);
            window.MakeKeyAndVisible ();
            
            if (selectedFilePathArray == null) {
                selectedFilePathArray = new List<string> ();
            }
            
            var root = CreateRootElement ();
            var dvc = new StyledDialogViewController (
                root,
                null,
                backgroundColor
            )
            {
                Autorotate = true, 
                HidesBottomBarWhenPushed = true
            };
            dvc.ViewLoaded += delegate {
                secretValue = string.Empty;
            };

            navigation = new UINavigationController ();
            navigation.PushViewController (dvc, false);
            navigation.SetNavigationBarHidden (true, false);
            
            window.RootViewController = navigation;
            
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
                PasteData (e.Result, DataItemDirection.In);
                Listen ();
            };
            


            Listen ();
            
            return true;
        }
        
        public override void OnActivated (UIApplication application)
        {
        }

        public override void DidEnterBackground (UIApplication application)
        {
            StoreHelper.Save ();
        }
        
        public override void WillTerminate (UIApplication application)
        {
            StoreHelper.Save ();
        }
        
        private RootElement CreateRootElement ()
        {
            var captionLabel = UIHelper.CreateLabel (
                "cross copy",
                true,
                32,
                32,
                UITextAlignment.Center,
                UIColor.Black
            );
            UILabel subcaptionLabel = UIHelper.CreateLabel (
                "Open this App or http://cross-copy.net on different devices and choose the same secret. " + 
                "You can then transfer messages and files between them without any further setup.",
                false,
                14,
                85,
                UITextAlignment.Center,
                lightTextColor
            );


            captionLabel.Frame = new Rectangle (0, 10, 320, 40);
            subcaptionLabel.Frame = new Rectangle (20, 55, 280, 100);
            UIView header = new UIView (new Rectangle (0, 0, 300, 145));
            header.AddSubviews (captionLabel, subcaptionLabel);

            var root = new RootElement ("Secrets") 
            {
                new Section (header),
                (secretsSection = new Section ("Secrets")),
                new Section () 
                {
                    (secretEntry = new AdvancedEntryElement ("Secret", "enter new phrase", "", 
                                                                       delegate { 
                                                                        secretValue = secretEntry.Value;
                                                                        server.secretValue = secretValue;
                                                                        Listen(); }))
                }
            };
            
            secretsSection.AddAll (from s in AppDelegate.HistoryData.Secrets select (Element)CreateImageButtonStringElement (s));

            secretEntry.AutocapitalizationType = UITextAutocapitalizationType.None;
            secretEntry.ShouldReturn += delegate {
                var newSecret = new Secret (secretEntry.Value);
                AppDelegate.HistoryData.Secrets.Add (newSecret);

                if (root.Count == 2)
                    root.Insert (1, secretsSection);

                secretsSection.Insert (
                    secretsSection.Elements.Count,
                    UITableViewRowAnimation.Fade,
                    CreateImageButtonStringElement (newSecret)
                );
                secretEntry.Value = "";
                secretEntry.ResignFirstResponder (false);
                DisplaySecretDetail (newSecret);

                return true;
            };
            secretEntry.ReturnKeyType = UIReturnKeyType.Go;
            if (secretsSection.Count == 0) {
                secretEntry.BecomeFirstResponder (true);
                root.RemoveAt (1);
            }
            return root;
        }
        
        private void Listen ()
        {
            if (String.IsNullOrEmpty (secretValue))
                return;
            Console.Out.WriteLine ("Listen for secret: {0}", secretValue);
            receiveClient.CancelAsync ();
            receiveClient.DownloadStringAsync (new Uri (String.Format (
                "{0}/api/{1}{2}",
                SERVER,
                secretValue,
                DeviceID
            )
            )
            );
        }

        private Element CreateDataItemElement (DataItem item)
        {
            Element element;

            string apiUrl = string.Format (API, secretValue);
            if (item.Data.IndexOf (apiUrl) != -1) {
                var dataElement = new DataImageStringElement (
                    Path.GetFileName (item.Data.Substring (apiUrl.Length + 1)),
                    (item.Direction == DataItemDirection.In) ? imgDownload : imgUpload,
                    item.Data
                );
                dataElement.Tapped += delegate {
                    OpenFile (dataElement.Caption);
                };
                dataElement.Alignment = (item.Direction == DataItemDirection.In) ? UITextAlignment.Right : UITextAlignment.Left;
                if (item.Direction == DataItemDirection.In) {
                    dataElement.Animating = true;
                    Server.DownloadFileAsync (SERVER + dataElement.Data, Path.Combine (
                        BaseDir,
                        dataElement.Caption
                    ), delegate {
                        dataElement.Animating = false;
                    }
                    );
                } else {
                    dataElement.Animating = false;
                }

                element = (Element)dataElement;
            } else {
                UITextAlignment alignment = (item.Direction == DataItemDirection.In) ? UITextAlignment.Right : UITextAlignment.Left;
                var htmlElement = UIHelper.CreateHtmlViewElement (
                    null,
                    item.Data,
                    alignment
                );
                element = (Element)htmlElement;
            }

            return element;
        }

        private void PasteData (string data, DataItemDirection direction)
        {
            UIApplication.SharedApplication.InvokeOnMainThread (delegate { 
                DataItem item = new DataItem (data, direction, DateTime.Now);
                currentSecret.DataItems.Insert (0, item);
                entriesSection.Insert (0, CreateDataItemElement (item));
            }
            );
        }
        
       
        private void OpenFile (string fileName)
        {
            var sbounds = UIScreen.MainScreen.Bounds;
            string filePath = Path.Combine (BaseDir, fileName);
            string ext = Path.GetExtension (fileName);
            
            if (ext.ToUpper () == ".MOV" || ext.ToUpper () == ".M4V") {
                var movieController = new AdvancedUIViewController ();
                moviePlayer = new MPMoviePlayerController (NSUrl.FromFilename (filePath));
                moviePlayer.View.Frame = new RectangleF (
                    sbounds.X,
                    sbounds.Y - 20,
                    sbounds.Width,
                    sbounds.Height
                );
                moviePlayer.ControlStyle = MPMovieControlStyle.Fullscreen;
                moviePlayer.View.AutoresizingMask = (UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight);
                moviePlayer.ShouldAutoplay = true;
                moviePlayer.PrepareToPlay ();
                moviePlayer.Play ();
                
                var btnClose = UIButton.FromType (UIButtonType.RoundedRect);
                btnClose.Frame = new RectangleF (3, 7, 60, 30);
                btnClose.SetTitle ("Close", UIControlState.Normal);
                btnClose.SetTitleColor (UIColor.Black, UIControlState.Normal);
                btnClose.TouchDown += delegate {
                    movieController.DismissModalViewControllerAnimated (true);
                };
                
                movieController.View.AddSubview (moviePlayer.View);
                movieController.View.AddSubview (btnClose);
                navigation.PresentModalViewController (movieController, true);
            } else if (ext.ToUpper () == ".JPG" || ext.ToUpper () == ".PNG") {
                var imageController = new AdvancedUIViewController (); 
                
                var imageView = new UIImageView (UIImage.FromFile (filePath));
                imageView.Frame = sbounds;
                imageView.UserInteractionEnabled = true;
                imageView.ClipsToBounds = true;
                imageView.ContentMode = UIViewContentMode.ScaleAspectFit;
                
                var btnClose = UIButton.FromType (UIButtonType.RoundedRect);
                btnClose.Frame = new RectangleF (
                    (sbounds.Width / 2) - 50,
                    20,
                    100,
                    30
                );
                btnClose.SetTitle ("Close", UIControlState.Normal);
                btnClose.SetTitleColor (UIColor.Black, UIControlState.Normal);
                btnClose.TouchDown += delegate {
                    imageController.DismissModalViewControllerAnimated (true);
                };
                
                var scrollView = new UIScrollView (sbounds);
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
                 
                scrollView.AddSubview (imageView);
                imageController.View.AddSubview (scrollView);
                imageController.View.AddSubview (btnClose);
                navigation.PresentModalViewController (imageController, true);
            }
        }
        
        private void ShowImagePicker ()
        {
            imagePicker = new UIImagePickerController ();
            imagePicker.SourceType = UIImagePickerControllerSourceType.PhotoLibrary;
            imagePicker.MediaTypes = UIImagePickerController.AvailableMediaTypes (UIImagePickerControllerSourceType.PhotoLibrary);
            
            imagePicker.FinishedPickingMedia += (sender, e) => {
                bool isImage = (e.Info [UIImagePickerController.MediaType].ToString () == "public.image");
                NSUrl referenceUrl = e.Info [UIImagePickerController.ReferenceUrl] as NSUrl;
                UIImage image = null;
                NSUrl mediaUrl = null;
                    
                if (isImage) {
                    image = e.Info [UIImagePickerController.OriginalImage] as UIImage;
                } else {
                    mediaUrl = e.Info [UIImagePickerController.MediaURL] as NSUrl;
                }
                
                UploadMedia (image, referenceUrl, mediaUrl);
                
                imagePicker.DismissModalViewControllerAnimated (true);
            }; 
            
            imagePicker.Canceled += (sender, e) => {
                imagePicker.DismissModalViewControllerAnimated (true);
            }; 
            
            navigation.PresentModalViewController (imagePicker, true);
        }
        
        private void UploadMedia (UIImage image, NSUrl referenceUrl, NSUrl mediaUrl)
        {
            byte[] mediaByteArray;
            if (image != null) {
                ByteHelper.ImageToByteArray (image, out mediaByteArray);
            } else if (mediaUrl != null) {
                ByteHelper.VideoToByteArray (mediaUrl, out mediaByteArray);
            } else {
                Console.Out.WriteLine ("No media to upload!");
                return;
            }

              LoadingView loading = new LoadingView ();
            loading.Show ("Uploading file, please wait ...");
             
            server.UploadFileAsync (
                referenceUrl.AbsoluteString,
                mediaByteArray,
                delegate { loading.Hide (); }
            );
        }

        private ImageButtonStringElement CreateImageButtonStringElement (Secret secret)
        {
            return new ImageButtonStringElement (secret.Phrase, secret, "Images/remove.png", 
                      delegate {
                DisplaySecretDetail (secret);
            }, 
                      delegate {
                AppDelegate.HistoryData.Secrets.Remove (secret);
                Element found = null;
                foreach (var element in secretsSection.Elements) {
                    if (element.Caption == secret.Phrase) {
                        found = element;
                        break;
                    }
                }
                        
                if (found != null) {
                    secretsSection.Remove (found);
                    if (secretsSection.Count == 0)
                        (secretsSection.Parent as RootElement).RemoveAt (1);
                }
            }
            );
        }

        private void DisplaySecretDetail (Secret s)
        {
            var subRoot = new RootElement (s.Phrase) 
            {
                new Section ("Share") {
                    (pickPhoto = new StyledStringElement ("Photo", delegate { ShowImagePicker(); })),
                    (dataEntry = new AdvancedEntryElement ("Text", "your message", null))},
                (entriesSection = new Section ("History"))
            };

            pickPhoto.Accessory = UITableViewCellAccessory.DisclosureIndicator;
            dataEntry.ShouldReturn += delegate {
                UIApplication.SharedApplication.InvokeOnMainThread (delegate {
                    dataEntry.GetContainerTableView ().EndEditing (true);
                }
                );
                
                server.ShareData (dataEntry.Value.Trim ());
                
                return true;
            };
            dataEntry.ReturnKeyType = UIReturnKeyType.Send;


            entriesSection.Elements.AddRange (
                from d in s.DataItems
                select ((Element)CreateDataItemElement (d))
            );

            subRoot.UnevenRows = true;

            var dvc = new StyledDialogViewController (
                subRoot,
                true,
                null,
                backgroundColor
            );
            dvc.HidesBottomBarWhenPushed = false;
            navigation.SetNavigationBarHidden (false, true);
            navigation.PushViewController (dvc, true);

            secretValue = s.Phrase;
            currentSecret = s;
            Listen ();
        }
        #endregion
    }
}

