using System;
using System.Collections.Generic;
using MonoTouch.Foundation;
using System.Xml.Serialization;
using System.ComponentModel;

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

    }

    [System.Diagnostics.DebuggerDisplay("DataItem - {Data}")]
    public class DataItem
    {
        public DataItem ()
        {
        }
        
        public DataItem (string data, DataItemDirection direction, DateTime date)
        {
            Data = data;
            Direction = direction;
            Date = date;
        }

        [XmlAttribute("data")]
        public string Data { get; set; }

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

