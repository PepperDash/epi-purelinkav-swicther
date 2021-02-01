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
    /// 
    /// </summary>
    public class PureLinkOutput : PureLinkIo
    {
        //'*255sACI001O020'
        public const string Route = "CI";
        public const string VideoRoute = "VCI";
        public const string AudiooRoute = "ACI";

        private readonly int _deviceId;
        private readonly int _deviceModel;

        /// <summary>
        /// 
        /// </summary>
        public IntFeedback CurrentlyRoutedVideoValue { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public StringFeedback CurrentlyRouteVideoName { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public IntFeedback CurrentlyRoutedAudioValue { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public StringFeedback CurrentlyRouteAudioName { get; private set; }

        private readonly CTimer _poll;

        private int _currentlyRoutedVideo;
        private int _requestedRoutedVideo;

        private int _currentlyRoutedAudio;
        private int _requestedRoutedAudio;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="index"></param>
        /// <param name="config"></param>
        /// <param name="deviceId"></param>
        /// <param name="deviceModel"></param>
        /// <param name="inputs"></param>
        public PureLinkOutput(string key, int index, PureLinkEntryConfig config, int deviceId, int deviceModel, IEnumerable<PureLinkInput> inputs)
            : base(key, index, config)
        {
            _deviceId = deviceId;
            _deviceModel = deviceModel;

            CurrentlyRoutedVideoValue = new IntFeedback(key + "-CurrentVideoValue", () => _currentlyRoutedVideo);
            CurrentlyRouteVideoName = new StringFeedback(key + "-CurrentVideoName",
                () =>
                    {
                        var result = inputs.FirstOrDefault(x => x.Index == _currentlyRoutedVideo);
                        return ( result == null ) ? "No Source" : result.VideoName;
                    });

            CurrentlyRoutedVideoValue.OutputChange += (sender, args) => CurrentlyRouteVideoName.FireUpdate();

            CurrentlyRoutedAudioValue = new IntFeedback(key + "-CurrentVideoValue", () => _currentlyRoutedAudio);
            CurrentlyRouteAudioName = new StringFeedback(key + "-CurrentVideoName",
                () =>
                    {
                        var result = inputs.FirstOrDefault(x => x.Index == _currentlyRoutedAudio);
                        return (result == null) ? "No Source" : result.AudioName;
                    });

            CurrentlyRoutedAudioValue.OutputChange += (sender, args) => CurrentlyRouteAudioName.FireUpdate();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void ProcessResponse(string response)
        {
            var responseEnding = String.Format("O{3:D2}!", Index);
            if (!response.EndsWith(responseEnding))
                return;


            if ()
            if (response.Contains("sc"))
            {
                Debug.Console(2, this, "Received Audio-Video Switch FB");
                ParseIoResponse(data, PureLinkDevice.RouteType.AudioVideo);
            }
            else if (data.Contains("sv") || data.ToLower().Contains("s?v"))
            {
                Debug.Console(2, this, "Received Video Switch FB");
                ParseIoResponse(data, PureLinkDevice.RouteType.Video);
            }
            else if (data.Contains("sa") || data.ToLower().Contains("s?a"))
            {
                Debug.Console(2, this, "Received Audio Switch FB");
                ParseIoResponse(data, PureLinkDevice.RouteType.Audio);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        public void RequestVideoRoute(int input)
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        public void RequestAudioRoute(int input)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetCurrentRouteCommand()
        {
            
            if (_requestedRoutedVideo > 0 && _requestedRoutedAudio > 0 && _requestedRoutedVideo == _requestedRoutedAudio)
            {
                var inputToRoute = _requestedRoutedVideo == 999 ? 0 : _requestedRoutedVideo;
                _requestedRoutedVideo = 0;
                switch (_deviceModel)
                {
                    case 0:
                        return string.Format("{0}{1}CI{2:D2}O{3:D2}", PureLinkDevice.StartChar, _deviceId, inputToRoute, Index);
                    case 1:
                        return string.Format("{0}{1}CI{2:D3}O{3:D3}", PureLinkDevice.StartChar, _deviceId, inputToRoute, Index);
                }
            }

            var cmd = new StringBuilder();
            if (_requestedRoutedVideo > 0)
            {
                var inputToRoute = _requestedRoutedVideo == 999 ? 0 : _requestedRoutedVideo;
                _requestedRoutedVideo = 0;
                switch (_deviceModel)
                {
                    case 0:
                        cmd.Append(string.Format("{0}{1}VCI{2:D2}O{3:D2}", PureLinkDevice.StartChar, _deviceId, inputToRoute, Index));
                        break;
                    case 1:
                        cmd.Append(string.Format("{0}{1}VCI{2:D3}O{3:D3}", PureLinkDevice.StartChar, _deviceId, inputToRoute, Index));
                        break;
                }
            }

            if (_requestedRoutedAudio > 0)
            {
                var inputToRoute = _requestedRoutedAudio == 999 ? 0 : _requestedRoutedAudio;
                _requestedRoutedAudio = 0;
                cmd.Append(",");
                switch (_deviceModel)
                {
                    case 0:
                        cmd.Append(string.Format("{0}{1}ACI{2:D2}O{3:D2}", PureLinkDevice.StartChar, _deviceId, inputToRoute, Index));
                        break;
                    case 1:
                        cmd.Append(string.Format("{0}{1}ACI{2:D3}O{3:D3}", PureLinkDevice.StartChar, _deviceId, inputToRoute, Index));
                        break;
                }
            }

            return cmd.ToString();
        }
    }
}