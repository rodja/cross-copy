using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
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
		static string DeviceID = string.Format("?device_id={0}", DeviceHelper.GetMacAddress());
		static string BaseDir = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
		static UIImage imgFolder = UIImage.FromFile("Images/folder.png");
		static UIImage imgFile = UIImage.FromFile("Images/file.png");
		static UIImage imgDownload = UIImage.FromFile("Images/download.png");
		static UIImage imgUpload = UIImage.FromFile("Images/upload.png");
		static UIColor backgroundColor = new UIColor (224 / 255.0f, 224 / 255.0f, 224 / 255.0f, 1.0f);
		static UIColor lightTextColor = new UIColor (163 / 255.0f, 163 / 255.0f, 163 / 255.0f, 1.0f);
		static UIColor darkTextColor = new UIColor (102 / 255.0f, 102 / 255.0f, 102 / 255.0f, 1.0f);
		#endregion
		
		#region Private members
		UIWindow window;
		UINavigationController navigation;
		EntryElement secret, data;
		Section entries;
		
		List<DataItem> history;
		
		WebRequest request;
		CancellationTokenSource listenCancel;
		CancellationToken listenToken;
		
		List<string> selectedFilePathArray;
		#endregion
		
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
					(secret = new EntryElement (" ", "secret phrase", ""))
				},
				new Section (section3Label) 
				{
					(data = new ImageButtonEntryElement (" ", "or type text", "", "Images/browse.png", delegate {
						ShowDirectoryTree(BaseDir, true);
					}))
				},
				(entries = new Section(section4Label))
			};
			
			secret.AutocapitalizationType = UITextAutocapitalizationType.None;
			secret.ShouldReturn += delegate 
			{
				UIApplication.SharedApplication.InvokeOnMainThread (delegate {
					secret.GetContainerTableView ().EndEditing (true);
				});
				
				Listen ();
				
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
			listenCancel = new CancellationTokenSource();
			listenToken = listenCancel.Token;
			
			var listenTask = Task.Factory.StartNew(() =>
            {
                while (true)
                {
					if (listenToken.IsCancellationRequested)
			        {
			            Console.Out.WriteLine("Listen task cancelled");
			            break;
			        }
					
					GetData();
					
                    Thread.Sleep(500);
                }
            });
		}
		
		private void GetData()
		{
			if (secret != null && !String.IsNullOrEmpty(secret.Value))
			{
				if (request != null)
				{
					request.Abort();
				}
				
				request = HttpWebRequest.Create(SERVER + String.Format(API, secret.Value) + DeviceID);
				request.ContentType = "application/json";
				request.Method = "GET";
				
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
							Console.Out.WriteLine("Get data: \r\n {0}", content);
							PasteData (content, DataItemDirection.In);
						}
				   }
				}
			}
		}
		
		private void PasteData(string data, DataItemDirection direction)
		{
			UIApplication.SharedApplication.InvokeOnMainThread(delegate
			{ 
				DataItem item = new DataItem(data, direction, DateTime.Now);
				
				history.Insert(0, item);
				
				string apiUrl = string.Format(API, secret.Value);
				if (data.IndexOf(apiUrl) != -1)
				{
					DataImageStringElement entry = new DataImageStringElement(Path.GetFileName(data.Substring(apiUrl.Length + 1)), 
					                                                          (item.Direction == DataItemDirection.In) ? imgDownload : imgUpload,
					                                                          data);
					entry.Tapped += delegate {
						MessageBoxResult result = UIHelper.ShowMessageBox("Download file", String.Format("Do you want to download file {0}?", entry.Caption));
						if (result == MessageBoxResult.OK)
						{
							LoadingView loading = new LoadingView(); 
							loading.Show ("Downloading, please wait..."); 
							
							DownloadFile(SERVER + entry.Data, Path.Combine(BaseDir, entry.Caption));
                        	
							loading.Hide();	
						}
					};
					entry.Alignment = (item.Direction == DataItemDirection.In) ? UITextAlignment.Right : UITextAlignment.Left;
					entries.Insert(0, entry);
				}
				else
				{
					StringElement entry = new StringElement(item.Data);
					entry.Alignment = (item.Direction == DataItemDirection.In) ? UITextAlignment.Right : UITextAlignment.Left;
					entries.Insert(0, entry);
				}
				
			});
		}
		
		private void ShareData(string dataToShare)
		{
			if (!String.IsNullOrEmpty(secret.Value))
			{
				try
				{
					listenCancel.Cancel();
																	
					if (request != null)
					{
						request.Abort();
					}
					
					request = HttpWebRequest.Create(SERVER + String.Format(API, secret.Value) + DeviceID);
	                request.Method = "PUT";
	                request.ContentType= "text";
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
					            Console.Out.WriteLine("Share data: \r\n {0}", content);
								PasteData (dataToShare, DataItemDirection.Out);
							}
					   }
					}
				}
				catch (Exception ex)
				{
					Console.Out.WriteLine("Exception {0}", ex.Message);
				}
				
				Listen();
			}
		}
		
		private void ShareFile(string filePath)
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
					
					using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
					{
						request = HttpWebRequest.Create(SERVER + String.Format(API, secret.Value) + "/" + Path.GetFileName(filePath));
						request.Method = "POST";
						request.ContentType = "application/octet-stream";
						request.ContentLength = fileStream.Length;
						
						int buffLength = 1024 * 5;
				        byte[] buffor = new byte[buffLength];
				        
				        using (var requestStream = request.GetRequestStream())
						{
					        while ((buffLength = fileStream.Read(buffor, 0, buffLength)) > 0)
							{
					            requestStream.Write(buffor, 0, buffLength);
							}
						}
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
					
					LoadingView loading = new LoadingView();
					loading.Show("Uploading files, please wait ...");
					
					foreach (string filePath in selectedFilePathArray)
					{
						ShareFile(filePath);
					}
					selectedFilePathArray.Clear();
					
					loading.Hide();
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

