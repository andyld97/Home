using System;
using System.Text.Json.Serialization;

namespace Home.API.Model
{
    /// <summary>
    /// The config used for this API
    /// </summary>
    public class Config
    {
        public static readonly string DEVICE_PATH = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "devices.xml");
        public static readonly string SCREENSHOTS_PATH = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots");

        /// <summary>
        /// The complete url where this api is hosted (port inclusve, see default value) <br />
        /// Can be set to http://0.0.0.0:5250 for using without reverse proxy!
        /// </summary>
        [JsonPropertyName("host")]
        public string HostUrl { get; set; } = "http://localhost:5250";

        /// <summary>
        /// If set to true a webhook for important events will be called (<see cref="WebHookUrl"/> must be also set)
        /// </summary>
        [JsonPropertyName("use_webhook")]
        public bool UseWebHook { get; set; } = false;

        /// <summary>
        /// The url which will be trigger your webhook (e.g. http://my-url.de/webhook.php/message?param=)<br/>
        /// The message will be appended directly onto this url, so make sure your url provides a parameter and looks like the example above!
        /// </summary>
        [JsonPropertyName("webhook_url")]
        public string WebHookUrl { get; set; }

        /// <summary>
        /// The HealthCheckTimer manages a few things:<br/><br/>
        /// - Aquires new screenshots <br/>
        /// - Deletes old screenshots <br/>
        /// - Removes inactive client queues <br/>
        /// - Saves devices.xml <br/>
        /// </summary>
        [JsonPropertyName("health_check_timer_interval")]
        public TimeSpan HealthCheckTimerInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The interval when inactive Home.WPF client queues should be removed
        /// </summary>
        [JsonPropertyName("remove_inactive_gui_clients")]
        public TimeSpan RemoveInactiveGUIClients { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Describes how much time must elapse after the last ACK for the client to be recognised as offline.
        /// </summary>
        [JsonPropertyName("remove_inactive_clients")]
        public TimeSpan RemoveInactiveClients { get; set; } = TimeSpan.FromMinutes(3);

        /// <summary>
        /// How many time must elapse to create a new screenshot
        /// </summary>
        [JsonPropertyName("aquire_new_screenshot")]
        public TimeSpan AquireNewScreenshot { get; set; } = TimeSpan.FromHours(12);

        /// <summary>
        /// Describes after how many hours/days an old screenshot gets deleted
        /// </summary>
        [JsonPropertyName("remove_old_screenshots")]
        public TimeSpan RemoveOldScreenshots { get; set; } = TimeSpan.FromDays(1);

        /// <summary>
        /// Treshold, when a storage warning should appear (%, percentage left)
        /// </summary>
        [JsonPropertyName("storage_warning_percentage")]
        public int StorageWarningPercentage { get; set; } = 10;

        /// <summary>
        /// Treshold, when a battery warning should appear (%, percentage left)
        /// </summary>
        [JsonPropertyName("battery_warning_percentage")]
        public int BatteryWarningPercentage { get; set; } = 10;
    }
}