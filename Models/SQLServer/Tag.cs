using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models.SQLServer
{
    public class Tag
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public int CreatedBy { get; set; }
        public System.DateTime CreatedDate { get; set; }
    }
}
