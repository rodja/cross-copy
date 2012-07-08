using System;
using System.Net;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using MonoDroid.Dialog;
using CrossCopy.Api;
using CrossCopy.BL;

namespace CrossCopy.AndroidClient
{
	[Activity (Label = "cross copy", MainLauncher = true, WindowSoftInputMode = SoftInput.AdjustPan)]
	public class Activity1 : Activity
	{
		#region Private members
		EntryElement secretEntry, dataEntry;
		Section entriesSection;
		Server server = new Server ();
		#endregion

		#region Methods
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			var root = CreateRootElement (); 
			var da = new DialogAdapter (this, root);
			var lv = new ListView (this) { Adapter = da };
			SetContentView (lv);

			server.TransferEvent += (sender, e) => {
				Paste (e.Data); 
			};
		}

		private RootElement CreateRootElement ()
		{
			var root = new RootElement ("CrossCopy") {
				new Section() {
               	 	(secretEntry = new EntryElement("Secret", "")),
					new ButtonElement("Listen", delegate {
						server.Send (dataEntry.Value.ToString().Trim ());
						server.CurrentSecret = new Secret(secretEntry.Value.Trim());
						server.Listen ();
					})
                },
                new Section() {
                    (dataEntry = new EntryElement("Message", "")),
					new ButtonElement("Send", delegate {
						server.Send (dataEntry.Value.ToString().Trim ());
						 
					})
                },
				(entriesSection = new Section("History"))
            };

			return root;
		}

		private void Paste (DataItem item)
		{
			RunOnUiThread (() => {
				StringElement element;
				if (item.Direction == DataItemDirection.Out) {
					element = new StringElement (item.Data);	
				} else {
					element = new StringElement ("", item.Data);
				}
				entriesSection.Insert (0, element);
			}
			);
		}
		#endregion
	}
}


