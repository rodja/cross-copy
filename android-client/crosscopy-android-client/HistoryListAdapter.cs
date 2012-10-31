using System.Text;

using Android.App;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;

namespace CrossCopy.AndroidClient
{ 
        class HistoryListAdapter:BaseAdapter<HistoryItem>
        {
                Activity _context;
                public List<HistoryItem> _listHistory;

                public HistoryListAdapter (Activity context, List<HistoryItem> lHistory): base()
                {
                        _context = context;
                        _listHistory = lHistory;
                }

                public override int Count {
                        get { return _listHistory.Count; }
                }

                public override HistoryItem this [int position] {
                        get {
                                // Assert entry conditions
                                System.Diagnostics.Debug.Assert (position < _listHistory.Count);
                                return _listHistory [position]; 
                        }
                }

                public override View GetView (int position, View convertView, ViewGroup parent)
                {
                        // Assert entry conditions
                        System.Diagnostics.Debug.Assert (position < _listHistory.Count);
                        var item = _listHistory [position];

                        var view = convertView;
                        if (convertView == null || !(convertView is LinearLayout))
                                view = _context.LayoutInflater.Inflate (Resource.Layout.ListViewHistory, parent, false);

                        var textLeft = view.FindViewById<TextView> (Resource.Id.textViewLeft);
                        var textRight = view.FindViewById<TextView> (Resource.Id.textViewRight);

                        textLeft.SetText (item.Outgoing, TextView.BufferType.Normal);
                        textRight.SetText (item.Incoming, TextView.BufferType.Normal);
                        return view;
                }

                public override long GetItemId (int position)
                {
                        return position;
                }
        }
}

