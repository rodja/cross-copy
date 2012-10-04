using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Json;
using System.Net;
using System.Net.Cache;
using CrossCopy.Api;

namespace CrossCopy.BL
{
	[System.Diagnostics.DebuggerDisplay("History - Secrets {Secrets.Count}")]
	[XmlRoot("history")]
	public class History
	{
		public History ()
		{
			Secrets = new List<Secret> ();
		}

		[XmlElement("secret")]
		public List<Secret> Secrets { get; set; }
	}

	public delegate void WatchEventHandler (Secret secret);

	[System.Diagnostics.DebuggerDisplay("Secret - {Phrase} {DataItems.Count}")]
	public class Secret
	{

		WebClient watchClient = new BugfixedWebClient ();
        
		public Secret ()
		{
			DataItems = new List<DataItem> ();
		}
        
		public Secret (string phrase) : this()
		{
			Phrase = phrase;

			StartWatching ();
		}

		public event WatchEventHandler WatchEvent;

		[XmlAttribute("phrase")]
		public string Phrase { get; private set; }

		[XmlElement("dataitem")]
		public List<DataItem> DataItems { get; set; }

		[XmlIgnore]
		public string LatestId {
			get {
				return (DataItems.Count == 0) ? "" : DataItems [0].Id;
			}
		}

		[XmlIgnore]
		public DateTime LastModified {
			get {
				return (DataItems.Count == 0) ? DateTime.MinValue : DataItems [0].Date;
			}
		}

		[XmlIgnore]
		public int ListenersCount { get; set; }

		public void StartWatching ()
		{
			watchClient.CachePolicy = new RequestCachePolicy (RequestCacheLevel.BypassCache);
			watchClient.CancelAsync ();
			watchClient.DownloadStringCompleted += (sender, e) => { 
				if (e.Cancelled) {

				} else if (e.Error != null) {
					Console.Out.WriteLine ("Error watching listeners: {0}", e.Error.Message);
				} else
					try {
						ListenersCount = Convert.ToInt32 (e.Result);
						if (WatchEvent != null) {
							WatchEvent (this);
						}
					} catch (Exception ex) {
						Console.Out.WriteLine ("Error downloding listener count: {0}", ex.Message);
					}
                 
				Watch ();
			};

			Watch ();
		}

		private void Watch ()
		{
			watchClient.CancelAsync ();
			watchClient.Dispose ();
			Uri uri = new Uri (String.Format ("{0}/api/{1}?watch=listeners&count={2}", 
                                              Server.SERVER, Phrase, ListenersCount)
			);
			watchClient.DownloadStringAsync (uri);   
		}

		public override string ToString ()
		{
			return Phrase;
		}

	}

	[System.Diagnostics.DebuggerDisplay("DataItem - {Data}")]
	public class DataItem
	{
		public DataItem ()
		{
		}

		private DataItem (DataItemDirection direction, DateTime date)
		{
			Direction = direction;
			Date = date;
		}

		public DataItem (string data, DataItemDirection direction, DateTime date)
        : this(direction, date)
		{
			Data = data;
		}

		public DataItem (JsonValue data, DataItemDirection direction, DateTime date)
        : this(direction, date)
		{
			Data = data ["data"];
			Id = data ["id"];
		}

		[XmlAttribute("data")]
		public string Data { get; set; }

		[XmlAttribute("id")]
		public string Id { get; set; }

		[XmlIgnore]
		public DataItemDirection Direction { get; set; }

        [XmlAttribute("itempath")]
        public string ItemPath { get; set; }

		[XmlAttribute("direction")]
		[EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
		public int DirectionInt {
			get { return (int)Direction; }
			set { Direction = (DataItemDirection)value; }
		}

		[XmlAttribute("date")]
		public DateTime Date { get; set; }
	}
    
	public enum DataItemDirection
	{
		In,
		Out
	}

	public class BugfixedWebClient : WebClient
	{
		protected override WebRequest GetWebRequest (Uri address)
		{
			var req = base.GetWebRequest (address) as HttpWebRequest;
			req.AllowWriteStreamBuffering = false;
			req.KeepAlive = false;
			req.Pipelined = false;
			Console.WriteLine ("getting request");
			return req;
		} 
	}
}

