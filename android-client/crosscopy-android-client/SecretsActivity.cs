using System;
using System.Threading.Tasks;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using CrossCopy.BL;
using System.Collections.Generic;

namespace CrossCopy.AndroidClient
{
        [Activity (Label = "cross copy", MainLauncher = true, WindowSoftInputMode = SoftInput.AdjustPan)]
        public class SecretsActivity : Activity
        {
                #region Private members
                #region UI Members
                ListView _secrets;
                SecretListAdapter _adapter;
                Button _showSecret;
                EditText _newSecret;
#endregion
                #region Secrets Members
                List<SecretItem> _previousSecrets = new List<SecretItem> ();
#endregion
#endregion

                #region Activity Lifecycle
                protected override void OnCreate (Bundle bundle)
                {
                        base.OnCreate (bundle);
                        SetContentView (Resource.Layout.MainScreen);

                        _secrets = FindViewById<ListView> (Resource.Id.listViewSecrets);
                        _newSecret = FindViewById<EditText> (Resource.Id.editTextSecret);
                        _showSecret = FindViewById<Button> (Resource.Id.buttonGo);

                        _showSecret.Click += OnNewSecret;
                        LoadPreviousSecrets ();
                }

                protected override void OnResume ()
                {
                        base.OnResume ();
                        
                        CrossCopyApp.Srv.Abort ();
                }
#endregion
 
                #region Secrets Management
                void LoadPreviousSecrets ()
                {
                        Task.Factory.StartNew (() => {
                                foreach (var s in CrossCopyApp.HistoryData.Secrets) {
                                        s.WatchEvent += UpdateSecretDeviceCount;
                                        _previousSecrets.Add (new SecretItem { Secret = s.Phrase, Devices = s.ListenersCount });
                                }
                                _adapter = new SecretListAdapter (this, _previousSecrets);
                                _secrets.Adapter = _adapter;
                                _secrets.ItemClick += OnShowSecret;
                        });
                }

                void UpdateSecretDeviceCount (Secret secret)
                {
                        var item = _previousSecrets.Where (s => s.Secret == secret.Phrase).SingleOrDefault ();
                        if (item == null)
                                return;

                        RunOnUiThread (() => {
                                item.Devices = secret.ListenersCount;
                                _adapter.NotifyDataSetChanged (); });
                }

                void OnShowSecret (object sender, AdapterView.ItemClickEventArgs e)
                {
                        var item = _adapter [e.Position];
                        ShowSecret (item.Secret);
                }

                void OnNewSecret (object sender, EventArgs e)
                {
                        var phrase = _newSecret.Text.Trim ();
                        if (String.IsNullOrEmpty (phrase))
                                return;

                        ShowSecret (phrase);
                }

                void ShowSecret (string phrase)
                {
                        var newSecret = new Secret (phrase);
                        CrossCopyApp.HistoryData.Secrets.Add (newSecret);
                        
                        var sessionIntent = new Intent ();
                        sessionIntent.SetClass (this, typeof(SessionActivity));
                        sessionIntent.PutExtra ("Secret", phrase);
                        StartActivity (sessionIntent);
                }
#endregion
        }
}


