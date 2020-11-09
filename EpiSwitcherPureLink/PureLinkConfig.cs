using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Core.JsonStandardObjects;
using DeviceConfig = PepperDash.Essentials.Core.Config.DeviceConfig;

namespace EpiSwitcherPureLink
{
	public class PureLinkConfig
	{
        [JsonProperty("pollTimeMs")]
        public long PollTimeMs { get; set; }

        [JsonProperty("pollString")]
        public string PollString { get; set; }

        [JsonProperty("warningTimeoutMs")]
        public long WarningTimeoutMs { get; set; }

        [JsonProperty("errorTimeoutMs")]
        public long ErrorTimeoutMs { get; set; }

        [JsonProperty("deviceId")]
        public long DeviceId { get; set; }

        [JsonProperty("audioFollowsVideo")]
        public bool AudioFollowsVideo { get; set; }

        [JsonProperty("inputs")]
        public Dictionary<uint, PureLinkEntryConfig> Inputs { get; set; }

        [JsonProperty("outputs")]
        public Dictionary<uint, PureLinkEntryConfig> Outputs { get; set; }

        /// <summary>
		/// Constructor
		/// </summary>
        public PureLinkConfig()
		{
            Inputs = new Dictionary<uint, PureLinkEntryConfig>();
            Outputs = new Dictionary<uint, PureLinkEntryConfig>();
		}

		/// <summary>
		/// Get config properties from config
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public static PureLinkConfig FromConfig(DeviceConfig config)
		{
			return JsonConvert.DeserializeObject<PureLinkConfig>(config.Properties.ToString());
		}
	}
}