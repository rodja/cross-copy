using System;
using Android.App;
using Android.Runtime;
using MonoDroid.Dialog;
using CrossCopy.Api;
using CrossCopy.BL;
using CrossCopy.AndroidClient.Helpers;
using System.Threading;

namespace CrossCopy.AndroidClient
{
	[Application(Label = "cross copy")]
	public class CrossCopyApp : Application
	{
		#region Public props
        public static History HistoryData { get; set; }
		public static Server Srv { get; set; }

        #endregion

		#region Ctor
		public CrossCopyApp (IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {
        }
		#endregion

		#region Methods
		public override void OnCreate()
        {
			base.OnCreate();
			System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
			ThreadPool.SetMinThreads(100, 4);
			StoreHelper.Load (ApplicationContext);
			Srv = new Server ();
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


		#endregion
	}
}

