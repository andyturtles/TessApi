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

        private string DoMFA_Code(string MFA_Code) {
            if ( MFA_Code == null || MFA_Code.Length != 6 ) throw new Exception("Wrong MFA Code Format!");
            Log.Debug("MFA Code: " + MFA_Code);

            while ( true ) {
                Log.Debug("transaction_id: " + transaction_id);
                string code = MFA1(MFA_Code);

                if (code.Length > 0 ) return code;
                Thread.Sleep(500);
            }
        }

        private string MFA1(string MFA_Code) {
            string resultContent;
            using ( HttpClient client = new TessHttpClient(new TessClientHandler(null, false)) ) {
                client.DefaultRequestHeaders.Add("Cookie", cookie);

                UriBuilder b = new UriBuilder(SSO_URI + "/oauth2/v3/authorize/mfa/factors");
                b.Port = -1;

                var q = HttpUtility.ParseQueryString(b.Query);
                q.Add("transaction_id", transaction_id);
                b.Query = q.ToString();
                string url = b.ToString();

                HttpResponseMessage result = client.GetAsync(url).Result;
                resultContent = result.Content.ReadAsStringAsync().Result;
                Log.Debug("MFA1 Result: " + resultContent);
                dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                string factor_id = null;
                try {
                    factor_id = jsonResult["data"][0]["id"];
                }
                catch ( Exception ex ) {
                    Log.Error("MFA1 ResultContent: " + resultContent, ex);
                }

                return MFA2(MFA_Code, factor_id);
            }
        }

        private string MFA2(string MFA_Code, string factor_id) {
            using ( HttpClient client = new TessHttpClient(new TessClientHandler(false, false)) ) {
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

                        if ( (bool)o ) return MFA3();
                    }
                    catch ( Exception ex ) {
                        Log.Error("Error: MFA2 : " + resultContent, ex);
                    }
                }
            }

            return "NULL";
        }

        private string MFA3() {
            using ( HttpClient client = new TessHttpClient(new TessClientHandler(false, false)) ) {
                client.DefaultRequestHeaders.Add("Cookie", cookie);

                Dictionary<string, string> d = new Dictionary<string, string>();
                d.Add("transaction_id", transaction_id);

                using ( FormUrlEncodedContent content = new FormUrlEncodedContent(d) ) {
                    UriBuilder b = new UriBuilder(SSO_URI + "/oauth2/v3/authorize");
                    b.Port = -1;
                    var q = HttpUtility.ParseQueryString(b.Query);
                    q.Add("client_id", "ownerapi");
                    q.Add("code_challenge", code_challenge);
                    q.Add("code_challenge_method", "S256");
                    q.Add("redirect_uri", SSO_URI + "/void/callback");
                    q.Add("response_type", "code");
                    q.Add("scope", "openid email offline_access");
                    q.Add("state", state);
                    b.Query = q.ToString();
                    string url = b.ToString();
                    var temp = content.ReadAsStringAsync().Result;
                    HttpResponseMessage result = client.PostAsync(url, content).Result;
                    string resultContent = result.Content.ReadAsStringAsync().Result;
                    Uri location = result.Headers.Location;

                    if ( result.StatusCode == HttpStatusCode.Redirect && location != null ) {
                        string code = HttpUtility.ParseQueryString(location.Query).Get("code");
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