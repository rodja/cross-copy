using System;
using System.Collections.Generic;
using MonoTouch.Foundation;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Json;

namespace CrossCopy.iOSClient.BL
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

    [System.Diagnostics.DebuggerDisplay("Secret - {Phrase} {DataItems.Count}")]
    public class Secret
    {
        public Secret ()
        {
            DataItems = new List<DataItem> ();
        }
        
        public Secret (string phrase)
        {
            Phrase = phrase;
            DataItems = new List<DataItem> ();
        }

        [XmlAttribute("phrase")]
        public string Phrase { get; set; }

        [XmlElement("dataitem")]
        public List<DataItem> DataItems { get; set; }

        [XmlIgnore]
        public string LatestId {
            get {
                return (DataItems.Count == 0) ? "" : DataItems [0].Id;
            }
        }

        [XmlIgnore]
        public int ListenersCount { get; set; }

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
}

