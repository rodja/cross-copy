using System;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using CrossCopy.iOSClient.Helpers;
using System.Drawing;

namespace CrossCopy.iOSClient.UI
{
	public class StyledDialogViewController : DialogViewController 
	{
	    UIImage backgroundImage;
		UIColor backgroundColor;
		
		public StyledDialogViewController (RootElement root, UIImage image, UIColor color) 
			: base (root)
	    {
			backgroundImage = image;
			backgroundColor = color;
	    }
		
		public StyledDialogViewController (RootElement root, bool pushing, UIImage image, UIColor color) 
			: base (root, pushing)
	    {
			backgroundImage = image;
			backgroundColor = color;
	    }
		
		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);
			if (HidesBottomBarWhenPushed)
			{
				if (this.NavigationController != null)
				{
					this.NavigationController.SetNavigationBarHidden(true, true);
				}
				else
				{
					this.NavigationController.SetNavigationBarHidden(false, true);
				}
			}
		}
		
		public override void LoadView ()
		{
			base.LoadView ();
			TableView.BackgroundColor = UIColor.Clear;
        	
			UIColor color;
			if (backgroundImage != null)
			{
				color = UIColor.FromPatternImage(backgroundImage);
			}
			else 
			{
				color = backgroundColor;
				if (color == null)
				{
					color = UIColor.White;
				}
			}
			
        	View.BackgroundColor = color;
		}
	}
	
	public class ImageButtonEntryElement : EntryElement
	{
		public event NSAction ButtonTapped;
		string buttonImage;
		UIButton button;
		
		public ImageButtonEntryElement (string caption, string placeholder, string value, string btnImage, NSAction buttonTapped) 
			: base(caption, placeholder, value)
		{
			buttonImage = btnImage;
			ButtonTapped += buttonTapped;
			button = UIHelper.CreateImageButton(buttonImage, 0f, 0f, 36f, 36f);
			button.TouchDown += HandleTouchDown;
		}
		
		public override UITableViewCell GetCell (UITableView tv)
		{
			var cell = base.GetCell (tv);
			cell.AccessoryView = button;
			return cell;
		}

		void HandleTouchDown (object sender, EventArgs e)
		{
			if (ButtonTapped != null) 
			{
				ButtonTapped();
			}
		}
	}
	
	public class AdvancedEntryElement : EntryElement
	{
		NSObject textFieldChangedObserver;
		
		static NSString cellKey = new NSString ("AdvancedEntryElement");      
	    protected override NSString CellKey 
		{ 
	        get { return cellKey; }
	    }
		public event NSAction TextChanged;
		
		public AdvancedEntryElement (string caption, string placeholder, string value, NSAction textChanged) 
			: base(caption, placeholder, value)
		{
			TextChanged += textChanged;
		}
		
	    protected override UITextField CreateTextField (RectangleF frame)
	    {
	        UITextField tf = base.CreateTextField (frame);
			textFieldChangedObserver = NSNotificationCenter.DefaultCenter.AddObserver(UITextField.TextFieldTextDidChangeNotification, (notification) =>
			{
			    if (notification.Object == tf)
				{
					HandleTextChanged(tf, new EventArgs());
				}
			});
			
	        return tf;
	    }

		void HandleTextChanged (object sender, EventArgs e)
		{
			if (TextChanged != null) 
			{
				TextChanged();
			}
		}
		
		protected override void Dispose (bool disposing)
		{
			NSNotificationCenter.DefaultCenter.RemoveObserver(textFieldChangedObserver);
			base.Dispose (disposing);
		}
	}
	
	public class ImageCheckboxElement : CheckboxElement
	{
        UIImage image;
		public string Path;
		
		public ImageCheckboxElement (string caption, bool value, string path, string grp, UIImage image) 
			: base (caption, value, grp)
        {
            this.image = image;
			this.Path = path;
        }
                        
        public ImageCheckboxElement (string caption, bool value, string path, UIImage image) 
			: base (caption, value)
        {
			this.image = image;
			this.Path = path;
        }

		public ImageCheckboxElement (string caption, string path, UIImage image) 
			: base (caption)
        {
			this.image = image;
			this.Path = path;
        }
		
        public override UITableViewCell GetCell (UITableView tv)
        {
            var cell = base.GetCell (tv);                   
            
			cell.TextLabel.Text = Caption;
			cell.TextLabel.TextAlignment = Alignment;
			cell.ImageView.Image = image;
			
            return cell;
        }
    }
	
	public class DataImageStringElement : ImageStringElement
	{
		public string Data;
		
		public DataImageStringElement(string caption, UIImage image, string data) 
			: base (caption, image)
		{
			this.Data = data;
		}
		
		public DataImageStringElement(string caption, string value, UIImage image, string data) 
			: base(caption, value, image)
		{
			this.Data = data;
		}
		
		public DataImageStringElement(string caption, NSAction tapped, UIImage image, string data) 
			: base(caption, tapped, image)
		{
			this.Data = data;
		}
	}
	
	public class LoadingView : UIAlertView 
    { 
        private UIActivityIndicatorView activityView; 
    
        public void Show(string title) 
        { 
        	InvokeOnMainThread(delegate() { 
            
				Title = title; 
	        
	            activityView = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.WhiteLarge); 
	        	activityView.Frame = new System.Drawing.RectangleF(122,50,40,40); 
	        	AddSubview(activityView); 
	        	        activityView.StartAnimating(); 
	            		Show();
				 
	    
				UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
			});
     	} 
    
        public void Hide() 
        {
			UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false; 
            DismissWithClickedButtonIndex(0, true); 
        } 
    } 
}


