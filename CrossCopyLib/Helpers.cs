using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Xml.Serialization;
using CrossCopy.BL;

namespace CrossCopy.Helpers
{
        public class DeviceHelper
        {
                public static string GetMacAddress ()
                {
                        var macAddresses = "";
    
                        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()) {
                                if (nic.OperationalStatus == OperationalStatus.Up) {
                                        macAddresses += nic.GetPhysicalAddress ().ToString ();
                                        break;
                                }
                        }
            
                        return macAddresses;
                }
        }
    
        public static class SerializeHelper<T>
        {
                public static string ToXmlString (T obj)
                {
                        var serializer = new XmlSerializer (typeof(T));
                        var writer = new System.IO.StringWriter ();
                        try {
                                serializer.Serialize (writer, obj);
                        } finally {
                                writer.Flush ();
                                writer.Close ();
                        }

                        return writer.ToString ();
                }
        
                public static T FromXmlString (string serialized)
                {
                        if (serialized.Length <= 0)
                                throw new ArgumentOutOfRangeException ("serialized", "Cannot thaw a zero-length string");

                        var serializer = new XmlSerializer (typeof(T));
                        var reader = new System.IO.StringReader (serialized);
                        var obj = default(T); 
                        try {
                                obj = (T)serializer.Deserialize (reader);
                        } finally {
                                reader.Close ();
                        }
                        return (T)obj;
                }
        }
}

