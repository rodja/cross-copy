using System;
using System.IO;

namespace CrossCopy.Lib.Api
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
    }

}

