﻿// Generated by Xamasoft JSON Class Generator
// http://www.xamasoft.com/json-class-generator

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Cryptsy.SampleResponse1JsonTypes;

namespace Cryptsy.SampleResponse1JsonTypes
{

    internal class DBLLTC
    {

        [JsonProperty("marketid")]
        public string Marketid { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("lasttradeprice")]
        public string Lasttradeprice { get; set; }

        [JsonProperty("volume")]
        public string Volume { get; set; }

        [JsonProperty("lasttradetime")]
        public string Lasttradetime { get; set; }

        [JsonProperty("primaryname")]
        public string Primaryname { get; set; }

        [JsonProperty("primarycode")]
        public string Primarycode { get; set; }

        [JsonProperty("secondaryname")]
        public string Secondaryname { get; set; }

        [JsonProperty("secondarycode")]
        public string Secondarycode { get; set; }

        [JsonProperty("recenttrades")]
        public Recenttrade14[] Recenttrades { get; set; }

        [JsonProperty("sellorders")]
        public Sellorder14[] Sellorders { get; set; }

        [JsonProperty("buyorders")]
        public Buyorder13[] Buyorders { get; set; }
    }

}
