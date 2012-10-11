using System;
using Android.App;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content.PM;
using Android.Content;
using Android.Database;
using MonoDroid.Dialog;
using CrossCopy.BL;
using System.Collections.Generic;
using CrossCopy.Api;
using System.Net;
using System.IO;
using Android.Provider;
using System.Threading.Tasks;
using Android.Util;
 
namespace CrossCopy.AndroidClient
{
        [Activity(Label = "SessionActivity",
        WindowSoftInputMode = SoftInput.AdjustPan,
        ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation,
        LaunchMode = LaunchMode.SingleTop)]
        public class SessionActivity : Activity
        {
                #region Private members
                #region UI Members
                HistoryListAdapter _adapter;
                ListView _dataList;
                TextView _textToSend;
                TextView _uploadFilename;
                TextView _tvShare;
                ProgressBar _uploadProgress;
#endregion
                Secret _secret;
                Section EntriesSection { get; set; }

                #endregion

                #region Activity Lifecycle
                protected override void OnCreate (Bundle bundle)
                {
                        base.OnCreate (bundle);
                        SetContentView (Resource.Layout.Share);

                        _dataList = FindViewById<ListView> (Resource.Id.listViewHistory);
                        _textToSend = FindViewById<EditText> (Resource.Id.textViewUpload);
                        _tvShare = FindViewById<TextView> (Resource.Id.textViewShare);

                        _uploadFilename = FindViewById<TextView> (Resource.Id.tvUploadFilename);
                        _uploadProgress = FindViewById<ProgressBar> (Resource.Id.uploadProgress);

                        var btnSend = FindViewById<Button> (Resource.Id.buttonGo);
                        btnSend.Click += SendText;

                        var btnChooseImage = FindViewById<Button> (Resource.Id.btnChooseImage);
                        btnChooseImage.Click += ChooseImage;

                        _tvShare.Text = GetString (Resource.String.ShareWith1Device);

                        _secret = new Secret (Intent.GetStringExtra ("Secret"));
                        CrossCopyApp.Srv.CurrentSecret = _secret;

                        LoadHistory ();
                }

                void LoadHistory ()
                {
                        Task.Factory.StartNew (() => {
                                var hl = new List<HistoryList> {
                                new HistoryList { Left="History1", Right="" },
                                new HistoryList { Left="", Right="History2" },
                                new HistoryList { Left="History3", Right="" },
                                new HistoryList { Left="", Right="History4" },
                                new HistoryList { Left="History5", Right="" },
                                new HistoryList { Left="", Right="History6" }
                        };
                        
                                _adapter = new HistoryListAdapter (this, hl);
                                _dataList.Adapter = _adapter;
                                _dataList.ItemClick += listView_ItemClick;
                        });
                }

                protected override void OnResume ()
                {
                        base.OnResume ();
                        CrossCopyApp.Srv.CurrentSecret = _secret;
                        CrossCopyApp.Srv.TransferEvent += Paste;
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

                public void Paste (DataItem item)
                {
                        CrossCopyApp.Srv.CurrentSecret.DataItems.Insert (0, item);
                        /*var history = FindViewById<TextView> (Resource.Id.history);

                RunOnUiThread (() => {
                history.Text = item.Data + "\n" + history.Text;
                });*/
                }
#endregion

        #region Image Management
                void ChooseImage (object sender, EventArgs e)
                {
                        var imageIntent = new Intent ();
                        imageIntent.SetType ("image/*");
                        imageIntent.SetAction (Intent.ActionGetContent);
                        StartActivityForResult (Intent.CreateChooser (imageIntent, GetString (Resource.String.SelectPhoto)), 0);
                }

                protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
                {
                        base.OnActivityResult (requestCode, resultCode, data);
                        
                        if (resultCode == Result.Ok && !String.IsNullOrEmpty (data.DataString)) {
                                _uploadProgress.Progress = 0;

                                var filePath = GetRealPathFromURI (data.Data);
                                if (!String.IsNullOrEmpty (filePath)) {
                                        _uploadFilename.Text = Path.GetFileName (filePath);
                                        CrossCopyApp.Srv.UploadFileAsync (GetRealPathFromURI (data.Data), OnUploadProgress, OnUploadCompleted);
                                }
                        }
                }

                void OnUploadProgress (UploadProgressChangedEventArgs e)
                {
                        RunOnUiThread (() => {
                                _uploadProgress.Progress = e.ProgressPercentage;

                                if (e.ProgressPercentage > 0) {
                                        _uploadProgress.Visibility = ViewStates.Visible;
                                        _uploadFilename.Visibility = ViewStates.Invisible;
                                }

                                // I'll call the OnUploadComplete here because the
                                // real event takes forever to fire...
                                if (e.ProgressPercentage == 100)
                                        OnUploadCompleted ();
                        });
                }

                void OnUploadCompleted ()
                {
                        RunOnUiThread (() => {
                                _uploadProgress.Progress = 100;
                                _uploadFilename.Visibility = ViewStates.Visible;
                                _uploadProgress.Visibility = ViewStates.Invisible;
                        });
                }
#endregion

                void listView_ItemClick (object sender, AdapterView.ItemClickEventArgs e)
                {
                        var item = this._adapter.GetItem (e.Position);
                        Toast.MakeText (this, " Clicked!", ToastLength.Short).Show ();
                }

                #region Text Management
                void SendText (object sender, EventArgs e)
                {
                        if (!String.IsNullOrEmpty (_textToSend.Text)) {
                                CrossCopyApp.Srv.Send (_textToSend.Text.Trim ());
                                //Paste (new DataItem (txtMsg.Text.Trim (), DataItemDirection.Out, DateTime.Now));
                        }
                }
#endregion

                string GetRealPathFromURI (Android.Net.Uri contentURI)
                {
                        var cursor = ContentResolver.Query (contentURI, null, null, null, null); 
                        if (cursor.MoveToFirst ()) {
                                var idx = cursor.GetColumnIndex (MediaStore.Images.ImageColumns.Data); 
                                return cursor.GetString (idx); 
                        }
                        return "";
                }
        }
}
