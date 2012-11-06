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
                Dictionary<View, SecretItem> _secretItems = new Dictionary<View, SecretItem> ();
                LinearLayout _mainLayout;
                Button _showSecret;
                EditText _newSecret;
                TextView _tvDesc;
                LayoutInflater _inflater;
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
                        SetContentView (Resource.Layout.CodeWordsView);

                        _mainLayout = FindViewById<LinearLayout> (Resource.Id.codeWordsLayout);
                        _newSecret = FindViewById<EditText> (Resource.Id.editTextSecret);
                        _showSecret = FindViewById<Button> (Resource.Id.btnOpen);
                        _tvDesc = FindViewById<TextView> (Resource.Id.textViewDesc);
                        _newSecret.KeyPress += (object sender, View.KeyEventArgs e) =>
                        {
                                if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Down) {
                                        var phrase = _newSecret.Text.Trim ();
                                        if (String.IsNullOrEmpty (phrase))
                                                return;
                                        ShowSecret (phrase);
                                }
                        };
                        _showSecret.Click += OnNewSecret;

                        _inflater = (LayoutInflater)GetSystemService (Context.LayoutInflaterService);
                        HandleIntentFilterFeature ();
                }

                protected override void OnResume ()
                {
                        base.OnResume ();
                        
                        CrossCopyApp.Srv.Abort ();

                        _newSecret.Text = "";
                        LoadCodeWords ();
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
                void LoadCodeWords ()
                {
                        // Remove the views that
                        // are based on the code words
                        foreach (var kvp in _secretItems) { 
                                _mainLayout.RemoveView (kvp.Key);
                        }
                        _secretItems.Clear ();

                        // Start adding all use code words
                        Task.Factory.StartNew (() => {
                                RunOnUiThread (() => {
                                        //  var its = CrossCopyApp.HistoryData.Secrets.ToList ();
                                        //    its.Reverse ();

                                        foreach (var s in CrossCopyApp.HistoryData.Secrets) {
                                                AddCodeWordToView (s);
                                        }
                                });
                        });
                }

                void AddCodeWordToView (Secret s)
                {
                        s.WatchEvent += UpdateSecretDeviceCount;
                        var secret = new SecretItem {
                                Secret = s.Phrase,
                                Devices = s.ListenersCount
                        };
                        var view = _inflater.Inflate (Resource.Layout.SecretItemView, _mainLayout, false);
                        var tv = view.FindViewById<TextView> (Resource.Id.textViewSecrets);
                        tv.Text = secret.Secret;
                        tv = view.FindViewById<TextView> (Resource.Id.textViewDevices);
                        tv.Text = secret.Devices.ToString ();
                        _secretItems [view] = secret;
                        view.Click += OnShowSecret;
                        _mainLayout.AddView (view, 4);
                }

                void UpdateSecretDeviceCount (Secret secret)
                {
                        var item = _secretItems.Where (s => s.Value.Secret == secret.Phrase).SingleOrDefault ();

                        RunOnUiThread (() => {
                                item.Value.Devices = secret.ListenersCount;
                                var tv = item.Key.FindViewById<TextView> (Resource.Id.textViewDevices);
                                tv.Text = item.Value.Devices.ToString ();
                        });
                }

                void OnShowSecret (object sender, EventArgs e)
                {
                        var item = _secretItems [sender as View];
                        if (item == null || string.IsNullOrEmpty (item.Secret))
                                return;

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
                                LoadCodeWords ();
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


