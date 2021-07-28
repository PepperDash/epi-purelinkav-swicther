using System;
using PepperDash.Core;

namespace PureLinkPlugin
{
    public class PurelinkResponseProcessor 
    {
        private PureLinkDevice _pureLinkDevice;

        public PurelinkResponseProcessor(PureLinkDevice pureLinkDevice)
        {
            _pureLinkDevice = pureLinkDevice;
        }

        public void ProcessResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return;

            if (CheckResponseForError(response))
                return;

            try
            {
                Debug.Console(2, _pureLinkDevice, "Processing response : {0}", response);

                if (response.Contains("VERSION"))
                    return;

                if (response.StartsWith(_pureLinkDevice.VideoResponseStart))
                {
                    ProcessVideoResponse(response);
                    return;
                }

                if (response.StartsWith(_pureLinkDevice.AudioResponseStart))
                {
                    ProcessAudioResponse(response);
                    return;
                }

                if (response.StartsWith(_pureLinkDevice.VideoPollResponseStart))
                {
                    ProcessVideoPollResponse(response);
                    return;
                }

                if (response.StartsWith(_pureLinkDevice.AudioPollResponseStart))
                {
                    ProcessAudioPollResponse(response);
                    return;
                }

                if (response.StartsWith(_pureLinkDevice.AudioVideoResponseStart))
                {
                    ProcessAudioVideoResponse(response);
                    return;
                }

                Debug.Console(2, _pureLinkDevice, "Not sure what to do with this {0}", response);
            }
            catch (FormatException ex)
            {
                Debug.Console(0,
                    _pureLinkDevice,
                    Debug.ErrorLogLevel.Notice,
                    "Caught an error processing the response:{0} {1}{2}",
                    response, ex.Message, ex.InnerException);
            }
            catch (IndexOutOfRangeException ex)
            {
                Debug.Console(0,
                    _pureLinkDevice,
                    Debug.ErrorLogLevel.Notice,
                    "Caught an error processing the response:{0} {1}{2}",
                    response, ex.Message, ex.InnerException);
            }
            catch (Exception ex)
            {
                Debug.Console(0,
                    _pureLinkDevice,
                    Debug.ErrorLogLevel.Notice,
                    "Caught an error processing the response:{0} {1}{2}",
                    response, ex.Message, ex.InnerException);
            }
        }

        private void ProcessAudioVideoResponse(string response)
        {

            var audioVideoResponses = response.Replace(_pureLinkDevice.AudioVideoResponseStart, string.Empty).Split(new[] { ',' });
            foreach (var audioVideoResponse in audioVideoResponses)
            {
                var responseToProcess = audioVideoResponse.Replace("I", string.Empty).Split(new[] { 'O' });
                var outputIndex = Convert.ToUInt32(responseToProcess[1]);
                PureLinkOutput output;
                if (!_pureLinkDevice.Outputs.TryGetValue(outputIndex, out output))
                    return;

                var inputIndex = Convert.ToInt32(responseToProcess[0]);
                output.UpdateCurrentVideoInput(inputIndex);
                output.UpdateCurrentAudioInput(inputIndex);
            }
        }

        private void ProcessAudioPollResponse(string response)
        {
            var audioPollResponses = response.Replace(_pureLinkDevice.AudioPollResponseStart, string.Empty).Split(new[] {','});

            foreach (var audioPollResponse in audioPollResponses)
            {
                var responseToProcess = audioPollResponse.Replace("I", string.Empty).Split(new[] {'O'});
                var outputIndex = Convert.ToUInt32(responseToProcess[1]);
                PureLinkOutput output;
                if (!_pureLinkDevice.Outputs.TryGetValue(outputIndex, out output))
                    continue;

                var inputIndex = Convert.ToInt32(responseToProcess[0]);
                output.UpdateCurrentAudioInput(inputIndex);
            }
        }

        private void ProcessVideoPollResponse(string response)
        {
            var videoPollResponses = response.Replace(_pureLinkDevice.VideoPollResponseStart, string.Empty).Split(new[] {','});

            foreach (var videoPollResponse in videoPollResponses)
            {
                var responseToProcess = videoPollResponse.Replace("I", string.Empty).Split(new[] { 'O' });
                var outputIndex = Convert.ToUInt32(responseToProcess[1]);
                PureLinkOutput output;
                if (!_pureLinkDevice.Outputs.TryGetValue(outputIndex, out output))
                    continue;

                var inputIndex = Convert.ToInt32(responseToProcess[0]);
                output.UpdateCurrentVideoInput(inputIndex);
            }
        }

        private void ProcessAudioResponse(string response)
        {
            var audioResponses = response.Replace(_pureLinkDevice.AudioResponseStart, string.Empty).Split(new[] {','});
            foreach (var audioResponse in audioResponses)
            {
                var responseToProcess = audioResponse.Replace("I", string.Empty).Split(new[] { 'O' });
                var outputIndex = Convert.ToUInt32(responseToProcess[1]);
                PureLinkOutput output;
                if (!_pureLinkDevice.Outputs.TryGetValue(outputIndex, out output))
                    continue;

                var inputIndex = Convert.ToInt32(responseToProcess[0]);
                output.UpdateCurrentAudioInput(inputIndex);
            }
        }

        private void ProcessVideoResponse(string response)
        {
            var videoResponses = response.Replace(_pureLinkDevice.VideoResponseStart, string.Empty).Split(new[] {','});
            foreach (var videoPollResponse in videoResponses)
            {
                var responseToProcess = videoPollResponse.Replace("I", string.Empty).Split(new[] { 'O' });
                var outputIndex = Convert.ToUInt32(responseToProcess[1]);
                PureLinkOutput output;
                if (!_pureLinkDevice.Outputs.TryGetValue(outputIndex, out output))
                    continue;

                var inputIndex = Convert.ToInt32(responseToProcess[0]);
                output.UpdateCurrentVideoInput(inputIndex);
            }
        }

        private bool CheckResponseForError(string data)
        {
            data = data.ToLower();
            if (data.ToLower().Contains("command code error"))
            {
                Debug.Console(0, _pureLinkDevice, Debug.ErrorLogLevel.Error, "ProcessResponse: Command Code Error");
                return true;
            }

            if (data.ToLower().Contains("router id error"))
            {
                Debug.Console(0, _pureLinkDevice, Debug.ErrorLogLevel.Error, "ProcessResponse: Router ID Error");
                return true;
            }

            return false;
        }
    }
}