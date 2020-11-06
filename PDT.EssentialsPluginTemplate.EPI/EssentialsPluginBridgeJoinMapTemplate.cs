using PepperDash.Essentials.Core;

namespace EssentialsPluginTemplateEPI
{
	public class EssentialsPluginBridgeJoinMapTemplate : JoinMapBaseAdvanced
	{
	    public JoinDataComplete DeviceName = new JoinDataComplete(new JoinData {JoinNumber = 1, JoinSpan = 1},
	        new JoinMetadata
	        {
	            Label = "Device Name",
	            JoinCapabilities = eJoinCapabilities.ToSIMPL,
	            JoinType = eJoinType.Serial
	        });

		public EssentialsPluginBridgeJoinMapTemplate(uint joinStart) 
            :base(joinStart)
		{
		}
	}
}