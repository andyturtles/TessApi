using System;

namespace TessApi {

    public class TessApiLoginResult : TessApiResult {

        public bool MfaNeeded;

        public TessApiLoginResult(bool mfa) : base() {
            MfaNeeded = mfa;
        }

        public TessApiLoginResult(Exception ex) : base(ex) {
        }
    }
}