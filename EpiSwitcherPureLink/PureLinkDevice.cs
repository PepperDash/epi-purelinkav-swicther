using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace PureLinkPlugin
{
	/// <summary>
	/// Plugin device
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed.
	/// </remarks>
	/// <example>
	/// "EssentialsPluginDeviceTemplate" renamed to "SamsungMdcDevice"
	/// </example>
	public class PureLinkDevice : EssentialsBridgeableDevice
    {
        #region Constants
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

        private const string    StartChar = "*";
        private const string    EndChar = "!";
        private const int       MaxIO = 72;
        private const string    ConnectBothAVChar = "C";
        private const string    ConnectAudioChar = "AC";
        private const string    ConnectVideoChar = "VC";
        private const string    DisconnectBothAVChar = "D";
        private const string    DisconnectAudioChar = "AD";
        private const string    DisconnectVideoChar = "VD";
        private PureLinkConfig _config; // It is often desirable to store the config
        #endregion Constants

		#region IBasicCommunication Properties and Constructor

		// TODO [X] Add, modify, remove properties and fields as needed for the plugin being developed
		private readonly IBasicCommunication _comms;
		private readonly GenericCommunicationMonitor _commsMonitor;
		
		// _comms gather for ASCII based API's
		// TODO [X] If not using an ASCII based API, delete the properties below
		private readonly CommunicationGather _commsGather;

		/// <summary>
		/// Set this value to that of the delimiter used by the API (if applicable)
		/// </summary>
		private const string CommsDelimiter = "\n";

		/// <summary>
		/// Connects/disconnects the comms of the plugin device
		/// </summary>
		/// <remarks>
		/// triggers the _comms.Connect/Disconnect as well as thee comms monitor start/stop
		/// </remarks>
		public bool Connect
		{
			get { return _comms.IsConnected; }
			set
			{
				if (value)
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

		/// <summary>
		/// Reports connect feedback through the bridge
		/// </summary>
		public BoolFeedback ConnectFeedback { get; private set; }

		/// <summary>
		/// Reports online feedback through the bridge
		/// </summary>
		public BoolFeedback OnlineFeedback { get; private set; }

        /// <summary>
        /// Reports AudioFollowVideo feedback through the bridge
        /// </summary>
        public BoolFeedback AudioFollowsVideoFeedback { get; private set; }
        
        /// <summary>
		/// Reports socket status feedback through the bridge
		/// </summary>
		public IntFeedback StatusFeedback { get; private set; }

        // Add feedback for routing and names // Video Routing, input names, and outputcurrentnames
        public Dictionary<uint, StringFeedback> InputNameFeedbacks { get; private set; }
        public Dictionary<uint, StringFeedback> InputVideoNameFeedbacks { get; private set; }
        public Dictionary<uint, StringFeedback> InputAudioNameFeedbacks { get; private set; }

        public Dictionary<uint, StringFeedback> OutputNameFeedbacks { get; private set; }
        public Dictionary<uint, StringFeedback> OutputVideoNameFeedbacks { get; private set; }
        public Dictionary<uint, StringFeedback> OutputAudioNameFeedbacks { get; private set; }

        public Dictionary<uint, StringFeedback> OutputCurrentVideoNameFeedbacks { get; private set; }
        public Dictionary<uint, StringFeedback> OutputCurrentAudioNameFeedbacks { get; private set; }

        public Dictionary<uint, IntFeedback> OutputCurrentVideoValueFeedbacks { get; private set; }
        public Dictionary<uint, IntFeedback> OutputCurrentAudioValueFeedbacks { get; private set; }

		/// <summary>
		/// Plugin device constructor
		/// </summary>
		/// <param name="key">device key</param>
		/// <param name="name">device name</param>
		/// <param name="config">device configuration object</param>
		/// <param name="comms">device communication as IBasicCommunication</param>
		/// <see cref="PepperDash.Core.IBasicCommunication"/>
		/// <seealso cref="Crestron.SimplSharp.CrestronSockets.SocketStatus"/>
		public PureLinkDevice(string key, string name, PureLinkConfig config, IBasicCommunication comms)
			: base(key, name)
		{
			Debug.Console(0, this, "Constructing new {0} instance", name);

			// TODO [ ] Update the constructor as needed for the plugin device being developed

			_config = config;
		    if (string.IsNullOrEmpty(_config.DeviceId))
		        _config.DeviceId = "999";

            // Consider enforcing default poll values IF NOT DEFINED in the JSON config
		    if (string.IsNullOrEmpty(_config.PollString))
                _config.PollString = "*999?version!";

		    if (_config.PollTimeMs == 0 )
		        _config.PollTimeMs = 45000;

            // TODO [ ] Do error and warning as well

			ConnectFeedback = new BoolFeedback(() => Connect);
			OnlineFeedback = new BoolFeedback(() => _commsMonitor.IsOnline);
            AudioFollowsVideoFeedback = new BoolFeedback(() => _config.AudioFollowsVideo);
			StatusFeedback = new IntFeedback(() => (int)_commsMonitor.Status);

            InputVideoNameFeedbacks = new Dictionary<uint, StringFeedback>();
            InputAudioNameFeedbacks = new Dictionary<uint, StringFeedback>();
            OutputVideoNameFeedbacks = new Dictionary<uint, StringFeedback>();
            OutputAudioNameFeedbacks = new Dictionary<uint, StringFeedback>();
            OutputCurrentVideoNameFeedbacks = new Dictionary<uint, StringFeedback>();
            OutputCurrentAudioNameFeedbacks = new Dictionary<uint, StringFeedback>();

			_comms = comms;
			_commsMonitor = new GenericCommunicationMonitor(this, _comms, _config.PollTimeMs, _config.WarningTimeoutMs, _config.ErrorTimeoutMs, Poll);

			var socket = _comms as ISocketStatus;
			if (socket != null)
			{
				// device comms is IP **ELSE** device comms is RS232
				socket.ConnectionChange += socket_ConnectionChange;
				Connect = true;
			}

			#region Communication data event handlers.  Comment out any that don't apply to the API type                      			

			// _comms gather for ASCII based API's that have a defined delimiter
			_commsGather = new CommunicationGather(_comms, CommsDelimiter);
			_commsGather.LineReceived += Handle_LineRecieved;						

			#endregion Communication data event handlers.  Comment out any that don't apply to the API type

		    InitializeInputNames(_config.Inputs);
            InitializeOutputNames(_config.Outputs);

			Debug.Console(0, this, "Constructing new {0} instance complete", name);
			Debug.Console(0, new string('*', 80));
			Debug.Console(0, new string('*', 80));
		}

	    private void InitializeOutputNames(Dictionary<uint, PureLinkEntryConfig> outputs)
	    {
	        if (outputs == null)
	        {
                Debug.Console(0, this, "what do you want to say");
	            return;
	        }
	        foreach (var output in outputs)
	        {
                // As the foreach runs, 'output' could potentially change
                // assign outputs to output then subsuquent changes don't matter 
                // on 'item'
	            var item = output;
                Debug.Console(2, this, "Output-{0} Name: {1}", item.Key, item.Value.Name);
	            Debug.Console(2, this, "Output-{0} Video Name: {1}", item.Key, item.Value.VideoName);
                Debug.Console(2, this, "Output-{0} Audio Name: {1}", item.Key, item.Value.AudioName);                

                // Could write in logic that if audio name or video name is null, populate the audio/video name from the 'Name' value

                OutputVideoNameFeedbacks.Add(item.Key, new StringFeedback(() => item.Value.VideoName));
                OutputAudioNameFeedbacks.Add(item.Key, new StringFeedback(() => item.Value.AudioName));
	        }

	    }

        private void InitializeInputNames(Dictionary<uint, PureLinkEntryConfig> inputs)
        {
            if (inputs == null)
            {
                Debug.Console(0, this, "what do you want to say");
                return;
            }
            foreach (var input in inputs)
            {
                // As the foreach runs, 'input' could potentially change
                // assign inputs to input then subsuquent changes don't matter 
                // on 'item'
                var item = input;
                Debug.Console(2, this, "input-{0} Name: {1}", item.Key, item.Value.Name);
                Debug.Console(2, this, "input-{0} Video Name: {1}", item.Key, item.Value.VideoName);
                Debug.Console(2, this, "input-{0} Audio Name: {1}", item.Key, item.Value.AudioName);

                // Could write in logic that if audio name or video name is null, populate the audio/video name from the 'Name' value

                InputVideoNameFeedbacks.Add(item.Key, new StringFeedback(() => item.Value.VideoName));
                InputAudioNameFeedbacks.Add(item.Key, new StringFeedback(() => item.Value.AudioName));
            }

        }

	    private void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
		{
			if (ConnectFeedback != null)
				ConnectFeedback.FireUpdate();

			if (StatusFeedback != null)
				StatusFeedback.FireUpdate();
		}

		// TODO [X] If using an API with a delimeter, keep the method below
		private void Handle_LineRecieved(object sender, GenericCommMethodReceiveTextArgs args)
		{
			// TODO [ ] Implement method, introduce parsing routines here
			throw new System.NotImplementedException();
		}

		// TODO [X] Delete below if not using ASCII based API
		/// <summary>
		/// Sends text to the device plugin comms
		/// </summary>
		/// <remarks>
		/// Can be used to test commands with the device plugin using the DEVPROPS and DEVJSON console commands
		/// </remarks>
		/// <param name="text">Command to be sent</param>		
		public void SendText(string text)
		{
			if (string.IsNullOrEmpty(text)) return;

			_comms.SendText(string.Format("{0}{1}", text, CommsDelimiter));
		}

		/// <summary>
		/// Polls the device
		/// </summary>
		/// <remarks>
		/// Poll method is used by the communication monitor.  Update the poll method as needed for the plugin being developed
		/// </remarks>
		public void Poll()
		{
			// TODO [ ] Update Poll method as needed for the plugin being developed
            SendText(_config.PollString);
		}

		#endregion IBasicCommunication Properties and Constructor.  Remove if not needed.

		#region Overrides of EssentialsBridgeableDevice

		/// <summary>
		/// Links the plugin device to the EISC bridge
		/// </summary>
		/// <param name="trilist"></param>
		/// <param name="joinStart"></param>
		/// <param name="joinMapKey"></param>
		/// <param name="bridge"></param>
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

			// TODO [ ] Implement bridge links as needed
            #region links to bridge
            // trilist.setX means its coming from SIMPL
            trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
			trilist.SetBoolSigAction(joinMap.Connect.JoinNumber, sig => Connect = sig);
            trilist.SetBoolSigAction(joinMap.AudioFollowsVideo.JoinNumber, SetAudioFollowsVideo);

            // X.LinkInputSig is the feedback going back to SIMPL
			ConnectFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Connect.JoinNumber]);
			StatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.Status.JoinNumber]);
			OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);                                           
            AudioFollowsVideoFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AudioFollowsVideo.JoinNumber]);

            // TODO [X] Need to update poll method
            // TODO [X] Reference your poll string in your poll method
            // TODO [ ] Need execute switch > determine input or output number > then sendText()
            // TODO [ ] Add parsing routines within the handlelinereceived

            // TODO [X] Create FOREACH loop(s) to update the bridge
            // Need to find the Crestron trilist join array value. Once array join is found your starting with a value of 1 already so account for this by minus 1
            foreach (var item in OutputVideoNameFeedbacks)
            {
                var join = item.Key + joinMap.InputVideoNames.JoinNumber - 1;
                // Example: Item.key = 4;
                // Example: joinmap.InputVideoNames.joinnumber = 101 - 1 = 100 + item.Key = 104
                // Local var feedback = item.value.AudioName
                var feedback = item.Value;
                if (feedback == null) continue;
                feedback.LinkInputSig(trilist.StringInput[join]);
            }

			UpdateFeedbacks();

			trilist.OnlineStatusChange += (o, a) =>
			{
				if (!a.DeviceOnLine) return;

				trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
				UpdateFeedbacks();
			};
            #endregion links to bridge
        }

        /// <summary>
        /// Set the value of the Audio Follows Video
        /// </summary>
        /// <param name="state">Method takes single bool parameter to force SetAudioFollowsVideo</param>
	    private void SetAudioFollowsVideo(bool state)
	    {
	        //Set the value of Audio Follows Video
	        _config.AudioFollowsVideo = state;
            //Fire feedback 
            AudioFollowsVideoFeedback.FireUpdate();
	    }

        /// <summary>
        /// Void Method that updates Feedbacks which updates Bridge
        /// </summary>
	    private void UpdateFeedbacks()
		{
			// TODO [Z] Update as needed for the plugin being developed
            
			ConnectFeedback.FireUpdate();
			OnlineFeedback.FireUpdate();
			StatusFeedback.FireUpdate();
            AudioFollowsVideoFeedback.FireUpdate();

            foreach (var item in OutputVideoNameFeedbacks)
                item.Value.FireUpdate();
            foreach (var item in OutputAudioNameFeedbacks)
                item.Value.FireUpdate();
            foreach (var item in OutputCurrentVideoValueFeedbacks)
                item.Value.FireUpdate();
            foreach (var item in OutputCurrentAudioValueFeedbacks)
                item.Value.FireUpdate();
            foreach (var item in OutputCurrentVideoNameFeedbacks)
                item.Value.FireUpdate();
            foreach (var item in OutputCurrentAudioNameFeedbacks)
                item.Value.FireUpdate();
		}

		#endregion Overrides of EssentialsBridgeableDevice

        #region ExecuteSwitch

        /// <summary>
        /// Executes switch
        /// </summary>
        /// <param name="inputSelector">Source number</param>
        /// <param name="outputSelector">Output number</param>
        /// <param name="signalType">AudioVideo, Video, or Audio</param>
        public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
        {
            var input = Convert.ToUInt32(inputSelector);
            var output = Convert.ToUInt32(outputSelector);

            Debug.Console(2, this, "ExecuteSwitch({0}, {1}, {2})", input, output, signalType.ToString());

            if (output < 0 || input < 0)
                return;
            if (output > MaxIO || input > MaxIO)
                return;

            uint inputIndex = 0;
            uint outputIndex = 0;
            var cmd = "";

            switch (signalType)
            {
                case eRoutingSignalType.AudioVideo:
                    {
                        // TODO [X] Add routing command
                        ExecuteSwitchAudioVideo(input, output);
                        break;
                    }
                case eRoutingSignalType.Video:
                    {
                        // TODO [X] Add routing command
                        ExecuteSwitchVideo(input, output);

                        if (_config.AudioFollowsVideo == true)
                            ExecuteSwitchAudio(input, output);
                        break;
                    }
                case eRoutingSignalType.Audio:
                    {
                        // TODO [X] Add routing command
                        ExecuteSwitchAudio(input, output);
                        break;
                    }
            }
        }

        private void ExecuteSwitchAudioVideo(uint input, uint output)
        {
            string cmd = string.Format("{0}{1}CI{02}O{3}{4}", StartChar, _config.DeviceId, input, output, CommsDelimiter);
            SendText(cmd);
        }

        private void ExecuteSwitchVideo(uint input, uint output)
        {
            string cmd = string.Format("{0}{1}VCI{02}O{3}{4}", StartChar, _config.DeviceId, input, output, CommsDelimiter);
            SendText(cmd);
        }

        private void ExecuteSwitchAudio(uint input, uint output)
        {
            //Example command *255ACI01O08! = Connect Audio Input 1 to Output 8
            string cmd = string.Format("{0}{1}ACI{02}O{3}{4}", StartChar, _config.DeviceId, input, output, CommsDelimiter);
            SendText(cmd);
        }
        #endregion

	}
}

