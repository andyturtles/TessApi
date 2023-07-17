using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using aBasics;
using TessApi.JsonData;

namespace TessApi {

    public class TessApiLoginNew {

        #region Fields

        private string        codeVerifier;
        public  LoginResponse LoginResponse;

        #endregion

        public async Task ComputeReturnUrl(string retUrl) {
            // den 'Code'-Teil in der URL Finden
            string code = retUrl.Substring(retUrl.IndexOf("&code=", StringComparison.Ordinal) + "&code=".Length);
            code = code.Remove(code.IndexOf("&", StringComparison.Ordinal));

            await GetTokenAsync(code);
        }

        private async Task GetTokenAsync(string code) {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("grant_type", "authorization_code");
            d.Add("client_id", "ownerapi");
            d.Add("code", code);
            d.Add("code_verifier", codeVerifier);
            d.Add("redirect_uri", "https://auth.tesla.com/void/callback");

            string json = new JavaScriptSerializer().Serialize(d);

            using ( HttpClient client = new TessHttpClient(new TessClientHandler(null, null)) ) {
                using ( StringContent content = new StringContent(json, Encoding.UTF8, "application/json") ) {
                    HttpResponseMessage result = await client.PostAsync("https://auth.tesla.com/oauth2/v3/token", content);

                    if ( result.StatusCode != HttpStatusCode.OK ) throw new Exception("authorization_code - Error: " + result.StatusCode);

                    string resultContent = result.Content.ReadAsStringAsync().Result;
                    LoginResponse = SerializeTool.DeSerializeJson<LoginResponse>(resultContent);
                }
            }
        }

        public async Task RefreshTokenAsync() {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("grant_type", "refresh_token");
            d.Add("client_id", "ownerapi");
            d.Add("refresh_token", LoginResponse.refresh_token);
            d.Add("scope", "openid email offline_access");

            string json = new JavaScriptSerializer().Serialize(d);

            using ( HttpClient client = new TessHttpClient(new TessClientHandler(null, null)) ) {
                using ( StringContent content = new StringContent(json, Encoding.UTF8, "application/json") ) {
                    HttpResponseMessage result = await client.PostAsync("https://auth.tesla.com/oauth2/v3/token", content);
                    if ( result.StatusCode != HttpStatusCode.OK ) throw new Exception("refresh_token - Error: " + result.StatusCode);

                    string resultContent = result.Content.ReadAsStringAsync().Result;
                    LoginResponse = SerializeTool.DeSerializeJson<LoginResponse>(resultContent);
                }
            }
        }

        public string BuildUrl() {
            // Create random strings with length of 83 and 16 characters
            // FYI for anyone looking into this, as of today 64 character code_verifier strings are no longer supported, only 86.
            // More: https://github.com/tomhollander/TeslaAuth/issues/30
            // THANK YOU ^

            codeVerifier = "ab" + RandomString(84);
            string state = RandomString(16);

            byte[] codeChallengeSha256 = ComputeSha256Hash(codeVerifier);
            string codeChallenge       = Base64UrlEncode(codeChallengeSha256);

            //Make URL and call it in default browser. User has to log in his TESLA account
            string url = "https://auth.tesla.com/oauth2/v3/authorize?audience=https%3A%2F%2Fownership.tesla.com%2F&client_id=ownerapi&code_challenge="                                                   +
                         codeChallenge                                                                                                                                                                   +
                         "&code_challenge_method=S256&locale=en-US&prompt=login&redirect_uri=https%3A%2F%2Fauth.tesla.com%2Fvoid%2Fcallback&response_type=code&scope=openid+email+offline_access&state=" +
                         state;
            return url;
        }

        public static byte[] ComputeSha256Hash(string text) {
            using ( SHA256 sha256 = SHA256.Create() ) {
                byte[] hash = sha256.ComputeHash(Encoding.Default.GetBytes(text));
                return hash;
            }
        }

        public static string Base64UrlEncode(byte[] bytes) {
            string base64 = Convert.ToBase64String(bytes);
            string encoded = base64
                            .Replace('+', '-')
                            .Replace('/', '_')
                            .Replace("=", String.Empty)
                            .Trim();
            return encoded;
        }

        private static string RandomString(int v) {
            Random r                           = new Random(DateTime.Now.Second);
            string val                         = "";
            for ( int i = 0; i <= v; i++ ) val += r.Next(0, 9);
            return val;
        }

    }

}
