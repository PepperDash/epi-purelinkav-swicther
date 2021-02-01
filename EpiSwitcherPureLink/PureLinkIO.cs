using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;

namespace PureLinkPlugin
{
    public abstract class PureLinkIo : IKeyName
    {
        protected PureLinkIo(string key, int index, PureLinkEntryConfig config)
        {
            Key = key;
            Name = config.Name;
            Index = index;
            VideoName = !String.IsNullOrEmpty(config.VideoName) ? config.VideoName : Name;
            AudioName = !String.IsNullOrEmpty(config.AudioName) ? config.AudioName : Name;
        }

        public string Key { get; private set; }
        public string Name { get; private set; }
        public string VideoName { get; private set; }
        public string AudioName { get; private set; }
        public int Index { get; private set; }
    }

    public class PureLinkInput : PureLinkIo
    {
        public PureLinkInput(string key, int index, PureLinkEntryConfig config) 
            : base(key, index, config)
        {
            
        }
    }
}