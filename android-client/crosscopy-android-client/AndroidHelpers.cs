using System;
using Android.Preferences;
using Android.Content;
using CrossCopy.Helpers;
using CrossCopy.BL;

namespace CrossCopy.AndroidClient.Helpers
{
        public class StoreHelper
        {
                private const string historyKey = "history";

                public static void Load (Context appContext)
                {
                        var preferences = PreferenceManager.GetDefaultSharedPreferences (appContext);
                        string serialized = preferences.GetString (historyKey, String.Empty);

                        if (!string.IsNullOrEmpty (serialized)) {
                                CrossCopyApp.HistoryData = SerializeHelper<History>.FromXmlString (serialized);
                                foreach (var s in CrossCopyApp.HistoryData.Secrets) {
                                        s.StartWatching ();
                                }
                        } else {
                                CrossCopyApp.HistoryData = new History ();
                        }
                }
     
                public static void Save (Context appContext)
                {
                        var serialized = SerializeHelper<History>.ToXmlString (CrossCopyApp.HistoryData);
                        var preferences = PreferenceManager.GetDefaultSharedPreferences (appContext);
                        var editor = preferences.Edit ();
                        editor.PutString (historyKey, serialized);
                        editor.Commit ();
                }
        }
}

