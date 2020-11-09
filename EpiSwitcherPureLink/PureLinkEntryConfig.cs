using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;

namespace EpiSwitcherPureLink
{
    public class PureLinkEntryConfig
    {
        [JsonProperty("name")]
        public string name { get; set; }
    }
}