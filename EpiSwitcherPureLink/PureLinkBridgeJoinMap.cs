using PepperDash.Essentials.Core;

namespace PureLinkPlugin
{
    /// <summary>
    /// Plugin Bridge Join Maps were ruffly copied from PepperDash.Essentials.Core.Bridges.DmChassisControllerJoinMap 
    /// </summary>
    /// <see cref="PepperDash.Essentials.Core.Bridges.DmChassisControllerJoinMap"/>
    public class PureLinkBridgeJoinMap : JoinMapBaseAdvanced
    {
        #region Digital

        /// <summary>
        /// Plugin request for video route
        /// </summary>
        /// <remarks>
        /// Typically used to trigger video routes and prevent video route on initial SIMPL bridge connect.
        /// </remarks>
        [JoinName("VideoEnter")]
        public JoinDataComplete VideoEnter = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Requests video route",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Plugin request for audio route
        /// </summary>
        /// <remarks>
        /// Typically used to trigger audio routes and prevent audio route on initial SIMPL bridge connect.
        /// </remarks>
        [JoinName("AudioEnter")]
        public JoinDataComplete AudioEnter = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Requests audio route",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Triggers and provides indication of audio follows video
        /// </summary>
        [JoinName("EnableAudioBreakawayFeedback")]
        public JoinDataComplete EnableAudioBreakaway = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Enable Audio Breakaway",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Plugin disconnect
        /// </summary>
        /// <remarks>
        /// Typically used with socket based communications. Disconects the socket connection.
        /// </remarks>
        [JoinName("Disconnect")]
        public JoinDataComplete Disconnect = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 12,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Disconnects the socket connection",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Plugin connect
        /// </summary>
        /// <remarks>
        /// Typically used with socket based communications. Connects the socket connection.
        /// </remarks>
        [JoinName("Connect")]
        public JoinDataComplete Connect = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 11,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Connects the socket connection",
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
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
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
                JoinNumber = 7,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Poll Video",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
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
                JoinNumber = 8,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Poll Audio",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Clear video routes to all outputs
        /// </summary>
        /// <remarks>
        /// Used to manually clear video routes on all outputs
        /// </remarks>
        [JoinName("ClearVideoRoutes")]
        public JoinDataComplete ClearVideoRoutes = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 15,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Clear Video Routes",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Clear video routes to all outputs
        /// </summary>
        /// <remarks>
        /// Used to manually clear video routes on all outputs
        /// </remarks>
        [JoinName("ClearAudioRoutes")]
        public JoinDataComplete ClearAudioRoutes = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 16,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Clear Audio Routes",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /// <summary>
        /// Polls switcher for video input sync status
        /// </summary>
        [JoinName("VideoSyncStatus")]
        public JoinDataComplete VideoSyncStatus = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 101,
                JoinSpan = PureLinkDevice.MaxIo
            },
            new JoinMetadata
            {
                Description = "Input Video Sync",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });
        #endregion

        #region Analog

        // TODO [ ] Add analog joins below plugin being developed

        /// <summary>
        /// Plugin status join map
        /// </summary>
        /// <remarks>
        /// Typically used with socket based communications. Reports the socket state to SiMPL as an analog value.
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

        /// <summary>
        /// Plugin model join map 
        /// </summary>
        /// /// <remarks>
        /// Typically used to indicate API type for Execute Switch calls.
        /// </remarks>
        [JoinName("Model")]
        public JoinDataComplete Model = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 5,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Model",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Analog
            });

        /// <summary>
        /// Plugin video values for every output
        /// </summary>
        [JoinName("OutputVideo")]
        public JoinDataComplete OutputVideo = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 101,
                JoinSpan = PureLinkDevice.MaxIo
            },
            new JoinMetadata
            {
                Description = "DM Chassis Output Video Set / Get",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        /// <summary>
        /// Plugin audio values for every output
        /// </summary>
        [JoinName("OutputAudio")]
        public JoinDataComplete OutputAudio = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 301,
                JoinSpan = PureLinkDevice.MaxIo
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
        public JoinDataComplete InputNames = new JoinDataComplete(new JoinData { JoinNumber = 101, JoinSpan = PureLinkDevice.MaxIo },
            new JoinMetadata { Description = "Switcher Input Name", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

        /// <summary>
        /// Plugin output names assigned destinations. Name is generic and will not override the specific 'OutputVideoNames' or 'OutputAudioNames' if defined.
        /// </summary>
        [JoinName("OutputNames")]
        public JoinDataComplete OutputNames = new JoinDataComplete(new JoinData { JoinNumber = 301, JoinSpan = PureLinkDevice.MaxIo },
            new JoinMetadata { Description = "Switcher Output Name", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

        /// <summary>
        /// Plugin input video names assigned to sources.
        /// </summary>
        [JoinName("InputVideoNames")]
        public JoinDataComplete InputVideoNames =
            new JoinDataComplete(new JoinData { JoinNumber = 501, JoinSpan = PureLinkDevice.MaxIo },
            new JoinMetadata
            {
                Description = "Video Input Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        /// <summary>
        /// Plugin input audio names assigned to sources.
        /// </summary>
        [JoinName("InputAudioNames")]
        public JoinDataComplete InputAudioNames =
            new JoinDataComplete(new JoinData { JoinNumber = 701, JoinSpan = PureLinkDevice.MaxIo },
            new JoinMetadata
            {
                Description = "Audio Input Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        /// <summary>
        /// Plugin output video names assigned to outputs.
        /// </summary>
        [JoinName("OutputVideoNames")]
        public JoinDataComplete OutputVideoNames =
            new JoinDataComplete(new JoinData { JoinNumber = 901, JoinSpan = PureLinkDevice.MaxIo },
            new JoinMetadata
            {
                Description = "Video Output Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        /// <summary>
        /// Plugin output audio names assigned to outputs.
        /// </summary>
        [JoinName("OutputAudioNames")]
        public JoinDataComplete OutputAudioNames =
            new JoinDataComplete(new JoinData { JoinNumber = 1001, JoinSpan = PureLinkDevice.MaxIo },
            new JoinMetadata
            {
                Description = "Audio Output Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        /// <summary>
        /// Plugin video source name currently routed to output.
        /// </summary>
        [JoinName("OutputCurrentVideoInputNames")]
        public JoinDataComplete OutputCurrentVideoInputNames =
            new JoinDataComplete(new JoinData { JoinNumber = 2001, JoinSpan = PureLinkDevice.MaxIo },
            new JoinMetadata
            {
                Description = "DM Chassis Video Output Currently Routed Video Input Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial 
            });

        /// <summary>
        /// Plugin audio source name currently routed to output.
        /// </summary>
        [JoinName("OutputCurrentAudioInputNames")]
        public JoinDataComplete OutputCurrentAudioInputNames =
            new JoinDataComplete(new JoinData { JoinNumber = 2201, JoinSpan = PureLinkDevice.MaxIo },
            new JoinMetadata
            {
                Description = "DM Chassis Audio Output Currently Routed Video Input Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });


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