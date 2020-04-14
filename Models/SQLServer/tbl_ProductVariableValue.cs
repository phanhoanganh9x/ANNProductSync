using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNProductSync.Models.SQLServer
{
    public class tbl_ProductVariableValue
    {
        public int ID { get; set; }
        public Nullable<int> ProductVariableID { get; set; }
        public string ProductvariableSKU { get; set; }
        public Nullable<int> VariableValueID { get; set; }
        public string VariableName { get; set; }
        public string VariableValue { get; set; }
        public Nullable<bool> IsHidden { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
    }
}
