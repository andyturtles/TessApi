using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using aBasics;

namespace TessApi {
    public class TessTools {
        
        public static void SaveResponse<T>(T resp) {
            SaveResponse(resp, "");
        }

        public static void SaveResponse<T>(T resp, string commandName) {
            string json = SerializeTool.SerializeJson<T>(resp);
            File.WriteAllText(GetSavedResponseFilePath(typeof(T), commandName), json);
        }

        public static T LoadResponse<T>(out DateTime? fileDate) {
            fileDate = null;
            string file = GetSavedResponseFilePath(typeof(T), "");
            if ( !File.Exists(file) ) return default(T);

            fileDate    = new FileInfo(file).LastWriteTime;
            string json = File.ReadAllText(file);
            return SerializeTool.DeSerializeJson<T>(json);
        }

        private static string GetSavedResponseFilePath(Type t, string add) {
            string filename             = t.Name + add;
            string savedResponseFolder  = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            Uri uri                     = new Uri(savedResponseFolder);
            savedResponseFolder         = uri.LocalPath;
            savedResponseFolder         = Path.Combine(savedResponseFolder, "json");

            if ( !Directory.Exists(savedResponseFolder) ) {
                Directory.CreateDirectory(savedResponseFolder);
#warning Info?
            }

            return Path.Combine(savedResponseFolder, "Tess_" + filename + ".json");
        }

        public static string GetDoubleAsString(double d) {
            NumberFormatInfo nfo        = new NumberFormatInfo();
            nfo.NumberDecimalSeparator  = ".";
            nfo.NumberGroupSeparator    = "";
            string s = d.ToString(nfo);
            if ( !s.Contains(".") ) s += ".0";
            return s;
        }

    }
}
