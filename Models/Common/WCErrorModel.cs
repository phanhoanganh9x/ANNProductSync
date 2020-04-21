using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNProductSync.Models
{
    public class WCErrorDataModel
    {
        public int status { get; set; }
        public int? resource_id { get; set; }
    }

    public class WCErrorModel
    {
        public string code { get; set; }
        public string message { get; set; }
        public WCErrorDataModel data { get; set; }
    }
}
