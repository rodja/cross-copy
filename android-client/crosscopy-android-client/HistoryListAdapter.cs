using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;

namespace CrossCopy.AndroidClient
{
        class HistoryListAdapter:BaseAdapter<HistoryList>
        {
                Activity _context;
                public List<HistoryList> _listHistory;

                public HistoryListAdapter (Activity context, List<HistoryList> lHistory): base()
                {
                        _context = context;
                        _listHistory = lHistory;
                }

                public override int Count {
                        get { return this._listHistory.Count; }
                }

                public override HistoryList this [int position] {
                        get { return this._listHistory [position]; }
                }

                public override View GetView (int position, View convertView, ViewGroup parent)
                {
                        var item = _listHistory [position];
                        var view = convertView;
                        if (convertView == null || !(convertView is LinearLayout))
                                view = _context.LayoutInflater.Inflate (Resource.Layout.ListViewHistory, parent, false);
                        var textLeft = view.FindViewById<TextView> (Resource.Id.textViewLeft);
                        var textRight = view.FindViewById<TextView> (Resource.Id.textViewRight);

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


