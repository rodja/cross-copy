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
			CrossCopyApp.Srv.TransferEvent += Paste;
			string phrase = Intent.GetStringExtra ("Secret");
			CrossCopyApp.Srv.CurrentSecret = secret;
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

			SetContentView(Resource.Layout.session);
			listData="";
			var btnSend = FindViewById<Button>(Resource.Id.btnListen);
			var txtMsg = FindViewById<EditText>(Resource.Id.txtMsg);
			dataList = FindViewById<ListView>(Resource.Id.dataList);
			btnSend.Click += (sender, args) => {
				if (!String.IsNullOrEmpty(txtMsg.Text))
				{
					CrossCopyApp.Srv.Send (txtMsg.Text.Trim());
					if(listData.Length>0)
					{
						listData = listData + ",";
						listData = listData + txtMsg.Text.Trim();
					}
					else
					{
						listData = listData + txtMsg.Text.Trim();
					}
					string [] myArr = listData.Split (',');
					if (myArr.Length > 0) {
						IListAdapter myAdapter= new ArrayAdapter<string>(this,Resource.Layout.listitem,myArr);
						dataList.Adapter = myAdapter;
					}
				}


			};



		//	string phrase = Intent.GetStringExtra ("Secret");
		//	secret = new Secret (phrase);
//			this.Root = new RootElement (secret.Phrase) 
//            {
//                new Section ("Keep on server (1 min)") {
//                    (dataEntry = new EntryElement ("Text", "your message", null)),
//					new ButtonElement("Send", delegate {
//						if (!String.IsNullOrEmpty(dataEntry.Value))
						//	CrossCopyApp.Srv.Send (dataEntry.Value.Trim ());
//					})
//				},
//                (EntriesSection = new Section ("History"))
//            };

//			EntriesSection.Elements.AddRange (
//                from d in secret.DataItems
//                select ((Element)new StringElement (d.Data))
//			);
            
		}

		public void Paste (DataItem item)
		{
			CrossCopyApp.Srv.CurrentSecret.DataItems.Insert (0, item);

			string [] myArr = listData.Split (',');
			if (myArr.Length > 0) {
				IListAdapter myAdapter= new ArrayAdapter<string>(this,Resource.Layout.listitem,myArr);
				dataList.Adapter = myAdapter;
			}


//			if (EntriesSection != null) {
//				StringElement element;
//				if (item.Direction == DataItemDirection.Out) {
//					element = new StringElement (item.Data);	
//				} else {
//					element = new StringElement ("", item.Data);
//				}
//				EntriesSection.Insert (0, element);
//				RunOnUiThread (() => {
//				//	ReloadData ();
//				});
//			}
		}
		#endregion
	}
}






