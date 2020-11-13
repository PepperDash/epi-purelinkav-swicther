using PepperDash.Essentials.Core;
using DmChassisControllerJoinMap = PepperDash.Essentials.Core.Bridges.DmChassisControllerJoinMap;

namespace EpiSwitcherPureLink
{
	public class PureLinkBridgeJoinMap : JoinMapBaseAdvanced
	{
        public DmChassisControllerJoinMap PureLinkBaseJoinMap { get; set; }

        // DmChassisControllerJoinMap is used as base, all join data below is an 
        // extension of what doesn't exist in the DmChassisControllerJoinMap
        [JoinName("Name")]
        public JoinDataComplete DeviceName = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Device Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("Connect")]
        public JoinDataComplete Connect = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Connect",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("Status")]
        public JoinDataComplete Status = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Status",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        //[JoinName("ErrorMessage")]
        //public JoinDataComplete ErrorMessage = new JoinDataComplete(
        //    new JoinData
        //    {
        //        JoinNumber = 2,
        //        JoinSpan = 1
        //    },
        //    new JoinMetadata
        //    {
        //        Description = "Error Message",
        //        JoinCapabilities = eJoinCapabilities.ToSIMPL,
        //        JoinType = eJoinType.Serial
        //    });

        [JoinName("Poll")]
        public JoinDataComplete Poll = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 6,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Poll",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PollLabels")]
        public JoinDataComplete PollLabels = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 7,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Poll Labels",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PollCrosspoints")]
        public JoinDataComplete PollCrosspoints = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 8,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Poll Crosspoints",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        public PureLinkBridgeJoinMap(uint joinStart)
            : base(joinStart, typeof(PureLinkBridgeJoinMap))
		{
			// the code below merges the EvertzQuartBaseJoinMap (referencing the DmChassisControllerJoinMap) to the join maps
			PureLinkBaseJoinMap = new DmChassisControllerJoinMap(joinStart);		// as of 1.5.6
			//EvertzQuartzBaseJoinMap = new DmChassisControllerJoinMap(joinStart, typeof(DmChassisControllerJoinMap));	// as of 1.5.8

            foreach (var join in PureLinkBaseJoinMap.Joins)
			{
				if (Joins.ContainsKey(join.Key))
					Joins[join.Key] = join.Value;
				else 
					Joins.Add(join.Key, join.Value);
			}
		}
	}
}