using ANNwpsync.Models;
using ANNwpsync.Models.SQLServer;
using ANNwpsync.Services.FactoryPattern;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Services
{
    public class StockService: IANNService
    {
        public List<StockModel> getQuantities(List<tbl_StockManager> stock)
        {
            var result = stock
                    .Select(x => new
                    {
                        parentID = x.ParentID.Value,
                        productID = x.ProductID.Value,
                        productVariableID = x.ProductVariableID.Value,
                        createDate = x.CreatedDate
                    })
                    .GroupBy(x => new { x.parentID, x.productID, x.productVariableID })
                    .Select(g => new
                    {
                        parentID = g.Key.parentID,
                        productID = g.Key.productID,
                        productVariableID = g.Key.productVariableID,
                        doneAt = g.Max(x => x.createDate)
                    })
                    .Join(
                        stock,
                        last => new
                        {
                            parentID = last.parentID,
                            productID = last.productID,
                            productVariableID = last.productVariableID,
                            doneAt = last.doneAt
                        },
                        rec => new
                        {
                            parentID = rec.ParentID.Value,
                            productID = rec.ProductID.Value,
                            productVariableID = rec.ProductVariableID.Value,
                            doneAt = rec.CreatedDate
                        },
                        (last, rec) => new
                        {
                            parentID = last.parentID,
                            quantity = rec.Quantity.HasValue ? rec.Quantity.Value : 0,
                            quantityCurrent = rec.QuantityCurrent.HasValue ? rec.QuantityCurrent.Value : 0,
                            type = rec.Type.HasValue ? rec.Type.Value : 0
                        }
                    )
                    .Select(x =>
                    {
                        var calQuantity = 0.0;
                        switch (x.type)
                        {
                            case 1:
                                calQuantity = x.quantityCurrent + x.quantity;
                                break;
                            case 2:
                                calQuantity = x.quantityCurrent - x.quantity;
                                break;
                            default:
                                calQuantity = 0;
                                break;
                        }

                        return new
                        {
                            parentID = x.parentID,
                            calQuantity = calQuantity
                        };
                    })
                    .GroupBy(x => x.parentID)
                    .Select(g => new StockModel()
                    {
                        productID = g.Key,
                        quantity = g.Sum(x => Convert.ToInt32(x.calQuantity)),
                        availability = g.Sum(x => x.calQuantity) > 0 ? true : false
                    })
                    .OrderBy(x => x.productID)
                    .ToList();

            return result;
        }

        public List<StockProductVariableModel> getProductVariableQuantities(List<tbl_StockManager> stock)
        {
            var result = stock
                    .Select(x => new
                    {
                        parentID = x.ParentID.Value,
                        productID = x.ProductID.Value,
                        productVariableID = x.ProductVariableID.Value,
                        sku = x.SKU,
                        createDate = x.CreatedDate
                    })
                    .GroupBy(x => new { x.parentID, x.productID, x.productVariableID, x.sku })
                    .Select(g => new
                    {
                        parentID = g.Key.parentID,
                        productID = g.Key.productID,
                        productVariableID = g.Key.productVariableID,
                        sku = g.Key.sku,
                        doneAt = g.Max(x => x.createDate)
                    })
                    .Join(
                        stock,
                        last => new
                        {
                            parentID = last.parentID,
                            productID = last.productID,
                            productVariableID = last.productVariableID,
                            sku = last.sku,
                            doneAt = last.doneAt
                        },
                        rec => new
                        {
                            parentID = rec.ParentID.Value,
                            productID = rec.ProductID.Value,
                            productVariableID = rec.ProductVariableID.Value,
                            sku = rec.SKU,
                            doneAt = rec.CreatedDate
                        },
                        (last, rec) => new
                        {
                            parentID = last.parentID,
                            productVariableID = last.productVariableID,
                            sku = last.sku,
                            quantity = rec.Quantity.HasValue ? rec.Quantity.Value : 0,
                            quantityCurrent = rec.QuantityCurrent.HasValue ? rec.QuantityCurrent.Value : 0,
                            type = rec.Type.HasValue ? rec.Type.Value : 0
                        }
                    )
                    .Select(x =>
                    {
                        var calQuantity = 0.0;
                        switch (x.type)
                        {
                            case 1:
                                calQuantity = x.quantityCurrent + x.quantity;
                                break;
                            case 2:
                                calQuantity = x.quantityCurrent - x.quantity;
                                break;
                            default:
                                calQuantity = 0;
                                break;
                        }

                        return new
                        {
                            parentID = x.parentID,
                            productVariableID = x.productVariableID,
                            sku = x.sku,
                            calQuantity = calQuantity
                        };
                    })
                    .GroupBy(x => new { x.parentID, x.productVariableID, x.sku })
                    .Select(g => new StockProductVariableModel()
                    {
                        productID = g.Key.parentID,
                        productVariableID = g.Key.productVariableID,
                        sku = g.Key.sku,
                        quantity = g.Sum(x => Convert.ToInt32(x.calQuantity)),
                        availability = g.Sum(x => x.calQuantity) > 0 ? true : false
                    })
                    .OrderBy(x => x.productID)
                    .ToList();

            return result;
        }
    }
}
