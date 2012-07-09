using System;
using Android.App;
using Android.Runtime;
using MonoDroid.Dialog;
using CrossCopy.Api;
using CrossCopy.BL;
using CrossCopy.AndroidClient.Helpers;

namespace CrossCopy.AndroidClient
{
	[Application(Label = "cross copy")]
	public class AppDelegate : Application
	{
		#region Public props
        public static History HistoryData { get; set; }
		public static Secret CurrentSecret { get; set; }
		public static Server Srv { get; set; }
		public static Section EntriesSection { get; set; }

        #endregion

		#region Ctor
		public AppDelegate (IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {
        }
		#endregion

		#region Methods
		public override void OnCreate()
        {
			base.OnCreate();
			StoreHelper.Load (ApplicationContext);
			Srv = new Server ();
			Srv.TransferEvent += (sender, e) => {
				Paste (e.Data); 
			};
        }

		public override void OnLowMemory()
       	{
			base.OnLowMemory();
       	}

		public override void OnTerminate()
       	{
            base.OnTerminate();
			StoreHelper.Save (ApplicationContext);
       	}

		public void Paste (DataItem item)
		{
			//RunOnUiThread (() => {
				CurrentSecret.DataItems.Insert (0, item);
			
				if (EntriesSection != null) {
					StringElement element;
					if (item.Direction == DataItemDirection.Out) {
						element = new StringElement (item.Data);	
					} else {
						element = new StringElement ("", item.Data);
					}
					EntriesSection.Insert (0, element);
				}
			//});
		}
		#endregion
	}
}

