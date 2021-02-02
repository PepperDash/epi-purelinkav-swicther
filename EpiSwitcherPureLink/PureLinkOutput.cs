using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PureLinkPlugin
{
    /// <summary>
    /// </summary>
    public class PureLinkOutput : PureLinkIo
    {
        private readonly string _audioPollResponseStart;
        private readonly string _audioResponseStart;
        private readonly string _audioVideoResponseStart;
        private readonly int _deviceId;
        private readonly int _deviceModel;

        private readonly string _responseEnding;
        private readonly string _videoPollResponseStart;
        private readonly string _videoResponseStart;

        private int _currentlyRoutedAudio;
        private int _currentlyRoutedVideo;
        private int _requestedRoutedAudio;
        private int _requestedRoutedVideo;

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="index"></param>
        /// <param name="config"></param>
        /// <param name="deviceId"></param>
        /// <param name="deviceModel"></param>
        /// <param name="inputs"></param>
        public PureLinkOutput(string key, uint index, PureLinkEntryConfig config, int deviceId, int deviceModel,
                              IEnumerable<PureLinkInput> inputs)
            : base(key, index, config)
        {
            _deviceId = deviceId;
            _deviceModel = deviceModel;
            _responseEnding = String.Format("O{0:D3}", index);

            _audioVideoResponseStart = String.Format("{0}{1}sCI",
                PureLinkDevice.StartChar,
                _deviceId);

            _videoResponseStart = String.Format("{0}{1}sVCI",
                PureLinkDevice.StartChar,
                _deviceId);

            _audioResponseStart = String.Format("{0}{1}sACI",
                PureLinkDevice.StartChar,
                _deviceId);

            _videoPollResponseStart = String.Format("{0}{1}s?VI",
                PureLinkDevice.StartChar,
                _deviceId);

            _audioPollResponseStart = String.Format("{0}{1}s?AI",
                PureLinkDevice.StartChar,
                _deviceId);

            CurrentlyRoutedVideoValue = new IntFeedback(key + "-CurrentVideoValue", 
                () => _currentlyRoutedVideo == 0 ? 999 : _currentlyRoutedVideo);

            CurrentlyRouteVideoName = new StringFeedback(key + "-CurrentVideoName",
                () =>
                    {
                        var result = inputs.FirstOrDefault(x => x.Index == CurrentlyRoutedVideoValue.IntValue);
                        return ( result == null ) ? "No Source" : result.VideoName;
                    });

            CurrentlyRoutedVideoValue.OutputChange += (sender, args) => CurrentlyRouteVideoName.FireUpdate();
            CurrentlyRoutedVideoValue.OutputChange +=
                (sender, args) => Debug.Console(1, this, "Video Routed Value Update : '{0}'", args.IntValue);
            CurrentlyRouteVideoName.OutputChange +=
                (sender, args) => Debug.Console(1, this, "Video Routed Name Update : '{0}'", args.StringValue);

            CurrentlyRoutedAudioValue = new IntFeedback(key + "-CurrentAudioValue", 
                () => _currentlyRoutedAudio == 0 ? 999 : _currentlyRoutedAudio);

            CurrentlyRouteAudioName = new StringFeedback(key + "-CurrentAudioName",
                () =>
                    {
                        var result = inputs.FirstOrDefault(x => x.Index == _currentlyRoutedAudio);
                        return ( result == null ) ? "No Source" : result.AudioName;
                    });

            CurrentlyRoutedAudioValue.OutputChange += (sender, args) => CurrentlyRouteAudioName.FireUpdate();
            CurrentlyRoutedAudioValue.OutputChange +=
                 (sender, args) => Debug.Console(1, this, "Audio Routed Value Update : '{0}'", args.IntValue);
            CurrentlyRouteAudioName.OutputChange +=
                (sender, args) => Debug.Console(1, this, "Audio Routed Name Update : '{0}'", args.StringValue);
        }

        /// <summary>
        /// </summary>
        public bool AudioRouteRequested
        {
            get { return _requestedRoutedAudio > 0; }
        }

        /// <summary>
        /// </summary>
        public StringFeedback CurrentlyRouteAudioName { get; private set; }

        /// <summary>
        /// </summary>
        public StringFeedback CurrentlyRouteVideoName { get; private set; }

        /// <summary>
        /// </summary>
        public IntFeedback CurrentlyRoutedAudioValue { get; private set; }

        /// <summary>
        /// </summary>
        public IntFeedback CurrentlyRoutedVideoValue { get; private set; }

        /// <summary>
        /// </summary>
        public bool VideoRouteRequested
        {
            get { return _requestedRoutedVideo > 0; }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetCurrentAudioRoutePoll()
        {
            switch (_deviceModel)
            {
                case 0:
                    return string.Format("{0}{1}?AO{2:D2}",
                        PureLinkDevice.StartChar,
                        _deviceId,
                        Index);
                case 1:
                    return string.Format("{0}{1}?AO{2:D3}",
                        PureLinkDevice.StartChar,
                        _deviceId,
                        Index);

                default:
                    throw new Exception("Invalid device model");
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetCurrentVideoRoutePoll()
        {
            switch (_deviceModel)
            {
                case 0:
                    return string.Format("{0}{1}?VO{2:D2}",
                        PureLinkDevice.StartChar,
                        _deviceId,
                        Index);
                case 1:
                    return string.Format("{0}{1}?VO{2:D3}",
                        PureLinkDevice.StartChar,
                        _deviceId,
                        Index);

                default:
                    throw new Exception("Invalid device model");
            }
        }

        public string GetRequestedAudioCommand()
        {
            var cmd = new StringBuilder();
            if (!AudioRouteRequested)
                return cmd.ToString();

            var inputToRoute = _requestedRoutedAudio == 999 ? 0 : _requestedRoutedAudio;
            _requestedRoutedAudio = 0;
            cmd.Append(",");
            switch (_deviceModel)
            {
                case 0:
                    cmd.Append(string.Format("{0}{1}ACI{2:D2}O{3:D2}",
                        PureLinkDevice.StartChar,
                        _deviceId,
                        inputToRoute,
                        Index));
                    break;
                case 1:
                    cmd.Append(string.Format("{0}{1}ACI{2:D3}O{3:D3}",
                        PureLinkDevice.StartChar,
                        _deviceId,
                        inputToRoute,
                        Index));
                    break;
                default:
                    throw new Exception("Invalid device model");
            }

            return cmd.ToString();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public string GetRequestedAudioVideoRouteCommand()
        {
            var cmd = new StringBuilder();
            if (!VideoRouteRequested)
                return cmd.ToString();

            var inputToRoute = _requestedRoutedVideo == 999 ? 0 : _requestedRoutedVideo;
            _requestedRoutedVideo = 0;
            switch (_deviceModel)
            {
                case 0:
                    cmd.Append(string.Format("{0}{1}CI{2:D2}O{3:D2}",
                        PureLinkDevice.StartChar,
                        _deviceId,
                        inputToRoute,
                        Index));
                    break;
                case 1:
                    cmd.Append(string.Format("{0}{1}CI{2:D3}O{3:D3}",
                        PureLinkDevice.StartChar,
                        _deviceId,
                        inputToRoute,
                        Index));
                    break;
            }

            return cmd.ToString();
        }

        public string GetRequestedVideoCommand()
        {
            var cmd = new StringBuilder();
            if (!VideoRouteRequested)
                return cmd.ToString();

            var inputToRoute = _requestedRoutedVideo == 999 ? 0 : _requestedRoutedVideo;
            _requestedRoutedVideo = 0;
            switch (_deviceModel)
            {
                case 0:
                    cmd.Append(string.Format("{0}{1}VCI{2:D2}O{3:D2}",
                        PureLinkDevice.StartChar,
                        _deviceId,
                        inputToRoute,
                        Index));
                    break;
                case 1:
                    cmd.Append(string.Format("{0}{1}VCI{2:D3}O{3:D3}",
                        PureLinkDevice.StartChar,
                        _deviceId,
                        inputToRoute,
                        Index));
                    break;
                default:
                    throw new Exception("Invalid device model");
            }

            return cmd.ToString();
        }

        /// <summary>
        /// </summary>
        /// <param name="response"></param>
        public void ProcessResponse(string response)
        {
            if (!response.EndsWith(_responseEnding))
                return;

            try
            {
                response = response.Replace(_responseEnding, String.Empty);
                Debug.Console(2, this, "Processing Response : {0}", response);

                if (response.StartsWith(_audioVideoResponseStart))
                {
                    Debug.Console(2, this, "Received Audio-Video Switch FB");
                    response = response.Replace(_audioVideoResponseStart, String.Empty);

                    var currentAudioVideoInput = Convert.ToInt16(response);
                    UpdateCurrentVideoInput(currentAudioVideoInput);
                    UpdateCurrentAudioInput(currentAudioVideoInput);

                    return;
                }

                if (response.StartsWith(_videoResponseStart))
                {
                    Debug.Console(2, this, "Received Video Switch FB");
                    response = response.Replace(_videoResponseStart, String.Empty);

                    var currentVideoInput = Convert.ToInt16(response);
                    UpdateCurrentVideoInput(currentVideoInput);

                    return;
                }

                if (response.StartsWith(_audioResponseStart))
                {
                    Debug.Console(2, this, "Received Audio Switch FB");
                    response = response.Replace(_audioResponseStart, String.Empty);

                    var currentAudioInput = Convert.ToInt16(response);
                    UpdateCurrentAudioInput(currentAudioInput);

                    return;
                }

                if (response.StartsWith(_videoPollResponseStart))
                {
                    Debug.Console(2, this, "Received Video Switch FB");
                    response = response.Replace(_videoPollResponseStart, String.Empty);

                    var currentVideoInput = Convert.ToInt16(response);
                    UpdateCurrentVideoInput(currentVideoInput);

                    return;
                }

                if (response.StartsWith(_audioPollResponseStart))
                {
                    Debug.Console(2, this, "Received Audio Switch FB");
                    response = response.Replace(_audioPollResponseStart, String.Empty);

                    var currentAudioInput = Convert.ToInt16(response);
                    UpdateCurrentAudioInput(currentAudioInput);

                    return;
                }

                Debug.Console(2, this, "Not sure what to do with this string : {0}", response);
            }
            catch (Exception ex)
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Notice, "Caught an error processing the response {0}{1}{2}", response, ex.Message, ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="input"></param>
        public void RequestAudioRoute(int input)
        {
            Debug.Console(1, this, "Requesting new Audio route : {0}", input);
            _requestedRoutedAudio = input;
        }

        /// <summary>
        /// </summary>
        /// <param name="input"></param>
        public void RequestVideoRoute(int input)
        {
            Debug.Console(1, this, "Requesting new video route : {0}", input);
            _requestedRoutedVideo = input;
        }

        private void UpdateCurrentAudioInput(int input)
        {
            _currentlyRoutedAudio = input;
            CurrentlyRoutedAudioValue.FireUpdate();
        }

        private void UpdateCurrentVideoInput(int input)
        {
            _currentlyRoutedVideo = input;
            CurrentlyRoutedVideoValue.FireUpdate();
        }
    }
}