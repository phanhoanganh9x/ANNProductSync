using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models.SQLServer
{
    public class tbl_VariableValue
    {
        public int ID { get; set; }
        public Nullable<int> VariableID { get; set; }
        public string VariableName { get; set; }
        public string VariableValue { get; set; }
        public Nullable<bool> IsHidden { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public string VariableValueText { get; set; }
        public string SKUText { get; set; }
    }
}
