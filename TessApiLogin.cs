using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using aBasics;
using TessApi.JsonData;

namespace TessApi {

    public partial class TessApiLogin {

        private const string SSO_URI    = "https://auth.tesla.com";
        private const string USER_AGENT = "Tess4Windows"; // oder das? "TeslaLogger"

        internal static readonly string TESLA_CLIENT_ID      = "81527cff06843c8634fdc09e8ac0abefb46ac849f38fe1e431c2ef2106796384";
        internal static readonly string TESLA_CLIENT_SECRET  = "c7257eb71a564034f9419ee651c7d0e5f7aa6bfbd18bafb5c5c033b093bb2fa3";

        public LoginResponse LoginResponse;

        private string code_verifier;
        private string code_challenge_SHA256;
        private string code_challenge;
        private string state;

#warning ggf. mal async machen, aber erstmal läuft es ;)

        public void DoLogin(string user, string pw) {
            string cookie = "";
            MatchCollection m;

            //string tempToken = UpdateTeslaTokenFromRefreshToken();
            //if ( !String.IsNullOrEmpty(tempToken) ) return tempToken;

            if ( String.IsNullOrEmpty(user) || String.IsNullOrEmpty(pw) ) throw new Exception("NO Credentials");

            code_verifier           = RandomString(86);
            code_challenge_SHA256   = ComputeSHA256Hash(code_verifier);
            code_challenge          = Convert.ToBase64String(Encoding.Default.GetBytes(code_challenge_SHA256));
            state                   = RandomString(20);

            using ( HttpClient client = new HttpClient() ) {
                client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
                Dictionary<string, string> values = new Dictionary<string, string> {
                            { "client_id", "ownerapi" },
                            { "code_challenge", code_challenge },
                            { "code_challenge_method", "S256" },
                            { "redirect_uri", SSO_URI + "/void/callback" },
                            { "response_type", "code" },
                            { "scope", "openid email offline_access" },
                            { "state", state }
                        };

                string json = new JavaScriptSerializer().Serialize(values);

                using ( StringContent content = new StringContent(json.ToString(), Encoding.UTF8, "application/json") ) {
                    UriBuilder b    = new UriBuilder(SSO_URI + "/oauth2/v3/authorize");
                    b.Port          = -1;
                    var q           = HttpUtility.ParseQueryString(b.Query);
                    foreach ( var v in values ) {
                        q[v.Key]    = v.Value;
                    }
                    b.Query         = q.ToString();
                    string url      = b.ToString();

                    HttpResponseMessage result  = client.GetAsync(url).Result;
                    string resultContent        = result.Content.ReadAsStringAsync().Result;
                    m                           = Regex.Matches(resultContent, "type=\\\"hidden\\\" name=\\\"(.*?)\\\" value=\\\"(.*?)\\\"");
                    IEnumerable<string> cookies = result.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
                    cookie                      = cookies.ToList()[0];
                    cookie                      = cookie.Substring(0, cookie.IndexOf(" "));
                    cookie                      = cookie.Trim();

                    if ( resultContent.Contains("authorization_required") ) throw new Exception("Wrong Credentials");
                }
            }

            GetTokenAsync2(cookie, m, user, pw);
        }

        private void GetTokenAsync2(string cookie, MatchCollection mc, string user, string pw) {
            int length = 0;
            string transaction_id = "";

            Dictionary<string, string> d = new Dictionary<string, string>();
            foreach ( Match m in mc ) {
                string key      = m.Groups[1].Value;
                string value    = m.Groups[2].Value;
                d.Add(key, value);
                if ( key == "transaction_id" ) transaction_id = value;
                length += m.Groups[1].Value.Length;
                length += m.Groups[2].Value.Length;
                length += 4;
            }

            d.Add("identity", user);
            d.Add("credential", pw);

            string code = "";

            using ( HttpClientHandler ch = new HttpClientHandler() ) {
                ch.AllowAutoRedirect = false;
                ch.UseCookies = false;
                using ( HttpClient client = new HttpClient(ch) ) {
                    // client.Timeout = TimeSpan.FromSeconds(10);
                    client.BaseAddress = new Uri(SSO_URI);
                    client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
                    client.DefaultRequestHeaders.Add("Cookie", cookie);

                    using ( FormUrlEncodedContent content = new FormUrlEncodedContent(d) ) {
                        UriBuilder b                = new UriBuilder(SSO_URI + "/oauth2/v3/authorize");
                        b.Port                      = -1;
                        var q                       = HttpUtility.ParseQueryString(b.Query);
                        q["client_id"]              = "ownerapi";
                        q["code_challenge"]         = code_challenge;
                        q["code_challenge_method"]  = "S256";
                        q["redirect_uri"]           = SSO_URI + "/void/callback";
                        q["response_type"]          = "code";
                        q["scope"]                  = "openid email offline_access";
                        q["state"]                  = state;
                        b.Query                     = q.ToString();
                        string url                  = b.ToString();

                        var temp = content.ReadAsStringAsync().Result;

                        HttpResponseMessage result  = client.PostAsync(url, content).Result;
                        string resultContent        = result.Content.ReadAsStringAsync().Result;
                        Uri location                = result.Headers.Location;
                        bool isMFA                  = false;

                        if ( result.StatusCode != HttpStatusCode.Redirect ) {
                            if ( result.StatusCode == HttpStatusCode.OK && resultContent.Contains("passcode") ) {
                                isMFA   = true;
                                code    = WaitForMFA_Code(cookie, transaction_id);
                                if ( String.IsNullOrEmpty(code) ) {
                                    Log.Warning("WaitForMFA_Code - Code Empty");
                                    return;
                                }
                            }
                            else {
                                Log.Error("GetTokenAsync2 HttpStatus: " + result.StatusCode.ToString() + " / Expecting: Redirect !!!");
                                throw new Exception("GetToken: " + result.StatusCode);
                            }
                        }

                        if ( !isMFA ) {
                            if ( location == null ) Log.Error("GetTokenAsync2 Redirect Location = null!!! Wrong credentials?");

                            if ( result.StatusCode == HttpStatusCode.Redirect && location != null ) {
                                code = HttpUtility.ParseQueryString(location.Query).Get("code");
                            }
                            else {
                                Log.Warning("GetTokenAsync2 - result.StatusCode: " + result.StatusCode);
                            }
                        }
                    }
                }
            }

            GetTokenAsync3(code);
        }

        private void GetTokenAsync3(string code) {
            var d = new Dictionary<string, string>();
            d.Add("grant_type", "authorization_code");
            d.Add("client_id", "ownerapi");
            d.Add("code", code);
            d.Add("code_verifier", code_verifier);
            d.Add("redirect_uri", SSO_URI + "/void/callback");

            string json = new JavaScriptSerializer().Serialize(d);
            string tmpAccessToken;
            using ( HttpClient client = new HttpClient() ) {
                client.BaseAddress = new Uri(SSO_URI);
                client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);

                using ( var content = new StringContent(json, Encoding.UTF8, "application/json") ) {
                    HttpResponseMessage result  = client.PostAsync(SSO_URI + "/oauth2/v3/token", content).Result;
                    string resultContent        = result.Content.ReadAsStringAsync().Result;
                    dynamic jsonResult          = new JavaScriptSerializer().DeserializeObject(resultContent);
                    //RefreshToken                = jsonResult["refresh_token"];
                    tmpAccessToken       = jsonResult["access_token"];
                }
            }

            GetTokenAsync4Async(tmpAccessToken);
        }

        private void GetTokenAsync4Async(string tmpAccessToken) {
            var d = new Dictionary<string, string>();
            d.Add("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer");
            d.Add("client_id", TESLA_CLIENT_ID);
            d.Add("client_secret", TESLA_CLIENT_SECRET);
            string json = new JavaScriptSerializer().Serialize(d);

            using ( HttpClient client = new HttpClient() ) {
                client.Timeout = TimeSpan.FromSeconds(5);
                client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + tmpAccessToken);

                using ( var content = new StringContent(json, Encoding.UTF8, "application/json") ) {
                    HttpResponseMessage result  = client.PostAsync("https://owner-api.teslamotors.com/oauth/token", content).Result;
                    string resultContent        = result.Content.ReadAsStringAsync().Result;
                    Log.Debug("HttpStatus: " + result.StatusCode.ToString());

                    LoginResponse           =  SerializeTool.DeSerializeJson<LoginResponse>(resultContent);
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

#warning TODO: Refresh token?

        /* ******************

        private string UpdateTeslaTokenFromRefreshToken() {
            string refresh_token = car.dbHelper.GetRefreshToken();

            if ( String.IsNullOrEmpty(refresh_token) ) {
                Log("No Refresh Token");
                return "";
            }

            try {
                Log("Update Tesla Token From Refresh Token!");
                var d = new Dictionary<string, string>();
                d.Add("grant_type", "refresh_token");
                d.Add("client_id", "ownerapi");
                d.Add("refresh_token", refresh_token);
                d.Add("scope", "openid email offline_access");

                string json = new JavaScriptSerializer().Serialize(d);

                using ( HttpClient client = new HttpClient() ) {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);

                    using ( var content = new StringContent(json, Encoding.UTF8, "application/json") ) {
                        HttpResponseMessage result = client.PostAsync(SSO_URI + "/oauth2/v3/token", content).Result;
                        string resultContent = result.Content.ReadAsStringAsync().Result;

                        car.Log("HttpStatus: " + result.StatusCode.ToString());

                        dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                        string access_token = jsonResult["access_token"];

                        string new_refresh_token = jsonResult["refresh_token"];
                        if ( new_refresh_token == refresh_token )
                            Log("refresh_token not changed");
                        else
                            car.dbHelper.UpdateRefreshToken(new_refresh_token);

                        return GetTokenAsync4Async(access_token);
                    }
                }
            }
            catch ( Exception ex ) {
                car.Log(ex.ToString());
            }
            return "";
        }

         ************************* */
    }
}