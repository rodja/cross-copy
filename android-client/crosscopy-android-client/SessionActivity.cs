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
	public class SessionActivity : DialogActivity
	{
		#region Private members
		Secret secret;
		EntryElement dataEntry;

		Section EntriesSection { get; set; }

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
		}

		protected override void OnPause ()
		{
			CrossCopyApp.Srv.TransferEvent -= Paste;
			base.OnPause ();
		}

		private void InitializeRoot ()
		{
			string phrase = Intent.GetStringExtra ("Secret");
			secret = new Secret (phrase);
			this.Root = new RootElement (secret.Phrase) 
            {
                new Section ("Keep on server (1 min)") {
                    (dataEntry = new EntryElement ("Text", "your message", null)),
					new ButtonElement("Send", delegate {
						CrossCopyApp.Srv.Send (dataEntry.Value.Trim ());
					})
				},
                (EntriesSection = new Section ("History"))
            };

			EntriesSection.Elements.AddRange (
                from d in secret.DataItems
                select ((Element)new StringElement (d.Data))
			);
            
			CrossCopyApp.Srv.CurrentSecret = secret;
			CrossCopyApp.Srv.Listen ();
		}

		public void Paste (DataItem item)
		{
			//RunOnUiThread (() => {
			CrossCopyApp.Srv.CurrentSecret.DataItems.Insert (0, item);
			
			if (EntriesSection != null) {
				StringElement element;
				if (item.Direction == DataItemDirection.Out) {
					element = new StringElement (item.Data);	
				} else {
					element = new StringElement ("", item.Data);
				}
				EntriesSection.Insert (0, element);
			}
			//});
		}
		#endregion
	}
}






