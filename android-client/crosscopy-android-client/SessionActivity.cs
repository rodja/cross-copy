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
                List<HistoryItem> _historyItems;
                ListView _history;
                TextView _textToSend;
                TextView _uploadFilename;
                TextView _tvShare;
                ProgressBar _uploadProgress;
#endregion
                #region CrossCopyApi Members
                Secret _secret;
#endregion
                #region Constants
                static string BaseDir = "/sdcard/cross-copy";
#endregion
#endregion

                #region Activity Lifecycle
                protected override void OnCreate (Bundle bundle)
                {
                        base.OnCreate (bundle);
                        SetContentView (Resource.Layout.Share);

                        _history = FindViewById<ListView> (Resource.Id.listViewHistory);
                        _textToSend = FindViewById<EditText> (Resource.Id.textViewUpload);
                        _tvShare = FindViewById<TextView> (Resource.Id.textViewShare);

                        _uploadFilename = FindViewById<TextView> (Resource.Id.tvUploadFilename);
                        _uploadProgress = FindViewById<ProgressBar> (Resource.Id.uploadProgress);

                        var btnSend = FindViewById<Button> (Resource.Id.buttonGo);
                        btnSend.Click += SendText;

                        var btnChooseImage = FindViewById<Button> (Resource.Id.btnChooseImage);
                        btnChooseImage.Click += ChooseFile;

                        _secret = new Secret (Intent.GetStringExtra ("Secret"));
                        _secret.WatchEvent += UpdateSharedDevicesCount;
                        CrossCopyApp.Srv.CurrentSecret = _secret;

                        Title = string.Format (GetString (Resource.String.SessionTitle), _secret.Phrase);
                        LoadHistory ();
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
#endregion

                #region History Management
                /// <summary>
                /// Populates the list of history items with the 
                /// values we already know
                /// </summary>
                void LoadHistory ()
                {
                        _historyItems = new List<HistoryItem> ();
                        Task.Factory.StartNew (() => {
                                _adapter = new HistoryListAdapter (this, _historyItems);
                                _history.Adapter = _adapter;
                                _history.ItemClick += DisplayHistoryItem;
                        });
                }

                /// <summary>
                /// Updates the shared devices count display.
                /// </summary>
                /// <param name='secret'>
                /// Secret.
                /// </param>
                void UpdateSharedDevicesCount (Secret secret)
                {
                        RunOnUiThread (() => { 
                                _tvShare.Text = _secret.ListenersCount == 1 
                                        ? GetString (Resource.String.ShareWith1Device)
                                                : string.Format (GetString (Resource.String.ShareWithNDevices), _secret.ListenersCount); 
                        });
                }

                /// <summary>
                /// Called when there is something new in the server for the current secret
                /// Here we have two options:
                ///  - If the item starts with  '/api/CURRENT_SECRET/' it means that this is 
                ///    really a file that should be downloaded, then we start the async download
                ///  - If the item doesn't start with the mentioned text, then is just some text to display.
                /// 
                /// In both cases we add the filename or the string to the history.
                /// </summary>
                /// <param name='item'>
                /// Item: The new item, that contains the information of the new staff.
                /// </param>
                public void Paste (DataItem item)
                {
                        CrossCopyApp.Srv.CurrentSecret.DataItems.Insert (0, item);

                        HistoryItem hItem;
                        if (item.Direction == DataItemDirection.In)
                                hItem = CreateIncomingItem (item);
                        else
                                hItem = CreateOutgoingItem (item);
                
                        RunOnUiThread (() => {
                                _historyItems.Add (hItem);
                                _adapter.NotifyDataSetChanged (); });
                }

                HistoryItem CreateIncomingItem (DataItem item)
                {
                        if (!item.Data.StartsWith (CrossCopyApp.Srv.CurrentPath))
                                return new HistoryItem { Incoming = item.Data, Downloading = false };

                        var lf = Path.Combine (BaseDir, item.Data.Substring (4, item.Data.Length - 4));
                        var hItem = new HistoryItem { Incoming = Path.GetFileName (item.Data), LocalPath = lf, Downloading = false};                               
                        hItem.Downloading = true;
                        Server.DownloadFileAsync (item.Data, (s, e) => {
                                var bytes = e.Result;   
                                if (bytes == null) {
                                        Console.Out.WriteLine ("Error fetching file");
                                        return;
                                }
                                
                                Directory.CreateDirectory (Path.GetDirectoryName (hItem.LocalPath));
                                File.WriteAllBytes (hItem.LocalPath, bytes);
                                hItem.Downloading = false;
                        });
                        return hItem;
                }
                
                HistoryItem CreateOutgoingItem (DataItem item)
                {
                        if (!item.Data.StartsWith (CrossCopyApp.Srv.CurrentPath))
                                return new HistoryItem { Outgoing = item.Data, Downloading = false };
                                        
                        var lf = Path.Combine (BaseDir, item.Data.Substring (4, item.Data.Length - 4));
                        return new HistoryItem { Outgoing = Path.GetFileName (item.Data), LocalPath = lf, Downloading = false};
                }
#endregion

                #region File Selection
                /// <summary>
                /// Launches the activity to allow the user
                /// to choos the file to be uploaded to the current secret.
                /// </summary>
                /// <param name='sender'>
                /// Sender (Ignored).
                /// </param>
                /// <param name='e'>
                /// Also Ignored.
                /// </param>
                void ChooseFile (object sender, EventArgs e)
                {
                        var intent = new Intent ();
                        intent.SetAction (Intent.ActionGetContent);
                        intent.SetType ("*/*");
                        StartActivityForResult (Intent.CreateChooser (intent, GetString (Resource.String.SelectFile)), 1);
                }

                /// <summary>
                /// Called after the user has selected or canceled the file selection
                /// </summary>
                /// <param name='requestCode'>
                /// Request code, maches the request code used during StartActivityforResult (see ChooseFile above)
                /// </param>
                /// <param name='resultCode'>
                /// Result code. If Ok, then the user selected something that we shoud upload
                /// </param>
                /// <param name='data'>
                /// Data, the uri for the selected file.
                /// </param>
                protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
                {
                        base.OnActivityResult (requestCode, resultCode, data);
                        
                        if (requestCode == 1 && resultCode == Result.Ok && !String.IsNullOrEmpty (data.DataString)) {
                                _uploadProgress.Progress = 0;

                                var filePath = GetRealPathFromURI (data.Data);
                                if (!String.IsNullOrEmpty (filePath)) {
                                        _uploadFilename.Text = Path.GetFileName (filePath);
                                        CrossCopyApp.Srv.UploadFileAsync (GetRealPathFromURI (data.Data), OnUploadProgress, OnUploadCompleted);
                                }
                        }
                }
#endregion

                #region Progress Display
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

                #region Display History Item
                void DisplayHistoryItem (object sender, AdapterView.ItemClickEventArgs e)
                {
                        var item = _adapter.GetItem (e.Position);

                        //    if(item == null || string.IsNullOrEmpty(item.Incoming))
                        //          return;

                        //var filename = UrlHelper.GetFileName(item.Incoming);


                        Toast.MakeText (this, " Clicked!", ToastLength.Short).Show ();
                }
#endregion

                #region Send Text
                void SendText (object sender, EventArgs e)
                {
                        if (!String.IsNullOrEmpty (_textToSend.Text)) {
                                CrossCopyApp.Srv.Send (_textToSend.Text.Trim ());
                                //Paste (new DataItem (txtMsg.Text.Trim (), DataItemDirection.Out, DateTime.Now));
                        }
                }
#endregion

                #region Utils
                string GetRealPathFromURI (Android.Net.Uri contentURI)
                {
                        var cursor = ContentResolver.Query (contentURI, null, null, null, null); 
                        if (cursor.MoveToFirst ()) {
                                var idx = cursor.GetColumnIndex (MediaStore.Images.ImageColumns.Data); 
                                return cursor.GetString (idx); 
                        }
                        return "";
                }
#endregion
        }
}
