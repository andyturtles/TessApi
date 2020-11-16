using System;

namespace TessApi.JsonData {

    public class LoginResponse {

        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
        public int created_at { get; set; }

        public string TypeAndToken { get { return token_type + " " + access_token; } }
        public DateTime TokenExpiryDate { get { return DateTime.Today + TimeSpan.FromSeconds(expires_in); } }


    }

}
