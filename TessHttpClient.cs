using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace TessApi {

    internal class TessHttpClient : HttpClient {

        internal TessClientHandler myClientHandler;

        private const string USER_AGENT_X   = ""; // oder das? "TeslaLogger", oder das "TL V1" - Oder leer, dann gar nicht setzten ....

        public TessHttpClient(TessClientHandler ch) : base(ch) {
            myClientHandler = ch;

            if ( !String.IsNullOrEmpty(USER_AGENT_X) )  DefaultRequestHeaders.Add("User-Agent", USER_AGENT_X);

            DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            DefaultRequestHeaders.Connection.Add("keep-alive");

            BaseAddress = new Uri(TessApiLogin.SSO_URI);
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if ( myClientHandler != null ) {
                myClientHandler.Dispose();
                myClientHandler = null;
            }
        }
    }
}