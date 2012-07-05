using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using CrossCopy.Lib.BL;

namespace CrossCopy.Lib.Helpers
{
    public class DeviceHelper
    {
        public static string GetMacAddress()
        {
            string macAddresses = "";
    
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    macAddresses += nic.GetPhysicalAddress().ToString();
                    break;
                }
            }
            
            return macAddresses;
        }
    }
    
    public static class SerializeHelper<T>
    {
        public static string ToXmlString(T obj)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            System.IO.TextWriter writer = new System.IO.StringWriter();
            try
            {
                serializer.Serialize(writer, obj);
            }
            finally
            {
                writer.Flush();
                writer.Close();
            }

            return writer.ToString();
        }
        
        public static T FromXmlString(string serialized)
        {
            if (serialized.Length <= 0) throw new ArgumentOutOfRangeException("serialized", "Cannot thaw a zero-length string");

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            System.IO.TextReader reader = new System.IO.StringReader(serialized);
            object @object = default(T); 
            try
            {
                @object = serializer.Deserialize(reader);
            }
            finally
            {
                reader.Close();
            }
            return (T)@object;
        }
    }
 
    public class FilesSavedToPhotosAlbumArgs : System.EventArgs
    {
        public FilesSavedToPhotosAlbumArgs(string referenceUrl) { 
            ReferenceUrl = referenceUrl; 
        } 
    
        public string ReferenceUrl { 
            get; 
            set; 
        }
    }
}

