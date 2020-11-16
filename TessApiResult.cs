using System;
using TessApi.JsonData;

namespace TessApi {
    public class TessApiResult {

        public string Message { get; internal set; }
        public bool Success { get; internal set; }
        public Exception Error { get; internal set; }



        public TessApiResult() {
            Success = true;
            Message = "";
        }

        public TessApiResult(Exception ex) {
            this.Error      = ex;
            this.Success    = false;
            this.Message    = ex.Message;
        }

        public TessApiResult(CommandResult cr) {
            this.Success    = cr.response.result;
            this.Message    = cr.response.reason;
        }

        public TessApiResult(bool OK, string msg) {
            this.Success    = OK;
            this.Message    = msg;
        }

    }
}
