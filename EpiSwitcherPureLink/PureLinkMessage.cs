using System;
using System.Text;
using PepperDash.Core;
using PepperDash_Essentials_Core.Queues;

namespace PureLinkPlugin
{
    public class PureLinkMessage : IQueueMessage
    {
        private readonly IBasicCommunication _coms;
        private readonly string _message = String.Empty;

        public PureLinkMessage(IBasicCommunication coms, string message)
        {
            _coms = coms;

            if (String.IsNullOrEmpty(message))
                return;

            var textToSend = new StringBuilder(message);
            textToSend.Append("!\r");

            _message = textToSend.ToString();
        }

        public void Dispatch()
        {
            if (String.IsNullOrEmpty(_message))
                return;

            _coms.SendText(_message);
        }

        public override string ToString()
        {
            return _message.Trim();
        }
    }
}