using System;
using MonoTouch.UIKit;
using System.Drawing;

namespace CrossCopy.iOSClient.Helpers
{
	public class UIHelper
	{
		/// <summary>
		/// Creates the label.
		/// </summary>
		/// <returns>
		/// The label.
		/// </returns>
		/// <param name='text'>
		/// Text.
		/// </param>
		/// <param name='textColor'>
		/// Text color.
		/// </param>
		/// <param name='backgroundColor'>
		/// Background color.
		/// </param>
		/// <param name='fontName'>
		/// Font name.
		/// </param>
		/// <param name='fontSize'>
		/// Font size.
		/// </param>
		/// <param name='x'>
		/// X.
		/// </param>
		/// <param name='y'>
		/// Y.
		/// </param>
		/// <param name='width'>
		/// Width.
		/// </param>
		/// <param name='height'>
		/// Height.
		/// </param>
		public static UILabel CreateLabel(string text, UIColor textColor, UIColor backgroundColor, string fontName, float fontSize, float x, float y, float width, float height)
		{
			UILabel label = new UILabel();
			label.Frame = new RectangleF (x, y, width, height);
	        label.Text = text;
			label.TextColor = textColor;
			label.BackgroundColor = backgroundColor;
        	label.Font = UIFont.FromName(fontName, fontSize);
			return label;
		}
		
		/// <summary>
		/// Creates the text field.
		/// </summary>
		/// <returns>
		/// The text field.
		/// </returns>
		/// <param name='text'>
		/// Text.
		/// </param>
		/// <param name='placeholder'>
		/// Placeholder.
		/// </param>
		/// <param name='x'>
		/// X.
		/// </param>
		/// <param name='y'>
		/// Y.
		/// </param>
		/// <param name='width'>
		/// Width.
		/// </param>
		/// <param name='height'>
		/// Height.
		/// </param>
		public static UITextField CreateTextField(string text, string placeholder, float x, float y, float width, float height)
		{
			UITextField txtField = new UITextField();
			txtField.Frame = new RectangleF (x, y, width, height);
			txtField.BorderStyle = UITextBorderStyle.RoundedRect;
		    txtField.Text = text;
			txtField.Placeholder = placeholder;
			return txtField;
		}
		
		/// <summary>
		/// Creates the text button.
		/// </summary>
		/// <returns>
		/// The text button.
		/// </returns>
		/// <param name='title'>
		/// Title.
		/// </param>
		/// <param name='x'>
		/// X.
		/// </param>
		/// <param name='y'>
		/// Y.
		/// </param>
		/// <param name='width'>
		/// Width.
		/// </param>
		/// <param name='height'>
		/// Height.
		/// </param>
		/// <param name='darkTextColor'>
		/// Dark text color.
		/// </param>
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
		
		/// <summary>
		/// Creates the image button.
		/// </summary>
		/// <returns>
		/// The image button.
		/// </returns>
		/// <param name='image'>
		/// Image.
		/// </param>
		/// <param name='x'>
		/// X.
		/// </param>
		/// <param name='y'>
		/// Y.
		/// </param>
		/// <param name='width'>
		/// Width.
		/// </param>
		/// <param name='height'>
		/// Height.
		/// </param>
		public static UIButton CreateImageButton(string image, float x, float y, float width, float height)
		{
			var button = CreateTextButton("", x, y, width, height, UIColor.Black, UIColor.White);
			button.ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;
			button.SetImage(UIImage.FromFile(image), UIControlState.Normal);
			return button;
		}
		
		/// <summary>
		/// Shows the alert.
		/// </summary>
		/// <param name='title'>
		/// Title.
		/// </param>
		/// <param name='message'>
		/// Message.
		/// </param>
		/// <param name='buttonText'>
		/// Button text.
		/// </param>
		public static void ShowAlert(string title, string message, string buttonText)
		{
			UIAlertView alert = new UIAlertView();
			alert.Title = title;
			alert.AddButton(buttonText);
			alert.Message = message;
			alert.Show();
		}
	}
}

