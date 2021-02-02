using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;

namespace PureLinkPlugin
{
    /// <summary>
    /// Abstract class representing a PureLink IO
    /// </summary>
    public abstract class PureLinkIo : IKeyName
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="index">io number</param>
        /// <param name="config">config</param>
        protected PureLinkIo(string key, uint index, PureLinkEntryConfig config)
        {
            Key = key;
            Name = config.Name;
            Index = index;
            VideoName = !String.IsNullOrEmpty(config.VideoName) ? config.VideoName : Name;
            AudioName = !String.IsNullOrEmpty(config.AudioName) ? config.AudioName : Name;
        }

        public string Key { get; private set; }
        public string Name { get; private set; }

        /// <summary>
        /// Video Name
        /// </summary>
        public string VideoName { get; private set; }

        /// <summary>
        /// Audio Name
        /// </summary>
        public string AudioName { get; private set; }

        /// <summary>
        /// IO Number
        /// </summary>
        public uint Index { get; private set; }
    }

    /// <summary>
    /// Class representing a PureLink input
    /// </summary>
    public class PureLinkInput : PureLinkIo
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="index">io number</param>
        /// <param name="config">config</param>
        public PureLinkInput(string key, uint index, PureLinkEntryConfig config) 
            : base(key, index, config)
        {
            
        }
    }
}