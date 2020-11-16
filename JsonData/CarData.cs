using System.Collections.Generic;

namespace TessApi.JsonData {

    public class CarDataResponse {
        public CarData response { get; set; }
    }

    public class ChargeState {
        public bool? battery_heater_on { get; set; }
        public int? battery_level { get; set; }
        public double? battery_range { get; set; }
        public int? charge_current_request { get; set; }
        public int? charge_current_request_max { get; set; }
        public bool? charge_enable_request { get; set; }
        public double? charge_energy_added { get; set; }
        public int? charge_limit_soc { get; set; }
        public int? charge_limit_soc_max { get; set; }
        public int? charge_limit_soc_min { get; set; }
        public int? charge_limit_soc_std { get; set; }
        public double? charge_miles_added_ideal { get; set; }
        public double? charge_miles_added_rated { get; set; }
        public object charge_port_cold_weather_mode { get; set; }
        public bool? charge_port_door_open { get; set; }
        public string charge_port_latch { get; set; }
        public double? charge_rate { get; set; }
        public bool? charge_to_max_range { get; set; }
        public int? charger_actual_current { get; set; }
        public int? charger_phases { get; set; }
        public int? charger_pilot_current { get; set; }
        public int? charger_power { get; set; }
        public int? charger_voltage { get; set; }
        public string charging_state { get; set; }
        public string conn_charge_cable { get; set; }
        public double? est_battery_range { get; set; }
        public string fast_charger_brand { get; set; }
        public bool? fast_charger_present { get; set; }
        public string fast_charger_type { get; set; }
        public double? ideal_battery_range { get; set; }
        public bool? managed_charging_active { get; set; }
        public object managed_charging_start_time { get; set; }
        public bool? managed_charging_user_canceled { get; set; }
        public int? max_range_charge_counter { get; set; }
        public bool? not_enough_power_to_heat { get; set; }
        public bool? scheduled_charging_pending { get; set; }
        public long? scheduled_charging_start_time { get; set; }
        public double? time_to_full_charge { get; set; }
        public long timestamp { get; set; }
        public bool? trip_charging { get; set; }
        public int? usable_battery_level { get; set; }
        public object user_charge_enable_request { get; set; }

    }

    public class ClimateState {
        public bool? battery_heater { get; set; }
        public bool? battery_heater_no_power { get; set; }
        public string climate_keeper_mode { get; set; }
        public double? driver_temp_setting { get; set; }
        public int? fan_status { get; set; }
        public double? inside_temp { get; set; }
        public bool? is_auto_conditioning_on { get; set; }
        public bool? is_climate_on { get; set; }
        public bool? is_front_defroster_on { get; set; }
        public bool? is_preconditioning { get; set; }
        public bool? is_rear_defroster_on { get; set; }
        public int? left_temp_direction { get; set; }
        public double? max_avail_temp { get; set; }
        public double? min_avail_temp { get; set; }
        public double? outside_temp { get; set; }
        public double? passenger_temp_setting { get; set; }
        public bool? remote_heater_control_enabled { get; set; }
        public int? right_temp_direction { get; set; }
        public int? seat_heater_left { get; set; }
        public int? seat_heater_rear_left { get; set; }
        public int? seat_heater_rear_right { get; set; }
        public int? seat_heater_right { get; set; }
        public int? seat_heater_third_row_left { get; set; }
        public int? seat_heater_third_row_right { get; set; }
        public bool? side_mirror_heaters { get; set; }
        public bool? smart_preconditioning { get; set; }
        public bool? steering_wheel_heater { get; set; }
        public long timestamp { get; set; }
        public bool? wiper_blade_heater { get; set; }
    }

    public class DriveState {
        public int? gps_as_of { get; set; }
        public int? heading { get; set; }
        public double? latitude { get; set; }
        public double? longitude { get; set; }
        public double? native_latitude { get; set; }
        public int? native_location_supported { get; set; }
        public double? native_longitude { get; set; }
        public string native_type { get; set; }
        public double? power { get; set; }
        public string shift_state { get; set; }
        public double? speed { get; set; }
        public long timestamp { get; set; }
    }

    public class GuiSettings {
        public bool? gui_24_hour_time { get; set; }
        public string gui_charge_rate_units { get; set; }
        public string gui_distance_units { get; set; }
        public string gui_range_display { get; set; }
        public string gui_temperature_units { get; set; }
        public long timestamp { get; set; }
    }

    public class VehicleConfig {
        public bool? can_accept_navigation_requests { get; set; }
        public bool? can_actuate_trunks { get; set; }
        public string car_special_type { get; set; }
        public string car_type { get; set; }
        public string charge_port_type { get; set; }
        public bool? eu_vehicle { get; set; }
        public string exterior_color { get; set; }
        public bool? has_air_suspension { get; set; }
        public bool? has_ludicrous_mode { get; set; }
        public bool? motorized_charge_port { get; set; }
        public string perf_config { get; set; }
        public bool? plg { get; set; }
        public int? rear_seat_heaters { get; set; }
        public int? rear_seat_type { get; set; }
        public bool? rhd { get; set; }
        public string roof_color { get; set; }
        public int? seat_type { get; set; }
        public string spoiler_type { get; set; }
        public int? sun_roof_installed { get; set; }
        public string third_row_seats { get; set; }
        public long timestamp { get; set; }
        public string trim_badging { get; set; }
        public string wheel_type { get; set; }
    }

    public class MediaState {
        public bool? remote_control_enabled { get; set; }
    }

    public class SoftwareUpdate {
        public int? expected_duration_sec { get; set; }
        public string status { get; set; }
    }

    public class SpeedLimitMode {
        public bool? active { get; set; }
        public double? current_limit_mph { get; set; }
        public int? max_limit_mph { get; set; }
        public int? min_limit_mph { get; set; }
        public bool? pin_code_set { get; set; }
    }

    public class VehicleState {
        public int? api_version { get; set; }
        public string autopark_state_v2 { get; set; }
        public string autopark_style { get; set; }
        public bool? calendar_supported { get; set; }
        public string car_version { get; set; }

        public string car_version_short {
            get {
                if ( car_version == null ) return null;
                return car_version.Remove(car_version.IndexOf(" "));
            }
        }

        public int? center_display_state { get; set; }
        public int? df { get; set; }
        public int? dr { get; set; }
        public int? ft { get; set; }
        public bool? homelink_nearby { get; set; }
        public bool? is_user_present { get; set; }
        public string last_autopark_error { get; set; }
        public bool? locked { get; set; }
        public MediaState media_state { get; set; }
        public bool? notifications_supported { get; set; }
        public double? odometer { get; set; }
        public bool? parsed_calendar_supported { get; set; }
        public int? pf { get; set; }
        public int? pr { get; set; }
        public bool? remote_start { get; set; }
        public bool? remote_start_enabled { get; set; }
        public bool? remote_start_supported { get; set; }
        public int? rt { get; set; }
        public bool? sentry_mode { get; set; }
        public SoftwareUpdate software_update { get; set; }
        public SpeedLimitMode speed_limit_mode { get; set; }
        public object sun_roof_percent_open { get; set; }
        public string sun_roof_state { get; set; }
        public long timestamp { get; set; }
        public bool? valet_mode { get; set; }
        public string vehicle_name { get; set; }
    }

    public class CarData {
        public long id { get; set; }
        public int? user_id { get; set; }
        public int? vehicle_id { get; set; }
        public string vin { get; set; }
        public string display_name { get; set; }
        public string option_codes { get; set; }
        public object color { get; set; }
        public IList<string> tokens { get; set; }
        public string state { get; set; }
        public bool? in_service { get; set; }
        public string id_s { get; set; }
        public bool? calendar_enabled { get; set; }
        public int? api_version { get; set; }
        public object backseat_token { get; set; }
        public object backseat_token_updated_at { get; set; }
        public ChargeState charge_state { get; set; }
        public ClimateState climate_state { get; set; }
        public DriveState drive_state { get; set; }
        public GuiSettings gui_settings { get; set; }
        public VehicleConfig vehicle_config { get; set; }
        public VehicleState vehicle_state { get; set; }


    }

}
