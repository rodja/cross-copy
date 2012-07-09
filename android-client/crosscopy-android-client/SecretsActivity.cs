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
		DialogAdapter rootDA;
		EntryElement secretEntry; 
		Section secretsSection;
		#endregion

		#region Methods
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			var root = CreateRootElement (); 
			rootDA = new DialogAdapter (this, root);
			var lv = new ListView (this) { Adapter = rootDA };
			SetContentView (lv);
		}

		protected override void OnResume ()
		{
			base.OnResume ();

			AppDelegate.Srv.Abort (); 
            AppDelegate.CurrentSecret = null;
		}

		private RootElement CreateRootElement ()
		{
			var root = new RootElement ("Secrets"); 
			secretsSection = new Section ("Secrets");
            var newSecretSection = new Section () 
            {
                (secretEntry = new EntryElement ("Secret", "")),
				new ButtonElement("Listen", delegate {
					if (String.IsNullOrEmpty (secretEntry.Value))
	                    return;

	                var newSecret = new Secret (secretEntry.Value);
	                AppDelegate.HistoryData.Secrets.Add (newSecret);

	                if (root.Count == 1)
	                    root.Insert (0, secretsSection);

	                secretsSection.Insert (
	                    secretsSection.Elements.Count,
	                    new StringElement(newSecret.Phrase)
	                );
	                secretEntry.Value = "";
	                DisplaySecretDetail (newSecret);
				})
            };

			foreach (var s in AppDelegate.HistoryData.Secrets) {
				secretsSection.Add((Element)new StringElement (s.Phrase));
			}
			if (secretsSection.Count == 0) {
                root.RemoveAt (0);
            }

			root.Add(new List<Section>() { secretsSection, newSecretSection });
			return root;
		}

		private void DisplaySecretDetail (Secret secret)
        {
            var sessionIntent = new Intent();
            sessionIntent.SetClass(this, typeof(SessionActivity));
            sessionIntent.AddFlags(ActivityFlags.NewTask);
			string serializedSecret = SerializeHelper<Secret>.ToXmlString(secret);
            sessionIntent.PutExtra("Secret", serializedSecret);
			StartActivity(sessionIntent);
        }
		#endregion
	}
}


