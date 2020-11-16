namespace TessApi.JsonData {

    public class CommandResult {

        public Response response { get; set; }
    }

    public class Response {
        public string reason { get; set; }
        public bool result { get; set; }
    }

}
