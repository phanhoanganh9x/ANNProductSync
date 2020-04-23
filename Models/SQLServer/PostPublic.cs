using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models.SQLServer
{
    public class PostPublic
    {
        public int ID { get; set; }
        public int CategoryID { get; set; }
        public string CategorySlug { get; set; }
        public string Title { get; set; }
        public string Thumbnail { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public string Action { get; set; }
        public string ActionValue { get; set; }
        public bool AtHome { get; set; }
        public bool IsPolicy { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public System.DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }
}
