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
                ListView _secretsList;
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

                        _secretsList = FindViewById<ListView> (Resource.Id.listViewSecrets);
                        _newSecret = FindViewById<EditText> (Resource.Id.editTextSecret);
                        _showSecret = FindViewById<Button> (Resource.Id.buttonGo);

                        _showSecret.Click += OnNewSecret;

                        _adapter = new SecretListAdapter (this, _previousSecrets);
                        _secretsList.Adapter = _adapter;
                        _secretsList.ItemClick += OnShowSecret;
                }

                protected override void OnResume ()
                {
                        base.OnResume ();
                        
                        CrossCopyApp.Srv.Abort ();

                        _newSecret.Text = "";
                        LoadSecretsList ();
                }

                protected override void OnPause ()
                {
                        base.OnPause ();
                        Task.Factory.StartNew (() => {
                                CrossCopyApp.Save (Application.Context);
                        });
                }
                        
#endregion
 
                #region Secrets Management
                void LoadSecretsList ()
                {
                        _previousSecrets.Clear ();
                        Task.Factory.StartNew (() => {
                                RunOnUiThread (() => {
                                        foreach (var s in CrossCopyApp.HistoryData.Secrets) {
                                                s.WatchEvent += UpdateSecretDeviceCount;
                                                _previousSecrets.Add (new SecretItem { Secret = s.Phrase, Devices = s.ListenersCount });
                                        }
                                        _previousSecrets.Reverse ();
                                        _adapter.NotifyDataSetChanged (); });
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

                public void OnShowSecret (object sender, AdapterView.ItemClickEventArgs e)
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
                        LoadSecretsList ();
                        var sessionIntent = new Intent ();
                        sessionIntent.SetClass (this, typeof(SessionActivity));
                        sessionIntent.PutExtra ("Secret", phrase);
                        StartActivity (sessionIntent);
                }
#endregion
        }
}


