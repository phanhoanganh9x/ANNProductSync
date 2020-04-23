using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models.SQLServer
{
    public class tbl_ProductVariable
    {
        public int ID { get; set; }
        public Nullable<int> ProductID { get; set; }
        public string ParentSKU { get; set; }
        public string SKU { get; set; }
        public Nullable<double> Stock { get; set; }
        public Nullable<int> StockStatus { get; set; }
        public Nullable<double> Regular_Price { get; set; }
        public Nullable<double> CostOfGood { get; set; }
        public string Image { get; set; }
        public Nullable<bool> ManageStock { get; set; }
        public Nullable<bool> IsHidden { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public string color { get; set; }
        public string size { get; set; }
        public Nullable<double> RetailPrice { get; set; }
        public Nullable<double> MinimumInventoryLevel { get; set; }
        public Nullable<double> MaximumInventoryLevel { get; set; }
        public Nullable<int> SupplierID { get; set; }
        public string SupplierName { get; set; }
    }
}
