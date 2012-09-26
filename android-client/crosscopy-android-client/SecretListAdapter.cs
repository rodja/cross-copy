using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace CrossCopy.AndroidClient
{
	class SecretListAdapter: BaseAdapter<SecretsList>
	{
		Activity context;
        public List<SecretsList> listSecrets;

        public SecretListAdapter (Activity context, List<SecretsList> lSecrets): base()
		{
			this.context = context;
			this.listSecrets = lSecrets;
        }

        public override int Count {
			get { return this.listSecrets.Count; }
        }

        public override SecretsList this [int position] {
			get { return this.listSecrets [position]; }
        }

        public override View GetView (int position, View convertView, ViewGroup parent)
		{
			var item = this.listSecrets [position];
			var view = convertView;
			if (convertView == null || !(convertView is LinearLayout))
				view = context.LayoutInflater.Inflate (Resource.Layout.ListViewSecrets, parent, false);
			var textSecret = view.FindViewById (Resource.Id.textViewSecrets) as TextView;
			var textDevices = view.FindViewById (Resource.Id.textViewDevices) as TextView;
			
			textSecret.SetText (item.Secret, TextView.BufferType.Normal);
			textDevices.SetText (item.Devices, TextView.BufferType.Normal);
			
			var imageButton = view.FindViewById (Resource.Id.imageButtonDel) as ImageButton;
			//imageButton.Click += imageButtonClick;
				
			return view;
        }

        void imageButtonClick (object sender, EventArgs e)
		{
			var imageButton = sender as ImageButton;
			
			Toast.MakeText (this.context, imageButton.Tag + "Button Clicked!", ToastLength.Short).Show ();
        }
		
        public override long GetItemId (int position)
        {
            return position;
        }
	}
}

