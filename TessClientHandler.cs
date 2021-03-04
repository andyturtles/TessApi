using System.Net;
using System.Net.Http;

namespace TessApi {

    internal class TessClientHandler : HttpClientHandler {

        public TessClientHandler(bool? allowAutoRedirect, bool? useCookies) : base() {
            AutomaticDecompression                                  = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            if ( allowAutoRedirect.HasValue )   AllowAutoRedirect   = allowAutoRedirect.Value;
            if ( useCookies.HasValue )          UseCookies          = useCookies.Value;
        }
    }
}