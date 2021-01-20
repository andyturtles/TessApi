using System;

namespace TessApi.JsonData {

    public class LoginResponse {

        public string access_token { get; set; }
        public string token_type { get; set; }
        public long expires_in { get; set; }
        public string refresh_token { get; set; }
        public long created_at { get; set; }

        public string TypeAndToken { get { return token_type + " " + access_token; } }
        public DateTime TokenExpiryDate { get { return DateTime.Today + TimeSpan.FromSeconds(expires_in); } }
    }
}