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
		EntryElement secretEntry, dataEntry;
        StyledStringElement pickPhoto;
		Section secretsSection, entriesSection;
        Secret currentSecret;
		string secretValue = string.Empty;
		
		WebRequest request;
	
		WebClient shareClient = new WebClient();
		WebClient receiveClient = new WebClient();
        
		List<string> selectedFilePathArray;
		#endregion
		
        #region Public props
        public static History HistoryData { get; set; }
		public delegate void EventDelegate(object sender, DownloadDataCompletedEventArgs e);
		#endregion

		#region Methods
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			StoreHelper.Load();

            window = new UIWindow (UIScreen.MainScreen.Bounds);
			window.MakeKeyAndVisible ();
			
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
            dvc.ViewLoaded += delegate {
                secretValue = string.Empty;
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

        public override void DidEnterBackground (UIApplication application)
        {
            StoreHelper.Save();
        }
        
        public override void WillTerminate (UIApplication application)
        {
            StoreHelper.Save();
        }
		
		private RootElement CreateRootElement ()
		{
			var captionLabel = UIHelper.CreateLabel ("cross copy", true, 32, 32, UITextAlignment.Center, UIColor.Black);
			var subcaptionLabel = UIHelper.CreateLabel ("Open this App or cross-copy.net on different devices and choose the same secret. " + 
                                                        "You can then transfer stuff between them without any further setup.", false, 13, 39, UITextAlignment.Center, lightTextColor);
			
			var root = new RootElement ("CrossCopy") 
			{
				new Section (captionLabel, subcaptionLabel),
                (secretsSection = new Section ()),
                new Section () 
                {
                    (secretEntry = new AdvancedEntryElement (" ", "pick another secret", "", 
                                                                       delegate { 
                                                                        secretValue = secretEntry.Value; 
                                                                        Listen(); }))
                }
			};
			
            secretsSection.AddAll(from s in AppDelegate.HistoryData.Secrets select (Element)CreateImageButtonStringElement(s));

            secretEntry.AutocapitalizationType = UITextAutocapitalizationType.None;
            secretEntry.ShouldReturn += delegate {
                var newSecret = new Secret(secretEntry.Value);
                AppDelegate.HistoryData.Secrets.Add(newSecret);
                secretsSection.Insert (secretsSection.Elements.Count, UITableViewRowAnimation.Fade, CreateImageButtonStringElement(newSecret));
                secretEntry.Value = "";
                secretEntry.ResignFirstResponder(false);
                DisplaySecretDetail(newSecret);

                return true;
            };
			
			return root;
		}
		
		private void Listen ()
		{
			if (String.IsNullOrEmpty(secretValue))
				return;
            Console.Out.WriteLine("Listen for secret: {0}", secretValue);
			receiveClient.CancelAsync();
			receiveClient.DownloadStringAsync(new Uri(String.Format("{0}/api/{1}{2}", SERVER, secretValue, DeviceID)));
		}

        private Element CreateDataItemElement (DataItem item)
        {
            Element element;

            string apiUrl = string.Format (API, secretValue);
            if (item.Data.IndexOf (apiUrl) != -1) {
                var dataElement = new DataImageStringElement (Path.GetFileName (item.Data.Substring (apiUrl.Length + 1)), (item.Direction == DataItemDirection.In) ? imgDownload : imgUpload, item.Data);
                dataElement.Tapped += delegate {
                    OpenFile (dataElement.Caption);
                };
                dataElement.Alignment = (item.Direction == DataItemDirection.In) ? UITextAlignment.Right : UITextAlignment.Left;
                if (item.Direction == DataItemDirection.In) 
                {
                    dataElement.Animating = true;
                    DownloadFileAsync (SERVER + dataElement.Data, Path.Combine (BaseDir, dataElement.Caption), delegate {
                        dataElement.Animating = false;
                    });
                }
                else 
                {
                    dataElement.Animating = false;
                }

                element = (Element)dataElement;
            }
            else 
            {
                UITextAlignment alignment = (item.Direction == DataItemDirection.In) ? UITextAlignment.Right : UITextAlignment.Left;
                var htmlElement = UIHelper.CreateHtmlViewElement (null, item.Data, alignment);
                element = (Element)htmlElement;
            }

            return element;
        }				
		private void PasteData(string data, DataItemDirection direction)
		{
			UIApplication.SharedApplication.InvokeOnMainThread(delegate
			{ 
				DataItem item = new DataItem(data, direction, DateTime.Now);
				currentSecret.DataItems.Insert(0, item);
				entriesSection.Insert (0, CreateDataItemElement (item));
			});
		}
		
		private void ShareData(string dataToShare)
		{
			if (String.IsNullOrEmpty(secretValue))
                return;

            shareClient.UploadStringAsync(new Uri(String.Format("{0}/api/{1}.json{2}", SERVER, secretValue, DeviceID)), "PUT", dataToShare);
    
		}
		
		private void ShareFile(string filePath)
		{
			using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
			{
				byte[] fileByteArray = new byte[fileStream.Length];
            	fileStream.Read(fileByteArray, 0, fileByteArray.Length);
				ShareFile(filePath, fileByteArray);
			}
		}
		
		private void ShareFile(string filePath, byte[] fileByteArray)
		{
			if (!String.IsNullOrEmpty(secretValue))
			{
				try
				{
					if (request != null)
					{
						request.Abort();
					}
					
					request = HttpWebRequest.Create(SERVER + String.Format(API, secretValue) + "/" + Path.GetFileName(filePath));
					request.Method = "POST";
					request.ContentType = "application/octet-stream";
					((HttpWebRequest)request).AllowWriteStreamBuffering = false;
					request.ContentLength = fileByteArray.Length;
						
					using (var requestStream = request.GetRequestStream())
					{
						requestStream.Write(fileByteArray, 0, fileByteArray.Length);	
					}
					
					using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
					{
						if (response.StatusCode != HttpStatusCode.OK)
						{
							Console.Out.WriteLine("Error fetching data. Server returned status code: {0}", response.StatusCode);
						}
						
					   	using (StreamReader reader = new StreamReader(response.GetResponseStream()))
					   	{
							var content = reader.ReadToEnd();
					        if (!String.IsNullOrWhiteSpace(content)) 
							{
					            Console.Out.WriteLine("Share file: \r\n {0}", content);
								ShareData(response.ResponseUri.AbsoluteUri);
							}
					   }
					}
				}
				catch (Exception ex)
				{
					Console.Out.WriteLine("Exception: {0}", ex.Message);
				}
				
				Listen();
			}
		}
		
		public void UploadFile()
		{
			LoadingView loading = new LoadingView();
			loading.Show("Uploading files, please wait ...");
			
			foreach (string filePath in selectedFilePathArray)
			{
				ShareFile(filePath);
			}
			selectedFilePathArray.Clear();
			
			loading.Hide();
		}
		
		public void UploadFile(string filePath, byte[] fileByteArray)
		{
			LoadingView loading = new LoadingView();
			loading.Show("Uploading file, please wait ...");
			
			ShareFile(filePath, fileByteArray);
			
			selectedFilePathArray.Clear();
			
			loading.Hide();
		}

        public void UploadFileAsync(string filePath, byte[] fileByteArray)
        {
            if (String.IsNullOrEmpty(secretValue))
                return;

            var loading = new LoadingView();
            loading.Show("Uploading file, please wait ...");

            var client = new WebClient ();
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
                    ShareData(string.Format("{0}{1}", SERVER, response));
                }
            };

            Uri fileUri = new Uri(String.Format("{0}/api/{1}/{2}", SERVER, secretValue, UrlHelper.GetFileName(filePath)));
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
//			ThreadPool.QueueUserWorkItem (o => {
//
//				UploadFile (referenceUrl.AbsoluteString, mediaByteArray);
//			});
		}
		
		private void ShowDirectoryTree(string basePath, bool pushing)
		{
			RootElement root = new RootElement(basePath, new RadioGroup (0))
			{
				PopulateTree(basePath)
			};
			
			var dv = new StyledDialogViewController (root, pushing, null, backgroundColor);
			navigation.PushViewController (dv, true);
			navigation.SetNavigationBarHidden(false, true);
			
			var btnSelect = new UIBarButtonItem("Send file", UIBarButtonItemStyle.Bordered, delegate {
				if (selectedFilePathArray.Count == 0)
				{
					UIHelper.ShowAlert("Warning", "No files selected!", "OK");
				}
				else
				{
					navigation.PopToRootViewController(true);
					UploadFile();
				}
            });
			btnSelect.TintColor = UIColor.Black;
			navigation.NavigationBar.TopItem.RightBarButtonItem = btnSelect;
		}
		
		private Section PopulateTree(string basePath)
		{
			Section sect = new Section();
			
			foreach (string dir in Directory.GetDirectories(basePath))
			{
				string strDir = dir;
				string strDirDisplay = strDir.Replace(basePath,"");
				if (strDirDisplay[0] == '/')
				{
					strDirDisplay = strDirDisplay.Remove(0,1);
				}
				
				ImageStringElement element = new ImageStringElement (strDirDisplay, imgFolder);
				element.Tapped += delegate { ShowDirectoryTree(strDir, true); };
			
				sect.Add(element);
			}
			
			foreach (string file in Directory.GetFiles(basePath))
			{
				string strFile = file;
				string strFileDisplay = strFile.Replace(basePath,"");
				if (strFileDisplay[0] == '/')
				{
					strFileDisplay = strFileDisplay.Remove(0,1);
				}
				
				ImageCheckboxElement element = new ImageCheckboxElement (strFileDisplay, false, file, imgFile);
				element.Tapped += delegate { 
					if (element.Value)
					{
						selectedFilePathArray.Add(element.Path); 
					}
				};
							
				sect.Add(element);
			}
			
			return sect;
			
		}

        private ImageButtonStringElement CreateImageButtonStringElement(Secret secret)
        {
            return new ImageButtonStringElement (secret.Phrase, secret, "Images/remove.png", 
                      delegate {
                        DisplaySecretDetail (secret);
                      }, 
                      delegate {
                        AppDelegate.HistoryData.Secrets.Remove(secret);
                        Element found = null;
                        foreach (var element in secretsSection.Elements)
                        {
                            if (element.Caption == secret.Phrase)
                            {
                                found = element;
                                break;
                            }
                        }
                        
                        if (found != null)
                        {
                            secretsSection.Remove (found);
                        }
                      }
                    );
        }

        private void DisplaySecretDetail (Secret s)
        {
            var subRoot = new RootElement (s.Phrase) 
            {
                new Section () {
                    (pickPhoto = new StyledStringElement ("Pick a photo", delegate { ShowImagePicker(); }))
                },
                new Section () {
                    (dataEntry = new AdvancedEntryElement (" ", "enter message", "", delegate {}))
                },
                (entriesSection = new Section ())
            };

            pickPhoto.Alignment = UITextAlignment.Center;
            pickPhoto.BackgroundColor = lightTextColor;
            pickPhoto.TextColor = darkTextColor;

            dataEntry.ShouldReturn += delegate {
                UIApplication.SharedApplication.InvokeOnMainThread (delegate {
                    dataEntry.GetContainerTableView ().EndEditing (true);
                } );
                
                ShareData(dataEntry.Value);
                
                return true;
            } ;

            entriesSection.Elements.AddRange (
                from d in s.DataItems
                select ((Element)CreateDataItemElement(d)));

            subRoot.UnevenRows = true;

            var dvc = new StyledDialogViewController (subRoot, true, null, backgroundColor);
            dvc.HidesBottomBarWhenPushed = false;
            navigation.SetNavigationBarHidden(false, true);
            navigation.PushViewController (dvc, true);

            secretValue = s.Phrase;
            currentSecret = s;
            Listen();
        }
		#endregion
	}
}

