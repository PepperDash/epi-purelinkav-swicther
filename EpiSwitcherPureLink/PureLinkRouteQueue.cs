using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core.Queues;

namespace PureLinkPlugin
{
    /// <summary>
    /// 
    /// </summary>
    public class PureLinkRouteQueue
    {
        private readonly IBasicCommunication _coms;
        private readonly IEnumerable<PureLinkOutput> _outputs;
        private readonly IQueue<IQueueMessage> _queue;
        private bool _allowAudioRouting;
        private bool _allowVideoRouting;
        private bool _audioFollowVideo;

        private readonly CCriticalSection _audioRouteLock = new CCriticalSection();
        private readonly CCriticalSection _videoRouteLock = new CCriticalSection();

        private readonly CTimer _videoRouteTimer;
        private readonly CTimer _audioRouteTimer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandQueue"></param>
        /// <param name="outputs"></param>
        /// <param name="coms"></param>
        public PureLinkRouteQueue(IQueue<IQueueMessage> commandQueue, IEnumerable<PureLinkOutput> outputs,
                                  IBasicCommunication coms)
        {
            _queue = commandQueue;
            _outputs = outputs;
            _coms = coms;

            _videoRouteTimer = new CTimer(o => _allowVideoRouting = false, Timeout.Infinite);
            _audioRouteTimer = new CTimer(o => _allowAudioRouting = false, Timeout.Infinite);
        }

        public bool AllowAudioRouting
        {
            get { return _allowAudioRouting; }
            set
            {
                try
                {
                    _audioRouteLock.Enter();
                    if (!value)
                    {
                        _audioRouteTimer.Reset(1000);
                        return;
                    }

                    _audioRouteTimer.Stop();
                    _allowAudioRouting = true;
                    ProcessAudioOutputsForRoutes();
                }
                finally
                {
                    _audioRouteLock.Leave();
                }
            }
        }

        public bool AllowVideoRouting
        {
            get { return _allowVideoRouting; }
            set
            {
                try
                {
                    _videoRouteLock.Enter();
                    if (!value)
                    {
                        _videoRouteTimer.Reset(1000);
                        return;
                    }

                    _videoRouteTimer.Stop();
                    _allowVideoRouting = true;
                    ProcessVideoOutputsForRoutes();
                }
                finally
                {
                    _videoRouteLock.Leave();
                }
            }
        }

        public bool AudioFollowVideo
        {
            get { return _audioFollowVideo; }
            set { _audioFollowVideo = value; }
        }

        public void EnqueueAudioOutputForRoute(PureLinkOutput output)
        {
            if (_allowAudioRouting)
                ProcessAudioRoute(output);
        }

        public void EnqueueVideoOutputForRoute(PureLinkOutput output)
        {
            if (_allowVideoRouting)
                ProcessVideoRoute(output);
        }

        private void ProcessAudioOutputsForRoutes()
        {
            if (AudioFollowVideo)
                return;

            foreach (var output in _outputs.Where(x => x.AudioRouteRequested))
                ProcessAudioRoute(output);
        }

        private void ProcessAudioRoute(PureLinkOutput output)
        {
            if (!output.AudioRouteRequested || AudioFollowVideo)
                return;

            var command = output.GetRequestedAudioCommand();
            if (String.IsNullOrEmpty(command))
                return;

            var message = new PureLinkMessage(_coms, command);
            _queue.Enqueue(message);
        }

        private void ProcessVideoOutputsForRoutes()
        {
            foreach (var output in _outputs.Where(x => x.VideoRouteRequested))
                ProcessVideoRoute(output);
        }

        private void ProcessVideoRoute(PureLinkOutput output)
        {
            if (!output.VideoRouteRequested)
                return;

            var command = _audioFollowVideo ? output.GetRequestedAudioVideoRouteCommand() : output.GetRequestedVideoCommand();

            if (String.IsNullOrEmpty(command))
                return;

            var message = new PureLinkMessage(_coms, command);
            _queue.Enqueue(message);
        }
    }
}