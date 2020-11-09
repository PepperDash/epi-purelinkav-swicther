using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using Newtonsoft.Json;

namespace EpiSwitcherPureLink
{
    public class PurelinkFactory : EssentialsPluginDeviceFactory<PureLinkDevice>
    {
        public PurelinkFactory()
        {
            // Set the minimum Essentials Framework Version
            MinimumEssentialsFrameworkVersion = "1.6.6";

            // In the constructor we initialize the list with the typenames that will build an instance of this device
            TypeNames = new List<string>() { "pureLink", "purelink" };
        }

        // Builds and returns an instance of EssentialsPluginDeviceTemplate
        public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
        {
            Debug.Console(1, "Factory Attempting to create new device from type: {0}", dc.Type);

            var comms = CommFactory.CreateCommForDevice(dc);
            if (comms == null)
            {
                Debug.Console(2, "[{0}] PureLinkFactory: Failed to create comms for {1}", dc.Key, dc.Name);
                return null;
            }

            var propertiesConfig = dc.Properties.ToObject<PureLinkConfig>();
            if (propertiesConfig == null)
            {
                Debug.Console(2, "[{0}] PureLinkFactory: Failed to read properties config for {1}", dc.Key, dc.Name);
                return null;
            }
            
            return new PureLinkDevice(dc.Key, dc.Name, propertiesConfig);
        }
    }
}