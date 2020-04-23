using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Models.SQLServer
{
    public class SQLServerContext: DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // https://stackoverflow.com/questions/45796776/get-connectionstring-from-appsettings-json-instead-of-being-hardcoded-in-net-co
            IConfigurationRoot configuration = new ConfigurationBuilder()
                  .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                  .AddJsonFile("appsettings.json")
                  .Build();
            options.UseSqlServer(configuration.GetConnectionString("SQLServer"));
        }

        
        public DbSet<tbl_Category> tbl_Category { get; set; }
        public DbSet<tbl_Product> tbl_Product { get; set; }
        public DbSet<tbl_ProductVariable> tbl_ProductVariable { get; set; }
        public DbSet<tbl_ProductImage> tbl_ProductImage { get; set; }
        public DbSet<tbl_ProductVariableValue> tbl_ProductVariableValue { get; set; }
        public DbSet<tbl_VariableValue> tbl_VariableValue { get; set; }
        public DbSet<tbl_StockManager> tbl_StockManager { get; set; }
        public DbSet<Tag> Tag { get; set; }
        public DbSet<ProductTag> ProductTag { get; set; }

    }
}
