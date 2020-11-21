using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro.UI;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PureLinkPlugin
{
	/// <summary>
	/// Plugin device factory
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed
	/// </remarks>
	/// <example>
	/// "EssentialsPluginFactoryTemplate" renamed to "SamsungMdcFactory"
	/// </example>
	public class PureLinkFactory : EssentialsPluginDeviceFactory<PureLinkDevice>
	{
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
		/// <remarks>
		/// Update the MinimumEssentialsFrameworkVersion and TypeNames as needed when creating a plugin
		/// </remarks>
		/// <example>
		/// Set the minimum Essentials Framework Version
		/// <code>
		///  MinimumEssentialsFrameworkVersion = "1.5.5";
		/// </code>
		/// In the constructor we initialize the list with the typenames that will build an instance of this device
		/// <code>
		/// TypeNames = new List<string>() { "SamsungMdc", "SamsungMdcDisplay" };
		/// </code>
		/// </example>
		public PureLinkFactory()
		{
			// Set the minimum Essentials Framework Version
			// TODO [ ] Update the Essentials minimum framework version which this plugin has been tested against
			MinimumEssentialsFrameworkVersion = "1.6.6";

			// In the constructor we initialize the list with the typenames that will build an instance of this device
			// TODO [X] Update the TypeNames for the plugin being developed. Note TypeName is not case sensitive.           
			TypeNames = new List<string>() { "PureLink", "MediaAxis" };
		}

		/// <summary>
		/// Builds and returns an instance of EssentialsPluginDeviceTemplate
		/// </summary>
		/// <param name="dc">device configuration</param>
		/// <returns>plugin device or null</returns>
		/// <remarks>		
		/// The example provided below takes the device key, name, properties config and the comms device created.
		/// Modify the EssetnialsPlugingDeviceTemplate constructor as needed to meet the requirements of the plugin device.
		/// </remarks>
		/// <seealso cref="PepperDash.Core.eControlMethod"/>
		public override EssentialsDevice BuildDevice(DeviceConfig dc)
		{
			try
			{
				Debug.Console(0, new string('*', 80));
				Debug.Console(0, new string('*', 80));
				Debug.Console(0, "[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);				
				
				// get the plugin device properties configuration object & check for null 
				var propertiesConfig = dc.Properties.ToObject<PureLinkConfig>();
				if (propertiesConfig == null)
				{
					Debug.Console(0, "[{0}] Factory: failed to read properties config for {1}", dc.Key, dc.Name);
					return null;
				}						

				// TODO [ ] If your device is using a PepperDash.Core.eControlMethod supported enum, the snippet below will support standard comm methods
				// build the plugin device comms (for all other comms methods) & check for null			
				var comms = CommFactory.CreateCommForDevice(dc);
				if (comms != null) return new PureLinkDevice(dc.Key, dc.Name, propertiesConfig, comms);
				Debug.Console(0, "[{0}] Factory: failed to create comm for {1}", dc.Key, dc.Name);
				return null;
			}
			catch (Exception ex)
			{
				Debug.Console(0, "[{0}] Factory BuildDevice Exception: {1}", dc.Key, ex);
				return null;
			}
		}
	}
}