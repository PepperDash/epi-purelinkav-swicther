using System;
using System.Text;
using PepperDash.Core;
using PepperDash_Essentials_Core.Queues;

namespace PureLinkPlugin
{
    /// <summary>
    /// Queue Message for sending messages to the PureLink Switcher
    /// </summary>
    public class PureLinkMessage : IQueueMessage
    {
        private readonly IBasicCommunication _coms;
        private readonly string _message = String.Empty;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="coms">coms to send message</param>
        /// <param name="message">unformatted message to send</param>
        public PureLinkMessage(IBasicCommunication coms, string message)
        {
            _coms = coms;

            if (String.IsNullOrEmpty(message))
                return;

            _message = BuildCommandFromString(message);
        }

        /// <summary>
        /// dispatches ths message
        /// </summary>
        public void Dispatch()
        {
            if (String.IsNullOrEmpty(_message))
                return;

            _coms.SendText(_message);
        }

        /// <summary>
        /// To String Override
        /// </summary>
        /// <returns>returns the message to be sent</returns>
        public override string ToString()
        {
            return _message.Trim();
        }


        /// <summary>
        /// Formats a string properly to send
        /// </summary>
        /// <param name="command">command to send</param>
        /// <returns>formatted command</returns>
        /// <exception cref="ArgumentNullException">if the command is null</exception>
        public static string BuildCommandFromString(string command)
        {
            if (String.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");

            var textToSend = new StringBuilder(command);
            textToSend.Append("!\r");

            return textToSend.ToString();
        }
    }
}