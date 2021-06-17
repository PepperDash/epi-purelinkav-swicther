using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PureLinkPlugin
{
    /// <summary>
    /// Class representing an output on a Purelink switch
    /// </summary>
    public class PureLinkOutput : PureLinkIo
    {
        private readonly int _deviceId;
        private readonly int _deviceModel;

        private int _currentlyRoutedAudio;
        private int _currentlyRoutedVideo;
        private int _requestedRoutedAudio;
        private int _requestedRoutedVideo;

        /// <summary>
        /// </summary>
        /// <param name="key">output key</param>
        /// <param name="index">io number of output</param>
        /// <param name="config">entry config</param>
        /// <param name="deviceId">parent device id</param>
        /// <param name="deviceModel">parent device model</param>
        /// <param name="inputs">enumeration of available inputs</param>
        public PureLinkOutput(string key, 
            uint index,
            PureLinkEntryConfig config, 
            int deviceId, 
            int deviceModel,
            IEnumerable<PureLinkInput> inputs)
            : base(key, index, config)
        {
            _deviceId = deviceId;
            _deviceModel = deviceModel;

            CurrentlyRoutedVideoValue = new IntFeedback(key + "-CurrentVideoValue", 
                () => _currentlyRoutedVideo == 0 ? 999 : _currentlyRoutedVideo);

            const string noSource = "No Source";
            CurrentlyRouteVideoName = new StringFeedback(key + "-CurrentVideoName",
                () =>
                    {
                        var result = inputs
                            .FirstOrDefault(x => x.Index == CurrentlyRoutedVideoValue.IntValue);

                        return (result == null) ? noSource : result.VideoName;
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
                        var result = inputs
                            .FirstOrDefault(x => x.Index == _currentlyRoutedAudio);

                        return (result == null) ? noSource : result.AudioName;
                    });

            CurrentlyRoutedAudioValue.OutputChange += (sender, args) => CurrentlyRouteAudioName.FireUpdate();

            CurrentlyRoutedAudioValue.OutputChange +=
                 (sender, args) => Debug.Console(1, this, "Audio Routed Value Update : '{0}'", args.IntValue);
            CurrentlyRouteAudioName.OutputChange +=
                (sender, args) => Debug.Console(1, this, "Audio Routed Name Update : '{0}'", args.StringValue);
        }

        /// <summary>
        /// Returns true if an audio route has been requested
        /// </summary>
        public bool AudioRouteRequested
        {
            get { return _requestedRoutedAudio > 0; }
        }

        /// <summary>
        /// String feedback indicating currently routed audio input name
        /// </summary>
        public StringFeedback CurrentlyRouteAudioName { get; private set; }

        /// <summary>
        /// String feedback indicating currently routed video input name
        /// </summary>
        public StringFeedback CurrentlyRouteVideoName { get; private set; }

        /// <summary>
        /// Int feedback indicating currently routed audio input value
        /// </summary>
        public IntFeedback CurrentlyRoutedAudioValue { get; private set; }

        /// <summary>
        /// Int feedback indicating currently routed video input value
        /// </summary>
        public IntFeedback CurrentlyRoutedVideoValue { get; private set; }

        /// <summary>
        /// Returns true if a video route has been requested
        /// </summary>
        public bool VideoRouteRequested
        {
            get { return _requestedRoutedVideo > 0; }
        }

        /// <summary>
        /// Gets poll string for current audio input
        /// </summary>
        /// <returns>string for current audio input poll</returns>
        /// <exception cref="Exception">Invalid Device Model</exception>
        public string GetCurrentAudioRoutePoll()
        {
            switch (_deviceModel)
            {
                case 0:
                    return string.Format("{0}{1}?AO{2:D2}!\r",
                        PureLinkDevice.StartChar,
                        _deviceId,
                        Index);
                case 1:
                    return string.Format("{0}{1}?AO{2:D3}!\r",
                        PureLinkDevice.StartChar,
                        _deviceId,
                        Index);

                default:
                    throw new Exception("Invalid device model");
            }
        }

        /// <summary>
        /// Gets poll string for current video input
        /// </summary>
        /// <returns>string for current video input poll</returns>
        /// <exception cref="Exception">Invalid Device Model</exception>
        public string GetCurrentVideoRoutePoll()
        {
            switch (_deviceModel)
            {
                case 0:
                    return string.Format("{0}{1}?VO{2:D2}!\r",
                        PureLinkDevice.StartChar,
                        _deviceId,
                        Index);
                case 1:
                    return string.Format("{0}{1}?VO{2:D3}!\r",
                        PureLinkDevice.StartChar,
                        _deviceId,
                        Index);

                default:
                    throw new Exception("Invalid device model");
            }
        }


        /// <summary>
        /// Gets command string to route requested audio input
        /// </summary>
        /// <returns>command or empty if none requested</returns>
        /// <exception cref="Exception">Invalid Device Model</exception>
        public string GetRequestedAudioCommand()
        {
            var cmd = new StringBuilder();
            if (!AudioRouteRequested)
                return cmd.ToString();

            var inputToRoute = _requestedRoutedAudio == 999 ? 0 : _requestedRoutedAudio;
            _requestedRoutedAudio = 0;
            //cmd.Append(",");
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

            cmd.Append("!\r");
            return cmd.ToString();
        }

        /// <summary>
        /// Gets command string to route requested AudioVideo input
        /// </summary>
        /// <returns>command or empty if none requested or !audioFollowsVideo</returns>
        /// <exception cref="Exception">Invalid Device Model</exception>
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
                default:
                    throw new Exception("Invalid device model");
            }

            cmd.Append("!\r");
            return cmd.ToString();
        }

        /// <summary>
        /// Gets command string to route requested video input
        /// </summary>
        /// <returns>command or empty if none requested</returns>
        /// <exception cref="Exception">Invalid Device Model</exception>
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

            cmd.Append("!\r");
            return cmd.ToString();
        }

        /// <summary>
        /// Request an updated input.  If routing is not enabled it will queue
        /// </summary>
        /// <param name="input">input to requeset</param>
        public void RequestAudioRoute(int input)
        {
            if (input == 0)
                return;

            Debug.Console(1, this, "Requesting new Audio route : {0}", input);
            _requestedRoutedAudio = input;
        }

        /// <summary>
        /// Request an updated input.  If routing is not enabled it will queue
        /// </summary>
        /// <param name="input">input to requeset</param>
        public void RequestVideoRoute(int input)
        {
            if (input == 0)
                return;

            Debug.Console(1, this, "Requesting new video route : {0}", input);
            _requestedRoutedVideo = input;
        }

        public void UpdateCurrentAudioInput(int input)
        {
            _currentlyRoutedAudio = input;
            CurrentlyRoutedAudioValue.FireUpdate();
        }

        public void UpdateCurrentVideoInput(int input)
        {
            _currentlyRoutedVideo = input;
            CurrentlyRoutedVideoValue.FireUpdate();
        }
    }
}