using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models
{
    public class PostPublicModel
    {
        public int id { get; set; }

        public int categoryID { get; set; }

        public string categoryName { get; set; }

        public string title { get; set; }

        public string thumbnail { get; set; }

        public string summary { get; set; }

        public string content { get; set; }

        public System.DateTime createdDate { get; set; }

        public System.DateTime modifiedDate { get; set; }
    }
}
