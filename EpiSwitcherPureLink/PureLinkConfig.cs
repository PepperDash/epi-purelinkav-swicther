using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace PureLinkPlugin
{
	/// <summary>
	/// Plugin device configuration object
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being created
	/// </remarks>
	/// <example>
	/// "EssentialsPluginConfigObjectTemplate" renamed to "SamsungMdcConfig"
	/// <code>
	/// {
	///		"devices": [
	///			{
	///				"key": "essentialsPluginKey",
	///				"name": "Essentials Plugin Name",
	///				"type": "essentialsPluginTypeName",
	///				"group": "pluginDevices",
	///				"properties": {
	///					"control": {
	///						"method": "PepperDash.Core.eControlMethod",
	///						"controlPortDevKey": "examplePortDevKey",
	///						"controlPortNumber": 1,
	///						"comParams": {
	///							"baudRate": 9600,
	///							"dataBits": 8,
	///							"stopBits": 1,
	///							"parity": "None",
	///							"protocol": "RS232",
	///							"hardwareHandshake": "None",
	///							"softwareHandshake": "None"
	///						},
	///						"tcpSshProperties": {
	///							"address": "172.22.0.101",
	///							"port": 23,
	///							"username": "admin",
	///							"password": "password",
	///							"autoReconnect": true,
	///							"autoReconnectIntervalMs": 10000
	///						}
	///					},
	///					"pollTimeMs": 30000,
	///					"warningTimeoutMs": 180000,
	///					"errorTimeoutMs": 300000,
	///					"pluginCollection": {
	///						"item1": {
	///							"name": "Item 1",
	///							"value": 1
	///						}
	///						"item2": {
	///							"name": "Item 2",,
	///							"value": 2
	///						}
	///					}
	///				}
	///			}
	///		]
	/// }
	/// </code>
	/// </example>
	[ConfigSnippet("{\"devices\":[{\"key\":\"essentialsPluginKey\",\"name\":\"Essentials Plugin Name\",\"type\":\"essentialsPluginTypeName\",\"group\":\"pluginDevices\",\"properties\":{\"control\":{\"method\":\"PepperDash.Core.eControlMethod\",\"controlPortDevKey\":\"exampleControlPortDevKey\",\"controlPortNumber\":1,\"comParams\":{\"baudRate\":9600,\"dataBits\":8,\"stopBits\":1,\"parity\":\"None\",\"protocol\":\"RS232\",\"hardwareHandshake\":\"None\",\"softwareHandshake\":\"None\"},\"tcpSshProperties\":{\"address\":\"172.22.0.101\",\"port\":22,\"username\":\"admin\",\"password\":\"password\",\"autoReconnect\":true,\"autoReconnectIntervalMs\":10000}},\"pollTimeMs\":30000,\"warningTimeoutMs\":180000,\"errorTimeoutMs\":300000,\"pluginCollection\":{\"item1\":{\"name\":\"Item 1\",\"value\":1},\"item2\":{\"name\":\"Item 2\",\"value\":2}}}}]}")]
	public class PureLinkConfig
	{
		/// <summary>
		/// JSON control object
		/// </summary>
		/// <remarks>
		/// Typically this object is not required, but in some instances it may be needed.  For example, when building a 
		/// plugin that is using Telnet (TCP/IP) communications and requires login, the device will need to handle the login.
		/// In order to do so, you will need the username and password in the "tcpSshProperties" object.
		/// </remarks>
		/// <example>
		/// <code>
		///	"control": {
        ///		"method": "tcpIp",
		///		"controlPortDevKey": "processor",
		///		"controlPortNumber": 1,
		///		"comParams": {
		///			"baudRate": 9600,
		///			"dataBits": 8,
		///			"stopBits": 1,
		///			"parity": "None",
		///			"protocol": "RS232",
		///			"hardwareHandshake": "None",
		///			"softwareHandshake": "None"
		///		},
		///		"tcpSshProperties": {
		///			"address": "172.22.0.101",
		///			"port": 23,
		///			"username": "admin",
		///			"password": "password",
		///			"autoReconnect": true,
		///			"autoReconnectIntervalMs": 10000
		///		}
		///	}				
		/// </code>
		/// </example>		
		[JsonProperty("control")]
		public EssentialsControlPropertiesConfig Control { get; set; }

		/// <summary>
		/// Serializes the poll time value
		/// </summary>
		/// <remarks>
		/// This is an exmaple device plugin property.  This should be modified or deleted as needed for the plugin being built.
		/// </remarks>
		/// <value>
		/// PollTimeMs property gets/sets the value as a long
		/// </value>
		/// <example>
		/// <code>
		/// "properties": {
		///		"polltimeMs": 30000
		/// }
		/// </code>
		/// </example>
		[JsonProperty("pollTimeMs")]
		public long PollTimeMs { get; set; }

        /// <summary>
        /// This is the poll string
        /// </summary>
        /// <remarks>
        /// This is an exmaple device plugin property.  This should be modified or deleted as needed for the plugin being built.
        /// </remarks>
        /// <value>
        /// PollTimeMs property gets/sets the value as a string
        /// </value>
        /// <example>
        /// <code>
        /// "properties": {
        ///		"pollString": "*255H000!"
        /// }
        /// </code>
        /// </example>
        [JsonProperty("pollString")]
        public string PollString { get; set; }

        /// <summary>
        /// Plugin bridge request to poll outputs for current video
        /// </summary>
        /// <value>
        /// PollVideo property gets/sets the value as a bool
        /// </value>
        [JsonProperty("pollvideo")]
        public bool PollVideo { get; set; }

        /// <summary>
        /// Plugin bridge request to poll outputs for current audio
        /// </summary>
        /// <value>
        /// PollAudio property gets/sets the value as a bool
        /// </value>
        [JsonProperty("pollaudio")]
        public bool PollAudio { get; set; }

		/// <summary>
		/// Serializes the warning timeout value
		/// </summary>
		/// <remarks>
		/// This is an exmaple device plugin property.  This should be modified or deleted as needed for the plugin being built.
		/// </remarks>
		/// <value>
		/// WarningTimeoutMs property gets/sets the value as a long
		/// </value>
		/// <example>
		/// <code>
		/// "properties": {
		///		"warningTimeoutMs": 180000
		/// }
		/// </code>
		/// </example>
		[JsonProperty("warningTimeoutMs")]
		public long WarningTimeoutMs { get; set; }

		/// <summary>
		/// Serializes the error timeout value
		/// </summary>
		/// /// <remarks>
		/// This is an exmaple device plugin property.  This should be modified or deleted as needed for the plugin being built.
		/// </remarks>
		/// <value>
		/// ErrorTimeoutMs property gets/sets the value as a long
		/// </value>
		/// <example>
		/// <code>
		/// "properties": {
		///		"errorTimeoutMs": 300000
		/// }
		/// </code>
		/// </example>
		[JsonProperty("errorTimeoutMs")]
		public long ErrorTimeoutMs { get; set; }

        /// <summary>
        /// Plugin device ID property specfic to the switcher used to distinquish between cascading switchers
        /// </summary>
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// Plugin model used to determine type of API Execute Switch commands to send
        /// </summary>
        [JsonProperty("model")]
        public uint Model { get; set; }

        ///// <summary>
        ///// Plugin property for audio follows video
        ///// </summary>
        //[JsonProperty("audioFollowsVideo")]
        //public bool EnableAudioBreakawayFeedback { get; set; }

        /// <summary>
        /// Plugin property for source inputs
        /// </summary>
        [JsonProperty("inputs")]
        public Dictionary<uint, PureLinkEntryConfig> Inputs { get; set; }

        /// <summary>
        /// Plugin property for source outputs
        /// </summary>
        [JsonProperty("outputs")]
        public Dictionary<uint, PureLinkEntryConfig> Outputs { get; set; }

		/// <summary>
		/// Constuctor
		/// </summary>
		/// <remarks>
		/// If using a collection you must instantiate the collection in the constructor
		/// to avoid exceptions when reading the configuration file 
		/// </remarks>
		public PureLinkConfig()
		{
            Inputs = new Dictionary<uint, PureLinkEntryConfig>();
            Outputs = new Dictionary<uint, PureLinkEntryConfig>();
		}
	}

    /// <summary>
    /// PureLink class creating properties for 'EntryConfig' IO names
    /// </summary>
    public class PureLinkEntryConfig
    {
        /// <summary>
        /// Generic source name. Name will override VideoName and AudioName if not defined.
        /// </summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// Video source name
        /// </summary>
        [JsonProperty("videoName", NullValueHandling = NullValueHandling.Ignore)]
        public string VideoName { get; set; }

        /// <summary>
        /// Audio source name
        /// </summary>
        [JsonProperty("audioName", NullValueHandling = NullValueHandling.Ignore)]
        public string AudioName { get; set; }
    }
}