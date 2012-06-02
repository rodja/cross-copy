using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
		#endregion
		
		#region Private members
		UIWindow window;
		UINavigationController navigation;
		EntryElement secret, data;
		Section entries;
		
		string secretValue;
		List<DataItem> history;
		
		WebRequest request;
		CancellationTokenSource listenCancel;
		CancellationToken listenToken;
		
		WebClient webClient = new WebClient();
		
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
			
			webClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
			webClient.DownloadStringCompleted += (sender, e) => { 
				if (e.Cancelled) return;
				if (e.Error != null) {
					Console.Out.WriteLine("Error fetching data: {0}", e.Error.Message);
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
			webClient.CancelAsync();
			webClient.DownloadStringAsync(new Uri(String.Format("{0}/api/{1}{2}", SERVER, secretValue, DeviceID)));
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
					
					DownloadFileAsync(SERVER + entry.Data, Path.Combine(BaseDir, entry.Caption), delegate { entry.Animating = false; });
					
					entry.Animating = true;
					entry.Alignment = (item.Direction == DataItemDirection.In) ? UITextAlignment.Right : UITextAlignment.Left;
					entries.Insert(0, entry);
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
			if (!String.IsNullOrEmpty(secretValue))
                return;

			try
			{
				
				if (request != null)
				{
					request.Abort();
				}
				
				request = HttpWebRequest.Create(SERVER + String.Format(API, secretValue) + DeviceID);
                request.Method = "PUT";
                request.ContentType = "text";
                var byteArray = Encoding.UTF8.GetBytes(dataToShare);
                request.ContentLength = byteArray.Length; 
				
				using (var dataStream = request.GetRequestStream())
				{
                	dataStream.Write(byteArray, 0, byteArray.Length);
                	dataStream.Close();
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
				        if(!String.IsNullOrWhiteSpace(content)) 
						{
							Console.Out.WriteLine("For request {0} share data: \r\n {1}", request.RequestUri.AbsoluteUri, content);
							PasteData (dataToShare, DataItemDirection.Out);
						}
						else 
						{
							Console.Out.WriteLine("Send response is empty");
						}
				   }
				}
			}
			catch (Exception ex)
			{
				Console.Out.WriteLine("Exception {0}", ex.Message);
			}
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
			if (secret != null && !String.IsNullOrEmpty(secret.Value))
			{
				try
				{
					listenCancel.Cancel();
																	
					if (request != null)
					{
						request.Abort();
					}
					
					request = HttpWebRequest.Create(SERVER + String.Format(API, secret.Value) + "/" + Path.GetFileName(filePath));
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
			
			ThreadPool.QueueUserWorkItem (o => {
				UploadFile (referenceUrl.AbsoluteString, mediaByteArray);
			});
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

		#endregion
	}
}

