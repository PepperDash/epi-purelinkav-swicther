using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Queues;

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
        
        /// <summary>
        /// "*999?version!" - Check firmware version
        /// "*999I000!" - Check ruoters ID
        /// "“*255sI Router ID 255" - Response from router ID check
        /// </summary>

        public const string PollString = "*999?version";
        public const string PollVideo = ("*999?vallio");
        public const string PollAudio = ("*999?aallio");
        public const string ClearVideoRoutes = ("*999vdallio");
        public const string ClearAudioRoutes = ( "*999adallio" );
        public const string StartChar = "*";
        
        /// <summary>
        /// Switcher Max Array value for all Input/Output
        /// </summary>
        public const int MaxIo = 72;

        private readonly StringResponseProcessor _responseProcessor;
        private readonly IDictionary<uint, PureLinkInput> _inputs = new Dictionary<uint, PureLinkInput>();
        private readonly IDictionary<uint, PureLinkOutput> _outputs = new Dictionary<uint, PureLinkOutput>();
        private readonly PureLinkRouteQueue _routes;
    
        #endregion Constants

        #region IBasicCommunication Properties, Constructor, and Feedbacks

        // TODO [X] Add, modify, remove properties and fields as needed for the plugin being developed
        private readonly IBasicCommunication _comms;
        private readonly GenericCommunicationMonitor _commsMonitor;

        /// <summary>
        /// Set this value to that of the delimiter used by the API (if applicable)
        /// </summary>
        public const string CommsDelimiter = "!\r";

        /// <summary>
        /// Sets and reports the state of EnableAudioBreakaway 
        /// </summary>
        public bool EnableAudioBreakaway 
        {
            get { return _routes.AudioFollowVideo; }
        }

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
            _commsMonitor.Start();
            if (_comms.IsConnected)
            {
                return;
            }

            _comms.Connect();
        }

        /// <summary>
        /// Disconnects the comms of the plugin device
        /// </summary>
        /// <remarks>
        /// triggers the _comms.Disconnects stops the comms monitor
        /// </remarks>
        public void Disconnect()
        {
            _commsMonitor.Stop();
            if (!_comms.IsConnected)
            {
                return;
            }

            _comms.Disconnect();
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
        /// Reports EnableAudioBreakawayFeedback feedback through the bridge
        /// </summary>
        public BoolFeedback EnableAudioBreakawayFeedback { get; private set; }

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
            Debug.Console(1, this, "Constructing new {0} instance", name);

            // TODO [X] Update the constructor as needed for the plugin device being developed
            _config = config;
            if (string.IsNullOrEmpty(_config.DeviceId))
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Error, "Config DeviceId value invalid. Setting value to 999.");
                _config.DeviceId = "999";
            }

            if (_config.Model > 1)
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
            var socket = _comms as ISocketStatus;

            // The comms monitor polls your device
            // The _commsMonitor.Status only changes based on the values placed in the Poll times
            // _commsMonitor.StatusChange is the poll status changing not the TCP/IP isOnline status changing
            _commsMonitor = new GenericCommunicationMonitor(this, _comms, _config.PollTimeMs, _config.WarningTimeoutMs, _config.ErrorTimeoutMs, Poll);
            OnlineFeedback = (socket != null) ? ConnectFeedback : _commsMonitor.IsOnlineFeedback;    

            #region Communication data event handlers.  Comment out any that don't apply to the API type

            // _comms gather for ASCII based API's that have a defined delimiter
            var commsGather = new CommunicationGather(_comms, CommsDelimiter);
            _responseProcessor = new StringResponseProcessor(commsGather, ProcessResponse);

            #endregion Communication data event handlers.  Comment out any that don't apply to the API type

            InitializeInputNames(_config.Inputs);
            InitializeOutputNames(_config.Outputs);

            _routes = new PureLinkRouteQueue(_outputs.Values, _comms);

            Debug.Console(1, this, "Constructing new {0} instance complete", name);
            Debug.Console(1, new string('*', 80));
            Debug.Console(1, new string('*', 80));
        }

        /// <summary>
        /// Called in between Pre and PostActivationActions when Activate() is called. 
        ///             Override to provide addtitional setup when calling activation.  Overriding classes 
        ///             do not need to call base.CustomActivate()
        /// </summary>
        /// <returns>
        /// true if device activated successfully.
        /// </returns>
        public override bool CustomActivate()
        {
            var socket = _comms as ISocketStatus;
            OnlineFeedback = (socket != null) ? ConnectFeedback : _commsMonitor.IsOnlineFeedback;    

            _commsMonitor.StatusChange += (sender, args) =>
            {
                if (!_commsMonitor.IsOnline)
                    return;

                SetPollVideo();
                SetPollAudio();
            };

            if (socket != null)
            {
                // device comms is IP **ELSE** device comms is RS232
                socket.ConnectionChange += socket_ConnectionChange;
            }
            else
            {
                _commsMonitor.Start();
            }

            return base.CustomActivate();
        }

        private int GetSocketStatus()
        {
            var tempSocket = _comms as ISocketStatus;
            // Check if tempSocket is null as protection in case configuration didn't implement it
            if (tempSocket == null) 
                return 0;

            return (int)tempSocket.ClientStatus;
        }

        private void InitializeOutputNames(Dictionary<uint, PureLinkEntryConfig> outputs)
        {
            if (outputs == null)
            {
                Debug.Console(2, this, "InitializeOutputNames: Outputs null");
                return;
            }

            var deviceId = Convert.ToInt16(_config.DeviceId);
            var model = Convert.ToInt16(_config.Model);

            foreach (var o in outputs)
            {
                var outputConfig = o;

                var output = new PureLinkOutput(
                    Key + "-out" + outputConfig.Key,
                    outputConfig.Key,
                    outputConfig.Value,
                    deviceId,
                    model,
                    _inputs.Values);

                _outputs.Add(output.Index, output);

                OutputNameFeedbacks.Add(outputConfig.Key, new StringFeedback(() => output.Name));
                OutputVideoNameFeedbacks.Add(outputConfig.Key, new StringFeedback(() => output.VideoName));
                OutputAudioNameFeedbacks.Add(outputConfig.Key, new StringFeedback(() => output.AudioName));

                OutputCurrentVideoValueFeedbacks.Add(outputConfig.Key, output.CurrentlyRoutedVideoValue);
                OutputCurrentAudioValueFeedbacks.Add(outputConfig.Key, output.CurrentlyRoutedAudioValue);
                OutputCurrentVideoNameFeedbacks.Add(outputConfig.Key, output.CurrentlyRouteVideoName);
                OutputCurrentAudioNameFeedbacks.Add(outputConfig.Key, output.CurrentlyRouteAudioName);
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
                var inputConfig = i;
                var input = new PureLinkInput(Key + "-in" + inputConfig.Key, inputConfig.Key, inputConfig.Value);

                _inputs.Add(input.Index, input);
                InputNameFeedbacks.Add(input.Index, new StringFeedback(() => input.Name));
                InputVideoNameFeedbacks.Add(input.Index, new StringFeedback(() => input.VideoName));
                InputAudioNameFeedbacks.Add(input.Index, new StringFeedback(() => input.AudioName));
            }
        }

        private void socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
        {
            if (ConnectFeedback != null)
                ConnectFeedback.FireUpdate();

            if (StatusFeedback != null)
                StatusFeedback.FireUpdate();
        }

        private void ProcessResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                Debug.Console(2, this, "Process Reponse: response is null or empty");
                return;
            }

            try
            {
                var data = response.Trim();

                if (CheckResponseForError(data)) 
                    return;

                foreach (var pureLinkOutput in _outputs.Values)
                    pureLinkOutput.ProcessResponse(data);
            }
            catch (Exception ex)
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Error, "ProcessResponse Exception: {0}", ex.InnerException.Message);
                throw;
            }
        }

        private bool CheckResponseForError(string data)
        {
            data = data.ToLower();
            if (data.ToLower().Contains("command code error"))
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Error, "ProcessResponse: Command Code Error");
                return true;
            }

            if (data.ToLower().Contains("router id error"))
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Error, "ProcessResponse: Router ID Error");
                return true;
            }

            return false;
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
            if (string.IsNullOrEmpty(text)) 
                return;

            _comms.SendText(text);
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
            var poll = PureLinkMessage.BuildCommandFromString(_config.PollString);
            _comms.SendText(poll);
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
            trilist.SetBoolSigAction(joinMap.VideoEnter.JoinNumber, SetVideoEnter);
            trilist.SetBoolSigAction(joinMap.AudioEnter.JoinNumber, SetAudioEnter);
            trilist.SetSigTrueAction(joinMap.Connect.JoinNumber, Connect);
            trilist.SetSigTrueAction(joinMap.Disconnect.JoinNumber, Disconnect);
            trilist.SetSigTrueAction(joinMap.PollVideo.JoinNumber, SetPollVideo);
            trilist.SetSigTrueAction(joinMap.PollAudio.JoinNumber, SetPollAudio);
            trilist.SetSigTrueAction(joinMap.ClearVideoRoutes.JoinNumber, SetClearVideoRoutes);
            trilist.SetSigTrueAction(joinMap.ClearAudioRoutes.JoinNumber, SetClearAudioRoutes);
            trilist.SetBoolSigAction(joinMap.EnableAudioBreakaway.JoinNumber, SetEnableAudioBreakaway);

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
                        PureLinkOutput outputToSet;
                        if (!_outputs.TryGetValue(output, out outputToSet))
                            return;

                        outputToSet.RequestVideoRoute(input);
                        _routes.EnqueueVideoOutputForRoute(outputToSet);
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
                    PureLinkOutput outputToSet;
                    if (!_outputs.TryGetValue(output, out outputToSet))
                        return;

                    outputToSet.RequestAudioRoute(input);
                    _routes.EnqueueAudioOutputForRoute(outputToSet);
                });
                var feedback = kvp.Value;
                if (feedback == null) continue;
                feedback.LinkInputSig(trilist.UShortInput[join]);
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
        /// Loop request dictionary skipping 0's and compare non 0's to current route dictionary to determine if video route should be called
        /// </summary>            
        private void SetVideoEnter(bool state)
        {
            _routes.AllowVideoRouting = state;
        }

        /// <summary>
        /// Loop request dictionary skipping 0's and compare non 0's to current route dictionary to determine if audio route should be called
        /// </summary>            
        private void SetAudioEnter(bool state)
        {
            _routes.AllowAudioRouting = state;
        }

        /// <summary>
        /// Toggles the state of EnableAudioBreakaway and triggers the FireUpdate method
        /// </summary>
        private void SetEnableAudioBreakaway(bool state)
        {
            _routes.AudioFollowVideo = !state;
            EnableAudioBreakawayFeedback.FireUpdate();
        }

        /// <summary>
        /// Triggers the SendText method to send the PollVideo command
        /// </summary>
        public void SetPollVideo()
        {
            foreach (var pollToSend in _outputs
                .Values
                .Select(pureLinkOutput => pureLinkOutput.GetCurrentVideoRoutePoll())
                .Where(poll => !String.IsNullOrEmpty(poll)))
                {
                    _comms.SendText(pollToSend);
                }
        }

        /// <summary>
        /// Triggers the SendText method to send the PollAudio command
        /// </summary>
        public void SetPollAudio()
        {
            foreach (var pollToSend in _outputs
                .Values
                .Select(pureLinkOutput => pureLinkOutput.GetCurrentAudioRoutePoll())
                .Where(poll => !String.IsNullOrEmpty(poll)))
                {
                    _comms.SendText(pollToSend);
                }
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

        /// <summary>
        /// Plugin public eumermeration with only three values including: Audio, Video, or AudioVideo        
        /// </summary>                
        public enum RouteType { Audio, Video, AudioVideo }

        #region ExecuteSwitch
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
                        SendText(cmd);
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
                        SendText(cmd);
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
                        SendText(cmd);
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
