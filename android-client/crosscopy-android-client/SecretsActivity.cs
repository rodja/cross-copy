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
        [Activity (Label = "cross copy", 
                   MainLauncher = true, 
                   WindowSoftInputMode = SoftInput.AdjustPan,
                   Theme = "@android:style/Theme.NoTitleBar")]
        [IntentFilter (new[]{Intent.ActionSend}, Categories = new []{ Intent.CategoryDefault }, DataMimeType = "text/*" )]
        [IntentFilter (new[]{Intent.ActionSend}, Categories = new []{ Intent.CategoryDefault }, DataMimeType = "image/*" )]
        [IntentFilter (new[]{Intent.ActionSend}, Categories = new []{ Intent.CategoryDefault }, DataMimeType = "video/*" )]
        [IntentFilter (new[]{Intent.ActionSend}, Categories = new []{ Intent.CategoryDefault }, DataMimeType = "*/*" )]
        public class SecretsActivity : Activity
        {
                //[IntentFilter (new[]{Intent.ActionSendMultiple}, DataPath="/")]  
                // Currently only support send one item at the time
                #region Private members
                #region UI Members
                ListView _secretsList;
                SecretListAdapter _adapter;
                Button _showSecret;
                EditText _newSecret;
                TextView _tvDesc;
#endregion
                #region Secrets Members
                List<SecretItem> _previousSecrets = new List<SecretItem> ();
#endregion
                #region Intent Filter Members
                string _theExtraText;
                Android.Net.Uri _theExtraUri;
                bool _sharingExternalContent;
                #endregion
#endregion

                #region Activity Lifecycle
                protected override void OnCreate (Bundle bundle)
                {
                        base.OnCreate (bundle);
                        SetContentView (Resource.Layout.MainScreen);

                        _secretsList = FindViewById<ListView> (Resource.Id.listViewSecrets);
                        _newSecret = FindViewById<EditText> (Resource.Id.editTextSecret);
                        _showSecret = FindViewById<Button> (Resource.Id.btnOpen);
                        _tvDesc = FindViewById<TextView> (Resource.Id.textViewDesc);

                        _showSecret.Click += OnNewSecret;

                        _adapter = new SecretListAdapter (this, _previousSecrets);
                        _secretsList.Adapter = _adapter;
                        _secretsList.ItemClick += (s,e) => OnShowSecret (e);

                        HandleIntentFilterFeature ();
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

                protected override void OnNewIntent (Android.Content.Intent intent)
                {
                        base.OnNewIntent (intent);
                        Intent = intent; // overwrite old intent
                        HandleIntentFilterFeature ();
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

                public void OnShowSecret (AdapterView.ItemClickEventArgs e)
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
                        if (!CrossCopyApp.HistoryData.Secrets.Any (s => s.Phrase == phrase)) {
                                CrossCopyApp.HistoryData.Secrets.Add (new Secret (phrase));
                                LoadSecretsList ();
                        }
                        var sessionIntent = new Intent ();
                        sessionIntent.SetClass (this, typeof(SessionActivity));
                        sessionIntent.PutExtra ("Secret", phrase);

                        if (_sharingExternalContent) {
                                if (!string.IsNullOrEmpty (_theExtraText))
                                        sessionIntent.PutExtra ("SharedText", _theExtraText);
                                else if (_theExtraUri != null)
                                        sessionIntent.PutExtra ("SharedUri", string.Format ("{0}:{1}", _theExtraUri.Scheme, _theExtraUri.EncodedSchemeSpecificPart));
                        }
                               
                        StartActivity (sessionIntent);
                }
#endregion

                #region Intent Filter Management
                void HandleIntentFilterFeature ()
                {
                        if (Intent.ActionSend == this.Intent.Action) {
                                _sharingExternalContent = true;
                                _tvDesc.SetText (Resource.String.FilterDesc);
                                if (this.Intent.Type.Equals ("text/plain"))
                                        _theExtraText = this.Intent.GetStringExtra (Intent.ExtraText);
                                else
                                        _theExtraUri = this.Intent.GetParcelableExtra (Intent.ExtraStream) as Android.Net.Uri;
                        } else {
                                _sharingExternalContent = false;
                                _theExtraText = "";
                                _theExtraUri = null;
                                _tvDesc.SetText (Resource.String.CrossCopyDesc);
                        }
                }
#endregion
        }
}


