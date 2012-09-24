using System;
using System.Net;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using MonoDroid.Dialog;
using CrossCopy.Api;
using CrossCopy.BL;
using System.Collections.Generic;
using CrossCopy.Helpers;

namespace CrossCopy.AndroidClient
{
	[Activity (Label = "cross copy", MainLauncher = true, WindowSoftInputMode = SoftInput.AdjustPan)]
	public class SecretsActivity : Activity
	{
		#region Private members
		//	DialogAdapter rootDA;
		//	EntryElement secretEntry; 
		//	Section secretsSection;
		ListView dataList;
		string listData;
#endregion
		
		#region Methods
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			SetContentView(Resource.Layout.Share);
			
			/*var normalButton = FindViewById<Button>(Resource.Id.btnListen);
			dataList = FindViewById<ListView>(Resource.Id.dataList);
			var txtEditSecret = FindViewById<EditText>(Resource.Id.txtEditSecret);
			normalButton.Click += (sender, args) => {
				Toast.MakeText(this, "Normal button clicked", ToastLength.Short).Show();
				if (!String.IsNullOrEmpty (txtEditSecret.Text))
				{
					Secret newSecret = new Secret (txtEditSecret.Text);
					CrossCopyApp.HistoryData.Secrets.Add (newSecret);
					DisplaySecretDetail (newSecret);
					if(listData.Length>0)
					{
						listData = listData + ",";
						listData = listData + txtEditSecret.Text.Trim();
					}
					else
					{
						listData = listData + txtEditSecret.Text.Trim();
					}
					string [] myArr = listData.Split (',');
					if (myArr.Length > 0) {
						IListAdapter myAdapter= new ArrayAdapter<string>(this,Resource.Layout.listitem,myArr);
						dataList.Adapter = myAdapter;
					}
				}
				
			};*/
		}
		
		protected override void OnResume ()
		{
			base.OnResume ();
			
			CrossCopyApp.Srv.Abort (); 
			
			//			foreach (Secret s in CrossCopyApp.HistoryData.Secrets) {
			//				secretsSection.Add((Element)new StringElement (s.Phrase));
			//			}
			//			if (secretsSection.Count == 0) {
			//                ((RootElement) secretsSection.Parent).RemoveAt (0);
			//            }
		}
		
		//		private RootElement CreateRootElement ()
		//		{
		//			RootElement root = new RootElement ("Secrets"); 
		//			secretsSection = new Section ("Secrets");
		//            var newSecretSection = new Section () 
		//            {
		//                (secretEntry = new EntryElement ("Secret", "")),
		//				new ButtonElement("Listen", delegate {
		//					if (String.IsNullOrEmpty (secretEntry.Value))
		//	                    return;
		//
		//	                Secret newSecret = new Secret (secretEntry.Value);
		//	                CrossCopyApp.HistoryData.Secrets.Add (newSecret);
		//
		//	                if (root.Count == 1)
		//	                    root.Insert (0, secretsSection);
		//
		//	                secretsSection.Insert (
		//	                    0,
		//	                    new StringElement(newSecret.Phrase)
		//	                );
		//	                secretEntry.Value = "";
		//	                DisplaySecretDetail (newSecret);
		//				})
		//            };
		//
		//
		//
		//			root.Add(new List<Section>() { secretsSection, newSecretSection });
		//			return root;
		//		}
		
		private void DisplaySecretDetail (Secret secret)
		{
			var sessionIntent = new Intent();
			sessionIntent.SetClass(this, typeof(SessionActivity));
			sessionIntent.PutExtra("Secret", secret.Phrase);
			StartActivity(sessionIntent);
		}
#endregion
	}
}


