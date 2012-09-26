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
using System.Collections.Generic;

namespace CrossCopy.AndroidClient
{
	[Activity(Label = "SessionActivity",
        WindowSoftInputMode = SoftInput.AdjustPan,
        ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation,
        LaunchMode = LaunchMode.SingleTop)]
	public class SessionActivity : Activity
	{
		#region Private members
		HistoryListAdapter historyListAdap;
		Secret secret;
		EntryElement dataEntry;

		Section EntriesSection { get; set; }

		ListView dataList;
		string listData;

		#endregion

		#region Methods
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			SetContentView (Resource.Layout.Share);
			
			List<HistoryList> hl = new List<HistoryList> () {
            new HistoryList() { Left="History1", Right="" },
             new HistoryList() { Left="", Right="History2" },
				new HistoryList() { Left="History3", Right="" },
             new HistoryList() { Left="", Right="History4" },
				new HistoryList() { Left="History5", Right="" },
             new HistoryList() { Left="", Right="History6" }
			};
			historyListAdap = new HistoryListAdapter (this, hl);
			var listView = FindViewById<ListView> (Resource.Id.listViewHistory);
			listView.Adapter = historyListAdap;
			listView.ItemClick += listView_ItemClick;
			
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
		
		void listView_ItemClick (object sender, AdapterView.ItemClickEventArgs e)
		{
			var item = this.historyListAdap.GetItem(e.Position);
			Toast.MakeText(this, " Clicked!", ToastLength.Short).Show();
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

		public void Paste (DataItem item)
		{
			CrossCopyApp.Srv.CurrentSecret.DataItems.Insert (0, item);
			/*var history = FindViewById<TextView> (Resource.Id.history);

			RunOnUiThread (() => {
				history.Text = item.Data + "\n" + history.Text;
			});*/
		}
		#endregion
	}
}






