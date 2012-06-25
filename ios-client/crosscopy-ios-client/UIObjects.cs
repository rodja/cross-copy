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

        public event EventHandler ViewLoaded;
        public event EventHandler ViewAppearing;
        
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
            if (HidesBottomBarWhenPushed) {
                if (this.NavigationController != null) {
                    this.NavigationController.SetNavigationBarHidden (
                        true,
                        true
                    );
                } else {
                    this.NavigationController.SetNavigationBarHidden (
                        false,
                        true
                    );
                }
            }
            if (ViewAppearing != null) {
                ViewAppearing (this, new EventArgs ());
            }
        }
        
        public override void LoadView ()
        {
            base.LoadView ();
            TableView.BackgroundColor = UIColor.Clear;
            
            UIColor color;
            if (backgroundImage != null) {
                color = UIColor.FromPatternImage (backgroundImage);
            } else {
                color = backgroundColor;
                if (color == null) {
                    color = UIColor.White;
                }
            }
            
            View.BackgroundColor = color;
        }
        
        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
            
            UITapGestureRecognizer tapGesture = new UITapGestureRecognizer (
                this,
                new MonoTouch.ObjCRuntime.Selector ("ViewTappedSelector:")
            );
            tapGesture.CancelsTouchesInView = false;
            this.TableView.AddGestureRecognizer (tapGesture);

            if (ViewLoaded != null) {
                ViewLoaded (this, new EventArgs ());
            }
        }
        
        [Export( "ViewTappedSelector:" )]
        public void ViewTapped (UIGestureRecognizer sender)
        {
            this.TableView.EndEditing (true);
        }

        public override Source CreateSizingSource (bool unevenRows)
        {
            if (unevenRows) {
                return new AdvancedSizingSource (this);
            } else {
                return new AdvancedSource (this);
            } 
        }

        public class AdvancedSource : Source
        {
            public AdvancedSource (DialogViewController container) : base(container)
            {

            }

            public override void WillDisplay (UITableView tableView, UITableViewCell cell, NSIndexPath indexPath)
            {
                base.WillDisplay(tableView, cell, indexPath);
                var section = Root[indexPath.Section];
                if (section.Caption == "History") {
                    cell.BackgroundColor = UIColor.Clear;
                    cell.BackgroundView = null;
                }
            }
        }

        public class AdvancedSizingSource : SizingSource
        {
            public AdvancedSizingSource (DialogViewController container) : base(container)
            {

            }

            public override void WillDisplay (UITableView tableView, UITableViewCell cell, NSIndexPath indexPath)
            {
                base.WillDisplay(tableView, cell, indexPath);
                var section = Root[indexPath.Section];
                if (section.Caption == "History") {
                    cell.BackgroundColor = UIColor.Clear;
                    cell.BackgroundView = null;
                }
            }
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
            button = UIHelper.CreateImageButton (buttonImage, 0f, 0f, 36f, 36f);
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
            if (ButtonTapped != null) {
                ButtonTapped ();
            }
        }
    }

    public class ImageButtonStringElement : StringElement
    {
        static NSString skey = new NSString ("ImageButtonStringElement");

        public event NSAction ButtonTapped;

        string buttonImage;
        UIButton button;
        UIImage image;

        public object Data { get; set; }
        
        public UITableViewCellAccessory Accessory { get; set; }
        
        public ImageButtonStringElement (string caption, object data, string btnImage, NSAction buttonTapped) 
            : base (caption)
        {
            InitElement (data, btnImage, buttonTapped);
        }

        public ImageButtonStringElement (string caption, string value, object data, string btnImage, NSAction buttonTapped) 
            : base (caption, value)
        {
            InitElement (data, btnImage, buttonTapped);
        }
        
        public ImageButtonStringElement (string caption, object data, string btnImage, NSAction tapped, NSAction buttonTapped) 
            : base (caption, tapped)
        {
            InitElement (data, btnImage, buttonTapped);
        }
        
        private void InitElement (object data, string btnImage, NSAction buttonTapped)
        {
            Data = data;
            image = UIImage.FromFile (btnImage);
            buttonImage = btnImage;
            ButtonTapped += buttonTapped;
            button = UIHelper.CreateImageButton (buttonImage, 0f, 0f, 25f, 25f);
            button.TouchDown += HandleTouchDown;
            this.Accessory = UITableViewCellAccessory.DisclosureIndicator;
        }
        
        protected override NSString CellKey {
            get {
                return skey;
            }
        }

        public override UITableViewCell GetCell (UITableView tv)
        {
            var cell = tv.DequeueReusableCell (CellKey);
            if (cell == null) {
                cell = new UITableViewCell (
                    Value == null ? UITableViewCellStyle.Default : UITableViewCellStyle.Value1,
                    CellKey
                );
                cell.SelectionStyle = UITableViewCellSelectionStyle.Blue;
            }
            
            cell.Accessory = Accessory;
            cell.TextLabel.Text = Caption;
            cell.TextLabel.TextAlignment = Alignment;
            
            cell.ImageView.Image = image;
            cell.ImageView.UserInteractionEnabled = true;
            cell.ImageView.AddSubview (button);
            cell.ImageView.BringSubviewToFront (button);
                                    
            if (cell.DetailTextLabel != null) {
                cell.DetailTextLabel.Text = Value == null ? "" : Value;
                cell.DetailTextLabel.TextColor = UIColor.Gray;
                cell.DetailTextLabel.Font = UIFont.SystemFontOfSize (13);
                cell.DetailTextLabel.TextAlignment = UITextAlignment.Right;
            }
            
            return cell;
        }
        
        void HandleTouchDown (object sender, EventArgs e)
        {
            if (ButtonTapped != null) {
                ButtonTapped ();
            }
        }
        
    }
    
    public class AdvancedEntryElement : EntryElement
    {
        NSObject textFieldChangedObserver;
        static NSString cellKey = new NSString ("AdvancedEntryElement");

        protected override NSString CellKey { 
            get { return cellKey; }
        }

        public event NSAction TextChanged;
        
        public AdvancedEntryElement (string caption, string placeholder, string value, NSAction textChanged = null) 
            : base(caption, placeholder, value)
        {
            TextChanged += textChanged;
        }

        public override UITableViewCell GetCell (UITableView tv)
        {
            UITableViewCell cell = base.GetCell (tv);
            cell.BackgroundColor = UIColor.White; 
            return cell;
        }

        protected override UITextField CreateTextField (RectangleF frame)
        {
            UITextField tf = base.CreateTextField (frame);
            textFieldChangedObserver = NSNotificationCenter.DefaultCenter.AddObserver (UITextField.TextFieldTextDidChangeNotification, (notification) =>
            {
                if (notification.Object == tf) {
                    HandleTextChanged (tf, new EventArgs ());
                }
            }
            );
            
            return tf;
        }

        void HandleTextChanged (object sender, EventArgs e)
        {
            if (TextChanged != null) {
                TextChanged ();
            }
        }
        
        protected override void Dispose (bool disposing)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver (textFieldChangedObserver);
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
    
    public class DataImageStringElement : ImageStringElement, IElementSizing
    {
        UIActivityIndicatorView activity;
        public string Data;
        
        public DataImageStringElement (string caption, UIImage image, string data) 
            : base (caption, image)
        {
            this.Data = data;
            InitActivity ();
        }
        
        public DataImageStringElement (string caption, string value, UIImage image, string data) 
            : base(caption, value, image)
        {
            this.Data = data;
            InitActivity ();
        }
        
        public DataImageStringElement (string caption, NSAction tapped, UIImage image, string data) 
            : base(caption, tapped, image)
        {
            this.Data = data;
            InitActivity ();
        }
                
        private void InitActivity ()
        {
            activity = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.Gray);
        }
        
        public override UITableViewCell GetCell (UITableView tv)
        {
            var cell = base.GetCell (tv);
            var sbounds = UIScreen.MainScreen.Bounds;
            activity.Frame = new RectangleF ((sbounds.Width - 30), 7, 20, 20);
            cell.AccessoryView = activity;
            cell.BackgroundColor = UIColor.Clear;
            return cell;
        }
        
        public bool Animating {
            get {
                return activity.IsAnimating;
            }
            set {
                if (value) {
                    activity.StartAnimating ();
                } else {
                    activity.StopAnimating ();
                }
            }
        }

        float IElementSizing.GetHeight (UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
        {
            var captionFont = UIFont.BoldSystemFontOfSize (17);
            float height = tableView.StringSize (Caption, captionFont).Height;
            return height + 10;
        }
    }
    
    public class LoadingView : UIAlertView
    { 
        private UIActivityIndicatorView activityView;
    
        public void Show (string title)
        { 
            InvokeOnMainThread (delegate() { 
            
                Title = title; 
            
                activityView = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.WhiteLarge); 
                activityView.Frame = new System.Drawing.RectangleF (
                    122,
                    50,
                    40,
                    40
                ); 
                AddSubview (activityView); 
                activityView.StartAnimating (); 
                Show ();
                 
        
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
            }
            );
        }
    
        public void Hide ()
        {
            InvokeOnMainThread (delegate() { 
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false; 
                DismissWithClickedButtonIndex (0, true); 
            }
            );
        } 
    }

    public class ProgressView : UIAlertView
    { 
        private UIProgressView progress;
    
        public void Show (string title)
        { 
            InvokeOnMainThread (delegate() { 
            
                Title = title; 
            
                progress = new UIProgressView ();
                progress.Frame = new RectangleF (30f, 80f, 225f, 80f);
                progress.Style = UIProgressViewStyle.Default;
                progress.Progress = 0;
                AddSubview (progress);

                Show ();

                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = true;
            }
            );
        }
    
        public void Hide ()
        {
            InvokeOnMainThread (delegate() { 
                UIApplication.SharedApplication.NetworkActivityIndicatorVisible = false; 
                DismissWithClickedButtonIndex (0, true); 
            }
            );
        }

        public void Update (int percent)
        {
            InvokeOnMainThread (delegate() { 
                progress.Progress = (float)(percent / 100); 
            }
            );
        }
    }
    
    public class AdvancedWebView : UIWebView
    {
        public AdvancedWebView () : base()
        {
            ShouldStartLoad += OpenInSafari;
            BackgroundColor = UIColor.Clear;
        }
        
        bool OpenInSafari (UIWebView sender, NSUrlRequest request, UIWebViewNavigationType navType)
        {
            Console.Out.WriteLine (
                "Request {0}, Navigation type {1}",
                request.Url.AbsoluteUrl,
                navType
            );
            if (navType == UIWebViewNavigationType.LinkClicked) {
                UIApplication.SharedApplication.OpenUrl (request.Url);
                return false;
            } else {
                return true;
            }
        }
    }
    
    public class AdvancedUIViewElement : UIViewElement, IElementSizing
    {
        public AdvancedUIViewElement (string caption, UIView view, bool transparent) 
            : base(caption, view, transparent)
        {
            View.AutosizesSubviews = true;
        }
        
        float IElementSizing.GetHeight (UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
        {
            return base.GetHeight (tableView, indexPath);
        }

        public override UITableViewCell GetCell (UITableView tv)
        {
            var cell = base.GetCell (tv);
            cell.BackgroundColor = UIColor.Clear;
            return cell;
        }
    }
    
    public class AdvancedUIViewController : UIViewController
    {
        public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
        {
            return true;
        }
    }
}


