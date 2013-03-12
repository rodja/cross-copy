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
using Android.Views.InputMethods;
 
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
                TextView _tvHistoryLabel;
                Button _chooseContent;
                LinearLayout _mainLayout;
                LayoutInflater _inflater;
#endregion
     
                #region Upload File Members
                string _tmpOutFilename;
                string _uploadingFileName;
                string _localUri;
#endregion

                #region CrossCopyApi Members
                Secret _secret;
#endregion
                #region Keyboard Members
                InputMethodManager _inputMethodManager;
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

                        _textToSend = FindViewById<EditText> (Resource.Id.textViewUpload);
                        _tvShareCount = FindViewById<TextView> (Resource.Id.tvShareCount);
                        _tvCodeWord = FindViewById<TextView> (Resource.Id.tvCodeWord);
                        _tvHistoryLabel = FindViewById<TextView> (Resource.Id.tvHistoryLabel);
                        _chooseContent = FindViewById<Button> (Resource.Id.btnChooseContent);
                        _mainLayout = FindViewById<LinearLayout> (Resource.Id.shareLayout);

                        var btnSend = FindViewById<Button> (Resource.Id.buttonSend);
                        btnSend.Click += (object sender, EventArgs e) => {
                                SendText ();
                        };

                        _textToSend.KeyPress += (object sender, View.KeyEventArgs e) => {
                                if (e.KeyCode == Keycode.Enter && e.Event.Action == KeyEventActions.Down) {
                                        SendText ();
                                } else
                                        e.Handled = false;
                        };
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
                        _inputMethodManager = (InputMethodManager)GetSystemService (InputMethodService);
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
                                        SendText ();
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
                        _tvHistoryLabel.Visibility = (_historyItems.Count == 0)
                                                        ? ViewStates.Invisible
                                                        : ViewStates.Visible;

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
                        RunOnUiThread (() => { 
                                var view = _inflater.Inflate (Resource.Layout.HistoryItemView, _mainLayout, false);
                                var progress = view.FindViewById<ProgressBarX> (Resource.Id.transferProgress);
                                var textView = view.FindViewById<TextView> (Resource.Id.tvText);
                                textView.Gravity = GravityFlags.Left;
                                var theNewItem = CreateIncomingItem (item, isOldItem, progress, textView);

                                textView.Text = progress.Text = theNewItem.Incoming; 

                                _historyItems [view] = theNewItem;

                                AddView (view);
                        });
                }

                void AddNewOutgoingItemToHistory (Android.Net.Uri data)
                {
                        // This is the dummy view we create just to speed up showing the
                        // item to the user
                        _localUri = data.Scheme + ":" + data.SchemeSpecificPart;
                        _uploadingFileName = GetDisplayNameFromURI (data);
       
                        var view = _inflater.Inflate (Resource.Layout.HistoryItemView, _mainLayout, false);
                        var textView = view.FindViewById<TextView> (Resource.Id.tvText);
                        textView.Gravity = GravityFlags.Right;
                        
                        var progress = view.FindViewById<ProgressBarX> (Resource.Id.transferProgress);
                        textView.Text = progress.Text = _uploadingFileName;
                        progress.Visibility = ViewStates.Visible;
                        textView.Visibility = ViewStates.Invisible;
                        var historyItem = CreateOutgoingItem (new DataItem (_localUri, DataItemDirection.Out, DateTime.Now));
                        historyItem.LocalPath = _localUri;
                        _historyItems [view] = historyItem;
                        AddView (view);

                        Task.Factory.StartNew (() => {
                                
                                var buffer = new byte[4096];
                                _tmpOutFilename = Path.Combine (System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal), _uploadingFileName);
                                var input = ContentResolver.OpenInputStream (data);
                                var outFile = File.Create (_tmpOutFilename);
                                
                                var readed = 0;
                                while ((readed = input.Read(buffer, 0, 4096)) > 0)
                                        outFile.Write (buffer, 0, (int)readed);
                                
                                input.Close ();
                                outFile.Close ();
                                CrossCopyApp.Srv.UploadFileAsync (_tmpOutFilename, 
                                                                  (e) => {
                                        UpdateProgress (progress, textView, e.ProgressPercentage);},
                                                                   () => {
                                        if (!string.IsNullOrEmpty (_tmpOutFilename)) {
                                                File.Delete (_tmpOutFilename);
                                                _tmpOutFilename = null;
                                        }});
                        });
                } 

                void AddOutgoingItemToHistory (DataItem item)
                {
                        // This happens when we are starting and we are
                        // adding the old items to the history
                        var view = _inflater.Inflate (Resource.Layout.HistoryItemView, _mainLayout, false);
                        var textView = view.FindViewById<TextView> (Resource.Id.tvText);
                        textView.Gravity = GravityFlags.Right;
                        textView.Text = GetDisplayNameFromURI (Android.Net.Uri.Parse (item.Data));
                        _historyItems [view] = CreateOutgoingItem (item);
                        AddView (view);
                }

                void AddView (View view)
                {
                        view.Click += DisplayHistoryItem;
                        RunOnUiThread (() => _mainLayout.AddView (view, 6));
                }

                HistoryItem CreateIncomingItem (DataItem item, bool alreadyDownloaded, ProgressBarX progress, View view)
                {
                        if (!item.Data.StartsWith (CrossCopyApp.Srv.CurrentPath))
                                return new HistoryItem { Incoming = item.Data, Downloading = false };

                        var hItem = new HistoryItem { Incoming = Path.GetFileName (item.Data),
                                                      LocalPath = BaseDir + item.Data.Substring (4, item.Data.Length - 4),
                                                      Downloading = !alreadyDownloaded
                                                     };
                        if (hItem.Downloading) {
                                progress.Visibility = ViewStates.Visible;
                                progress.Indeterminate = true;
                                view.Visibility = ViewStates.Invisible;
                                StartDownload (item.Data, hItem, progress, view);
                        }        

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

                private void StartDownload (string url, HistoryItem hItem, ProgressBarX progress, View view)
                {
                        Directory.CreateDirectory (Path.GetDirectoryName (hItem.LocalPath));
                        Server.DownloadFileAsync (url, hItem.LocalPath,
                                (s, e) => {
                                if (e.Error != null)
                                        Console.Out.WriteLine ("Error fetching file: " + e.Error.ToString ());
                                hItem.Downloading = false;
                                UpdateProgress (progress, view, 100);
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
                        AddNewOutgoingItemToHistory (data);
                }

#endregion

                #region Progress Display
                void UpdateProgress (ProgressBarX progressBar, View view, int progress)
                {
                        RunOnUiThread (() => {
                                progressBar.Progress = progress;

                                if (progress >= 0 && progress < 100) {
                                        progressBar.Visibility = ViewStates.Visible;
                                        view.Visibility = ViewStates.Invisible;
                                } else if (progress == 100) {
                                        progressBar.Indeterminate = false;
                                        progressBar.Visibility = ViewStates.Invisible;
                                        view.Visibility = ViewStates.Visible;
                                }
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
                void SendText ()
                {
                        HideKeyboard ();
                        if (String.IsNullOrEmpty (_textToSend.Text.Trim ()))
                                return;
                        CrossCopyApp.Srv.Send (_textToSend.Text.Trim ());
                        _textToSend.Text = string.Empty;
                }

                void HideKeyboard ()
                {
                        RunOnUiThread (() => {
                                _inputMethodManager.HideSoftInputFromWindow (_textToSend.WindowToken, 0);
                        });
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
