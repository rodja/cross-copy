using System;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;

namespace CrossCopy.AndroidClient
{
        class SecretListAdapter: BaseAdapter<SecretsList>
        {
                Activity _context;
                public List<SecretsList> _listSecrets;

                public SecretListAdapter (Activity context, List<SecretsList> lSecrets): base()
                {
                        _context = context;
                        _listSecrets = lSecrets;
                }

                public override int Count {
                        get { return _listSecrets.Count; }
                }

                public override SecretsList this [int position] {
                        get {
                                // Assert entry conditions
                                System.Diagnostics.Debug.Assert (position < _listSecrets.Count);
                                return _listSecrets [position]; 
                        }
                }

                public override View GetView (int position, View convertView, ViewGroup parent)
                {
                        // Assert entry conditions
                        System.Diagnostics.Debug.Assert (position < _listSecrets.Count);

                        var item = _listSecrets [position];
                        var view = convertView;
                        if (convertView == null || !(convertView is LinearLayout))
                                view = _context.LayoutInflater.Inflate (Resource.Layout.ListViewSecrets, parent, false);
                        var textSecret = view.FindViewById<TextView> (Resource.Id.textViewSecrets);
                        var textDevices = view.FindViewById<TextView> (Resource.Id.textViewDevices);
			
                        textSecret.SetText (item.Secret, TextView.BufferType.Normal);
                        textDevices.SetText (item.Devices, TextView.BufferType.Normal);
			
                        var imageButton = view.FindViewById (Resource.Id.imageButtonDel) as ImageButton;
                        //imageButton.Click += imageButtonClick;
				
                        return view;
                }

                void imageButtonClick (object sender, EventArgs e)
                {
                        var imageButton = sender as ImageButton;
			
                        Toast.MakeText (_context, imageButton.Tag + "Button Clicked!", ToastLength.Short).Show ();
                }
		
                public override long GetItemId (int position)
                {
                        return position;
                }
        }
}

