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
	class HistoryListAdapter:BaseAdapter<HistoryList>
	{
		Activity context;
        public List<HistoryList> listHistory;

        public HistoryListAdapter (Activity context, List<HistoryList> lHistory): base()
		{
			this.context = context;
			this.listHistory = lHistory;
        }

        public override int Count {
			get { return this.listHistory.Count; }
        }

        public override HistoryList this [int position] {
			get { return this.listHistory [position]; }
        }

        public override View GetView (int position, View convertView, ViewGroup parent)
		{
			var item = this.listHistory [position];
			var view = convertView;
			if (convertView == null || !(convertView is LinearLayout))
				view = context.LayoutInflater.Inflate (Resource.Layout.ListViewHistory, parent, false);
			var textLeft = view.FindViewById (Resource.Id.textViewLeft) as TextView;
			var textRight = view.FindViewById (Resource.Id.textViewRight) as TextView;

			textLeft.SetText (item.Left, TextView.BufferType.Normal);
			textRight.SetText (item.Right, TextView.BufferType.Normal);
            return view;
        }

        public override long GetItemId (int position)
        {
            return position;
        }
		
    }
}


