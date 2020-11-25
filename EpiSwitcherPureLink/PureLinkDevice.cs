using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Devices.Common.VideoCodec.Cisco;

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
    /// Notes: Delate is a signature for a method. 

	public class PureLinkDevice : EssentialsBridgeableDevice
    {
        private readonly PureLinkConfig _config; // It is often desirable to store the config

        #region Constants
        /// <summary>
        /// "*999?version!" - Check firmware version
        /// "*999I000!" - Check ruoters ID
        /// "“*255sI Router ID 255" - Response from router ID check
        /// </summary>
       
        private const string PollString = "*999?version";
	    private const string PollVideo = ("*999?vallio");
        private const string PollAudio = ("*999?aallio");
        private const string ClearVideoRoutes = ("*999vdallio");
        private const string ClearAudioRoutes = ("*999adallio");
        private const string StartChar = "*";
        /// <summary>
        /// Switcher Max Array value for all Input/Output
        /// </summary>
        public const int MaxIo = 72;

        private CrestronQueue<string> _commsQueue;
        private CCriticalSection _commsQueueLock;

        private CrestronQueue<string> _parserQueue;
        private CCriticalSection _parserLock;

        private readonly PureLinkCmdProcessor _cmdProcessor = new PureLinkCmdProcessor();
        
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
		private const string CommsDelimiter = "!\r";

        /// <summary>
        /// Reports the comms state of the plugin device
        /// </summary>
        /// <remarks>
        /// Reports if the comms device is currently connected
        /// </remarks>
        public bool ConnectFb
        {
            get
            {
                return _comms.IsConnected;                
            }
        }

        /// <summary>
        /// Connects the comms of the plugin device
        /// </summary>
        /// <remarks>
        /// triggers the _comms.Connect as well and starts the comms monitor
        /// </remarks>
        public void Connect()
        {
            if (_comms.IsConnected)
            {
                return;
            }
            _comms.Connect();
            _commsMonitor.Start();
        }

        /// <summary>
        /// Disconnects the comms of the plugin device
        /// </summary>
        /// <remarks>
        /// triggers the _comms.Disconnects stops the comms monitor
        /// </remarks>
        public void Disconnect()
        {
            if (!_comms.IsConnected)
            {
                return;
            }
            _comms.Disconnect();
            _commsMonitor.Stop();
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

        // TODO [X] Add feedback for routing, names, video routing, audio routing, and outputcurrentnames
        /// <summary>
        /// The first uint is the output, then input
        /// </summary>
        private readonly Dictionary<uint, uint> _outputCurrentVideoInput = new Dictionary<uint, uint>();
        /// <summary>
        /// The first uint is the output, then input
        /// </summary>
        private readonly Dictionary<uint, uint> _outputCurrentAudioInput = new Dictionary<uint, uint>();

        /// <summary>
        /// Plugin property for generic input names
        /// </summary>
        public Dictionary<uint, StringFeedback> InputNameFeedbacks { get; private set; }

        /// <summary>
        /// Plugin property for video input names
        /// </summary>
        public Dictionary<uint, StringFeedback> InputVideoNameFeedbacks { get; private set; }

        /// <summary>
        /// Plugin property for audio input names
        /// </summary>
        public Dictionary<uint, StringFeedback> InputAudioNameFeedbacks { get; private set; }

        /// <summary>
        /// Plugin property for generic output names
        /// </summary>
        public Dictionary<uint, StringFeedback> OutputNameFeedbacks { get; private set; }

        /// <summary>
        /// Plugin property for video output names
        /// </summary>
        public Dictionary<uint, StringFeedback> OutputVideoNameFeedbacks { get; private set; }

        /// <summary>
        /// Plugin property for audio output names
        /// </summary>
        public Dictionary<uint, StringFeedback> OutputAudioNameFeedbacks { get; private set; }

        /// <summary>
        /// Plugin property for current video source name routed per output
        /// </summary>
        public Dictionary<uint, StringFeedback> OutputCurrentVideoNameFeedbacks { get; private set; }

        /// <summary>
        /// Plugin property for current audio source name routed per output
        /// </summary>
        public Dictionary<uint, StringFeedback> OutputCurrentAudioNameFeedbacks { get; private set; }

        /// <summary>
        /// Plugin property for current video source value routed per output
        /// </summary>
        public Dictionary<uint, IntFeedback> OutputCurrentVideoValueFeedbacks { get; private set; }

        /// <summary>
        /// Plugin property for current audio source value routed per output
        /// </summary>
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

			// TODO [X] Update the constructor as needed for the plugin device being developed

			_config = config;
		    if (string.IsNullOrEmpty(_config.DeviceId))
		        _config.DeviceId = "999";

            if (_config.Model < 1)
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Error, "Config Model value invalid. Current value: {0}. Valid values are 0 or 1. Setting value to 0.", _config.Model.ToString());
                _config.Model = 0;
            }

            // Consider enforcing default poll values IF NOT DEFINED in the JSON config
		    if (string.IsNullOrEmpty(_config.PollString))
                _config.PollString = PollString;

		    if (_config.PollTimeMs == 0 )
		        _config.PollTimeMs = 45000;

            if (_config.WarningTimeoutMs == 0)
                _config.WarningTimeoutMs = 180000;

            if (_config.ErrorTimeoutMs == 0)
                _config.ErrorTimeoutMs = 300000;            

			ConnectFeedback = new BoolFeedback(() => ConnectFb);
			OnlineFeedback = new BoolFeedback(() => _commsMonitor.IsOnline);
            AudioFollowsVideoFeedback = new BoolFeedback(() => _config.AudioFollowsVideo);
		    StatusFeedback = new IntFeedback(GetSocketStatus);

            InputNameFeedbacks = new Dictionary<uint, StringFeedback>();
            InputVideoNameFeedbacks = new Dictionary<uint, StringFeedback>();
            InputAudioNameFeedbacks = new Dictionary<uint, StringFeedback>();
            OutputNameFeedbacks = new Dictionary<uint, StringFeedback>();
            OutputVideoNameFeedbacks = new Dictionary<uint, StringFeedback>();
            OutputAudioNameFeedbacks = new Dictionary<uint, StringFeedback>();
            OutputCurrentVideoNameFeedbacks = new Dictionary<uint, StringFeedback>();
            OutputCurrentAudioNameFeedbacks = new Dictionary<uint, StringFeedback>();
            OutputCurrentVideoValueFeedbacks = new Dictionary<uint, IntFeedback>();
            OutputCurrentAudioValueFeedbacks = new Dictionary<uint, IntFeedback>();

			_comms = comms;
            // The comms monitor polls your device
            // The _commsMonitor.Status only changes based on the values placed in the Poll times
            // _commsMonitor.StatusChange is the poll status changing not the TCP/IP isOnline status changing
			_commsMonitor = new GenericCommunicationMonitor(this, _comms, _config.PollTimeMs, _config.WarningTimeoutMs, _config.ErrorTimeoutMs, Poll);		                

            var socket = _comms as ISocketStatus;
            if (socket != null)
            {
                // device comms is IP **ELSE** device comms is RS232
                socket.ConnectionChange += socket_ConnectionChange;                
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

        private int GetSocketStatus()
        {
                var tempSocket = _comms as ISocketStatus;
                // Check if tempSocket is null as protection in case the JSON didn't implement it
			    if (tempSocket == null) return 0;
			    return (int)tempSocket.ClientStatus;
		}

	    private void InitializeOutputNames(Dictionary<uint, PureLinkEntryConfig> outputs)
	    {
	        if (outputs == null)
	        {
                Debug.Console(2, this, "Cannot inialize output names, outputs null");
	            return;
	        }
	        foreach (var output in outputs)
	        {
                // As the foreach runs, 'output' could potentially change
                // assign outputs to output then subsuquent changes don't matter on 'item'
	            var item = output;
                Debug.Console(0, this, "InitializeOutputNames: item name = {0}", item.Value.Name);

	            if (string.IsNullOrEmpty(item.Value.VideoName))
	            {
                    if(!string.IsNullOrEmpty(item.Value.Name))
                        item.Value.VideoName = item.Value.Name;
	            }
                if (string.IsNullOrEmpty(item.Value.AudioName))
                {
                    if (!string.IsNullOrEmpty(item.Value.Name))
                        item.Value.AudioName = item.Value.Name;
                }
                OutputNameFeedbacks.Add(item.Key, new StringFeedback(() => item.Value.Name));
                OutputVideoNameFeedbacks.Add(item.Key, new StringFeedback(() => item.Value.VideoName));
                OutputAudioNameFeedbacks.Add(item.Key, new StringFeedback(() => item.Value.AudioName));
                _outputCurrentVideoInput.Add(item.Key, 0);
                _outputCurrentAudioInput.Add(item.Key, 0);                

                OutputCurrentVideoValueFeedbacks.Add(item.Key, new IntFeedback(() =>
                {
                    uint sourceKey;                    
                    var success = _outputCurrentVideoInput.TryGetValue(item.Key, out sourceKey);
                    Debug.Console(2, this, "OutputCurrentVideoValueFeedbacks.Add output {0} success = {1}", item.Key, success);
                    if (!success)
                        return 0;                    
                    return Convert.ToInt32(sourceKey);
                }));

                OutputCurrentAudioValueFeedbacks.Add(item.Key, new IntFeedback(() =>
                {
                    uint sourceKey;
                    return Convert.ToInt32(_outputCurrentAudioInput.TryGetValue(item.Key, out sourceKey) ? sourceKey : 0);
                }));

                OutputCurrentVideoNameFeedbacks.Add(item.Key, new StringFeedback(() =>
                {
                    uint sourceKey;
                    PureLinkEntryConfig config;
                    var success = _outputCurrentVideoInput.TryGetValue(item.Key, out sourceKey);
                    if (!success)
                        return string.Empty;
                    success = _config.Inputs.TryGetValue(sourceKey, out config);
                    if (!success)
                        return string.Empty;
                    return string.IsNullOrEmpty(config.VideoName) ? config.Name : config.VideoName;
                }));

                OutputCurrentAudioNameFeedbacks.Add(item.Key, new StringFeedback(() =>
                {
                    uint sourceKey;
                    PureLinkEntryConfig config;
                    var success = _outputCurrentAudioInput.TryGetValue(item.Key, out sourceKey);
                    if (!success)
                        return string.Empty;
                    success = _config.Inputs.TryGetValue(sourceKey, out config);
                    if (!success)
                        return string.Empty;
                    return string.IsNullOrEmpty(config.AudioName) ? config.Name : config.AudioName;
                }));
	        }
	    }

        private void InitializeInputNames(Dictionary<uint, PureLinkEntryConfig> inputs)
        {
            if (inputs == null)
            {
                Debug.Console(2, this, "Cannot inialize input names, inputs null");
                return;
            }
            foreach (var input in inputs)
            {
                // As the foreach runs, 'input' could potentially change
                // assign inputs to input then subsuquent changes don't matter on 'item'
                var item = input;

                Debug.Console(0, this, "InitializeInputNames: item name = {0}", item.Value.Name);
                
                if (string.IsNullOrEmpty(item.Value.VideoName))
                {
                    if (!string.IsNullOrEmpty(item.Value.Name))
                        item.Value.VideoName = item.Value.Name;
                }
                if (string.IsNullOrEmpty(item.Value.AudioName))
                {
                    if (!string.IsNullOrEmpty(item.Value.Name))
                        item.Value.AudioName = item.Value.Name;
                }
                InputNameFeedbacks.Add(item.Key, new StringFeedback(() => item.Value.Name));
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

	        if (!ConnectFb) return;
	        SetPollVideo();
	        SetPollAudio();
		}

		// TODO [X] If using an API with a delimeter, keep the method below
		private void Handle_LineRecieved(object sender, GenericCommMethodReceiveTextArgs args)
		{
            if (args == null)
            {
                Debug.Console(2, this, "HandleLineReceived: args are null");
                return;
            }
            if (string.IsNullOrEmpty(args.Text))
            {
                Debug.Console(2, this, "HandleLineReceived: args.Text is null or empty");
                return;
            }

            try
            {
                var data = args.Text.Trim(); // Remove leading/trailing white-space characters
                if (string.IsNullOrEmpty(data))
                {
                    Debug.Console(2, this, "HandleLineReceived: data is null or empty");
                    return;
                }

                if (data.ToLower().Contains("Command Code Error!"))
                {
                    Debug.Console(2, this, Debug.ErrorLogLevel.Error, "HandleLineReceived: Command Code Error");
                    return;
                }

                if (data.ToLower().Contains("Router ID Error!"))
                {
                    Debug.Console(2, this, Debug.ErrorLogLevel.Error, "HandleLineReceived: Router ID Error");
                    return;
                }

                if (data.ToLower().Contains("sc"))
                {
                    Debug.Console(2, this, "Received Audio-Video Switch FB");
                    _cmdProcessor.EnqueueTask(() => ParseIoResponse(data, RouteType.AudioVideo));
                }
                else if (data.ToLower().Contains("sv") || data.ToLower().Contains("s?v"))
                {
                    Debug.Console(2, this, "Received Video Switch FB");
                    _cmdProcessor.EnqueueTask(() => ParseIoResponse(data, RouteType.Video));
                }
                else if (data.ToLower().Contains("sa") || data.ToLower().Contains("s?a"))
                {
                    Debug.Console(2, this, "Received Audio Switch FB");
                    _cmdProcessor.EnqueueTask(() => ParseIoResponse(data, RouteType.Audio));
                }
                else
                {
                    Debug.Console(2, this, "HandleLineReceived: No matches found");
                }
            }
            catch (Exception ex)
            {
                Debug.Console(2, this, Debug.ErrorLogLevel.Error, "HandleLineReceived Exception: {0}", ex.InnerException.Message);
            }
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
            Debug.Console(2, this, "SendText = {0}{1}", text, CommsDelimiter);
			_comms.SendText(string.Format("{0}{1}{2}", text, CommsDelimiter, "\n"));
		}

		/// <summary>
		/// Polls the device
		/// </summary>
		/// <remarks>
		/// Poll method is used by the communication monitor.  Update the poll method as needed for the plugin being developed
		/// </remarks>
		public void Poll()
		{
			// TODO [X] Update Poll method as needed for the plugin being developed
            SendText(_config.PollString);
		}

		#endregion IBasicCommunication Properties and Constructor
    
	    #region Overrides of EssentialsBridgeableDevice

		/// <summary>
		/// Links the plugin device to the EISC bridge Post Activation
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

			// TODO [X] Implement bridge links as needed
            #region links to bridge
            // trilist.setX means its coming from SIMPL            
            trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
            trilist.SetSigTrueAction(joinMap.Connect.JoinNumber, Connect);
            trilist.SetSigTrueAction(joinMap.Disconnect.JoinNumber, Disconnect);
            trilist.SetSigTrueAction(joinMap.PollVideo.JoinNumber, SetPollVideo);
            trilist.SetSigTrueAction(joinMap.PollAudio.JoinNumber, SetPollAudio);
            trilist.SetSigTrueAction(joinMap.ClearVideoRoutes.JoinNumber, SetClearVideoRoutes);
            trilist.SetSigTrueAction(joinMap.ClearAudioRoutes.JoinNumber, SetClearAudioRoutes);
            trilist.SetBoolSigAction(joinMap.AudioFollowsVideo.JoinNumber, SetAudioFollowsVideo);
            trilist.SetSigTrueAction(joinMap.GetIpInfo.JoinNumber, GetIpInfo);
            
            // X.LinkInputSig is feedback to SIMPL
            ConnectFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Connect.JoinNumber]);
            StatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.Status.JoinNumber]);
            OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            AudioFollowsVideoFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AudioFollowsVideo.JoinNumber]);
            
            // TODO [X] Need to update poll method
            // TODO [X] Reference your poll string in your poll method
            // TODO [X] Need execute switch > determine input or output number > then sendText()
            // TODO [X] Add parsing routines within the handlelinereceived
            //for (var x = 1; x <= joinMap.OutputVideo.JoinSpan; x++)
            //{
            //    var joinActual = x + joinMap.OutputVideo.JoinNumber - 1;
            //    int analogOutput = x;
            //    trilist.SetUShortSigAction((uint)joinActual,
            //        analogInput => ExecuteSwitch(analogInput, analogOutput, eRoutingSignalType.Video));
            //}

            //for (var x = 1; x <= joinMap.OutputAudio.JoinSpan; x++)
            //{
            //    var joinActual = x + joinMap.OutputAudio.JoinNumber - 1;
            //    int analogOutput = x;
            //    trilist.SetUShortSigAction((uint)joinActual,
            //        analogInput => ExecuteSwitch(analogInput, analogOutput, eRoutingSignalType.Audio));
            //}

		    // TODO [X] Create FOREACH loop(s) to update the bridge
            // Need to find the Crestron trilist join array value. Once array join is found your starting with a value of 1 already so account for this by minus 1
            foreach (var item in InputNameFeedbacks)
            {
                var join = item.Key + joinMap.InputNames.JoinNumber - 1;
                var feedback = item.Value;
                if (feedback == null) continue;
                feedback.LinkInputSig(trilist.StringInput[join]);
            }

            foreach (var item in OutputNameFeedbacks)
            {
                var join = item.Key + joinMap.OutputNames.JoinNumber - 1;
                var feedback = item.Value;
                if (feedback == null) continue;
                feedback.LinkInputSig(trilist.StringInput[join]);
            }

            foreach (var item in OutputVideoNameFeedbacks)
            {
                var join = item.Key + joinMap.OutputVideoNames.JoinNumber - 1;              
                var feedback = item.Value;
                if (feedback == null) continue;
                feedback.LinkInputSig(trilist.StringInput[join]);
            }

            foreach (var item in InputVideoNameFeedbacks)
            {
                var join = item.Key + joinMap.InputVideoNames.JoinNumber - 1;
                var feedback = item.Value;
                if (feedback == null) continue;
                feedback.LinkInputSig(trilist.StringInput[join]);
            }

            foreach (var item in OutputAudioNameFeedbacks)
            {                
                var join = item.Key + joinMap.OutputAudioNames.JoinNumber - 1;                
                var feedback = item.Value;
                if (feedback == null) continue;
                feedback.LinkInputSig(trilist.StringInput[join]);
            }

            foreach (var item in InputAudioNameFeedbacks)
            {
                var join = item.Key + joinMap.InputAudioNames.JoinNumber - 1;
                var feedback = item.Value;
                if (feedback == null) continue;
                feedback.LinkInputSig(trilist.StringInput[join]);
            }

            foreach (var item in OutputCurrentVideoNameFeedbacks)
            {
                var join = item.Key + joinMap.OutputCurrentVideoInputNames.JoinNumber - 1;
                var feedback = item.Value;
                if (feedback == null) continue;
                feedback.LinkInputSig(trilist.StringInput[join]);
            }

            foreach (var item in OutputCurrentAudioNameFeedbacks)
            {
                var join = item.Key + joinMap.OutputCurrentAudioInputNames.JoinNumber - 1;
                var feedback = item.Value;
                if (feedback == null) continue;
                feedback.LinkInputSig(trilist.StringInput[join]);
            }

            foreach (var item in OutputCurrentVideoValueFeedbacks)
            {
                // get the actual join number of the signal
                var join = item.Key + joinMap.OutputVideo.JoinNumber - 1;
                // this is the actual output number which is the item.Key as read in from the configuraiton file
                var output = item.Key;
                // this is linking incoming from SIMPL EISC bridge (aka route request) to the routing method defined
                trilist.SetUShortSigAction(join, input => ExecuteSwitch(input, output, eRoutingSignalType.Video));
                // this is linking route feedbacks to SIMPL EISC bridge
                var feedback = item.Value;
                if (feedback == null) continue;
                feedback.LinkInputSig(trilist.UShortInput[join]);
            }

            foreach (var item in OutputCurrentAudioValueFeedbacks)
            {
                var join = item.Key + joinMap.OutputAudio.JoinNumber - 1;
                var output = item.Key;
                trilist.SetUShortSigAction(join, input => ExecuteSwitch(input, output, eRoutingSignalType.Audio));
                var feedback = item.Value;
                if (feedback == null) continue;
                feedback.LinkInputSig(trilist.UShortInput[join]);
            }

			UpdateFeedbacks();
		    Connect();

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
        /// Triggers the SendText method to send the PollVideo command
        /// </summary>
        public void SetPollVideo()
	    {
	        SendText(PollVideo);
	    }

        /// <summary>
        /// Triggers the SendText method to send the PollAudio command
        /// </summary>
        public void SetPollAudio()
        {
            SendText(PollAudio);
        }

        /// <summary>
        /// Triggers the SendText method to send ClearVideoRoutes command
        /// </summary>
        public void SetClearVideoRoutes()
        {

            SendText(ClearVideoRoutes);
        }

        /// <summary>
        /// Triggers the SendText method to send ClearAudioRoutes command
        /// </summary>
        public void SetClearAudioRoutes()
        {

            SendText(ClearAudioRoutes);
        }

	    /// <summary>
        /// Void Method that updates Feedbacks which updates Bridge
        /// </summary>
	    private void UpdateFeedbacks()
		{			            
			ConnectFeedback.FireUpdate();
			OnlineFeedback.FireUpdate();
			StatusFeedback.FireUpdate();
            AudioFollowsVideoFeedback.FireUpdate();

            foreach (var item in InputNameFeedbacks)
                item.Value.FireUpdate();
            foreach (var item in InputVideoNameFeedbacks)
                item.Value.FireUpdate();
            foreach (var item in InputAudioNameFeedbacks)
                item.Value.FireUpdate();
            foreach (var item in OutputNameFeedbacks)
                item.Value.FireUpdate();
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

        #region ParseData
        /// <summary>
        /// Plugin public eumermeration with only three values including: Audio, Video, or AudioVideo
        /// </summary>
        public enum RouteType{Audio, Video, AudioVideo}

	    /// <summary>
        /// Plugin parse method calling for Regex pattern on incoming Handle_LineReceived data
        /// </summary>
        /// <param name="response"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public void ParseIoResponse(string response, RouteType type)
        {
	        try
	        {                
                //Get string response and return an array of IO
                //The '@' character below means its a string literal not requiring escape characters
                var regex = new Regex(@"I(\d{3})O(\d{3})");
                var matches = regex.Matches(response);

	            foreach (Match match in matches)
	            {	                	           
                    if (match.Groups == null) continue;

                    var output = Convert.ToUInt16(match.Groups[2].Value);
                    var input = Convert.ToUInt16(match.Groups[1].Value);

                    if (type == RouteType.Video || type == RouteType.AudioVideo)
                        UpdateVideoRoute(output, input);
                    if (type == RouteType.Audio || type == RouteType.AudioVideo)
                        UpdateAudioRoute(output, input);                    
                } 
            }
	        catch (Exception ex)
	        {
                Debug.Console(2, this, Debug.ErrorLogLevel.Error, "ParseIoResponse Exception:{0} StackTrace:{1}\r", ex.Message, ex.StackTrace);
	        }
        }

        private void UpdateVideoRoute(uint output, uint value)
        {     
            try
            {
                Debug.Console(2, this, "UpdateVideoRoute Input:{0} Output:{1}", value, output);                
                _outputCurrentVideoInput[output] = value;

                StringFeedback nameFeedback;
                var success = OutputCurrentVideoNameFeedbacks.TryGetValue(output, out nameFeedback);
                Debug.Console(0, this, "Output {0} has feedback {1}", output, success);
                Debug.Console(0, this, "OutputCurrentVideoNameFeedbacks.Count {0}", OutputCurrentVideoNameFeedbacks.Count);
                if (OutputCurrentVideoNameFeedbacks.TryGetValue(output, out nameFeedback))
                {
                    Debug.Console(2, this, "UpdateVideoRoute TryGetValue nameFeedback");
                    nameFeedback.FireUpdate();
                }

                IntFeedback numberFeedback;
                success = OutputCurrentVideoValueFeedbacks.TryGetValue(output, out numberFeedback);
                Debug.Console(0, this, "Output {0} has feedback {1}", output, success);
                Debug.Console(0, this, "OutputCurrentVideoValueFeedbacks.Count {0}", OutputCurrentVideoValueFeedbacks.Count);
                if (OutputCurrentVideoValueFeedbacks.TryGetValue(output, out numberFeedback))
                {
                    Debug.Console(2, this, "UpdateVideoRoute TryGetValue numberFeedback");
                    numberFeedback.FireUpdate();
                }
            }
            catch (Exception ex)
            {
                Debug.Console(2, this, Debug.ErrorLogLevel.Error, "UpdateVideoRoute Exception:{0} StackTrace:{1}\r", ex.Message, ex.StackTrace);
            }
        }

        private void UpdateAudioRoute(uint output, uint value)
        {
            try
            {
                Debug.Console(2, this, "UpdateAudioRoute Input:{0} Output:{1}", value, output);                
                _outputCurrentAudioInput[output] = value;

                StringFeedback nameFeedback;
                if (OutputCurrentAudioNameFeedbacks.TryGetValue(output, out nameFeedback))
                {
                    nameFeedback.FireUpdate();
                }
                IntFeedback numberFeedback;
                if (OutputCurrentAudioValueFeedbacks.TryGetValue(output, out numberFeedback))
                {
                    numberFeedback.FireUpdate();
                }
            }
            catch (Exception ex)
            {
                Debug.Console(2, this, Debug.ErrorLogLevel.Error, "UpdateAudioRoute Exception:{0} StackTrace:{1}\r", ex.Message, ex.StackTrace);
            }

        }
        #endregion

        #region EnqueueSentText DequeueSentText ExecuteSwitch

        /// <summary>
        /// Enqueues the SendText mehtod with a command
        /// </summary>
        /// <param name="cmd">string command to be enqueued in the SendText method, not including the delimiter</param>
        public void EnqueueSendText(string cmd)
        {
            if (cmd == null)
                return;

            _commsQueue.TryToEnqueue(cmd);

            var lockState = _commsQueueLock.TryEnter();
            if (lockState)
                CrestronInvoke.BeginInvoke((o) => DequeueSendText());
        }

        /// <summary>
        /// Plugin dequeue and call SentText() method
        /// </summary>
        private void DequeueSendText()
        {
            try
            {
                while (true)
                {
                    //var cmd = _commsQueue.TryToDequeue();
                    var cmd = _commsQueue.Dequeue();
                    if (!string.IsNullOrEmpty(cmd))
                    {
                        SendText(cmd);
                        //Thread.Sleep(200);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Console(2, this, "DequeueSendText Exception: {0}", ex);
            }
            finally
            {
                if (_commsQueueLock != null)
                    _commsQueueLock.Leave();
            }
        }

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

            Debug.Console(2, this, "ExecuteSwitch({0}, {1}, {2}, {3})", _config.Model.ToString(), input, output, signalType.ToString());

            if (output > MaxIo || input > MaxIo)
            {
                Debug.Console(0, this, "ExecuteSwitch IO invalid. Values greater than MaxIo. Output: {0}, Input: {1}, MaxIo: {3}", output, input, MaxIo);
                return;
            }

            var cmd = "";

            switch (signalType)
            {
                case eRoutingSignalType.AudioVideo:
                    {
                        switch (_config.Model)
                        {
                            case 0:
                                cmd = string.Format("{0}{1}CI{2:D2}O{3:D2}", StartChar, _config.DeviceId, input, output);
                                break;
                            case 1:
                                cmd = string.Format("{0}{1}CI{2:D3}O{3:D3}", StartChar, _config.DeviceId, input, output);
                                break;
                        }
                        //EnqueueSendText(cmd);
                        SendText(cmd);
                        break;
                    }
                case eRoutingSignalType.Video:
                    {
                        switch (_config.Model)
                        {
                            case 0:
                                cmd = string.Format("{0}{1}VCI{2:D2}O{3:D2}", StartChar, _config.DeviceId, input, output);
                                break;
                            case 1:
                                cmd = string.Format("{0}{1}VCI{2:D3}O{3:D3}", StartChar, _config.DeviceId, input, output);
                                break;
                        }
                        //EnqueueSendText(cmd);
                        SendText(cmd);

                        if (_config.AudioFollowsVideo == true)
                        {
                            switch (_config.Model)
                            {
                                case 0:
                                    cmd = string.Format("{0}{1}ACI{2:D2}O{3:D2}", StartChar, _config.DeviceId, input, output);
                                    break;
                                case 1:
                                    cmd = string.Format("{0}{1}ACI{2:D3}O{3:D3}", StartChar, _config.DeviceId, input, output);
                                    break;
                            }
                            //EnqueueSendText(cmd);
                            SendText(cmd);
                        }
                        break;
                    }
                case eRoutingSignalType.Audio:
                    {                        
                        switch (_config.Model)
                        {
                            case 0:
                                cmd = string.Format("{0}{1}ACI{2:D2}O{3:D2}", StartChar, _config.DeviceId, input, output);
                                break;
                            case 1:
                                cmd = string.Format("{0}{1}ACI{2:D3}O{3:D3}", StartChar, _config.DeviceId, input, output);
                                break;
                        }
                        //EnqueueSendText(cmd);
                        SendText(cmd);
                        break;
                    }
            }
        }
	    #endregion

        #region Misc Methods
        /// <summary>
        /// Plugin method to recall IP information
        /// </summary>
	    public void GetIpInfo()
	    {
            Debug.Console(0, this, "properties.control.tcpSshProperties.method: {0}", _config.Control.Method.ToString());
            Debug.Console(0, this, "properties.control.tcpSshProperties.address: {0}", _config.Control.TcpSshProperties.Address);
            Debug.Console(0, this, "properties.control.tcpSshProperties.port: {0}", _config.Control.TcpSshProperties.Port);
            Debug.Console(0, this, "_comms is connected: {0}", _comms.IsConnected.ToString());
            Debug.Console(0, this, "_comms is online: {0}", _commsMonitor.IsOnline.ToString());
            Debug.Console(0, this, "_comms status: {0}", _commsMonitor.Status.ToString());
        }
        #endregion
    }
}

