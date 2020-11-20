using PepperDash.Essentials.Core;

namespace PureLinkPlugin
{
    /// <summary>
    /// Plugin Bridge Join Maps were copied from PepperDash.Essentials.Core.Bridges.DmChassisControllerJoinMap 
    /// <//summary>
    /// <see cref="PepperDash.Essentials.Core.Bridges.DmChassisControllerJoinMap"/>
    public class PureLinkBridgeJoinMap : JoinMapBaseAdvanced
    {
        #region Digital

        // TODO [ ] Add digital joins below plugin being developed

        /// <summary>
        /// Plugin online join map
        /// </summary>
        /// <remarks>
        /// Reports the plugin online sate to SiMPL as a boolean value
        /// </remarks>
        [JoinName("IsOnline")]
        public JoinDataComplete IsOnline = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Is Online",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Plugin connect join map
        /// </summary>
        /// <remarks>
        /// Typically used with socket based communications.  Connects (held) and disconnects (released) socket based communcations when triggered from SiMPL.
        /// Additionally, the connection state feedback will report to SiMP Las a boolean value.
        /// </remarks>
        [JoinName("Connect")]
        public JoinDataComplete Connect = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Connect (Held)/Disconnect (Release) & Connect state feedback",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Poll plugin device
        /// </summary>
        /// <remarks>
        /// Typically used to manually poll device. Note, Essentials includes polling mechanism aside from this.
        /// </remarks>
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

        /// <summary>
        /// Poll outputs for audio
        /// </summary>
        /// <remarks>
        /// Used to manually poll outputs for audio sources
        /// </remarks>        
        [JoinName("PollAudio")]
        public JoinDataComplete PollAudio = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 7,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Poll Audio",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
        /// <summary>
        /// Poll outputs for video
        /// </summary>
        /// <remarks>
        /// Used to manually poll outputs for video sources
        /// </remarks>
        [JoinName("PollVideo")]
        public JoinDataComplete PollVideo = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 8,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Poll Video",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("VideoSyncStatus")]
        public JoinDataComplete VideoSyncStatus = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 101,
                JoinSpan = 72
            },
            new JoinMetadata
            {
                Description = "Input Video Sync",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        // TODO [ ] Check join with Essentails DM
        [JoinName("AudioFollowsVideo")]
        public JoinDataComplete AudioFollowsVideo = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Audio Follows Video",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        #endregion


        #region Analog

        // TODO [ ] Add analog joins below plugin being developed

        /// <summary>
        /// Plugin status join map
        /// </summary>
        /// <remarks>
        /// Typically used with socket based communications.  Reports the socket state to SiMPL as an analog value.
        /// </remarks>
        /// <see cref="Crestron.SimplSharp.CrestronSockets.SocketStatus"/>
        [JoinName("Status")]
        public JoinDataComplete Status = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Socket Status",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("OutputVideo")]
        public JoinDataComplete OutputVideo = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 101,
                JoinSpan = 32
            },
            new JoinMetadata
            {
                Description = "DM Chassis Output Video Set / Get",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("OutputAudio")]
        public JoinDataComplete OutputAudio = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 301,
                JoinSpan = 32
            },
            new JoinMetadata
            {
                Description = "DM Chassis Output Audio Set / Get",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });


        #endregion


        #region Serial

        // TODO [X] Add serial joins below plugin being developed

        /// <summary>
        /// Plugin device name
        /// </summary>
        /// <remarks>
        /// Reports the plugin name, as read from the configuration file, to SiMPL as a string value.
        /// </remarks>
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
        /// <summary>
        /// Plugin input names assigned to sources. Name is generic and will not override the specific 'InputVideoNames' or 'InputAudioNames' if defined.
        /// </summary>
        [JoinName("InputNames")]
        public JoinDataComplete InputNames = new JoinDataComplete(new JoinData { JoinNumber = 101, JoinSpan = 32 },
            new JoinMetadata { Description = "DM Chassis Input Name", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

        /// <summary>
        /// Plugin output names assigned destinations. Name is generic and will not override the specific 'OutputVideoNames' or 'OutputAudioNames' if defined.
        /// </summary>
        [JoinName("OutputNames")]
        public JoinDataComplete OutputNames = new JoinDataComplete(new JoinData { JoinNumber = 301, JoinSpan = 32 },
            new JoinMetadata { Description = "DM Chassis Output Name", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
        /// <summary>
        /// 
        /// </summary>
        [JoinName("InputVideoNames")]
        public JoinDataComplete InputVideoNames =
            new JoinDataComplete(new JoinData { JoinNumber = 501, JoinSpan = 200 },
            new JoinMetadata
            {
                Description = "Video Input Name",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("InputAudioNames")]
        public JoinDataComplete InputAudioNames =
            new JoinDataComplete(new JoinData { JoinNumber = 701, JoinSpan = 200 },
            new JoinMetadata
            {
                Description = "Audio Input Name",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Serial
            });
        [JoinName("OutputVideoNames")]
        public JoinDataComplete OutputVideoNames =
            new JoinDataComplete(new JoinData { JoinNumber = 901, JoinSpan = 200 },
            new JoinMetadata
            {
                Description = "Video Output Name",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Serial
            });
        [JoinName("OutputAudioNames")]
        public JoinDataComplete OutputAudioNames =
            new JoinDataComplete(new JoinData { JoinNumber = 1001, JoinSpan = 200 },
            new JoinMetadata
            {
                Description = "Audio Output Name",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("OutputCurrentVideoInputNames")]
        public JoinDataComplete OutputCurrentVideoInputNames = new JoinDataComplete(new JoinData { JoinNumber = 2001, JoinSpan = 32 },
            new JoinMetadata { Description = "DM Chassis Video Output Currently Routed Video Input Name", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

        [JoinName("OutputCurrentAudioInputNames")]
        public JoinDataComplete OutputCurrentAudioInputNames = new JoinDataComplete(new JoinData { JoinNumber = 2201, JoinSpan = 32 },
            new JoinMetadata { Description = "DM Chassis Audio Output Currently Routed Video Input Name", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });


        #endregion

        /// <summary>
        /// Plugin device BridgeJoinMap constructor
        /// </summary>
        /// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
        public PureLinkBridgeJoinMap(uint joinStart)
            : base(joinStart, typeof(PureLinkBridgeJoinMap))
        {
        }
    }
}