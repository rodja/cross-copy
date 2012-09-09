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
using CrossCopy.BL;
using CrossCopy.iOSClient.UI;
using CrossCopy.iOSClient.Helpers;
using MonoTouch.MediaPlayer;
using Analytics = GoogleAnalytics.GANTracker;
using MonoTouch.TestFlight;

using CrossCopy.Api;
using MonoTouch.AssetsLibrary;

using Flurry = FlurryAnalytics.FlurryAnalytics;

namespace CrossCopy.iOSClient
{
    [Register ("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate
    {
        #region Constants
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
        static UIImagePickerController imagePicker;
        static MPMoviePlayerController moviePlayer;
        const string ASSETS_LIBRARY = "assets-library://";
        #endregion
        
        #region Private members
        UIWindow window;
        UINavigationController navigation;
        EntryElement secretEntry, dataEntry;
        StyledStringElement pickPhoto;
        Section secretsSection, entriesSection, shareSection;
        Secret currentSecret;
        Server server = new Server ();
        List<string> selectedFilePathArray;
        StyledDialogViewController rootDVC, sectionDVC;
        UIDocumentInteractionController interactionController;
        UIDocumentInteractionControllerDelegateClass interactionControllerDelegate;
        #endregion
        
        #region Public props
        public static History HistoryData { get; set; }
        #endregion

        #region Methods
        public override bool FinishedLaunching (UIApplication app, NSDictionary options)
        {
            NSError error;
#if TESTFLIGHT
            Analytics.SharedTracker.StartTracker("UA-31324545-3",120, null);
            Analytics.SharedTracker.SetReferrer("TestFlight", out error);

            TestFlight.TakeOff("88e03730ca852e81d199baba95b9fc61_MTAxMDc3MjAxMi0wNi0xNyAyMzo0OToxNy44NzEzOTI");
#endif
#if APPSTORE
            Analytics.SharedTracker.StartTracker("UA-31324545-3",120, null);
#endif
            Flurry.StartSession("M3QY6NMPR8H9HSWT6YSM");
            Flurry.SetSessionReportsOnPause(true);

            Analytics.SharedTracker.TrackPageView ("/launched", out error);

            StoreHelper.Load ();

            window = new UIWindow (UIScreen.MainScreen.Bounds);
            window.MakeKeyAndVisible ();
            
            if (selectedFilePathArray == null) {
                selectedFilePathArray = new List<string> ();
            }
            
            RootElement root = CreateRootElement ();
            rootDVC = new StyledDialogViewController (root, null, backgroundColor)
            {
                Autorotate = true, 
                HidesBottomBarWhenPushed = true
            };
            rootDVC.ViewAppearing += (sender, e) => {
                server.Abort (); 
                currentSecret = null;
                NSError err;
                Analytics.SharedTracker.TrackPageView ("/secrets", out err);
                ReOrderSecrets();
            };

            var aboutButton = UIHelper.CreateInfoButton(40f, 60f);
            aboutButton.TouchDown += (sender, e) => {
                ShowAboutView();
            };
            rootDVC.View.AddSubview(aboutButton);

            navigation = new UINavigationController ();
            Flurry.LogAllPageViews(navigation);
            navigation.PushViewController (rootDVC, false);
            navigation.SetNavigationBarHidden (true, false);
            window.RootViewController = navigation;
            
            server.TransferEvent += Paste;

            return true;
        }
        
        public override void OnActivated (UIApplication application)
        {
        }

        public override void DidEnterBackground (UIApplication application)
        {
            Analytics.SharedTracker.Dispatch ();
            StoreHelper.Save ();
        }
        
        public override void WillTerminate (UIApplication application)
        {
            StoreHelper.Save ();
            Analytics.SharedTracker.StopTracker ();
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
                    (secretEntry = new AdvancedEntryElement ("Secret", "enter new phrase", "", null))
                }
            };
            
            secretsSection.AddAll (from s in AppDelegate.HistoryData.Secrets select (Element)CreateImageButtonStringElement (s));

            secretEntry.AutocapitalizationType = UITextAutocapitalizationType.None;
            secretEntry.ShouldReturn += delegate {

                if (String.IsNullOrEmpty (secretEntry.Value))
                    return false;

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

        private Element CreateDataItemElement (DataItem item)
        {
            Element element;

            if ((item.Data.StartsWith (server.CurrentPath)) ||
                (item.Data.StartsWith (ASSETS_LIBRARY)) ||
                (item.Data.StartsWith (BaseDir))) {
                var dataElement = new DataImageStringElement (
                    Path.GetFileName (item.Data),
                    (item.Direction == DataItemDirection.In) ? imgDownload : imgUpload,
                    item.Data
                );
                dataElement.Tapped += delegate {
                    OpenFile (dataElement.Data);
                };
                dataElement.Alignment = (item.Direction == DataItemDirection.In) ? UITextAlignment.Right : UITextAlignment.Left;
                if ((item.Direction == DataItemDirection.In) && 
                    (item.Data.StartsWith (server.CurrentPath))) {
                    dataElement.Animating = true;
                    var localFilePath = Path.Combine (
                        BaseDir,
                        dataElement.Caption);
                    Server.DownloadFileAsync (dataElement.Data, 
                        (s, e) => {
                        var bytes = e.Result;
                        if (bytes == null)
                            throw e.Error;
                        var mediaHelper = new MediaHelper ();
                        mediaHelper.FileSavedToPhotosAlbum += (sender, args) => {
                            dataElement.Data = args.ReferenceUrl;
                            item.Data = dataElement.Data;
                            dataElement.Animating = false;
                        };
                        mediaHelper.SaveFileToPhotosAlbum (localFilePath, bytes);
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
            DataItem item = new DataItem (data, direction, DateTime.Now);
            Paste (item);                
        }

        private void Paste (DataItem item)
        {
            UIApplication.SharedApplication.InvokeOnMainThread (delegate { 
                currentSecret.DataItems.Insert (0, item);
                entriesSection.Insert (0, CreateDataItemElement (item));
            }
            );

        }
       
        private void OpenFile (string filePath)
        {
            var sbounds = UIScreen.MainScreen.Bounds;
            string ext = UrlHelper.GetExtension(filePath);
                  
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
            }  else if (ext.ToUpper () == ".JPEG" || ext.ToUpper () == ".JPG" || ext.ToUpper () == ".PNG") {
                ALAssetsLibrary library = new ALAssetsLibrary ();
                library.AssetForUrl (new NSUrl (filePath), 
                    (asset) => {
                    if (asset != null) {
                        var imageController = new AdvancedUIViewController (); 
                        var image = UIImage.FromImage (asset.DefaultRepresentation.GetFullScreenImage ());
                        var imageView = new UIImageView (image);
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
                    } else {
                        Console.Out.WriteLine ("Asset is null.");
                    }
                }, 
                    (error) => {
                    if (error != null) {
                        Console.Out.WriteLine ("Error: " + error.LocalizedDescription);
                    }
                }
                );
            }  else {
                interactionControllerDelegate = new  UIDocumentInteractionControllerDelegateClass(navigation);
                interactionController = UIDocumentInteractionController.FromUrl(NSUrl.FromFilename(filePath));
                interactionController.Delegate = interactionControllerDelegate;
                InvokeOnMainThread(delegate {
                    interactionController.PresentPreview(true);
                });
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
                delegate {
                loading.Hide ();
            }
            );
        }

        private ImageButtonStringElement CreateImageButtonStringElement (Secret secret)
        {
            var secretElement = new ImageButtonStringElement (secret.Phrase, secret, "Images/remove.png", 
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
            secretElement.Value = " ";
            secret.WatchEvent += (s) => { 
                InvokeOnMainThread (() => {
                    int peers = s.ListenersCount;
                    secretElement.Value = peers > 0 ? peers + " device" + (peers > 1 ? "s" : "") : " ";
                    rootDVC.ReloadData ();
                }
                );
            };

            return secretElement;
        }

        private void DisplaySecretDetail (Secret s)
        {
            NSError error;
            Analytics.SharedTracker.TrackPageView ("/session", out error);

            var subRoot = new RootElement (s.Phrase) 
            {
                (shareSection = new Section ("Keep on server (1 min)") {
                    (pickPhoto = new StyledStringElement ("Photo", delegate { ShowImagePicker(); })),
                    (dataEntry = new AdvancedEntryElement ("Text", "your message", null))}),
                (entriesSection = new Section ("History"))
            };

            pickPhoto.Accessory = UITableViewCellAccessory.DisclosureIndicator;
            dataEntry.ShouldReturn += delegate {
                UIApplication.SharedApplication.InvokeOnMainThread (delegate {
                    dataEntry.GetContainerTableView ().EndEditing (true);
                }
                );
                
                server.Send (dataEntry.Value.Trim ());
                
                return true;
            };
            dataEntry.ReturnKeyType = UIReturnKeyType.Send;


            entriesSection.Elements.AddRange (
                from d in s.DataItems
                select ((Element)CreateDataItemElement (d))
            );

            subRoot.UnevenRows = true;

            sectionDVC = new StyledDialogViewController (
                subRoot,
                true,
                null,
                backgroundColor
            );
            sectionDVC.HidesBottomBarWhenPushed = false;
            navigation.SetNavigationBarHidden (false, true);
            navigation.PushViewController (sectionDVC, true);

            server.CurrentSecret = s;
            currentSecret = s;
            server.Listen ();

            currentSecret.WatchEvent += (secret) => {
                int count = secret.ListenersCount - 1;
                string pattern = "Keep on server (1 min)";
                if (count > 0) {
                    pattern = (count) > 1 ? "Share with {0} devices" : "Share with {0} device";
                }
                UIApplication.SharedApplication.InvokeOnMainThread (delegate {
                    shareSection.Caption = string.Format (pattern, count); 
                    if (sectionDVC != null) {
                        sectionDVC.ReloadData ();
                    }
                }
                );
            };
        }

        private void ShowAboutView ()
        {
            var captionLabel = UIHelper.CreateLabel (
                "about",
                true,
                32,
                32,
                UITextAlignment.Center,
                UIColor.Black
            );
            captionLabel.Frame = new Rectangle (0, 10, 320, 40);
            UIView header = new UIView (new Rectangle (0, 0, 300, 40));
            header.AddSubviews (captionLabel);

            var closeButton = new StyledStringElement ("Close");
            closeButton.BackgroundColor = UIColor.LightGray;
            closeButton.Alignment = UITextAlignment.Center;
            closeButton.Tapped += delegate { 
                navigation.DismissModalViewControllerAnimated(true); 
            };

            var root = new RootElement ("About") 
            {
                new Section (header),
                new Section(UIHelper.CreateHtmlView("About.html", 290f, 300f)),
                new Section() 
                {
                    closeButton
                }
            };
            root.UnevenRows = true;
            var dvc = new StyledDialogViewController (root, null, backgroundColor)
            {
                Autorotate = true,
            };
            navigation.PresentModalViewController(dvc, true);
        }

        private void ReOrderSecrets ()
        {
            secretsSection.Elements.Sort (delegate(Element e1, Element e2) {
                ImageButtonStringElement se1 = e1 as ImageButtonStringElement;
                ImageButtonStringElement se2 = e2 as ImageButtonStringElement;
                if ((se1 != null) && (se2 != null)) {
                    Secret s1 = se1.Data as Secret;
                    Secret s2 = se2.Data as Secret;
                    if ((s1 != null) && (s2 != null)) {
                        return (s1.LastModified.CompareTo (s2.LastModified)) * -1;
                    }
                }
                return -1;
            });
            rootDVC.ReloadComplete();
        }
        #endregion
    }
}

