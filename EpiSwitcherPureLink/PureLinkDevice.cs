// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Core;

namespace EssentialsPluginTemplateEPI 
{
	public class EssentialsPluginDeviceTemplate : EssentialsBridgeableDevice
	{
        /// <summary>
        /// "*999?version!" - Check firmware version
        /// "*999I000!" - Check ruoters ID
        /// "“*255sI Router ID 255" - Response from router ID check
        /// "*255?ALLIO!" - Check video status of all inputs and outputs
        /// "*255A?ALLIO!" - Check audio status of all inputs and outputs
        /// "*255DALLIO!" - Disconnect video and audio, 
        /// "*255VDALLIO!" - Disconnect video, all inputs and outputs
        /// "*255ADALLIO!" - Disconnect audio, all inputs and outputs
        /// "255VCI01O01!" - Connect Video Input 1 to Output 1
        /// "*255ACI01O01!" - Connect Audio Input 1 to Output 1
        /// "*255CI01O01!" - Connect both Video and Audio Input 1 to Output 1
        /// "Command Code Error" - The command was not executed due to error
        /// "Router ID Error" - Actual Router ID and entered Router ID did not match
        /// </summary>
        private const string Delimter = "\n";
        private const string StartChar = "*";
        private const string EndChar = "!";
        private const string ConnectBothAVChar = "C";
        private const string ConnectAudioChar = "AC";
        private const string ConnectVideoChar = "VC";
        private const string DisconnectBothAVChar = "D";
        private const string DisconnectAudioChar = "AD";
        private const string DisconnectVideoChar = "VD";

        private IBasicCommunication _comms;
        private GenericCommunicationMonitor _commsMonitor;
        private CommunicationGather _commsGather;

	    private EssentialsPluginConfigObjectTemplate _config;

		public EssentialsPluginDeviceTemplate(string key, string name, EssentialsPluginConfigObjectTemplate config)
			: base(key, name)
		{
            Debug.Console(0, this, "Constructing new EssentialsPluginDeviceTemplate instance");
		    _config = config;
		}

	    #region Overrides of EssentialsBridgeableDevice

	    public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
	    {
	        var joinMap = new EssentialsPluginBridgeJoinMapTemplate(joinStart);

	        // This adds the join map to the collection on the bridge
	        if (bridge != null)
	        {
	            bridge.AddJoinMap(Key, joinMap);
	        }

	        var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

	        if (customJoins != null)
	        {
	            joinMap.SetCustomJoinData(customJoins);
	        }

	        Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
	        Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);


	        trilist.OnlineStatusChange += (o, a) =>
	        {
	            if (a.DeviceOnLine)
	            {
	                trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
	            }
	        };
	    }


	    #endregion

        #region Polls

        /// <summary>
        /// Device poll
        /// </summary>
        public void Poll()
        {
            // Poll to see if router connected
            // delimiter will be added in the SendText method called by the queue
            EnqueueSendText(".#01");
        }

        /// <summary>
        /// Names poll
        /// </summary>
        public void PollNames()
        {
            // Source indexes: ".RS{source_index}"
            // Destination indexes: ".RD{destination_index}"
            // delimiter will be added in the SendText method called by the queue
            if (!string.IsNullOrEmpty(VideoLevel))
            {
                foreach (var item in _sourceQvIndexes)
                {
                    Debug.Console(2, this, "PollNames Source {0}: {1}-{2}", VideoLevel, item.Key, item.Value);
                    var cmd = string.Format(".RS{0}", item.Value);
                    EnqueueSendText(cmd);
                }

                foreach (var item in _destQvIndexes)
                {
                    Debug.Console(2, this, "PollNames Destination {0}: {1}-{2}", VideoLevel, item.Key, item.Value);
                    var cmd = string.Format(".RD{0}", item.Value);
                    EnqueueSendText(cmd);
                }
            }

            if (!string.IsNullOrEmpty(AudioLevel))
            {
                foreach (var item in _sourceQaIndexes)
                {
                    Debug.Console(2, this, "PollNames Source {0}: {1}-{2}", AudioLevel, item.Key, item.Value);
                    var cmd = string.Format(".RS{0}", item.Value);
                    EnqueueSendText(cmd);
                }

                foreach (var item in _destQaIndexes)
                {
                    Debug.Console(2, this, "PollNames Destination {0}: {1}-{2}", AudioLevel, item.Key, item.Value);
                    var cmd = string.Format(".RD{0}", item.Value);
                    EnqueueSendText(cmd);
                }
            }

            if (!string.IsNullOrEmpty(UsbLevel))
            {
                foreach (var item in _sourceQuIndexes)
                {
                    Debug.Console(2, this, "PollNames Source {0}: {1}-{2}", UsbLevel, item.Key, item.Value);
                    var cmd = string.Format(".RS{0}", item.Value);
                    EnqueueSendText(cmd);
                }

                foreach (var item in _destQuIndexes)
                {
                    Debug.Console(2, this, "PollNames Destination {0}: {1}-{2}", UsbLevel, item.Key, item.Value);
                    var cmd = string.Format(".RD{0}", item.Value);
                    EnqueueSendText(cmd);
                }
            }
        }

        /// <summary>
        /// Crosspont poll
        /// </summary>
        public void PollCrosspoints()
        {
            // Tx: ".L{level}{index}"
            // delimiter will be added in the SendText method called by the queue
            if (!string.IsNullOrEmpty(VideoLevel))
            {
                foreach (var item in _destQvIndexes)
                {
                    Debug.Console(2, this, "PollCrosspoint Destination {0}: {1}-{2}", VideoLevel, item.Key, item.Value);
                    var cmd = string.Format(".L{0}{1}", VideoLevel, item.Value);
                    EnqueueSendText(cmd);
                }
            }

            if (!string.IsNullOrEmpty(AudioLevel))
            {
                foreach (var item in _destQaIndexes.Where(item => AudioLevel != null))
                {
                    Debug.Console(2, this, "PollCrosspoint Destination {0}: {1}-{2}", AudioLevel, item.Key, item.Value);
                    var cmd = string.Format(".L{0}{1}", AudioLevel, item.Value);
                    EnqueueSendText(cmd);
                }
            }

            if (!string.IsNullOrEmpty(UsbLevel))
            {
                foreach (var item in _destQuIndexes.Where(item => UsbLevel != null))
                {
                    Debug.Console(2, this, "PollCrosspoint Destination {0}: {1}-{2}", UsbLevel, item.Key, item.Value);
                    var cmd = string.Format(".L{0}{1}", UsbLevel, item.Value);
                    EnqueueSendText(cmd);
                }
            }
        }

        #endregion


        #region SetBreakawayStates

        /// <summary>
        /// Sets the audio breakaway state
        /// </summary>
        /// <param name="state"></param>
        public void SetAudioBreakawayState(bool state)
        {
            AudioBreakawayEnabled = state;
        }

        /// <summary>
        /// Toggles the audio breakaway state
        /// </summary>
        public void ToggleAudioBreakawayState()
        {
            AudioBreakawayEnabled = !AudioBreakawayEnabled;
        }

        /// <summary>
        /// Sets the usb breakaway state
        /// </summary>
        /// <param name="state"></param>
        public void SetUsbBreakawayState(bool state)
        {
            UsbBreakawayEnabled = state;
        }

        /// <summary>
        /// Toggles the usb breakaway state
        /// </summary>
        public void ToggleUsbBreakawayState()
        {
            UsbBreakawayEnabled = !UsbBreakawayEnabled;
        }

        #endregion

        #region ExecuteSwitch

        /// <summary>
        /// Executes switch
        /// </summary>
        /// <param name="inputSelector"></param>
        /// <param name="outputSelector"></param>
        /// <param name="signalType"></param>
        public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
        {
            var input = Convert.ToUInt32(inputSelector);
            var output = Convert.ToUInt32(outputSelector);

            Debug.Console(2, this, "ExecuteSwitch({0}, {1}, {2})", input, output, signalType.ToString());

            if (output <= 0)
                return;

            uint inputIndex = 0;
            uint outputIndex = 0;
            var cmd = "";

            // Valid levels
            // 8 level system: V,A,B,C,D,E,F,G
            // 16 level system: V,A,B,C,D,E,F,G,H,I,J,K,L,M,N,O
            // DeviceTx: ".S{level}{output},{input}{delimiter}"
            switch (signalType)
            {
                case eRoutingSignalType.AudioVideo:
                    {
                        // TODO [X] Add routing command
                        ExecuteSwitchVideo(input, output);
                        ExecuteSwitchAudio(input, output);
                        break;
                    }
                case eRoutingSignalType.Video:
                    {
                        // TODO [X] Add routing command
                        ExecuteSwitchVideo(input, output);

                        if (AudioBreakawayEnabled)
                            ExecuteSwitchAudio(input, output);

                        if (UsbBreakawayEnabled && UsbLevel != null)
                            ExecuteSwitchUsb(input, output);

                        break;
                    }
                case eRoutingSignalType.Audio:
                    {
                        // TODO [X] Add routing command
                        ExecuteSwitchAudio(input, output);
                        break;
                    }
                case eRoutingSignalType.UsbInput:
                    {
                        // TODO [X] Add routing command
                        ExecuteSwitchUsb(input, output);
                        break;
                    }
                case eRoutingSignalType.UsbOutput:
                    {
                        // TODO [X] Add routing command
                        ExecuteSwitchUsb(input, output);
                        break;
                    }
            }
        }

        private void ExecuteSwitchVideo(uint input, uint output)
        {
            uint outputIndex;
            if (!_destQvIndexes.TryGetValue(output, out outputIndex))
            {
                Debug.Console(2, this, "ExecuteSwitchVideo: video output-{0} does not have an index ({1}) defined, unable to execute switch", output, outputIndex);
                return;
            }

            uint inputIndex;
            if (!_sourceQvIndexes.TryGetValue(input, out inputIndex))
            {
                Debug.Console(2, this, "ExecuteSwitchVideo: video input-{0} does not have an index ({1}) defined, executing blank switch", input, inputIndex);
            }

            if (VideoLevel == null)
            {
                Debug.Console(2, this, "ExecuteSwitchVideo: VideoLevel is null, unable to execute switch");
                return;
            }

            string cmd = string.Format(".S{0}{1},{2}", VideoLevel, outputIndex, inputIndex);
            EnqueueSendText(cmd);
        }

        private void ExecuteSwitchAudio(uint input, uint output)
        {
            uint outputIndex;
            if (!_destQaIndexes.TryGetValue(output, out outputIndex))
            {
                Debug.Console(2, this, "ExecuteSwitchAudio: audio output-{0} does not have an index ({1}) defined, unable to execute switch", output, outputIndex);
                return;
            }

            uint inputIndex;
            if (!_sourceQaIndexes.TryGetValue(input, out inputIndex))
            {
                Debug.Console(2, this, "ExecuteSwitchAudio: audio input-{0} does not have an index ({1}) defined", input, inputIndex);
            }

            if (AudioLevel == null)
            {
                Debug.Console(2, this, "ExecuteSwitchAudio: AudioLevel is null, unable to execute switch");
                return;
            }

            string cmd = string.Format(".S{0}{1},{2}", AudioLevel, outputIndex, inputIndex);
            EnqueueSendText(cmd);
        }

        private void ExecuteSwitchUsb(uint input, uint output)
        {
            uint outputIndex;
            if (!_destQuIndexes.TryGetValue(output, out outputIndex))
            {
                Debug.Console(2, this, "ExecuteSwitchUsb: usb output-{0} does not have an index ({1}) defined, unable to execute switch", output, outputIndex);
                return;
            }

            uint inputIndex;
            if (!_sourceQuIndexes.TryGetValue(input, out inputIndex))
            {
                Debug.Console(2, this, "ExecuteSwitchUsb: usb input-{0} does not have an index ({1}) defined", input, inputIndex);
            }

            if (UsbLevel == null)
            {
                Debug.Console(2, this, "ExecuteSwitchUsb: UsbLevel is null, unable to execute switch");
                return;
            }

            string cmd = string.Format(".S{0}{1},{2}", UsbLevel, outputIndex, inputIndex);
            EnqueueSendText(cmd);
        }

        #endregion

	}
}

