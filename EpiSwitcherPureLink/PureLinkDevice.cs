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
        private PureLinkConfig _config; // It is often desirable to store the config

        #region Constants
        /// <summary>
        /// "*999?version!" - Check firmware version
        /// "*999I000!" - Check ruoters ID
        /// "“*255sI Router ID 255" - Response from router ID check
        /// "*255V?ALLIO!" - Check video status of all inputs and outputs
        /// "*255A?ALLIO!" - Check audio status of all inputs and outputs
        /// "*255DALLIO!" - Disconnect video and audio, 
        /// "*255VDALLIO!" - Disconnect video, all inputs and outputs
        /// "*255ADALLIO!" - Disconnect audio, all inputs and outputs
        /// "*255VCI01O01!" - Connect Video Input 1 to Output 1
        /// "*255ACI01O01!" - Connect Audio Input 1 to Output 1
        /// "*255CI01O01!" - Connect both Video and Audio Input 1 to Output 1
        /// "Command Code Error" - The command was not executed due to error
        /// "Router ID Error" - Actual Router ID and entered Router ID did not match
        /// </summary>
       
        private const string PollString = "*999?version!";
        private const string StartChar = "*";
        private const int MaxIO = 72;

        private CrestronQueue<string> _commsQueue;
        private CCriticalSection _commsQueueLock;

        private CrestronQueue<string> _parserQueue;
        private CCriticalSection _parserLock;

        private readonly PureLinkCmdProcessor cmdProcessor = new PureLinkCmdProcessor();
        
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
		private const string CommsDelimiter = "!\r\n";

        /// <summary>
        /// Connects/Disconnects the comms of the plugin device
        /// </summary>
        /// <remarks>
        /// triggers the _comms.Connect/Disconnect as well as thee comms monitor start/stop
        /// </remarks>
        public bool ConnectFb
        {
            get { return _comms.IsConnected; }
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

			// TODO [ ] Update the constructor as needed for the plugin device being developed

			_config = config;
		    if (string.IsNullOrEmpty(_config.DeviceId))
		        _config.DeviceId = "999";

            if (_config.Model < 1)
            {
                Debug.Console(2, this, "Config Model value invalid. Current value: {0}. Valid values are 0 or 1. Setting value to 0.", _config.Model.ToString());
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

            InputVideoNameFeedbacks = new Dictionary<uint, StringFeedback>();
            InputAudioNameFeedbacks = new Dictionary<uint, StringFeedback>();
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
            // _commsMonitor.StatusChange += (sender, args) => StatusFeedback.FireUpdate();

            var socket = _comms as ISocketStatus;
            if (socket != null)
            {
                // device comms is IP **ELSE** device comms is RS232
                socket.ConnectionChange += socket_ConnectionChange;
                //Connect = true;
                Connect();
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
                Debug.Console(0, this, "Cannot inialize output names, outputs null");
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
                // Text.Trim() Removes all leading and trailing white-space characters from the current string
                var data = args.Text.Trim();
                if (string.IsNullOrEmpty(data))
                {
                    Debug.Console(2, this, "HandleLineReceived: data is null or empty");
                    return;
                }

                Debug.Console(2, this, "HandleLineReceived data:{0}", data);


                if (data.ToLower().Contains("Command Code Error!"))
                {
                    Debug.Console(2, this, "Received Command Code Error");
                    return;
                }

                if (data.ToLower().Contains("sC"))
                {
                    Debug.Console(2, this, "Received Audio-Video Switch FB");
                    cmdProcessor.EnqueueTask(() => ParseIOResponse(data, RouteType.AudioVideo));
                }
                else if (data.ToLower().Contains("sV"))
                {
                    Debug.Console(2, this, "Received Video Switch FB");
                    cmdProcessor.EnqueueTask(() => ParseIOResponse(data, RouteType.Video));
                }
                else if (data.ToLower().Contains("sA"))
                {
                    Debug.Console(2, this, "Received Audio Switch FB");
                    cmdProcessor.EnqueueTask(() => ParseIOResponse(data, RouteType.Audio));
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
			//trilist.SetBoolSigAction(joinMap.Connect.JoinNumber, sig => Connect = sig);            
            trilist.SetSigTrueAction(joinMap.Connect.JoinNumber, Connect);
            trilist.SetSigTrueAction(joinMap.Disconnect.JoinNumber, Disconnect);
            trilist.SetBoolSigAction(joinMap.AudioFollowsVideo.JoinNumber, SetAudioFollowsVideo);
            trilist.SetSigTrueAction(joinMap.GetIpInfo.JoinNumber, GetIpInfo);
            
            // X.LinkInputSig is the feedback going back to SIMPL
            ConnectFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Connect.JoinNumber]);
            StatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.Status.JoinNumber]);
            OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            AudioFollowsVideoFeedback.LinkInputSig(trilist.BooleanInput[joinMap.AudioFollowsVideo.JoinNumber]);
            
            // TODO [X] Need to update poll method
            // TODO [X] Reference your poll string in your poll method
            // TODO [X] Need execute switch > determine input or output number > then sendText()
            // TODO [X] Add parsing routines within the handlelinereceived
		    for (var x = 1; x <= joinMap.OutputVideo.JoinSpan; x++)
		    {
		        var joinActual = x + joinMap.OutputVideo.JoinNumber - 1;
		        int analogOutput = x;
		        trilist.SetUShortSigAction((uint) joinActual,
		            analogInput => ExecuteSwitch(analogInput, analogOutput, eRoutingSignalType.Video));
		    }

            for (var x = 1; x <= joinMap.OutputAudio.JoinSpan; x++)
            {
                var joinActual = x + joinMap.OutputAudio.JoinNumber - 1;
                int analogOutput = x;
                trilist.SetUShortSigAction((uint)joinActual,
                    analogInput => ExecuteSwitch(analogInput, analogOutput, eRoutingSignalType.Audio));
            }

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

            foreach (var item in OutputAudioNameFeedbacks)
            {
                // get the actual join number of the signal
                var join = item.Key + joinMap.OutputAudio.JoinNumber - 1;
                // this is the actual output number which is the item.Key as read in from the configuraiton file
                var output = item.Key;
                // this is linking incoming from SIMPL EISC bridge (aka route request) to the routing method defined
                trilist.SetUShortSigAction(join, input => ExecuteSwitch(input, output, eRoutingSignalType.Audio));
                // this is linking route feedbacks to SIMPL EISC bridge
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
                // get the actual join number of the signal
                var join = item.Key + joinMap.OutputAudio.JoinNumber - 1;
                // this is the actual output number which is the item.Key as read in from the configuraiton file
                var output = item.Key;
                // this is linking incoming from SIMPL EISC bridge (aka route request) to the routing method defined
                trilist.SetUShortSigAction(join, input => ExecuteSwitch(input, output, eRoutingSignalType.Audio));
                // this is linking route feedbacks to SIMPL EISC bridge
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
        /// Void Method that updates Feedbacks which updates Bridge
        /// </summary>
	    private void UpdateFeedbacks()
		{
			// TODO [X] Update as needed for the plugin being developed
            
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

        #region ParseData
        public enum RouteType{Audio, Video, AudioVideo}

	    /// <summary>
        /// Plugin parse method calling for Regex pattern on incoming Handle_LineReceived data
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public void ParseIOResponse(string response, RouteType type)        
        {
	        try
	        {
                //Get string response and return an array of IO
                //The '@' character below means its a string literal
                var regex = new Regex(@"(I(\d{3})O(\d{3}))");
                var matches = regex.Match(response);

                Debug.Console(0, this, "Group 0 = {0}", matches.Groups[0]);

                if (matches.Groups == null) return;

                Debug.Console(0, this, "matches.Groups.Count: {0}", matches.Groups.Count);
                foreach (Match match in matches.Groups)
                {
                    if (match.Groups.Count != 3)
                    {
                        return;
                    }

                    var output = UInt16.Parse(match.Groups[2].Value);
                    var input = UInt16.Parse(match.Groups[1].Value);

                    if (type == RouteType.Video || type == RouteType.AudioVideo)
                        UpdateVideoRoute(output, input);
                    if (type == RouteType.Audio || type == RouteType.AudioVideo)
                        UpdateAudioRoute(output, input);
                }

	        }
	        catch (Exception ex)
	        {

                Debug.ConsoleWithLog(0, this, "ParseIOResponse Exception:{0}\r", ex.Message);
	        }
        }

        private void UpdateVideoRoute(int output, int value)
        {     
            try
            {
                Debug.Console(2, this, "UpdateVideoRoute Input:{0} Output:{1}\r", value, output);
                OutputCurrentVideoValueFeedbacks.Add((uint)output, new IntFeedback(() => value));

                StringFeedback nameFeedback;
                if (OutputCurrentVideoNameFeedbacks.TryGetValue((uint)output, out nameFeedback))
                {
                    nameFeedback.FireUpdate();
                }
            }
            catch (Exception ex)
            {
                Debug.ConsoleWithLog(0, this, "UpdateVideoRoute Exception:{0}\r", ex.Message);
            }
        }

        private void UpdateAudioRoute(int output, int value)
        {
            try
            {
                Debug.Console(2, this, "UpdateVideoRoute Input:{0} Output:{1}\r", value, output);
                OutputCurrentAudioValueFeedbacks.Add((uint)output, new IntFeedback(() => value));

                StringFeedback nameFeedback;
                if (OutputCurrentAudioNameFeedbacks.TryGetValue((uint)output, out nameFeedback))
                {
                    nameFeedback.FireUpdate();
                }
            }
            catch (Exception ex)
            {
                Debug.ConsoleWithLog(0, this, "UpdateAudioRoute Exception:{0}\r", ex.Message);
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

            if (output < 0 || input < 0)
                return;
            if (output > MaxIO || input > MaxIO)
                return;

            var cmd = "";

            switch (signalType)
            {
                case eRoutingSignalType.AudioVideo:
                    {
                        // Example command *255CI01O08! = Connect Audio/Video Input 1 to Output 8
                        if(_config.Model == 0)
                            cmd = string.Format("{0}{1}CI{2}O{3}", StartChar, _config.DeviceId, input, output);
                        else if(_config.Model == 1)
                            cmd = string.Format("{0}{1}CI{2:D3}O{3:D3}", StartChar, _config.DeviceId, input, output);
                        //EnqueueSendText(cmd);
                        SendText(cmd);
                        break;
                    }
                case eRoutingSignalType.Video:
                    {
                        // Example command *255VCI01O08! = Connect Audio Input 1 to Output 8
                        if(_config.Model == 0)
                            cmd = string.Format("{0}{1}VCI{2}O{3}", StartChar, _config.DeviceId, input, output);
                        else if (_config.Model == 1)
                            cmd = string.Format("{0}{1}VCI{2:D3}O{3:D3}", StartChar, _config.DeviceId, input, output);
                        //EnqueueSendText(cmd);
                        SendText(cmd);

                        if (_config.AudioFollowsVideo == true)
                        {
                            if(_config.Model == 0)
                                cmd = string.Format("{0}{1}ACI{2}O{3}", StartChar, _config.DeviceId, input, output);
                            else if (_config.Model == 1)
                                cmd = string.Format("{0}{1}ACI{2:D3}O{3:D3}", StartChar, _config.DeviceId, input, output);
                            //EnqueueSendText(cmd);
                            SendText(cmd);
                        }
                        break;
                    }
                case eRoutingSignalType.Audio:
                    {
                        // Example command *255ACI01O08! = Connect Audio Input 1 to Output 8
                        if(_config.Model == 0)
                            cmd = string.Format("{0}{1}ACI{2}O{3}", StartChar, _config.DeviceId, input, output);
                        else if (_config.Model == 1)
                            cmd = string.Format("{0}{1}ACI{2:D3}O{3:D3}", StartChar, _config.DeviceId, input, output);
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

