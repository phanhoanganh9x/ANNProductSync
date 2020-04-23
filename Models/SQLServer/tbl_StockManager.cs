using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models.SQLServer
{
    public class tbl_StockManager
    {
        public int ID { get; set; }
        public Nullable<int> AgentID { get; set; }
        public Nullable<int> ProductID { get; set; }
        public Nullable<int> ProductVariableID { get; set; }
        public Nullable<double> Quantity { get; set; }
        public Nullable<double> QuantityCurrent { get; set; }
        public Nullable<int> Type { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public string NoteID { get; set; }
        public Nullable<int> OrderID { get; set; }
        public Nullable<int> Status { get; set; }
        public string SKU { get; set; }
        public Nullable<int> MoveProID { get; set; }
        public Nullable<int> ParentID { get; set; }
    }
}
