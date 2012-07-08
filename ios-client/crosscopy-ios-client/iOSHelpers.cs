using System;
using System.Drawing;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using CrossCopy.BL;
using CrossCopy.iOSClient.UI;
using MonoTouch.AssetsLibrary;
using CrossCopy.Helpers;

namespace CrossCopy.iOSClient.Helpers
{
    public enum MessageBoxResult
    {
        OK,
        Cancel
    }

    public class UIHelper
    {
        public static UILabel CreateLabel (string text, UIFont font, int fontSize, int labelSize, UITextAlignment alignment, UIColor textColor, UIColor backgroundColor)
        {
            var label = new UILabel (new Rectangle(0,20,300,100));
            label.Lines = 0;
            label.LineBreakMode = UILineBreakMode.WordWrap;
            label.Font = font;
            label.Text = text;
            label.TextAlignment = alignment;
            label.TextColor = textColor;
            label.BackgroundColor = backgroundColor;
            return label;
        }
        
        public static UILabel CreateLabel (string text, bool bold, int fontSize, int labelSize, UITextAlignment alignment, UIColor textColor)
        {
            UIFont font;
            if (bold)
            {
                font = UIFont.BoldSystemFontOfSize(fontSize);
            }
            else
            {
                font = UIFont.SystemFontOfSize(fontSize);
            }
            
            return CreateLabel(text, font, fontSize, labelSize, alignment, textColor, UIColor.Clear);
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
            var button = CreateTextButton("", x, y, width, height, UIColor.Black, UIColor.Clear);
            button.ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;
            button.SetImage(UIImage.FromFile(image), UIControlState.Normal);
            return button;
        }

        public static UIButton CreateInfoButton(float xOffset, float yOffset)
        {
            var button = UIButton.FromType (UIButtonType.InfoDark);
            var bounds = UIScreen.MainScreen.Bounds;
            button.Frame = new RectangleF (bounds.Width-xOffset, bounds.Height-yOffset, 30f, 30f);
            button.SetTitle ("Info", UIControlState.Normal);
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
            var style = @"<style type='text/css'>body { color: #000; background-color:#e0e0e0; font-family: Helvetica, Arial, sans-serif; font-size:16px; float:" + ((alignment == UITextAlignment.Left) ? "left" : "right") + "; }</style>";
            html = "<html><head>" + style + "</head><body>" + html + "</body>";
            Console.Out.WriteLine("Parsed html: {0}", html);
            
            var web = new AdvancedWebView();
            web.LoadHtmlString(html, null);
                    
            var size = web.StringSize(html, 
                                      UIFont.SystemFontOfSize(10), 
                                      new SizeF(UIScreen.MainScreen.Bounds.Width - 20, 2000), 
                                      UILineBreakMode.WordWrap);
            float width = size.Width;
            float height = size.Height;
            web.Bounds = new RectangleF(0, 0, width, height); 
            web.Center = new PointF(width/2, height/2);

            return new AdvancedUIViewElement(caption, web, false);
        }

        public static AdvancedWebView CreateHtmlView(string webFilePath, float width, float height){
            NSUrl webFile = NSUrl.FromFilename (webFilePath);
            NSUrlRequest request = new NSUrlRequest (webFile);
            
            AdvancedWebView web = new AdvancedWebView();
            web.LoadRequest (request);
            web.ScalesPageToFit = false;
            web.SizeToFit ();
            web.Bounds = new RectangleF (0, 0, width, height);
            web.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin | UIViewAutoresizing.FlexibleBottomMargin;
            web.Center = new PointF(width/2+5, height/2+5);
            return web;
        }

        public static UIViewElement CreateHtmlViewElement(string webFilePath, float width, float height)
        {
            return new AdvancedUIViewElement(null, CreateHtmlView(webFilePath, width, height), false);
        }
    }

    public class ByteHelper
    {
        public static void ImageToByteArray (UIImage image, out byte[] mediaByteArray)
        {
            using (NSData imgData = image.AsJPEG ()) 
            {
                mediaByteArray = new byte[imgData.Length];
                System.Runtime.InteropServices.Marshal.Copy (imgData.Bytes, mediaByteArray, 0, Convert.ToInt32 (imgData.Length));
            }
            image.Dispose ();
        }

        public static void VideoToByteArray (NSUrl mediaUrl, out byte[] mediaByteArray)
        {
            using (NSData videoData = NSData.FromUrl (mediaUrl)) 
            {
                mediaByteArray = new byte[videoData.Length];
                System.Runtime.InteropServices.Marshal.Copy (videoData.Bytes, mediaByteArray, 0, Convert.ToInt32 (videoData.Length));
            }
        }
    }

    public class StoreHelper
    {
        private static string historyKey = "history";
        
        public static void Load()
        {
            string serialized = Convert.ToString(NSUserDefaults.StandardUserDefaults[historyKey]);
            
            if (!string.IsNullOrEmpty(serialized))
            {
                AppDelegate.HistoryData = SerializeHelper<History>.FromXmlString(serialized);
                foreach (Secret s in AppDelegate.HistoryData.Secrets){
                    s.StartWatching();
                }
            }
            else 
            {
                AppDelegate.HistoryData = new History();
            }
        }
     
        public static void Save()
        {
            string serialized = SerializeHelper<History>.ToXmlString(AppDelegate.HistoryData);
            NSUserDefaults.StandardUserDefaults[historyKey] = NSObject.FromObject(serialized);
            NSUserDefaults.StandardUserDefaults.Synchronize();
        }
    }

    public class FilesSavedToPhotosAlbumArgs : System.EventArgs
    {
        public FilesSavedToPhotosAlbumArgs(string referenceUrl) { 
            ReferenceUrl = referenceUrl; 
        } 
    
        public string ReferenceUrl { 
            get; 
            set; 
        }
    }

    public class MediaHelper
    {
        public delegate void FileSavedToPhotosAlbumHandler (object sender, FilesSavedToPhotosAlbumArgs args); 
        public event FileSavedToPhotosAlbumHandler FileSavedToPhotosAlbum;

        public void SaveFileToPhotosAlbum (string filePath, byte[] fileData)
        {
            string ext = Path.GetExtension (filePath);

            if (ext.ToUpper () == ".MOV" || ext.ToUpper () == ".M4V") {
                File.WriteAllBytes (filePath, fileData);
                if (UIVideo.IsCompatibleWithSavedPhotosAlbum(filePath)) {
                    UIVideo.SaveToPhotosAlbum(filePath, (path, error) => {
                        if (error == null) {
                            if (FileSavedToPhotosAlbum != null) {
                                FileSavedToPhotosAlbum (this, new FilesSavedToPhotosAlbumArgs (path));
                            }
                        } else {
                            Console.Out.WriteLine ("Video {0} cannot be saved to photos album!", filePath);
                        }
                    });
                }
            } else if (ext.ToUpper () == ".JPEG" || ext.ToUpper () == ".JPG" || ext.ToUpper () == ".PNG") {
                NSData imgData = NSData.FromArray(fileData);
                var img = UIImage.LoadFromData(imgData);
                var meta = new NSDictionary(); 

                ALAssetsLibrary library = new ALAssetsLibrary();
                library.WriteImageToSavedPhotosAlbum (img.CGImage, 
                    meta, 
                    (assetUrl, error) => {
                        if (error == null) {
                            if (FileSavedToPhotosAlbum != null) {
                                FileSavedToPhotosAlbum (this, new FilesSavedToPhotosAlbumArgs (assetUrl.ToString()));
                        } else {
                            Console.Out.WriteLine ("Image {0} cannot be saved to photos album!", filePath);
                        }
                    }
                });
                img.Dispose();
            } else {
                // TODO save other files in the App and make them accessable through itunes; also they should still be opend with iOS build in preview
            }
        }
    }
}

