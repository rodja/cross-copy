using System;
using Android.Preferences;
using Android.Content;
using CrossCopy.Helpers;
using CrossCopy.BL;

namespace CrossCopy.AndroidClient.Helpers
{
	public class StoreHelper
    {
        private static string historyKey = "history";
        
        public static void Load(Context appContext)
        {
            var preferences = PreferenceManager.GetDefaultSharedPreferences(appContext);
			string serialized = preferences.GetString(historyKey, String.Empty);

            if (!string.IsNullOrEmpty(serialized))
            {
                AppDelegate.HistoryData = SerializeHelper<History>.FromXmlString(serialized);
                foreach (Secret s in AppDelegate.HistoryData.Secrets){
                    s.StartWatching();
                }
            }
            else 
            {
                AppDelegate.HistoryData = new History();
            }
        }
     
        public static void Save(Context appContext)
        {
            string serialized = SerializeHelper<History>.ToXmlString(AppDelegate.HistoryData);
			var preferences = PreferenceManager.GetDefaultSharedPreferences(appContext);
			var editor = preferences.Edit();
			editor.PutString(historyKey, serialized);
			editor.Commit();
        }
    }
}

