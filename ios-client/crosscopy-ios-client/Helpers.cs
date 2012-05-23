using System;
using MonoTouch.UIKit;
using System.Drawing;
using System.Net.NetworkInformation;
using MonoTouch.Foundation;

namespace CrossCopy.iOSClient.Helpers
{
	public enum MessageBoxResult
    {
        OK,
        Cancel
    }

	public class UIHelper
	{
		public static UILabel CreateLabel (string text, UIFont font, int size, UITextAlignment alignment, UIColor textColor, UIColor backgroundColor)
		{
			var label = new UILabel ();
	        var frame = label.Frame;
	        frame.Inflate(0, size);
	        label.Frame = frame;
			label.Font = font;
	        label.Text = text;
	        label.TextAlignment = alignment;
			label.TextColor = textColor;
	        label.BackgroundColor = backgroundColor;
			return label;
		}
		
		public static UILabel CreateLabel (string text, bool bold, int size, UITextAlignment alignment, UIColor textColor)
		{
			UIFont font;
			if (bold)
			{
				font = UIFont.BoldSystemFontOfSize(size);
			}
			else
			{
				font = UIFont.SystemFontOfSize(size);
			}
			
			return CreateLabel(text, font, size, alignment, textColor, UIColor.Clear);
		}
		
		public static UIButton CreateTextButton(string title, float x, float y, float width, float height, UIColor titleColor, UIColor backgroundColor)
		{
			var frame = new RectangleF (x, y, width, height);
			var button = new UIButton (frame) 
			{
				VerticalAlignment = UIControlContentVerticalAlignment.Center,
				HorizontalAlignment = UIControlContentHorizontalAlignment.Center,
				BackgroundColor = UIColor.Clear
			};

			button.SetTitle (title, UIControlState.Normal);
			button.SetTitleColor (titleColor, UIControlState.Normal);
			button.BackgroundColor = backgroundColor;
			
			return button;
		}
		
		public static UIButton CreateImageButton(string image, float x, float y, float width, float height)
		{
			var button = CreateTextButton("", x, y, width, height, UIColor.Black, UIColor.White);
			button.ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;
			button.SetImage(UIImage.FromFile(image), UIControlState.Normal);
			return button;
		}
		
		public static void ShowAlert(string title, string message, string buttonText)
		{
			UIApplication.SharedApplication.InvokeOnMainThread(delegate
			{
				UIAlertView alert = new UIAlertView();
				alert.Title = title;
				alert.AddButton(buttonText);
				alert.Message = message;
				alert.Show();
			});
		}

    	public static MessageBoxResult ShowMessageBox(string caption, string message)
        {
            MessageBoxResult res = MessageBoxResult.Cancel;
			
			int clicked = -1;
		    var alert = new UIAlertView (caption, message,  null, "Cancel", "OK");
		    alert.Show ();
		    alert.Clicked += (sender, buttonArgs) => {
		        clicked = buttonArgs.ButtonIndex;
		    };    
		    while (clicked == -1)
			{
				NSRunLoop.Current.RunUntil (NSDate.FromTimeIntervalSinceNow (0.5));
		    }
		
		    if (clicked == 1)
			{
				res = MessageBoxResult.OK;
			}
            
            return res;
        }
	}
	
	public class DeviceHelper
	{
		public static string GetMacAddress()
	    {
	        string macAddresses = "";
	
	        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
	        {
	            if (nic.OperationalStatus == OperationalStatus.Up)
	            {
	                macAddresses += nic.GetPhysicalAddress().ToString();
	                break;
	            }
	        }
			
	        return macAddresses;
	    }
	}
}

