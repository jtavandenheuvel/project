﻿// Generated by Xamasoft JSON Class Generator
// http://www.xamasoft.com/json-class-generator

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MarketOrders.ordersJsonTypes;

namespace MarketOrders.ordersJsonTypes
{

    internal class Return
    {

        [JsonProperty("sellorders")]
        public Sellorder[] Sellorders { get; set; }

        [JsonProperty("buyorders")]
        public Buyorder[] Buyorders { get; set; }
    }

}
