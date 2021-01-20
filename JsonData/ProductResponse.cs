using System.Collections.Generic;

namespace TessApi.JsonData {

    public class ProductResponse {
        public IList<Product> response { get; set; }
        public int count { get; set; }
    }

    public class Product {
        public long id { get; set; }
        public long user_id { get; set; }
        public long vehicle_id { get; set; }
        public string vin { get; set; }
        public string display_name { get; set; }
        public string option_codes { get; set; }
        public object color { get; set; }
        public IList<string> tokens { get; set; }
        public string state { get; set; }
        public bool in_service { get; set; }
        public string id_s { get; set; }
        public bool calendar_enabled { get; set; }
        public int api_version { get; set; }
        public object backseat_token { get; set; }
        public object backseat_token_updated_at { get; set; }
    }
}