using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models.SQLServer
{
    public class PostVideo
    {
        public Int64 Id { get; set; }
        public string VideoId { get; set; }
        public Nullable<int> PostId { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
    }
}
