using System;
using System.IO;

namespace CrossCopy.Api
{
        public class UrlHelper
        {
                public static string GetFileName (string filePath)
                {
                        var fileName = Path.GetFileName (filePath);
                        var idIdx = fileName.IndexOf ("?id=");
                        var extIdx = fileName.IndexOf ("&ext=");
                        if (idIdx > 0 && extIdx > 0) {
                                var start = idIdx + 4;
                                var name = fileName.Substring (start, extIdx - start);
                                start = extIdx + 5;
                                var extension = fileName.Substring (start);
                                fileName = string.Format ("{0}.{1}", name, extension);
                        }

                        return fileName;
                }

                public static string GetExtension (string filePath)
                {
                        var ext = Path.GetExtension (filePath);
                        var idx = ext.IndexOf ("?");
                        if (idx >= 0) {
                                ext = ext.Substring (0, idx);
                        }
                        return ext;
                }
        }

}

