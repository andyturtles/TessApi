using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using aBasics;
using TessApi.JsonData;

namespace TessApi {

    public partial class TessApiLogin {

        internal const string   SSO_URI     = "https://auth.tesla.com";
        private string          authHost    = SSO_URI;

        internal static readonly string TESLA_CLIENT_ID      = "81527cff06843c8634fdc09e8ac0abefb46ac849f38fe1e431c2ef2106796384";
        internal static readonly string TESLA_CLIENT_SECRET  = "c7257eb71a564034f9419ee651c7d0e5f7aa6bfbd18bafb5c5c033b093bb2fa3";

        public LoginResponse LoginResponse;

        private string code_verifier;
        private string code_challenge_SHA256;
        private string code_challenge;
        private string state;
        private string cookie;
        private string transaction_id;
        private Uri referrer;

        /// <summary>
        /// Does the login.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="pw">The password.</param>
        /// <returns>true: login OK / finished; false: MFA needed!</returns>
        public async Task<bool> DoLogin(string user, string pw) {
            MatchCollection m;
            if ( String.IsNullOrEmpty(user) || String.IsNullOrEmpty(pw) ) throw new Exception("NO Credentials");

            code_verifier           = RandomString(86);
            code_challenge_SHA256   = ComputeSHA256Hash(code_verifier);
            code_challenge          = Convert.ToBase64String(Encoding.Default.GetBytes(code_challenge_SHA256));
            state                   = RandomString(20);

            using ( TessHttpClient client = new TessHttpClient(new TessClientHandler(false, null)) ) {
                client.Timeout = TimeSpan.FromSeconds(30);

                Dictionary<string, string> values = new Dictionary<string, string> {
                    { "client_id", "ownerapi" },
                    { "code_challenge", code_challenge },
                    { "code_challenge_method", "S256" },
                    { "redirect_uri", authHost + "/void/callback" },
                    { "response_type", "code" },
                    { "scope", "openid email offline_access" },
                    { "state", state },
                    { "login_hint", "" }
                };

                string json = new JavaScriptSerializer().Serialize(values);

                using ( StringContent content = new StringContent(json.ToString(), Encoding.UTF8, "application/json") ) {
                    UriBuilder b = new UriBuilder(authHost + "/oauth2/v3/authorize");
                    b.Port = -1;
                    var q = HttpUtility.ParseQueryString(b.Query);
                    foreach ( var v in values ) {
                        q[v.Key] = v.Value;
                    }
                    b.Query = q.ToString();
                    string url = b.ToString();

                    HttpResponseMessage result = await client.GetAsync(url);
                    string resultContent = result.Content.ReadAsStringAsync().Result;

                    m = Regex.Matches(resultContent, "type=\\\"hidden\\\" name=\\\"(.*?)\\\" value=\\\"(.*?)\\\"");
                    IEnumerable<string> cookies = result.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
                    cookie = cookies.ToList()[0];
                    cookie = cookie.Substring(0, cookie.IndexOf(" "));
                    cookie = cookie.Trim();

                    if ( resultContent.Contains("authorization_required") ) throw new Exception("Wrong Credentials");

                    if ( result.StatusCode == HttpStatusCode.RedirectMethod ) {
                        if ( result.Headers.Location.Host == "auth.tesla.cn" && authHost != "https://auth.tesla.cn" ) {
                            authHost = "https://auth.tesla.cn";
                            Log.Info("Use chinese auth server: auth.tesla.cn!");
                            return await DoLogin(user, pw);
                        }
                    }
                }
            }

            return await GetTokenAsync2(m, user, pw);
        }

        private async Task<bool> GetTokenAsync2(MatchCollection mc, string user, string pw) {
            int length = 0;

            Dictionary<string, string> d = new Dictionary<string, string>();
            foreach ( Match m in mc ) {
                string key      = m.Groups[1].Value;
                string value    = m.Groups[2].Value;
                if ( d.ContainsKey(key) ) continue;
                if ( key.Contains("cancel") ) continue;
                d.Add(key, value);
                if ( key == "transaction_id" ) transaction_id = value;
                length += m.Groups[1].Value.Length;
                length += m.Groups[2].Value.Length;
                length += 4;
            }

            d.Add("identity", user);
            d.Add("credential", pw);

            string code = "";

            using ( HttpClient client = new TessHttpClient(new TessClientHandler(false, false)) ) {
                client.DefaultRequestHeaders.Add("Cookie", cookie);

                using ( FormUrlEncodedContent content = new FormUrlEncodedContent(d) ) {
                    UriBuilder b                = new UriBuilder(authHost + "/oauth2/v3/authorize");
                    b.Port                      = -1;
                    var q                       = HttpUtility.ParseQueryString(b.Query);
                    q["client_id"]              = "ownerapi";
                    q["code_challenge"]         = code_challenge;
                    q["code_challenge_method"]  = "S256";
                    q["redirect_uri"]           = authHost + "/void/callback";
                    q["response_type"]          = "code";
                    q["scope"]                  = "openid email offline_access";
                    q["state"]                  = state;
                    b.Query                     = q.ToString();
                    string url                  = b.ToString();

                    referrer = b.Uri;

                    var temp = content.ReadAsStringAsync().Result;

                    HttpResponseMessage result  = await client.PostAsync(url, content);
                    string resultContent        = result.Content.ReadAsStringAsync().Result;
                    Uri location                = result.Headers.Location;

                    if ( result.StatusCode != HttpStatusCode.Redirect ) {
                        if ( result.StatusCode == HttpStatusCode.OK && resultContent.Contains("passcode") ) {
                            return false; // Signalisieren das wir mit MFA weiter machen müssen
                        }
                        else {
                            Log.Error("GetTokenAsync2 HttpStatus: " + result.StatusCode.ToString() + " / Expecting: Redirect !!!");
                            throw new Exception("GetToken: " + result.StatusCode);
                        }
                    }

                    // Ohne MFA
                    if ( location == null ) throw new Exception("GetTokenAsync2 Redirect Location = null!!! Wrong credentials?");
                    if ( result.StatusCode == HttpStatusCode.Redirect && ( location != null ) ) {
                        code = HttpUtility.ParseQueryString(location.Query).Get("code");
                        await GetTokenAsync3(code);
                        return true;
                    }
                    else throw new Exception("GetTokenAsync2 - result.StatusCode: " + result.StatusCode);
                }
            }
        }

        internal async Task ContinueLoginAfterMfa(string mfaCode) {
            string code = DoMFA_Code(mfaCode);
            await GetTokenAsync3(code);
            return;
        }

        private async Task GetTokenAsync3(string code) {
            var d = new Dictionary<string, string>();
            d.Add("grant_type", "authorization_code");
            d.Add("client_id", "ownerapi");
            d.Add("code", code);
            d.Add("code_verifier", code_verifier);
            d.Add("redirect_uri", authHost + "/void/callback");

            string json = new JavaScriptSerializer().Serialize(d);
            string tmpAccessToken;

            using ( HttpClient client = new TessHttpClient(new TessClientHandler(null, null)) ) {
                client.DefaultRequestHeaders.Referrer = referrer;
                using ( StringContent content = new StringContent(json, Encoding.UTF8, "application/json") ) {
                    HttpResponseMessage result = await client.PostAsync(authHost + "/oauth2/v3/token", content);

                    if ( result.StatusCode != HttpStatusCode.OK ) throw new Exception("authorization_code - Error: " + result.StatusCode);

                    string resultContent    = result.Content.ReadAsStringAsync().Result;
                    dynamic jsonResult      = new JavaScriptSerializer().DeserializeObject(resultContent);
                    //RefreshToken                = jsonResult["refresh_token"];
                    tmpAccessToken          = jsonResult["access_token"];
                }
            }

            await GetTokenAsync4Async(tmpAccessToken);
        }

        private async Task GetTokenAsync4Async(string tmpAccessToken) {
            var d = new Dictionary<string, string>();
            d.Add("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer");
            d.Add("client_id", TESLA_CLIENT_ID);
            d.Add("client_secret", TESLA_CLIENT_SECRET);
            string json = new JavaScriptSerializer().Serialize(d);

            using ( HttpClient client = new TessHttpClient(new TessClientHandler(null, null)) ) {
                client.Timeout = TimeSpan.FromSeconds(5);

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + tmpAccessToken);

                using ( var content = new StringContent(json, Encoding.UTF8, "application/json") ) {
                    HttpResponseMessage result  = await client.PostAsync("https://owner-api.teslamotors.com/oauth/token", content);
                    string resultContent        = result.Content.ReadAsStringAsync().Result;
                    Log.Debug("HttpStatus: " + result.StatusCode.ToString());

                    LoginResponse = SerializeTool.DeSerializeJson<LoginResponse>(resultContent);
                }
            }
        }

        public static string ComputeSHA256Hash(string text) {
            string hashString;
            using ( var sha256 = SHA256Managed.Create() ) {
                var hash = sha256.ComputeHash(Encoding.Default.GetBytes(text));
                hashString = ToHex(hash, false);
            }

            return hashString;
        }

        private static string ToHex(byte[] bytes, bool upperCase) {
            StringBuilder result = new StringBuilder(bytes.Length * 2);
            for ( int i = 0; i < bytes.Length; i++ )
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));
            return result.ToString();
        }

        private string RandomString(int v) {
            Random r = new Random(DateTime.Now.Second);
            string val = "";
            for ( int i = 0; i <= v; i++ ) {
                val += r.Next(0, 9);
            }
            return val;
        }

        //public async Task UpdateTeslaTokenFromRefreshToken() {
        //    string refresh_token = LoginResponse.refresh_token;

        //    if ( String.IsNullOrEmpty(refresh_token) ) {
        //        Log.Warning("No Refresh Token");
        //        return;
        //    }

        //    Log.Info("Update Tesla Token From Refresh Token!");

        //                     //https://auth.tesla.com
        //    string authHost = "https://auth.tesla.com";
        //    if ( LoginResponse.access_token.StartsWith("cn-") ) authHost = "https://auth.tesla.cn";

        //    try {
        //        var d = new Dictionary<string, string>();
        //        d.Add("grant_type", "refresh_token");
        //        d.Add("client_id", "ownerapi");
        //        d.Add("refresh_token", refresh_token);

        //        string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(d);

        //        using ( TessClientHandler handler = new TessClientHandler(null, null) ) {

        //            DateTime start = DateTime.UtcNow;

        //            using ( TessHttpClient client = new TessHttpClient(handler) ) {
        //                client.Timeout = TimeSpan.FromSeconds(30);

        //                using ( var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") ) {
        //                    HttpResponseMessage result = client.PostAsync(authHost + "/oauth2/v3/token", content).Result;
        //                    string resultContent = result.Content.ReadAsStringAsync().Result;

        //                    Log.Info("HttpStatus: " + result.StatusCode.ToString());

        //                    dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
        //                    string access_token = jsonResult["access_token"];

        //                    string new_refresh_token = jsonResult["refresh_token"];
        //                    if ( new_refresh_token == refresh_token ) Log.Info("refresh_token not changed");
        //                    else LoginResponse.refresh_token = new_refresh_token;

        //                    await GetTokenAsync4Async(access_token);
        //                }
        //            }
        //        }
        //    }
        //    catch ( Exception ex ) {
        //        Log.Error("UpdateTeslaTokenFromRefreshToken", ex);
        //    }
        //}
    }
}