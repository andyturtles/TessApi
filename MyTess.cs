using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using aBasics;
using TessApi.JsonData;

namespace TessApi {

    /// <summary>
    /// API Info: https://tesla-api.timdorr.com/api-basics/authentication
    /// or
    /// https://www.teslaapi.io/authentication/oauth
    /// </summary>
    public class MyTess {

        private TessApiLogin tessApiLogin;

        private long? myCarId;

        public LoginResponse LoginResponse { get; internal set; }
        public IList<Product> ProductList { get; internal set; }
        public Product MyCar { get; internal set; }
        public CarData MyCarData { get; internal set; }

        public DateTime? DiskDataDate;

        public bool IsLoggedIn { get { return ( LoginResponse != null ); } }
        public bool IsSleeping { get { return ( ( MyCar?.state == "asleep" ) || ( MyCar?.state == "offline" ) ); } }

        public bool IsCharging {
            get { return ( MyCarData?.charge_state?.charging_state == "Charging" ); }
        }

        public bool CanDoCharging {
            get { return ( !IsSleeping &&
                           !IsCharging &&
                           ( MyCarData?.charge_state?.charging_state != "Complete" ) &&
                           ( MyCarData?.charge_state?.charging_state != "Disconnected" ) ); }
        }

        public bool IsFullCharged {
            get { return ( MyCarData?.charge_state?.battery_level >= MyCarData?.charge_state?.charge_limit_soc ); }
        }

        public MyTess() {
        }

        public void LoadLogin() {
            LoginResponse   = TessTools.LoadResponse<LoginResponse>(out _, true);
        }

        public void SetCarId(long cId) {
            myCarId = cId;
        }

        #region Doors etc. ...

        public async Task<TessApiResult> DoorsUnlock() {
            return await SendCommand("door_unlock");
        }

        public async Task<TessApiResult> DoorsLock() {
            return await SendCommand("door_lock");
        }

        public async Task<TessApiResult> OpenFrontTrunk() {
            string cmdTxt = $"which_trunk=front";
            return await SendCommand("actuate_trunk", cmdTxt);
        }

        public async Task<TessApiResult> OpenRearTrunk() {
            string cmdTxt = $"which_trunk=rear";
            return await SendCommand("actuate_trunk", cmdTxt);
        }

        public async Task<TessApiResult> SetSentryMode(bool activate) {
            string cmdTxt = $"on=" + activate;
            return await SendCommand("set_sentry_mode", cmdTxt);
        }

        public async Task<TessApiResult> HonkHorn() {
            return await SendCommand("honk_horn");
        }

        public async Task<TessApiResult> FlashLights() {
            return await SendCommand("flash_lights");
        }

        #endregion Doors etc. ...

        #region Charging Commands ...

        public async Task<TessApiResult> OpenChargePort() {
            return await SendCommand("charge_port_door_open");
        }

        public async Task<TessApiResult> CloseChargePort() {
            return await SendCommand("charge_port_door_close");
        }

        public async Task<TessApiResult> StartCharge() {
            return await SendCommand("charge_start");
        }

        public async Task<TessApiResult> StopCharge() {
            return await SendCommand("charge_stop");
        }

        public async Task<TessApiResult> SetChargeLimit(int limitPercentage) {
            string cmdTxt = "percent=" + limitPercentage;
            return await SendCommand("set_charge_limit", cmdTxt);
        }

        #endregion Charging Commands ...

        #region Climate Commands ...

        public async Task<TessApiResult> StartAutoconditioning() {
            return await SendCommand("auto_conditioning_start");
        }

        public async Task<TessApiResult> StopAutoconditioning() {
            return await SendCommand("auto_conditioning_stop");
        }

        public async Task<TessApiResult> SetTemps(double driver, double passenger) {
            string drvStr = TessTools.GetDoubleAsString(driver);
            string pasStr = TessTools.GetDoubleAsString(passenger);

            string cmdTxt = $"driver_temp={drvStr}&passenger_temp={pasStr}";
            return await SendCommand("set_temps", cmdTxt);
        }

        public async Task<TessApiResult> SetPreconditioningMax(bool on) {
            string cmdTxt = $"on={on}";
            return await SendCommand("set_preconditioning_max", cmdTxt);
        }

        private string GetCoordinatesString(double lat, double lon) {
            NumberFormatInfo nfi        = new NumberFormatInfo();
            nfi.NumberDecimalSeparator  = ".";
            nfi.NumberGroupSeparator    = "";
            string latStr               = lat.ToString(nfi);
            string lonStr               = lon.ToString(nfi);
            string coordStr             = $"lat={latStr}&lon={lonStr}";
            return coordStr;
        }

        public async Task<TessApiResult> CloseWindows(double lat, double lon) {
            string cmdTxt = $"command=close&" + GetCoordinatesString(lat, lon);
            return await SendCommand("window_control", cmdTxt);
        }

        public async Task<TessApiResult> VentWindows(double lat, double lon) {
            string cmdTxt = $"command=vent&" + GetCoordinatesString(lat, lon);
            return await SendCommand("window_control", cmdTxt);
        }

        public async Task<TessApiResult> TriggerHomelink(double lat, double lon) {
            string cmdTxt = GetCoordinatesString(lat, lon);
            return await SendCommand("trigger_homelink", cmdTxt);
        }

    public enum SeatHeaterNumber {
            Driver      = 0,
            Passenger   = 1,
            Rear_left   = 2,
            Rear_center = 4,
            Rear_right  = 5
        }

        public enum SeatHeaterLevel {
            Off     = 0,
            Low     = 1,
            Med     = 2,
            High    = 3
        }

        public async Task<TessApiResult> SetSeatHeater(SeatHeaterNumber seat, SeatHeaterLevel lev) {
            string cmdTxt = $"heater={( (int)seat )}&level={( (int)lev )}";
            return await SendCommand("remote_seat_heater_request", cmdTxt);
        }

        #endregion Climate Commands ...

        #region State / Wakeup etc. ...

        public async Task<TessApiResult> WakeUp() {
            this.MyCarData          = null;
            string url              = $"https://owner-api.teslamotors.com/api/1/vehicles/{myCarId.Value}/wake_up";
            try {
                string result           = await CallUrl(url, "POST");
                CarDataResponse pr      = SerializeTool.DeSerializeJson<CarDataResponse>(result);
                TessTools.SaveResponse(pr);
                MyCarData               = pr.response;

                return new TessApiResult();
            }
            catch ( Exception ex ) {
                return new TessApiResult(ex);
            }
        }

        public async Task<TessApiResult> GetCarInfo(bool loadFromDisk) {
            this.MyCarData = null;
            try {
                CarDataResponse cdr = null;
                if ( loadFromDisk ) {
                    cdr = TessTools.LoadResponse<CarDataResponse>(out DiskDataDate);
                    Log.Debug("GetCarInfo - loadFromDisk cdr Null:" + ( cdr == null ));
                    if ( cdr != null ) {
                        MyCarData               = cdr.response;
                        return new TessApiResult();
                    }
                }

                if ( MyCar == null ) return new TessApiResult(false, "No Car Data");

                if ( !loadFromDisk ) {
                    string url      = $"https://owner-api.teslamotors.com/api/1/vehicles/{myCarId.Value}/vehicle_data";
                    string result   = await CallUrl(url, "GET");
                    cdr             = SerializeTool.DeSerializeJson<CarDataResponse>(result);
                }

                MyCarData               = cdr?.response;
                if ( cdr != null ) TessTools.SaveResponse(cdr);
                return new TessApiResult();
            }
            catch ( Exception ex ) {
                Log.Error("MyTess.GetCarInfo", ex);
                return new TessApiResult(ex);
            }
        }

        public async Task<TessApiResult> ListProducts(bool loadFromDisk) {
            this.MyCar      = null;
            this.MyCarData  = null;

            try {
                ProductResponse pr;
                if ( loadFromDisk ) {
                    pr              = TessTools.LoadResponse<ProductResponse>(out _);
                }
                else {
                    // Optional https://owner-api.teslamotors.com/api/1/vehicles
                    string url      = "https://owner-api.teslamotors.com/api/1/products/";
                    string result   = await CallUrl(url, "GET");
                    pr              = SerializeTool.DeSerializeJson<ProductResponse>(result);
                    TessTools.SaveResponse(pr);
                }

                ProductList         = pr.response;

                if ( myCarId.HasValue ) {
                    foreach ( Product p in pr.response ) {
                        if ( p.id == myCarId ) MyCar = p;
                    }
                }

                return new TessApiResult();
            }
            catch ( Exception ex ) {
                Log.Error("MyTess.ListProducts", ex);
                return new TessApiResult(ex);
            }
        }

        public async Task<TessApiLoginResult> Login(string username, string pass) {
            try {
                tessApiLogin        = new TessApiLogin();
                bool loginFinished  = await tessApiLogin.DoLogin(username, pass);
                if ( !loginFinished ) return new TessApiLoginResult(true); // MFA required!

                HandleLoginSuccess();
                return new TessApiLoginResult(false);
            }
            catch ( Exception ex ) {
                Log.Error("MyTess.Login", ex);
                return new TessApiLoginResult(ex);
            }
        }

        public async Task<TessApiLoginResult> ContinueMfaLogin(string mfaCode) {
            try {
                if ( tessApiLogin == null ) throw new Exception("No Login Object!");
                await tessApiLogin.ContinueLoginAfterMfa(mfaCode);

                HandleLoginSuccess();
                return new TessApiLoginResult(false);
            }
            catch ( Exception ex ) {
                Log.Error("MyTess.ContinueMfaLogin", ex);
                return new TessApiLoginResult(ex);
            }
        }

        private void HandleLoginSuccess() {
            LoginResponse   = tessApiLogin.LoginResponse;
            if ( String.IsNullOrEmpty(LoginResponse?.access_token) ) throw new Exception("access_token LEER!");
            TessTools.SaveResponse(LoginResponse, null, true);

            tessApiLogin = null; // brauchen wir jetzt nicht mehr!
            myCarId = null; // Can change for other user! Need to reset after login.
        }

        #endregion State / Wakeup etc. ...

         private async Task<TessApiResult> SendCommand(string command, string commandText = null) {
            try {
                string url              = $"https://owner-api.teslamotors.com/api/1/vehicles/{myCarId.Value}/command/" + command;
                string result           = await CallUrl(url, "POST", true, commandText);
                CommandResult cr        = SerializeTool.DeSerializeJson<CommandResult>(result);
                TessTools.SaveResponse(cr, command);
                return new TessApiResult(cr);
            }
            catch ( Exception ex ) {
                Log.Error("MyTess.SendCommand: " + command, ex);
                return new TessApiResult(ex);
            }
        }

        private async Task<string> CallUrl(string url, string method, bool addToken = true, string bodyText = null) {
            string action;
            if ( url.Contains("oauth") )    action = "login / oauth";
            else                            action = url.Substring(url.LastIndexOf("/"));

            using ( HttpClient cl = new HttpClient() ) {
                cl.DefaultRequestHeaders.UserAgent.Clear();
                cl.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Linux; Android 8.1.0; Pixel XL Build/OPM4.171019.021.D1; wv) AppleWebKit/537.36 (KHTML, Like Gecko) Version/4.0 Chrome/68.0.3440.91 Mobile Safari/537.36");

                cl.DefaultRequestHeaders.Add("x-tesla-user-agent", "TeslaApp/3.4.4-350/fad4a582e/android/8.1.0");
                if ( addToken ) cl.DefaultRequestHeaders.Add("authorization", LoginResponse.TypeAndToken);

                HttpContent cont = null;
                if ( !String.IsNullOrEmpty(bodyText) ) {
                    // StringContent hat irgendwie nicht funktioniert, auch mit Encoding...
                    Encoding enc        = Encoding.ASCII;
                    byte[] ba           = enc.GetBytes(bodyText);
                    cont                = new ByteArrayContent(ba);
                }

                HttpResponseMessage response = null;
                try {
                    if ( method == "GET" )  response    = await cl.GetAsync(url);
                    else                    response    = await cl.PostAsync(url, cont);

                    string responseContent              = await response.Content.ReadAsStringAsync();

                    Log.Debug($"CallUrl [{action}], Response: {responseContent}; StatusCode: {response.StatusCode}");

                    if ( response.StatusCode != System.Net.HttpStatusCode.OK ) throw new Exception("Bad response.StatusCode: " + response.StatusCode);

                    return responseContent;
                }
                finally {
                    response?.Dispose();
                }
            } // end using ( HttpClient cl
        }
    }
}