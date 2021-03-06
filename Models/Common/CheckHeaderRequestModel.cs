﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models
{
    public class CheckHeaderRequestModel
    {
        public string domain { get; set; }
        public int statusCode { get; set; }
        public bool success { get; set; }
        public string message { get; set; }
        public WooCommerce wc { get; set; }
        public Wordpress wp { get; set; }
        public string priceType { get; set; }
        public string rootPath { get; set; }
        public string mainDomain { get; set; }
    }
}
