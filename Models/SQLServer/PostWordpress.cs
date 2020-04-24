﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models.SQLServer
{
    public class PostWordpress
    {
        public int ID { get; set; }

        public int PostPublicID { get; set; }

        public string WebWordpress { get; set; }

        public int PostWordpressID { get; set; }

        public Nullable<int> CategoryID { get; set; }

        public string Title { get; set; }

        public string Summary { get; set; }

        public string Content { get; set; }

        public string Thumbnail { get; set; }

        public Nullable<System.DateTime> CreatedDate { get; set; }

        public string CreatedBy { get; set; }

        public Nullable<System.DateTime> ModifiedDate { get; set; }

        public string ModifiedBy { get; set; }
    }
}
