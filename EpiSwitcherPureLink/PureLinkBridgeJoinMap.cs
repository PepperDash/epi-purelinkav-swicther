using PepperDash.Essentials.Core;

namespace EpiSwitcherPureLink
{
	public class PureLinkBridgeJoinMap : JoinMapBaseAdvanced
	{
	    public JoinDataComplete DeviceName = new JoinDataComplete(new JoinData {JoinNumber = 1, JoinSpan = 1},
	        new JoinMetadata
	        {
	            Label = "Device Name",
	            JoinCapabilities = eJoinCapabilities.ToSIMPL,
	            JoinType = eJoinType.Serial
	        });

		public PureLinkBridgeJoinMap(uint joinStart) 
            :base(joinStart)
		{
		}
	}
}