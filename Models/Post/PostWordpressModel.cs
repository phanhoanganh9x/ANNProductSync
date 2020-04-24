using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models
{
    public class PostWordpressModel
    {
        public int id { get; set; }

        public int postPublicID { get; set; }

        public string webWordpress { get; set; }

        public int postWordpressID { get; set; }

        public Nullable<int> categoryID { get; set; }
        public string categoryName { get; set; }
        public string title { get; set; }

        public string summary { get; set; }

        public string content { get; set; }

        public string thumbnail { get; set; }

        public Nullable<System.DateTime> createdDate { get; set; }

        public string createdBy { get; set; }

        public Nullable<System.DateTime> modifiedDate { get; set; }

        public string modifiedBy { get; set; }
    }
}
