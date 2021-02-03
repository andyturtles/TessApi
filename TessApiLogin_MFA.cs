using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using aBasics;

namespace TessApi {

    public partial class TessApiLogin {

        private string MFA_Code;

        private string WaitForMFA_Code(string cookie, string transaction_id) {
            DateTime timeout = DateTime.UtcNow;

            while ( MFA_Code == null || MFA_Code.Length != 6 ) {
                Thread.Sleep(10);

                if ( DateTime.UtcNow - timeout > TimeSpan.FromSeconds(10) ) {
                    timeout = DateTime.UtcNow;
                    Log.Debug("Wait for MFA code !!!");
#warning abort loop !?
                }
            }

            Log.Debug("MFA Code: " + MFA_Code);

            while ( true ) {
                Log.Debug("transaction_id: " + transaction_id);
                string code = MFA1(cookie, transaction_id);
                if ( code.Length > 0 ) return code;
                Thread.Sleep(500);
            }
        }

        private string MFA1(string cookie, string transaction_id) {
            string resultContent;
            using ( HttpClientHandler ch = new HttpClientHandler() ) {
                ch.UseCookies = false;
                using ( HttpClient client = new HttpClient(ch) ) {
                    client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
                    client.DefaultRequestHeaders.Add("Cookie", cookie);

                    UriBuilder b    = new UriBuilder(SSO_URI + "/oauth2/v3/authorize/mfa/factors");
                    b.Port          = -1;

                    var q           = HttpUtility.ParseQueryString(b.Query);
                    q.Add("transaction_id", transaction_id);
                    b.Query         = q.ToString();
                    string url      = b.ToString();

                    HttpResponseMessage result  = client.GetAsync(url).Result;
                    resultContent               = result.Content.ReadAsStringAsync().Result;
                    Log.Debug("MFA1 Result: " + resultContent);
                    dynamic jsonResult          = new JavaScriptSerializer().DeserializeObject(resultContent);
                    string factor_id            = null;
                    try {
                        factor_id               = jsonResult["data"][0]["id"];
                    }
                    catch ( Exception ex ) {
                        Log.Error("MFA1 ResultContent: " + resultContent, ex);
                    }

                    return MFA2(cookie, code_challenge, state, transaction_id, factor_id);
                }
            }
        }

        private string MFA2(string cookie, string code_challenge, string state, string transaction_id, string factor_id) {
            using ( HttpClientHandler ch = new HttpClientHandler() ) {
                ch.AllowAutoRedirect = false;
                ch.UseCookies = false;
                using ( HttpClient client = new HttpClient(ch) ) {
                    // client.Timeout = TimeSpan.FromSeconds(10);
                    client.BaseAddress = new Uri(SSO_URI);
                    client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
                    client.DefaultRequestHeaders.Add("Cookie", cookie);

                    Dictionary<string, string> d = new Dictionary<string, string>();
                    d.Add("factor_id", factor_id);
                    d.Add("passcode", MFA_Code);
                    d.Add("transaction_id", transaction_id);

                    string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(d);

                    using ( var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") ) {
                        HttpResponseMessage result  = client.PostAsync(SSO_URI + "/oauth2/v3/authorize/mfa/verify", content).Result;
                        string resultContent        = result.Content.ReadAsStringAsync().Result;
                        try {
                            dynamic jsonResult  = new JavaScriptSerializer().DeserializeObject(resultContent);
                            object o            = jsonResult["data"]["valid"];

                            if ( (bool)o ) return MFA3(cookie, code_challenge, state, transaction_id);
                        }
                        catch ( Exception ex ) {
                            Log.Error("Error: MFA2 : " + resultContent, ex);
                        }
                    }
                }
            }

            return "NULL";
        }

        private string MFA3(string cookie, string code_challenge, string state, string transaction_id) {
            using ( HttpClientHandler ch = new HttpClientHandler() ) {
                ch.AllowAutoRedirect = false;
                ch.UseCookies = false;
                using ( HttpClient client = new HttpClient(ch) ) {
                    // client.Timeout = TimeSpan.FromSeconds(10);
                    client.BaseAddress = new Uri(SSO_URI);
                    client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
                    client.DefaultRequestHeaders.Add("Cookie", cookie);

                    Dictionary<string, string> d = new Dictionary<string, string>();
                    d.Add("transaction_id", transaction_id);

                    using ( FormUrlEncodedContent content = new FormUrlEncodedContent(d) ) {
                        UriBuilder b    = new UriBuilder(SSO_URI + "/oauth2/v3/authorize");
                        b.Port          = -1;
                        var q           = HttpUtility.ParseQueryString(b.Query);
                        q.Add("client_id", "ownerapi");
                        q.Add("code_challenge", code_challenge);
                        q.Add("code_challenge_method", "S256");
                        q.Add("redirect_uri", SSO_URI + "/void/callback");
                        q.Add("response_type", "code");
                        q.Add("scope", "openid email offline_access");
                        q.Add("state", state);
                        b.Query                     = q.ToString();
                        string url                  = b.ToString();
                        var temp                    = content.ReadAsStringAsync().Result;
                        HttpResponseMessage result  = client.PostAsync(url, content).Result;
                        string resultContent        = result.Content.ReadAsStringAsync().Result;
                        Uri location                = result.Headers.Location;

                        if ( result.StatusCode == HttpStatusCode.Redirect && location != null ) {
                            string code         = HttpUtility.ParseQueryString(location.Query).Get("code");
                            Log.Debug("Code: " + code);
                            return code;
                        }
                        else {
                            Log.Warning("Error: MFA2 Fail!");
                            return "NULL";
                        }
                    }
                }
            }
        }
    }
}