using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.DM;
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

    public class PureLinkDevice : EssentialsBridgeableDevice, IOnline, ICommunicationMonitor
    {
        private readonly PureLinkConfig _config; // Store the config locally

        #region Constants

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

        //private readonly PureLinkCmdProcessor _cmdProcessor = new PureLinkCmdProcessor();

        #endregion Constants

        #region IBasicCommunication Properties, Constructor, and Feedbacks

        // TODO [X] Add, modify, remove properties and fields as needed for the plugin being developed
        private readonly IBasicCommunication _comms;
        private readonly GenericCommunicationMonitor _commsMonitor;

        /// <summary>
        /// Set this value to that of the delimiter used by the API (if applicable)
        /// </summary>
        private const string CommsDelimiter = "!\r";

        /// <summary>
        /// Sets and reports the state of EnableAudioBreakaway 
        /// </summary>
        public bool EnableAudioBreakaway { get; protected set; }

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
        /// Implement IOnline
        /// </summary>
        public BoolFeedback IsOnline
        {
            get { return _commsMonitor.IsOnlineFeedback; }
        }

        /// <summary>
        /// Implement ICommunicationMonitor
        /// </summary>
        public StatusMonitorBase CommunicationMonitor
        {
            get { return _commsMonitor; }
        }

        /// <summary>
        /// Reports online feedback through the bridge
        /// </summary>
        public BoolFeedback OnlineFeedback { get; private set; }

        /// <summary>
        /// Reports EnableAudioBreakawayFeedback feedback through the bridge
        /// </summary>
        public BoolFeedback EnableAudioBreakawayFeedback { get; private set; }

        /// <summary>
        /// Reports socket status feedback through the bridge
        /// </summary>
        public IntFeedback StatusFeedback { get; private set; }

        // TODO [X] Add feedback for routing, names, video routing, audio routing, and outputcurrentnames
        /// <summary>
        /// Output current video source. The first uint is the output, then input
        /// </summary>
        private readonly Dictionary<uint, uint> _outputCurrentVideoInput = new Dictionary<uint, uint>();
        /// <summary>
        /// Output current audio source. The first uint is the output, then input
        /// </summary>
        private readonly Dictionary<uint, uint> _outputCurrentAudioInput = new Dictionary<uint, uint>();

        /// <summary>
        /// Requested video source. The first uint is the output, then input
        /// </summary>
        private readonly Dictionary<uint, uint> _requestedVideoInputs = new Dictionary<uint, uint>();
        /// <summary>
        /// Requested audio source. The first uint is the output, then input
        /// </summary>
        private readonly Dictionary<uint, uint> _requestedAudioInputs = new Dictionary<uint, uint>();

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
            _commsQueueLock = new CCriticalSection();
            _commsQueue = new CrestronQueue<string>();
            _parserLock = new CCriticalSection();
            _parserQueue = new CrestronQueue<string>();

            _config = config;
            if (string.IsNullOrEmpty(_config.DeviceId))
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Error, "Config DeviceId value invalid. Setting value to 999.");
                _config.DeviceId = "999";
            }

            if (_config.Model < 1)
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Error, "Config Model value invalid. Current value: {0}. Valid values are 0 or 1. Setting value to 0.", _config.Model.ToString());
                _config.Model = 0;
            }

            // Consider enforcing default poll values IF NOT DEFINED in the JSON config
            if (string.IsNullOrEmpty(_config.PollString))
                _config.PollString = PollString;

            if (_config.PollTimeMs == 0)
                _config.PollTimeMs = 45000;

            if (_config.WarningTimeoutMs == 0)
                _config.WarningTimeoutMs = 180000;

            if (_config.ErrorTimeoutMs == 0)
                _config.ErrorTimeoutMs = 300000;

            var socket = _comms as ISocketStatus;

            var result = (socket != null) ? ConnectFb : _commsMonitor.IsOnline;
            OnlineFeedback = new BoolFeedback(() => result);

            ConnectFeedback = new BoolFeedback(() => ConnectFb);
            EnableAudioBreakawayFeedback = new BoolFeedback(() => EnableAudioBreakaway);
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
            
            if (socket != null)
            {
                // device comms is IP **ELSE** device comms is RS232
                socket.ConnectionChange += socket_ConnectionChange;
            }

            #region Communication data event handlers.  Comment out any that don't apply to the API type

            // _comms gather for ASCII based API's that have a defined delimiter
            var commsGather = new CommunicationGather(_comms, CommsDelimiter);
            commsGather.LineReceived += Handle_LineRecieved;

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
            // Check if tempSocket is null as protection in case configuration didn't implement it
            if (tempSocket == null) return 0;
            return (int)tempSocket.ClientStatus;
        }

        private void InitializeOutputNames(Dictionary<uint, PureLinkEntryConfig> outputs)
        {
            if (outputs == null)
            {
                Debug.Console(2, this, "InitializeOutputNames: Outputs null");
                return;
            }
            foreach (var o in outputs)
            {
                var output = o;

                if (string.IsNullOrEmpty(output.Value.VideoName))
                {
                    if (!string.IsNullOrEmpty(output.Value.Name))
                        output.Value.VideoName = output.Value.Name;
                }
                if (string.IsNullOrEmpty(output.Value.AudioName))
                {
                    if (!string.IsNullOrEmpty(output.Value.Name))
                        output.Value.AudioName = output.Value.Name;
                }
                OutputNameFeedbacks.Add(output.Key, new StringFeedback(() => output.Value.Name));
                OutputVideoNameFeedbacks.Add(output.Key, new StringFeedback(() => output.Value.VideoName));
                OutputAudioNameFeedbacks.Add(output.Key, new StringFeedback(() => output.Value.AudioName));
                _outputCurrentVideoInput.Add(output.Key, 0);
                _outputCurrentAudioInput.Add(output.Key, 0);
                _requestedVideoInputs.Add(output.Key, 0);
                _requestedAudioInputs.Add(output.Key, 0);

                OutputCurrentVideoValueFeedbacks.Add(output.Key, new IntFeedback(() =>
                {
                    uint sourceKey;
                    var success = _outputCurrentVideoInput.TryGetValue(output.Key, out sourceKey);
                    return !success ? 0 : Convert.ToInt32(sourceKey);
                }));

                OutputCurrentAudioValueFeedbacks.Add(output.Key, new IntFeedback(() =>
                {
                    uint sourceKey;
                    return Convert.ToInt32(_outputCurrentAudioInput.TryGetValue(output.Key, out sourceKey) ? sourceKey : 0);
                }));

                OutputCurrentVideoNameFeedbacks.Add(output.Key, new StringFeedback(() =>
                {
                    uint sourceKey;
                    PureLinkEntryConfig config;
                    var success = _outputCurrentVideoInput.TryGetValue(output.Key, out sourceKey);
                    if (!success)
                        return string.Empty;
                    success = _config.Inputs.TryGetValue(sourceKey, out config);
                    if (!success)
                        return string.Empty;
                    return string.IsNullOrEmpty(config.VideoName) ? config.Name : config.VideoName;
                }));

                OutputCurrentAudioNameFeedbacks.Add(output.Key, new StringFeedback(() =>
                {
                    uint sourceKey;
                    PureLinkEntryConfig config;
                    var success = _outputCurrentAudioInput.TryGetValue(output.Key, out sourceKey);
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
            foreach (var i in inputs)
            {
                // As the foreach runs, 'input' could potentially change
                // assign inputs to input then subsuquent changes don't matter on 'item'
                var input = i;

                if (string.IsNullOrEmpty(input.Value.VideoName))
                {
                    if (!string.IsNullOrEmpty(input.Value.Name))
                        input.Value.VideoName = input.Value.Name;
                }
                if (string.IsNullOrEmpty(input.Value.AudioName))
                {
                    if (!string.IsNullOrEmpty(input.Value.Name))
                        input.Value.AudioName = input.Value.Name;
                }
                InputNameFeedbacks.Add(input.Key, new StringFeedback(() => input.Value.Name));
                InputVideoNameFeedbacks.Add(input.Key, new StringFeedback(() => input.Value.VideoName));
                InputAudioNameFeedbacks.Add(input.Key, new StringFeedback(() => input.Value.AudioName));
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

            Debug.Console(1, this, "handleLineReceived args.Text: {0}", args.Text); //Show me what we received
            EnqueueParseData(args.Text); 
        }

        /// <summary>
        /// Parse incoming data
        /// </summary>
        /// <param name="data"></param>
        public void ParseData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                Debug.Console(2, this, "HandleLineReceived: data is null or empty");
                return;
            }

            data.Trim().ToLower(); // Remove leading/trailing white-space characters             

            if (data.Contains("error"))
            {
                Debug.Console(2, this, Debug.ErrorLogLevel.Error, "HandleLineReceived: {0}", data);
                return;
            }

            if (data.Contains("sc"))
            {
                Debug.Console(2, this, "Received Audio-Video Switch FB");                
                ParseIoResponse(data, RouteType.AudioVideo);
            }
            else if (data.Contains("sv") || data.ToLower().Contains("s?v"))
            {
                Debug.Console(2, this, "Received Video Switch FB");
                ParseIoResponse(data, RouteType.Video);
            }
            else if (data.Contains("sa") || data.ToLower().Contains("s?a"))
            {
                Debug.Console(2, this, "Received Audio Switch FB");
                ParseIoResponse(data, RouteType.Audio);
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
            //SendText(_config.PollString);
            EnqueueSendText(_config.PollString);
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
            trilist.SetSigTrueAction(joinMap.VideoEnter.JoinNumber, SetVideoEnter);
            trilist.SetSigTrueAction(joinMap.AudioEnter.JoinNumber, SetAudioEnter);
            trilist.SetSigTrueAction(joinMap.Connect.JoinNumber, Connect);
            trilist.SetSigTrueAction(joinMap.Disconnect.JoinNumber, Disconnect);
            trilist.SetSigTrueAction(joinMap.PollVideo.JoinNumber, SetPollVideo);
            trilist.SetSigTrueAction(joinMap.PollAudio.JoinNumber, SetPollAudio);
            trilist.SetSigTrueAction(joinMap.ClearVideoRoutes.JoinNumber, SetClearVideoRoutes);
            trilist.SetSigTrueAction(joinMap.ClearAudioRoutes.JoinNumber, SetClearAudioRoutes);
            trilist.SetSigTrueAction(joinMap.EnableAudioBreakaway.JoinNumber, SetEnableAudioBreakaway);

            // X.LinkInputSig is feedback to SIMPL
            OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            ConnectFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Connect.JoinNumber]);
            StatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.Status.JoinNumber]);
            EnableAudioBreakawayFeedback.LinkInputSig(trilist.BooleanInput[joinMap.EnableAudioBreakaway.JoinNumber]);

            // TODO [X] Need to update poll method
            // TODO [X] Reference your poll string in your poll method
            // TODO [X] Need ExecuteSwitch to determine input or output number then sendText()
            // TODO [X] Add parsing routines within the Handle_LineReceived

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

            foreach (var kvp in OutputCurrentVideoValueFeedbacks)
            {
                // Get the actual join number of the signal
                var join = kvp.Key + joinMap.OutputVideo.JoinNumber - 1;
                // Get the actual output number which is the item.Key as read in from the configuraiton file
                var output = kvp.Key;
                // Link incoming from SIMPL EISC bridge (aka route request) to internal method 
                trilist.SetUShortSigAction(join, (input) =>
                {
                    if (_requestedVideoInputs.ContainsKey(output))
                        _requestedVideoInputs[output] = input;
                });
                // Link route feedbacks to SIMPL EISC bridge
                var feedback = kvp.Value;
                if (feedback == null) continue;
                feedback.LinkInputSig(trilist.UShortInput[join]);
            }

            foreach (var kvp in OutputCurrentAudioValueFeedbacks)
            {
                var join = kvp.Key + joinMap.OutputAudio.JoinNumber - 1;
                var output = kvp.Key;
                trilist.SetUShortSigAction(join, (input) =>
                {
                    if (_requestedAudioInputs.ContainsKey(output))
                        _requestedAudioInputs[output] = input;
                });
                var feedback = kvp.Value;
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

#warning Method SetVideoEnter() below should be refactored
        /// <summary>
        /// Loop request dictionary skipping 0's and compare non 0's to current route dictionary to determine if video route should be called
        /// </summary>            
        private void SetVideoEnter()
        {
            foreach (var kvp in _requestedVideoInputs)
            {
                var requestsedInput = kvp.Value;
                var output = kvp.Key;
                if (requestsedInput == 0)
                    continue;
                uint currentInput;
                var success = _outputCurrentVideoInput.TryGetValue(output, out currentInput);
                if (!success)
                {
                    Debug.Console(2, this, Debug.ErrorLogLevel.Error, "SetVideoEnter: Output {0} does not exist in configuration", output);
                    continue;
                }
                if (currentInput == requestsedInput) continue;
                var inputToSend = (requestsedInput == 999) ? 0 : requestsedInput;

                var sType = EnableAudioBreakaway ? eRoutingSignalType.Video : eRoutingSignalType.AudioVideo;
                ExecuteSwitch(inputToSend, output, sType);
            }
        }

#warning Method SetAudioEnter() below should be refactored
        /// <summary>
        /// Loop request dictionary skipping 0's and compare non 0's to current route dictionary to determine if audio route should be called
        /// </summary>            
        private void SetAudioEnter()
        {
            foreach (var kvp in _requestedAudioInputs)
            {
                var requestedInput = kvp.Value;
                var output = kvp.Key;
                if (requestedInput == 0)
                    continue;
                uint currentInput;
                var success = _outputCurrentAudioInput.TryGetValue(output, out currentInput);
                if (!success)
                {
                    Debug.Console(2, this, Debug.ErrorLogLevel.Error, "SetAudioEnter: Output {0} does not exist in configuration", output);
                    continue;
                }
                if (currentInput == requestedInput) continue;
                var inputToSend = (requestedInput == 999) ? 0 : requestedInput;

                var sType = EnableAudioBreakaway ? eRoutingSignalType.Audio : eRoutingSignalType.AudioVideo;
                ExecuteSwitch(inputToSend, output, sType);
            }
        }

        /// <summary>
        /// Toggles the state of EnableAudioBreakaway and triggers the FireUpdate method
        /// </summary>
        private void SetEnableAudioBreakaway()
        {
            EnableAudioBreakaway = !EnableAudioBreakaway;
            EnableAudioBreakawayFeedback.FireUpdate();
        }

        /// <summary>
        /// Triggers the SendText method to send the PollVideo command
        /// </summary>
        public void SetPollVideo()
        {
            //SendText(PollVideo);
            EnqueueSendText(PollVideo);
        }

        /// <summary>
        /// Triggers the SendText method to send the PollAudio command
        /// </summary>
        public void SetPollAudio()
        {
            //SendText(PollAudio);
            EnqueueSendText(PollAudio);
        }

        /// <summary>
        /// Triggers the SendText method to send ClearVideoRoutes command
        /// </summary>
        public void SetClearVideoRoutes()
        {

            //SendText(ClearVideoRoutes);
            EnqueueSendText(ClearVideoRoutes);
        }

        /// <summary>
        /// Triggers the SendText method to send ClearAudioRoutes command
        /// </summary>
        public void SetClearAudioRoutes()
        {

            //SendText(ClearAudioRoutes);
            EnqueueSendText(ClearAudioRoutes);
        }

        /// <summary>
        /// Void Method that updates Feedbacks which updates Bridge
        /// </summary>
        private void UpdateFeedbacks()
        {
            OnlineFeedback.FireUpdate();
            ConnectFeedback.FireUpdate();
            StatusFeedback.FireUpdate();
            EnableAudioBreakawayFeedback.FireUpdate();

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
        public enum RouteType { Audio, Video, AudioVideo }

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
                _outputCurrentVideoInput[output] = value;

                StringFeedback nameFeedback;
                if (OutputCurrentVideoNameFeedbacks.TryGetValue(output, out nameFeedback))
                {
                    nameFeedback.FireUpdate();
                }

                IntFeedback numberFeedback;
                if (OutputCurrentVideoValueFeedbacks.TryGetValue(output, out numberFeedback))
                {
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

            var lockState = _commsQueueLock.TryEnter(); //lockState is asking if Dequeue is active or inuse or triggered
                                                        //This prevents multiple queues in action
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
        /// Plugin Enqueue data to parse
        /// </summary>
        /// <param name="cmd"></param>
        public void EnqueueParseData(string cmd)
        {
            if (cmd == null)
                return;

            _parserQueue.TryToEnqueue(cmd);

            var lockState = _parserLock.TryEnter();
            if (lockState)
                CrestronInvoke.BeginInvoke((o) => DequeueParseData());
        }

        /// <summary>
        /// Plugin dequeue and call ParseData() method
        /// </summary>
        private void DequeueParseData()
        {
            try
            {
                while (true)
                {                    
                    var cmd = _parserQueue.Dequeue();
                    if (!string.IsNullOrEmpty(cmd))
                    {                        
                        ParseData(cmd);
                        //Thread.Sleep(200);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Console(2, this, "DequeueParseData Exception: {0}", ex);
            }
            finally
            {
                if (_parserLock != null)
                    _parserLock.Leave();
            }
        }

        /// <summary>
        /// Executes switch
        /// </summary>
        /// <param name="input">Source number</param>
        /// <param name="output">Output number</param>
        /// <param name="signalType">AudioVideo, Video, or Audio</param>
        public void ExecuteSwitch(uint input, uint output, eRoutingSignalType signalType)
        {
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
                        EnqueueSendText(cmd);
                        //SendText(cmd);
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
                        EnqueueSendText(cmd);
                        //SendText(cmd);
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
                        EnqueueSendText(cmd);
                        //SendText(cmd);
                        break;
                    }
            }
        }
        #endregion

        #region Interfaces
        /// <summary>
        /// IOnline Members
        /// </summary>
        BoolFeedback IOnline.IsOnline
        {
            get { return OnlineFeedback; }
        }

        /// <summary>
        /// ICommunicationMonitor Members
        /// </summary>
        StatusMonitorBase ICommunicationMonitor.CommunicationMonitor
        {
            get { return _commsMonitor; }
        }
        #endregion
    }
}
