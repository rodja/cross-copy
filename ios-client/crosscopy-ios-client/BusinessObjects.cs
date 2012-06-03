using System;

namespace CrossCopy.iOSClient.BL
{
    public enum DataItemDirection
    {
        In,
        Out
    }
    
    public class DataItem
    {
        #region Properties
        public string Data { get; set; }

        public DataItemDirection Direction { get; set; }

        public DateTime Date { get; set; }
        #endregion
        
        #region Ctor
        public DataItem ()
        {
        }
        
        public DataItem (string data, DataItemDirection direction, DateTime date)
        {
            Data = data;
            Direction = direction;
            Date = date;
        }
        #endregion
    }
}

