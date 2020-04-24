using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models.SQLServer
{
    public class WebWordpress
    {
        public int id { get; set; }

        public string web { get; set; }

        public string wpKey { get; set; }

        public string wpSecret { get; set; }

        public string wpToken { get; set; }

        public string wpTokenSecret { get; set; }

        public string wcKey { get; set; }

        public string wcSecret { get; set; }

        public string wcPriceType { get; set; }

        public bool active { get; set; }
    }
}
