using System;
using System.Drawing;
using System.Net.NetworkInformation;
using MonoTouch.Foundation;
using MonoTouch.Dialog;
using MonoTouch.UIKit;
using CrossCopy.iOSClient.UI;
using System.Text.RegularExpressions;

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
		
		public static UIViewElement CreateHtmlViewElement(string caption, string value, UITextAlignment alignment)
		{
			var html = Regex.Replace(value, @"((http|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)", "<a href='$1'>$1</a>", RegexOptions.Compiled);
			var style =	@"<style type='text/css'>body { color: #000; background-color:#fafafa; font-family: Helvetica, Arial, sans-serif; font-size:16px; float:" + ((alignment == UITextAlignment.Left) ? "left" : "right") + "; }</style>";
			html = "<html><head>" + style + "</head><body>" + html + "</body>";
			Console.Out.WriteLine("Parsed html: {0}", html);
			
			var web = new AdvancedWebView();
			web.LoadHtmlString(html, null);
					
			var size = new UITextView().StringSize(html, 
			                                       UIFont.SystemFontOfSize(10), 
			                                       new SizeF(300, 2000), 
			                                       UILineBreakMode.WordWrap);
	
			float width = size.Width;
			float height = size.Height;
			web.Bounds = new RectangleF(0, 0, width, height); 
			web.Center = new PointF(width/2+5, height/2+5);

			return new AdvancedUIViewElement(caption, web, false);
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

