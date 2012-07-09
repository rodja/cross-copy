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
		#endregion

		#region Methods
		protected override void OnCreate(Bundle bundle)
        {
            InitializeRoot();
            base.OnCreate(bundle);
        }

		protected override void OnStop ()
		{
			base.OnStop ();
			AppDelegate.EntriesSection = null;
		}

        private void InitializeRoot()
        {
			string serializedSecret = Intent.GetStringExtra("Secret");
			secret = SerializeHelper<Secret>.FromXmlString(serializedSecret);
			this.Root = new RootElement (secret.Phrase) 
            {
                new Section ("Keep on server (1 min)") {
                    (dataEntry = new EntryElement ("Text", "your message", null)),
					new ButtonElement("Send", delegate {
						AppDelegate.Srv.Send (dataEntry.Value.Trim ());
					})
				},
                (AppDelegate.EntriesSection = new Section ("History"))
            };

            AppDelegate.EntriesSection.Elements.AddRange (
                from d in secret.DataItems
                select ((Element)new StringElement(d.Data))
            );
            
            AppDelegate.Srv.CurrentSecret = secret;
			AppDelegate.CurrentSecret = secret;
            AppDelegate.Srv.Listen ();
		}
		#endregion
    }
}






