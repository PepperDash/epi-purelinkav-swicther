// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Core;

namespace EpiSwitcherPureLink 
{
	public class PureLinkDevice : EssentialsBridgeableDevice
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

        private long PollTime = 30000; // 30s
        private long WarningTimeout = 60000; // 60s
        private long ErrorTimeout = 180000; // 180s

        public bool Connect
        {
            get { return _comms.IsConnected; }
            set
            {
                if (value == true)
                {
                    _comms.Connect();
                    _commsMonitor.Start();
                }
                else
                {
                    _comms.Disconnect();
                    _commsMonitor.Stop();
                }
            }
        }

        private Dictionary<uint, string> _sourceVideoNames;
        private Dictionary<uint, string> _sourceAudioNames;

        private Dictionary<uint, string> _destVideoNames;
        private Dictionary<uint, string> _destAudioNames;

        private bool _audioBreakawayEnabled;

        /// <summary>
        /// Audio breakaway enabled property
        /// </summary>
        public bool AudioBreakawayEnabled
        {
            get { return _audioBreakawayEnabled; }
            set
            {
                if (value == _audioBreakawayEnabled) return;
                _audioBreakawayEnabled = value;
                AudioBreakawayEnabledFeedback.FireUpdate();
            }
        }
        /// <summary>
        /// Audio breakaway enabled feedback
        /// </summary>
        public BoolFeedback AudioBreakawayEnabledFeedback { get; private set; }

        public BoolFeedback ConnectFeedback { get; private set; }
        public BoolFeedback OnlineFeedback { get; private set; }
        public IntFeedback StatusFeedback { get; private set; }

        public Dictionary<uint, StringFeedback> SourceVideoNameFeedbacks { get; private set; }
        public Dictionary<uint, StringFeedback> SourceAudioNameFeedbacks { get; private set; }

        public Dictionary<uint, StringFeedback> DestVideoNameFeedbacks { get; private set; }
        public Dictionary<uint, StringFeedback> DestAudioNameFeedbacks { get; private set; }

        public Dictionary<uint, IntFeedback> DestCurrentVideoInputValueFeedbacks { get; private set; }
        public Dictionary<uint, IntFeedback> DestCurrentAudioInputValueFeedbacks { get; private set; }

        public Dictionary<uint, StringFeedback> DestCurrentVideoInputNameFeedbacks { get; private set; }
        public Dictionary<uint, StringFeedback> DestCurrentAudioInputNameFeedbacks { get; private set; }

	    private PureLinkConfig _config;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <param name="config"></param>
        public PureLinkDevice(string key, string name, PureLinkConfig config, IBasicCommunication comms)
			: base(key, name)
		{
            Debug.Console(0, this, "Constructing new Pure Link plugin instance");

            _sourceVideoNames = new Dictionary<uint, string>();
            _sourceAudioNames = new Dictionary<uint, string>();

            _destVideoNames = new Dictionary<uint, string>();
            _destAudioNames = new Dictionary<uint, string>();

            _destCurrentVideoInputValue = new Dictionary<uint, uint>();
            _destCurrentAudioInputValue = new Dictionary<uint, uint>();

            ConnectFeedback = new BoolFeedback(() => Connect);
            OnlineFeedback = new BoolFeedback(() => _commsMonitor.IsOnline);
            StatusFeedback = new IntFeedback(() => (int)_commsMonitor.Status);

            AudioBreakawayEnabledFeedback = new BoolFeedback(() => AudioBreakawayEnabled);

            SourceVideoNameFeedbacks = new Dictionary<uint, StringFeedback>();
            SourceAudioNameFeedbacks = new Dictionary<uint, StringFeedback>();

            DestVideoNameFeedbacks = new Dictionary<uint, StringFeedback>();
            DestAudioNameFeedbacks = new Dictionary<uint, StringFeedback>();

            DestCurrentVideoInputValueFeedbacks = new Dictionary<uint, IntFeedback>();
            DestCurrentAudioInputValueFeedbacks = new Dictionary<uint, IntFeedback>();

            DestCurrentVideoInputNameFeedbacks = new Dictionary<uint, StringFeedback>();
            DestCurrentAudioInputNameFeedbacks = new Dictionary<uint, StringFeedback>();

            if (config.PollTime > 0 && config.PollTime != PollTime)
                PollTime = config.PollTime;

            if (config.WarningTimeout > 0 && config.WarningTimeout != WarningTimeout)
                WarningTimeout = config.WarningTimeout;

            if (config.ErrorTimeout > 0 && config.ErrorTimeout != ErrorTimeout)
                ErrorTimeout = config.ErrorTimeout;

            _comms = comms;
            _commsGather = new CommunicationGather(_comms, Delimter);
            if (_commsGather == null)
            {
                Debug.Console(2, this, "_commsGather is null");
            }
            _commsMonitor = new GenericCommunicationMonitor(this, _comms, PollTime, WarningTimeout, ErrorTimeout, Poll);

            var socket = _comms as ISocketStatus;
            if (socket != null)
            {
                socket.ConnectionChange += (sender, args) =>
                {
                    Debug.Console(2, this, args.Client.ClientStatus.ToString());
                    if (ConnectFeedback != null) ConnectFeedback.FireUpdate();
                    if (StatusFeedback != null) StatusFeedback.FireUpdate();
                };
            }

            AddPostActivationAction(() => _commsGather.LineReceived += HandleLineReceived);
            AddPostActivationAction(() => Connect = true);

            AddPostActivationAction(() => InitializeInputs(config.Inputs));
            AddPostActivationAction(() => InitializeOutputs(config.Outputs));
		}

	    #region Overrides of EssentialsBridgeableDevice

	    public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
	    {
            			var joinMap = new PureLinkBridgeJoinMap(joinStart);

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

			// link joins to bridge
			trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

			ConnectFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Connect.JoinNumber]);
			StatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.Status.JoinNumber]);
			OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PureLinkBaseJoinMap.IsOnline.JoinNumber]);

			//ErrorMessageFeedback.LinkInputSig(trilist.StringInput[joinMap.ErrorMessage.JoinNumber]);

			trilist.SetSigTrueAction(joinMap.Poll.JoinNumber, Poll);
			trilist.SetSigTrueAction(joinMap.PollCrosspoints.JoinNumber, PollCrosspoints);
			trilist.SetSigTrueAction(joinMap.PollLabels.JoinNumber, PollNames);

			trilist.SetBoolSigAction(joinMap.PureLinkBaseJoinMap.EnableAudioBreakaway.JoinNumber, SetAudioBreakawayState);
			AudioBreakawayEnabledFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.PureLinkBaseJoinMap.EnableAudioBreakaway.JoinNumber]);

			foreach (var item in SourceVideoNameFeedbacks)
			{
				var join = item.Key + joinMap.PureLinkBaseJoinMap.InputVideoNames.JoinNumber - 1;
				var feedback = item.Value;
				if (feedback == null) continue;
				feedback.LinkInputSig(trilist.StringInput[join]);
			}

			foreach (var item in SourceAudioNameFeedbacks)
			{
				var join = item.Key + joinMap.PureLinkBaseJoinMap.InputAudioNames.JoinNumber - 1;
				;
				var feedback = item.Value;
				if (feedback == null) continue;
				feedback.LinkInputSig(trilist.StringInput[join]);
			}

			foreach (var item in DestVideoNameFeedbacks)
			{
				var join = item.Key + joinMap.PureLinkBaseJoinMap.OutputVideoNames.JoinNumber - 1;
				var feedback = item.Value;
				if (feedback == null) continue;
				feedback.LinkInputSig(trilist.StringInput[join]);
			}

			foreach (var item in DestAudioNameFeedbacks)
			{
				var join = item.Key + joinMap.PureLinkBaseJoinMap.OutputAudioNames.JoinNumber - 1;
				var feedback = item.Value;
				if (feedback == null) continue;
				feedback.LinkInputSig(trilist.StringInput[join]);
			}

			foreach (var item in DestCurrentVideoInputValueFeedbacks)
			{
				var join = item.Key + joinMap.PureLinkBaseJoinMap.OutputVideo.JoinNumber - 1;

				var output = item.Key;
				trilist.SetUShortSigAction(join, input => ExecuteSwitch(input, output, eRoutingSignalType.Video));

				var feedback = item.Value;
				if (feedback == null) continue;
				feedback.LinkInputSig(trilist.UShortInput[join]);
			}

			foreach (var item in DestCurrentAudioInputValueFeedbacks)
			{
				var join = item.Key + joinMap.PureLinkBaseJoinMap.OutputAudio.JoinNumber - 1;

				var output = item.Key;
				trilist.SetUShortSigAction(join, input => ExecuteSwitch(input, output, eRoutingSignalType.Audio));

				var feedback = item.Value;
				if (feedback == null) continue;
				feedback.LinkInputSig(trilist.UShortInput[join]);
			}

			foreach (var item in DestCurrentVideoInputNameFeedbacks)
			{
				var join = item.Key + joinMap.PureLinkBaseJoinMap.OutputCurrentVideoInputNames.JoinNumber - 1;
				var feedback = item.Value;
				if (feedback == null) continue;
				feedback.LinkInputSig(trilist.StringInput[join]);
			}

			foreach (var item in DestCurrentAudioInputNameFeedbacks)
			{
				var join = item.Key + joinMap.PureLinkBaseJoinMap.OutputCurrentAudioInputNames.JoinNumber - 1;
				var feedback = item.Value;
				feedback.LinkInputSig(trilist.StringInput[join]);
			}

			UpdateFeedbacks();

			trilist.OnlineStatusChange += (o, a) =>
			{
				if (a.DeviceOnLine)
				{
					trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

					UpdateFeedbacks();
				}
			};
	    }
	    #endregion

        #region UpdateRouteFeedbacks

        private void UpdateRouteFeedback(uint input, uint output, string level)
        {
            Debug.Console(2, this, "UpdateRouteFeedback({0}, {1}, {2})", input, output, level);

            if (level == VideoLevel)
                UpdateRouteVideoFeedback(input, output);

            else if (level == AudioLevel)
                UpdateRouteAudioFeedback(input, output);

            else if (level == UsbLevel)
                UpdateRouteUsbFeedback(input, output);

            else
                Debug.Console(2, this, "UpdateRouteFeedback: undefined route type {0}", level);
        }

        private void UpdateRouteVideoFeedback(uint input, uint output)
        {
            Debug.Console(2, this, "UpdateRouteVideoFeedback({0}, {1})", input, output);

            uint inputKey = 0;
            uint outputKey = 0;
            var inputName = "Unknown";

            IntFeedback valueFeedback;
            StringFeedback nameFeedback;

            // update route value feedback
            if (DestCurrentVideoInputValueFeedbacks.TryGetValue(outputKey, out valueFeedback))
                if (valueFeedback != null) valueFeedback.FireUpdate();

            // get the input name
            _sourceVideoNames.TryGetValue(inputKey, out inputName);

            // update route name feedback
            if (!DestCurrentVideoInputNameFeedbacks.TryGetValue(outputKey, out nameFeedback))
            {
                nameFeedback = new StringFeedback(() => inputName);
                DestCurrentVideoInputNameFeedbacks.Add(outputKey, nameFeedback);
            }
            else
            {
                DestCurrentVideoInputNameFeedbacks[outputKey] = nameFeedback;
            }
            nameFeedback.FireUpdate();
        }

        private void UpdateRouteAudioFeedback(uint input, uint output)
        {
            Debug.Console(2, this, "UpdateRouteAudioFeedback({0}, {1})", input, output);

            uint inputKey = 0;
            uint outputKey = 0;
            var inputName = "Unknown";

            IntFeedback valueFeedback;
            StringFeedback nameFeedback;

            // update route value feedback
            if (DestCurrentAudioInputValueFeedbacks.TryGetValue(outputKey, out valueFeedback))
                if (valueFeedback != null) valueFeedback.FireUpdate();

            // get the input name
            _sourceAudioNames.TryGetValue(inputKey, out inputName);

            // update route name feedback
            if (!DestCurrentAudioInputNameFeedbacks.TryGetValue(outputKey, out nameFeedback))
            {
                nameFeedback = new StringFeedback(() => inputName);
                DestCurrentAudioInputNameFeedbacks.Add(outputKey, nameFeedback);
            }
            else
            {
                DestCurrentAudioInputNameFeedbacks[outputKey] = nameFeedback;
            }
            nameFeedback.FireUpdate();
        }
        #endregion

        #region UpdateSourceNameFeedback & UpdateDestinationNameFeedback

        private void UpdateSourceNameFeedback(uint index, string name)
        {
            Debug.Console(2, this, "UpdateSourceNameFeedback({0}, {1})", index, name);

            // TODO [WIP] Complete method
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var feedback = new StringFeedback(() => name);

            var key = _sourceQvIndexes.FirstOrDefault(k => k.Value == index).Key;
            if (key != 0)
            {
                _sourceVideoNames[key] = name;
                if (SourceVideoNameFeedbacks.ContainsKey(key))
                    SourceVideoNameFeedbacks[key] = feedback;
                else
                    SourceVideoNameFeedbacks.Add(key, feedback);

                feedback.FireUpdate();
                return;
            }

            key = _sourceQaIndexes.FirstOrDefault(k => k.Value == index).Key;
            if (key != 0)
            {
                _sourceAudioNames[key] = name;
                if (SourceAudioNameFeedbacks.ContainsKey(key))
                    SourceAudioNameFeedbacks[key] = feedback;
                else
                    SourceAudioNameFeedbacks.Add(key, feedback);

                feedback.FireUpdate();
                return;
            }

            key = _sourceQuIndexes.FirstOrDefault(k => k.Value == index).Key;
            _sourceUsbNames[key] = name;
            if (SourceUsbNameFeedbacks.ContainsKey(key))
                SourceUsbNameFeedbacks[key] = feedback;
            else
                SourceUsbNameFeedbacks.Add(key, feedback);
        }

        private void UpdateDestinationNameFeedback(uint index, string name)
        {
            Debug.Console(2, this, "UpdateDestinationNameFeedback({0}, {1})", index, name);

            // TODO [WIP] Complete method
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var feedback = new StringFeedback(() => name);

            var key = _destQvIndexes.FirstOrDefault(k => k.Value == index).Key;
            if (key != 0)
            {
                _destVideoNames[key] = name;
                if (DestVideoNameFeedbacks.ContainsKey(key))
                    DestVideoNameFeedbacks[key] = feedback;
                else
                    DestVideoNameFeedbacks.Add(key, feedback);

                feedback.FireUpdate();
                return;
            }

            key = _destQaIndexes.FirstOrDefault(k => k.Value == index).Key;
            if (key != 0)
            {
                if (DestAudioNameFeedbacks.ContainsKey(key))
                    DestAudioNameFeedbacks[key] = feedback;
                else
                    DestAudioNameFeedbacks.Add(key, feedback);

                feedback.FireUpdate();
                return;
            }

            key = _destQuIndexes.FirstOrDefault(k => k.Value == index).Key;
            _destUsbNames[key] = name;
            if (DestUsbNameFeedbacks.ContainsKey(key))
                DestUsbNameFeedbacks[key] = feedback;
            else
                DestUsbNameFeedbacks.Add(key, feedback);

            feedback.FireUpdate();
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

