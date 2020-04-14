using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNProductSync.Models.SQLServer
{
    public class tbl_Category
    {
        public int ID { get; set; }
        public string CategoryName { get; set; }
        public string CategoryDescription { get; set; }
        public Nullable<int> CategoryLevel { get; set; }
        public Nullable<int> ParentID { get; set; }
        public Nullable<bool> IsHidden { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public string Slug { get; set; }
        public string Icon { get; set; }
    }
}
