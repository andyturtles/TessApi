using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using aBasics;

namespace TessApi {

    public class TessTools {

        private static readonly byte[] DEFAULT_ENCRYPT_ENTROPY = new byte[] { 56, 83, 184, 102, 70, 207, 216, 115, 119, 219, 189, 252, 101, 231, 145, 56, 224, 140, 141, 158 };

        private static byte[] encryptEntropy;

        public static void SetEncryptEntropy(string enc) {
            encryptEntropy = Encoding.UTF8.GetBytes(enc);
        }

        public static void SaveResponse<T>(T resp) {
            SaveResponse(resp, "");
        }

        public static void SaveResponse<T>(T resp, string commandName, bool encrypt = false) {
            string json = SerializeTool.SerializeJson<T>(resp);

            if ( encrypt ) json = EncryptResponse(json);

            File.WriteAllText(GetSavedResponseFilePath(typeof(T), commandName), json);
        }

        public static T LoadResponse<T>(out DateTime? fileDate, bool decrypt = false) {
            fileDate = null;
            string file = GetSavedResponseFilePath(typeof(T), "");
            if ( !File.Exists(file) ) return default(T);

            fileDate    = new FileInfo(file).LastWriteTime;
            string json = File.ReadAllText(file);

            T resp;
            try {
                if ( decrypt ) json = DecryptResponse(json);
                resp = SerializeTool.DeSerializeJson<T>(json);
            }
            catch ( Exception ex) {
                if ( decrypt ) throw new Exception("Error Loading Response, Maybe Wrong Password?", ex);
                else throw new Exception("Error Loading Response", ex);
            }
            return resp;
        }

        private static string GetSavedResponseFilePath(Type t, string add) {
            string filename                             = t.Name;
            if ( !String.IsNullOrEmpty(add) ) filename  += "_" + add;

            string savedResponseFolder  = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            Uri uri                     = new Uri(savedResponseFolder);
            savedResponseFolder         = uri.LocalPath;
            savedResponseFolder         = Path.Combine(savedResponseFolder, "json");

            if ( !Directory.Exists(savedResponseFolder) ) {
                Directory.CreateDirectory(savedResponseFolder);
            }

            return Path.Combine(savedResponseFolder, "Tess_" + filename + ".json");
        }

        private static string EncryptResponse(string json) {
            byte[] textBytes    = Encoding.UTF8.GetBytes(json);
            byte[] cipherBytes  = ProtectedData.Protect(textBytes, encryptEntropy ?? DEFAULT_ENCRYPT_ENTROPY, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(cipherBytes);
        }

        private static string DecryptResponse(string str) {
            byte[] encrypted        = Convert.FromBase64String(str);
            byte[] restore          = ProtectedData.Unprotect(encrypted, encryptEntropy ?? DEFAULT_ENCRYPT_ENTROPY, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(restore);
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