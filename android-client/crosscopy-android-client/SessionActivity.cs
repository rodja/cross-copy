using System;
using System.Linq;
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
        LaunchMode = LaunchMode.SingleTop,
        Theme = "@android:style/Theme.NoTitleBar") ]
        public class SessionActivity : Activity
        {
                #region Private members
                #region UI Members
                Dictionary<View, HistoryItem> _historyItems = new Dictionary<View, HistoryItem> ();
                TextView _textToSend;
                TextView _tvShareCount;
                TextView _tvCodeWord;
                ProgressBar _uploadProgress;
                Button _chooseContent;
                LinearLayout _mainLayout;
                LayoutInflater _inflater;
#endregion
     
                #region Upload File Members
                string _uploadingFileName;
                string _localUri;
#endregion

                #region CrossCopyApi Members
                Secret _secret;
#endregion
   
                #region Constants
                static string BaseDir = "/sdcard/cross-copy";
                static int SELECT_FILE_CODE = 1;
                static int VIEW_FILE_CODE = 2;
#endregion
#endregion

                #region Activity Lifecycle
                protected override void OnCreate (Bundle bundle)
                {
                        base.OnCreate (bundle);
                        SetContentView (Resource.Layout.SessionView);

                        //_history = FindViewById<ListView> (Resource.Id.listViewHistory);
                        _textToSend = FindViewById<EditText> (Resource.Id.textViewUpload);
                        _tvShareCount = FindViewById<TextView> (Resource.Id.tvShareCount);
                        _tvCodeWord = FindViewById<TextView> (Resource.Id.tvCodeWord);
                        _uploadProgress = FindViewById<ProgressBar> (Resource.Id.uploadProgress);
                        _chooseContent = FindViewById<Button> (Resource.Id.btnChooseContent);
                        _mainLayout = FindViewById<LinearLayout> (Resource.Id.shareLayout);

                        var btnSend = FindViewById<Button> (Resource.Id.buttonSend);
                        btnSend.Click += SendText;

                        _chooseContent.Click += ChooseFile;

                        var phrase = Intent.GetStringExtra ("Secret");
                        _secret = CrossCopyApp.HistoryData.Secrets.Where (s => s.Phrase == phrase).SingleOrDefault ();
                        if (_secret == null)
                                Finish ();

                        _secret.WatchEvent += UpdateSharedDevicesCount;
                        CrossCopyApp.Srv.CurrentSecret = _secret;

                        GetString (Resource.String.SessionTitle);

                        _tvCodeWord.Text = _secret.Phrase;
                        _tvShareCount.Text = GetString (Resource.String.ShareWithNoDevices);

                        _inflater = (LayoutInflater)GetSystemService (Context.LayoutInflaterService);
                        HandlePossibleSharedContent ();
                }

                protected override void OnResume ()
                {
                        base.OnResume ();
                        CrossCopyApp.Srv.CurrentSecret = _secret;
                        CrossCopyApp.Srv.TransferEvent += Paste;
                        CrossCopyApp.Srv.Listen ();
                        LoadHistory ();
                }

                protected override void OnPause ()
                {
                        CrossCopyApp.Srv.TransferEvent -= Paste;
                        base.OnPause ();
                }

                protected override void OnNewIntent (Android.Content.Intent intent)
                {
                        base.OnNewIntent (intent);
                        Intent = intent; // overwrite old intent
                }
#endregion

                #region Intent Filter Management
                void HandlePossibleSharedContent ()
                {
                        if (this.Intent.HasExtra ("SharedText")) {
                                RunOnUiThread (() => { 
                                        _textToSend.Text = Intent.GetStringExtra ("SharedText");
                                        SendText (this, EventArgs.Empty);
                                });
                        } else if (this.Intent.HasExtra ("SharedUri")) {
                                var uri = Intent.GetStringExtra ("SharedUri");
                                if (uri != null)
                                        UploadFile (Android.Net.Uri.Parse (uri));
                        }
                }
#endregion
  
                #region History Management
                /// <summary>
                /// Populates the list of history items with the 
                /// values we already know
                /// </summary>
                void LoadHistory ()
                {
                        if (CrossCopyApp.Srv.CurrentSecret.DataItems.Count <= _historyItems.Count)
                                return;

                        foreach (var v in _historyItems.Keys)
                                _mainLayout.RemoveView (v);

                        _historyItems.Clear ();
                        Task.Factory.StartNew (() => {
                                var its = CrossCopyApp.Srv.CurrentSecret.DataItems.ToList ();
                                its.Reverse ();
                                foreach (var d in its)
                                        AddOldItemToHistory (d);
                         
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
                                _tvShareCount.Text = secret.ListenersCount == 1 
                                        ? GetString (Resource.String.ShareWithNoDevices)
                                                : string.Format (GetString (Resource.String.ShareWithNDevices), secret.ListenersCount); 
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
                        if (item.Direction == DataItemDirection.Out && !string.IsNullOrEmpty (_uploadingFileName))
                                item.Data = _localUri;

                        CrossCopyApp.Srv.CurrentSecret.DataItems.Insert (0, item);
                        Task.Factory.StartNew (() => {
                                CrossCopyApp.Save (Application.Context);
                        });

                        if (item.Direction == DataItemDirection.In)
                                AddIncomingItemToHistory (item, false);
                        else if (!string.IsNullOrEmpty (_uploadingFileName)) {
                                _uploadingFileName = string.Empty;
                                _localUri = string.Empty;
                        } else
                                AddOutgoingItemToHistory (item);
                }

                private void AddOldItemToHistory (DataItem item)
                {
                        if (item.Direction == DataItemDirection.In)
                                AddIncomingItemToHistory (item, true);
                        else
                                AddOutgoingItemToHistory (item);
                }

                void AddIncomingItemToHistory (DataItem item, bool isOldItem)
                {
                        var view = _inflater.Inflate (Resource.Layout.HistoryItemView, _mainLayout, false);
                        var theNewItem = CreateIncomingItem (item, isOldItem);
                        var tv = view.FindViewById<TextView> (Resource.Id.textViewLeft);
                        tv.Text = theNewItem.Incoming; 

                        _historyItems [view] = theNewItem;

                        AddView (view);
                }

                void AddOutgoingItemToHistory (DataItem item)
                {
                        View view;
                        HistoryItem historyItem;
                        if (string.IsNullOrEmpty (_uploadingFileName)) {
                                // This happens when we are starting and we are
                                // adding the old items to the history
                                view = _inflater.Inflate (Resource.Layout.HistoryItemView, _mainLayout, false);
                                var tv = view.FindViewById<TextView> (Resource.Id.textViewRight);
                                tv.Text = GetDisplayNameFromURI (Android.Net.Uri.Parse (item.Data));
                                historyItem = CreateOutgoingItem (item);
                        } else {
                                // This is the dummy view we create just to speed up showing the
                                // item to the user
                                view = _inflater.Inflate (Resource.Layout.HistoryItemView, _mainLayout, false);
                                var tv = view.FindViewById<TextView> (Resource.Id.textViewRight);
                                tv.Text = _uploadingFileName;
                                historyItem = CreateOutgoingItem (item);
                                historyItem.LocalPath = _localUri;
                        } 
                        
                        _historyItems [view] = historyItem;
                        AddView (view);
                }

                void AddView (View view)
                {
                        view.Click += DisplayHistoryItem;
                        RunOnUiThread (() => _mainLayout.AddView (view, 6));
                }

                HistoryItem CreateIncomingItem (DataItem item, bool alreadyDownloaded)
                {
                        if (!item.Data.StartsWith (CrossCopyApp.Srv.CurrentPath))
                                return new HistoryItem { Incoming = item.Data, Downloading = false };

                        var hItem = new HistoryItem { Incoming = Path.GetFileName (item.Data),
                                                      LocalPath = BaseDir + item.Data.Substring (4, item.Data.Length - 4),
                                                      Downloading = !alreadyDownloaded
                                                     };
                        if (hItem.Downloading)
                                StartDownload (item.Data, hItem);

                        return hItem;
                }
                
                HistoryItem CreateOutgoingItem (DataItem item)
                {
                        try {
                                var input = GetRealPathFromURI (Android.Net.Uri.Parse (item.Data));
                                if (!string.IsNullOrEmpty (input) && File.Exists (input))
                                        return new HistoryItem { Outgoing = Path.GetFileName (input), 
                                                 LocalPath = item.Data, 
                                                 Downloading = false};
                        } catch (Exception) {
                        }

                        return new HistoryItem { Outgoing = item.Data, Downloading = false };
                }

                private void StartDownload (string url, HistoryItem hItem)
                {
                        Server.DownloadFileAsync (url, (s, e) => {
                                if (e.Result == null) {
                                        Console.Out.WriteLine ("Error fetching file");
                                } else {
                                        Directory.CreateDirectory (Path.GetDirectoryName (hItem.LocalPath));
                                        File.WriteAllBytes (hItem.LocalPath, e.Result);
                                }
                                hItem.Downloading = false;
                        });
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
                        StartActivityForResult (Intent.CreateChooser (intent, GetString (Resource.String.SelectFile)), SELECT_FILE_CODE);
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
                        CrossCopyApp.Srv.CurrentSecret = _secret;
                        if (requestCode == SELECT_FILE_CODE && resultCode == Result.Ok)
                                UploadFile (data.Data);

                        base.OnActivityResult (requestCode, resultCode, data);                        
                }
#endregion

                #region Upload File
                void UploadFile (Android.Net.Uri data)
                {
                        _uploadProgress.Progress = 0;
                        Console.WriteLine (data.Scheme);
                        _localUri = data.Scheme + ":" + data.SchemeSpecificPart;
                        _uploadingFileName = GetDisplayNameFromURI (data);
                        AddOutgoingItemToHistory (new DataItem (_localUri, DataItemDirection.Out, DateTime.Now));
                        Task.Factory.StartNew (() => {
                                _uploadProgress.Progress = 0;
                                var buffer = new byte[4096];
                        
                                var input = ContentResolver.OpenInputStream (data);
                                var mem = new MemoryStream ();
                                var readed = 0;
                                while ((readed = input.Read(buffer, 0, 4096)) > 0)
                                        mem.Write (buffer, 0, (int)readed);

                                input.Close ();
                                if (mem.Length > 0)
                                        CrossCopyApp.Srv.UploadFileAsync (_uploadingFileName, mem.ToArray (), OnUploadCompleted);
                        });
                }

#endregion

                #region Progress Display
                void OnUploadProgress (UploadProgressChangedEventArgs e)
                {
                        RunOnUiThread (() => {
                                _uploadProgress.Progress = e.ProgressPercentage;

                                if (e.ProgressPercentage > 0) {
                                        _uploadProgress.Visibility = ViewStates.Visible;
                                        _chooseContent.Visibility = ViewStates.Invisible;
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
                                _chooseContent.Visibility = ViewStates.Visible;
                                _uploadProgress.Visibility = ViewStates.Invisible;
                        });
                }
#endregion

                #region Display History Item
                void DisplayHistoryItem (object sender, EventArgs e)
                {
                        var item = _historyItems [sender as View];
                        if (item == null || string.IsNullOrEmpty (item.LocalPath))
                                return;

                        if (item.Downloading) {
                                Toast.MakeText (this, "This file is still being transfered, wait until it finishes", ToastLength.Short).Show ();
                                return;
                        }

                        var intent = CreateProperIntent (item);

                        if (PackageManager.QueryIntentActivities (intent, PackageInfoFlags.IntentFilters).Any ())
                                StartActivityForResult (intent, VIEW_FILE_CODE);
                        else
                                Toast.MakeText (this, "No application found to open this content.", ToastLength.Short).Show ();
                }

                Intent CreateProperIntent (HistoryItem item)
                {
                        var intent = new Intent ();
                        intent.SetAction (Intent.ActionView);

                        if (!string.IsNullOrEmpty (item.Outgoing)) {
                                var uri = Android.Net.Uri.Parse (item.LocalPath);
                                intent.SetDataAndType (uri, GetMimeTypeForFile (GetDisplayNameFromURI (uri)));
                        } else {
                                var uri = Android.Net.Uri.FromFile (new Java.IO.File (item.LocalPath));
                                intent.SetDataAndType (uri, GetMimeTypeForFile (item.LocalPath));
                        }

                        return intent;
                }
                #region Mime types

                string GetMimeTypeForFile (string fileAndPath)
                {
                        switch (CrossCopy.Api.UrlHelper.GetExtension (fileAndPath)) {
                        case  ".apk":
                                return "application/vnd.android.package-archive";
                        case ".txt":
                                return "text/plain";
                        case ".srt":
                                return "text/plain";
                        case ".csv":
                                return "text/csv";
                        case ".xml":
                                return "text/xml";
                        case ".htm":
                                return "text/html";
                        case ".html":
                                return "text/html";
                        case ".php":
                                return "text/php";
                        case ".png":
                                return "image/png";
                        case ".gif":
                                return "image/gif";
                        case ".jpg":
                                return "image/jpg";
                        case ".jpeg":
                                return "image/jpeg";
                        case ".bmp":
                                return "image/bmp";
                        case ".mp3":
                                return "audio/mp3";
                        case ".wav":
                                return "audio/wav";
                        case ".ogg":
                                return "audio/x-ogg";
                        case ".mid":
                                return "audio/mid";
                        case ".midi":
                                return "audio/midi";
                        case ".amr":
                                return "audio/AMR";
                        case ".mpeg":
                                return "video/mpeg";
                        case ".mpg":
                                return "video/mpeg";
                        case ".3gp":
                                return "video/3gpp";
                        case ".jar":
                                return "application/java-archive";
                        case ".zip":
                                return "application/zip";
                        case ".rar":
                                return "application/x-rar-compressed";
                        case ".gz":
                                return "application/gzip";
                        }
                
                        return "";
                }
                #endregion
#endregion

                #region Send Text
                void SendText (object sender, EventArgs e)
                {
                        if (String.IsNullOrEmpty (_textToSend.Text))
                                return;
                        CrossCopyApp.Srv.Send (_textToSend.Text.Trim ());
                }
#endregion

                #region Utils
                string GetDisplayNameFromURI (Android.Net.Uri contentURI)
                {
                        if (contentURI.Scheme == "content") {

                                var filePathColumn = new []{
                                        MediaStore.Images.ImageColumns.DisplayName
                                };
                                var cursor = ContentResolver.Query (contentURI, filePathColumn, null, null, null); 
                                if (cursor != null && cursor.MoveToFirst ()) {
                                        var columnIndex = cursor.GetColumnIndex (MediaStore.Images.ImageColumns.DisplayName);
                                        return cursor.GetString (columnIndex); 
                                }
                        } else if (contentURI.Scheme == Uri.UriSchemeFile) {
                                return Path.GetFileName (contentURI.Path);
                        }
                        // return contentURI.Path;
                        return contentURI.Path;
                }

                string GetRealPathFromURI (Android.Net.Uri contentURI)
                {
                        if (contentURI.Scheme == "content") {
                                
                                var filePathColumn = new []{
                                        MediaStore.Images.ImageColumns.Data
                                };
                                var cursor = ContentResolver.Query (contentURI, filePathColumn, null, null, null); 
                                if (cursor != null && cursor.MoveToFirst ()) {
                                        var columnIndex = cursor.GetColumnIndex (MediaStore.Images.ImageColumns.Data);
                                        return cursor.GetString (columnIndex); 
                                }
                        } 
                        //                        else if (contentURI.Scheme == Uri.UriSchemeFile)
                        // return contentURI.Path;
                        return contentURI.Path;
                }
#endregion
        }
}
