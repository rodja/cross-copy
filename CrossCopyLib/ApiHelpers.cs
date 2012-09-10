using System;
using System.IO;

namespace CrossCopy.Api
{
    public class UrlHelper
    {
        public static string GetFileName (string filePath)
        {
            string fileName = Path.GetFileName (filePath);
            int idIdx = fileName.IndexOf ("?id=");
            int extIdx = fileName.IndexOf ("&ext=");
            if (idIdx > 0 && extIdx > 0) {
                int start = idIdx + 4;
                string name = fileName.Substring (start, extIdx - start);
                start = extIdx + 5;
                string extension = fileName.Substring (start);
                fileName = string.Format ("{0}.{1}", name, extension);
            }

            return fileName;
        }

        public static string GetExtension (string filePath)
        {
            string ext = Path.GetExtension (filePath);
            int idx = ext.IndexOf("?");
            if (idx >= 0) {
                ext = ext.Substring(0, idx);
            }
            return ext;
        }
    }

}

