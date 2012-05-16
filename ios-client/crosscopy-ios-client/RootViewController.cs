using System;
using System.Collections.Generic;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using CrossCopy.iOSClient.BL;
using CrossCopy.iOSClient.Helpers;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Threading;
using System.Collections;
using System.Text;

namespace CrossCopy.iOSClient
{
	public partial class RootViewController : UIViewController
	{
		#region Constants
		const string SERVER_API = @"http://www.cross-copy.net/api/{0}";
		#endregion
		
		#region Private members
		UIScrollView scrollView;
		
		UITextField txtSecret, txtData;
		UIButton btnPickFile;
		UITableView tvHistory;
		
		//Hashtable histories;
		List<DataItem> history;
		
		WebRequest request;
		CancellationTokenSource listenCancel;
		CancellationToken listenToken;
		#endregion
		
		#region Ctor
		public RootViewController ()
		{
			
		}
		#endregion
		
		#region ViewControllers methods
		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
		}
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			
			InitLayout();
			
			InitHistory();
		}
		
		public override void ViewDidUnload ()
		{
			base.ViewDidUnload ();
		}
		
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			return true;			
		}
		#endregion
		
		#region Custom methods
		private void InitLayout()
		{
			// colors
			UIColor backgroundColor = new UIColor(224/255.0f, 224/255.0f, 224/255.0f, 1.0f);
			UIColor lightTextColor = new UIColor(163/255.0f, 163/255.0f, 163/255.0f, 1.0f);
			UIColor darkTextColor = new UIColor(102/255.0f, 102/255.0f, 102/255.0f, 1.0f);
			
			scrollView = new UIScrollView (View.Frame);
			scrollView.ShowsHorizontalScrollIndicator = false;
			scrollView.ShowsVerticalScrollIndicator = false;
			scrollView.BackgroundColor = backgroundColor;
			scrollView.AutoresizingMask = UIViewAutoresizing.All;
			View.AddSubview (scrollView);
			
			// caption "cross copy"
			UILabel lblCaption = UIHelper.CreateLabel("cross copy", UIColor.Black, UIColor.Clear, "HelveticaNeue-Bold", 36f, 60f, 5f, 200f, 50f);
			lblCaption.ShadowColor = UIColor.Gray;
        	lblCaption.ShadowOffset = new SizeF(0, 1f);
        	scrollView.AddSubview(lblCaption);
			
			// subcaption "... can transfer stuff between devices"
			UILabel lblSubcaption = UIHelper.CreateLabel("... can transfer stuff between devices", lightTextColor, UIColor.Clear, "HelveticaNeue", 18f, 10f, 40f, 300f, 50f);
			scrollView.AddSubview(lblSubcaption);
			
			// step 1 
			UILabel lblStepOneNumber = UIHelper.CreateLabel("1", lightTextColor, UIColor.Clear, "HelveticaNeue", 36f, 5f, 95f, 20f, 30f);
			scrollView.AddSubview(lblStepOneNumber);
			UILabel lblStepOneText = UIHelper.CreateLabel("open this page on different devices", darkTextColor, UIColor.Clear, "HelveticaNeue", 18f, 30f, 100f, 300f, 20f);
			scrollView.AddSubview(lblStepOneText);
			
			// step 2 
			UILabel lblStepTwoNumber = UIHelper.CreateLabel("2", lightTextColor, UIColor.Clear, "HelveticaNeue", 36f, 5f, 145f, 20f, 30f);
			scrollView.AddSubview(lblStepTwoNumber);
			UILabel lblStepTwoText = UIHelper.CreateLabel("enter the same secret", darkTextColor, UIColor.Clear, "HelveticaNeue", 18f, 30f, 150f, 300f, 20f);
			scrollView.AddSubview(lblStepTwoText);
			
			txtSecret = UIHelper.CreateTextField("", "secret phrase", 30f, 180f, 280f, 30f);
			txtSecret.AutocapitalizationType = UITextAutocapitalizationType.None;
			txtSecret.EditingDidBegin += delegate 
			{
				txtSecret.BecomeFirstResponder();
				UIApplication.SharedApplication.InvokeOnMainThread(delegate
				{
					RectangleF rc = txtSecret.Bounds;
				    rc = txtSecret.ConvertRectToView(rc, scrollView);
				    PointF pt = rc.Location;
				    pt.X = 0;
				    pt.Y -= 180;
				    scrollView.SetContentOffset(pt, true);
				});
			};
			txtSecret.ShouldReturn = delegate
		    {
		        UIApplication.SharedApplication.InvokeOnMainThread(delegate
				{
					scrollView.SetContentOffset(new PointF(0, 0), true); 
				});
				
				txtSecret.ResignFirstResponder();
				Listen ();
				return true;
		    };
			scrollView.AddSubview(txtSecret);
			
			// step 3 
			UILabel lblStepThreeNumber = UIHelper.CreateLabel("3", lightTextColor, UIColor.Clear, "HelveticaNeue", 36f, 5f, 235f, 20f, 30f);
			scrollView.AddSubview(lblStepThreeNumber);
			UILabel lblStepThreeText = UIHelper.CreateLabel("pick a file you want to share", darkTextColor, UIColor.Clear, "HelveticaNeue", 18f, 30f, 240f, 300f, 20f);
			scrollView.AddSubview(lblStepThreeText);
			
			txtData = UIHelper.CreateTextField("", "or type a text and press return", 30f, 270f, 280f, 30f);
			txtData.EditingDidBegin += delegate 
			{
				txtData.BecomeFirstResponder();
				UIApplication.SharedApplication.InvokeOnMainThread(delegate
				{
					RectangleF rc = txtData.Bounds;
				    rc = txtData.ConvertRectToView(rc, scrollView);
				    PointF pt = rc.Location;
				    pt.X = 0;
				    pt.Y -= 170;
				    scrollView.SetContentOffset(pt, true);
				});
			};
			txtData.ShouldReturn = delegate
		    {
		        UIApplication.SharedApplication.InvokeOnMainThread(delegate
				{
					scrollView.SetContentOffset(new PointF(0, 0), true); 
				});
				
				txtData.ResignFirstResponder();
				ShareData();
				
		        return true;
		    };
			scrollView.AddSubview(txtData);
						
			btnPickFile = UIHelper.CreateImageButton("Images/upload.png", 280f, 274f, 20f, 20f);
			btnPickFile.TouchUpInside += (sender, e) => 
			{
				UIHelper.ShowAlert("Button tapped", txtData.Text, "OK");
			};
			scrollView.AddSubview(btnPickFile);
						
			// step 4 
			UILabel lblStepFourNumber = UIHelper.CreateLabel("4", lightTextColor, UIColor.Clear, "HelveticaNeue", 36f, 5f, 325f, 20f, 30f);
			scrollView.AddSubview(lblStepFourNumber);
			UILabel lblStepFourText = UIHelper.CreateLabel("wait for appearing cross copies", darkTextColor, UIColor.Clear, "HelveticaNeue", 18f, 30f, 330f, 300f, 20f);
			scrollView.AddSubview(lblStepFourText);
		}
		
		private void InitHistory()
		{
			if (history == null)
			{
			   	history = new List<DataItem>()
	            {
	                new DataItem("", DataItemDirection.In, new DateTime(2000, 1, 1))
	            };
			}

            tvHistory = new UITableView()
            {
                Delegate = new TableViewDelegate(history),
                DataSource = new TableViewDataSource(history),
                AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth,
                BackgroundColor = UIColor.Clear,
            };
			tvHistory.SizeToFit();
			float height = history.Count * 30f;
			if (height < 100f)
			{
				height = 100f;
			}
			tvHistory.Frame = new RectangleF (5f, 360f, this.View.Frame.Width, height);

            View.AddSubview(tvHistory);
		}
		
		/// <summary>
		/// Listens for data for given secret.
		/// </summary>
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
		
		/// <summary>
		/// Gets the data for given secret.
		/// </summary>
		private void GetData()
		{
			if (txtSecret != null && !String.IsNullOrEmpty(txtSecret.Text))
			{
				if (request != null)
				{
					request.Abort();
				}
				
				request = HttpWebRequest.Create(String.Format(SERVER_API, txtSecret.Text));
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
		
		/// <summary>
		/// Pastes the data.
		/// </summary>
		/// <param name='data'>
		/// Data.
		/// </param>
		/// <param name='direction'>
		/// Direction.
		/// </param>
		private void PasteData(string data, DataItemDirection direction)
		{
			UIApplication.SharedApplication.InvokeOnMainThread(delegate
			{ 
				DataItem item = new DataItem(data, direction, DateTime.Now);
				history.Insert(0, item);
				tvHistory.ReloadData(); 
				tvHistory.SetNeedsDisplay();
			});
		}
		
		/// <summary>
		/// Shares the data.
		/// </summary>
		private void ShareData()
		{
			if (txtSecret != null && !String.IsNullOrEmpty(txtSecret.Text) && 
			    txtData != null && !String.IsNullOrEmpty(txtData.Text))
			{
				try
				{
					listenCancel.Cancel();
																	
					string data = txtData.Text;
					
					if (request != null)
					{
						request.Abort();
					}
					
					request = HttpWebRequest.Create(String.Format(SERVER_API, txtSecret.Text));
	                request.Method = "PUT";
	                request.ContentType= "text";
	                var byteArray = Encoding.UTF8.GetBytes(data);
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
								PasteData (data, DataItemDirection.Out);
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

		#endregion
		
		#region tvHistory delegate & data source
		private class TableViewDelegate : UITableViewDelegate
        {
            private List<DataItem> list;

            public TableViewDelegate(List<DataItem> list)
            {
                this.list = list;
            }

            public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                //Console.WriteLine("TableViewDelegate.RowSelected: Label={0}", list[indexPath.Row]);
            }
        }

        private class TableViewDataSource : UITableViewDataSource
        {
            static NSString kCellIdentifier = new NSString ("MyIdentifier");
            private List<DataItem> list;

            public TableViewDataSource (List<DataItem> list)
            {
//                list.Sort(delegate(DataItem item1, DataItem item2)
//	            {
//					if (item1 != null && item2 != null)
//					{
//						return item1.Date.CompareTo (item2.Date) * -1;
//					}
//					else
//					{
//						return -1;
//					}
//	            });
				
				this.list = list;
            }
			
            public override int RowsInSection (UITableView tableview, int section)
            {
                return list.Count;
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                UITableViewCell cell = tableView.DequeueReusableCell (kCellIdentifier);
                if (cell == null)
                {
                    cell = new UITableViewCell (UITableViewCellStyle.Default, kCellIdentifier);
                }
				
				DataItem item = list[indexPath.Row] as DataItem;
				if (item != null)
				{
					cell.TextLabel.Text = item.Data;
					cell.TextLabel.TextAlignment = (item.Direction == DataItemDirection.In) ? UITextAlignment.Right : UITextAlignment.Left;
				}
                return cell;
            }
        }
		#endregion
	}
}

