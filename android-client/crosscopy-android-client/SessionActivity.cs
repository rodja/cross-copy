using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content.PM;
using MonoDroid.Dialog;
using CrossCopy.BL;
using CrossCopy.Helpers;

namespace CrossCopy.AndroidClient
{
	[Activity(Label = "SessionActivity",
        WindowSoftInputMode = SoftInput.AdjustPan,
        ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation,
        LaunchMode = LaunchMode.SingleTop)]
	public class SessionActivity : Activity
	{
		#region Private members
		Secret secret;
		EntryElement dataEntry;

		Section EntriesSection { get; set; }

		ListView dataList;
		string listData;

		#endregion

		#region Methods
		protected override void OnCreate (Bundle bundle)
		{
			InitializeRoot ();
			base.OnCreate (bundle);
		}

		protected override void OnResume ()
		{
			base.OnResume ();
			string phrase = Intent.GetStringExtra ("Secret");
			CrossCopyApp.Srv.CurrentSecret = secret;
			CrossCopyApp.Srv.TransferEvent += Paste;
			CrossCopyApp.Srv.Listen ();
		}

		protected override void OnPause ()
		{
			CrossCopyApp.Srv.TransferEvent -= Paste;
			CrossCopyApp.Srv.Abort ();
			base.OnPause ();
		}

		protected override void OnNewIntent (Android.Content.Intent intent)
		{
			base.OnNewIntent (intent);
			Intent = intent; // overwrite old intent
		}

		private void InitializeRoot ()
		{

			SetContentView (Resource.Layout.Share);
			listData = "";
			var btnSend = FindViewById<Button> (Resource.Id.buttonGo);
			var txtMsg = FindViewById<EditText> (Resource.Id.textViewUpload);
			btnSend.Click += (sender, args) => {
				if (!String.IsNullOrEmpty (txtMsg.Text)) {
					CrossCopyApp.Srv.Send (txtMsg.Text.Trim ());
					//Paste (new DataItem (txtMsg.Text.Trim (), DataItemDirection.Out, DateTime.Now));
				}
			};

			string phrase = Intent.GetStringExtra ("Secret");
			secret = new Secret (phrase);
			CrossCopyApp.Srv.CurrentSecret = secret;
		}

		public void Paste (DataItem item)
		{
			CrossCopyApp.Srv.CurrentSecret.DataItems.Insert (0, item);
			var history = FindViewById<TextView> (Resource.Id.history);

			RunOnUiThread (() => {
				history.Text = item.Data + "\n" + history.Text;
			});
		}
		#endregion
	}
}






