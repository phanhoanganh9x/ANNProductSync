using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNProductSync.Models.SQLServer
{
    public class PostCategory
    {
        public int ID { get; set; }
        public string Icon { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public int Level { get; set; }
        public int ParentID { get; set; }
        public bool Hidden { get; set; }
        public bool AtPost { get; set; }
        public int IndexPost { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public System.DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }
}
