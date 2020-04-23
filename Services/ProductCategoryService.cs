using ANNwpsync.Models;
using ANNwpsync.Models.SQLServer;
using ANNwpsync.Services.FactoryPattern;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Services
{
    public class ProductCategoryService: IANNService
    {
        #region Lấy danh sách dạnh mục con dựa theo danh mục cha
        /// <summary>
        /// Thực thi đệ quy để lấy tất cả category theo nhánh parent
        /// </summary>
        /// <param name="con"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private List<ProductCategoryModel> getCategoryChild(SQLServerContext con, ProductCategoryModel parent)
        {
            var result = new List<ProductCategoryModel>();
            result.Add(parent);

            var child = con.tbl_Category
                .Where(x => x.ParentID.Value == parent.id)
                .Select(x => new ProductCategoryModel()
                {
                    id = x.ID,
                    name = x.CategoryName,
                    description = x.CategoryDescription,
                    slug = x.Slug
                })
                .ToList();

            if (child.Count > 0)
            {
                foreach (var id in child)
                {
                    result.AddRange(getCategoryChild(con, id));
                }
            }

            return result;
        }

        /// <summary>
        /// Tìm các category thuộc nhánh slug
        /// </summary>
        /// <param name="slug"></param>
        /// <returns></returns>
        public List<ProductCategoryModel> getCategoryChild(string slug)
        {
            using (var con = new SQLServerContext())
            {
                var parent = con.tbl_Category
                    .Where(x =>
                        (!String.IsNullOrEmpty(slug) && x.Slug == slug) ||
                        (String.IsNullOrEmpty(slug) && x.CategoryLevel == 0)
                    )
                    .Select(x => new ProductCategoryModel()
                    {
                        id = x.ID,
                        name = x.CategoryName,
                        description = x.CategoryDescription,
                        slug = x.Slug
                    })
                    .FirstOrDefault();
                if (parent != null)
                    return getCategoryChild(con, parent);
                else
                    return null;
            }
        }
        #endregion

        #region Lấy thông tin về danh mục
        /// <summary>
        /// Lấy thông tin category theo slug
        /// </summary>
        /// <param name="slug"></param>
        /// <returns></returns>
        public ProductCategoryModel getCategoryBySlug(string slug)
        {
            using (var con = new SQLServerContext())
            {
                var parent = con.tbl_Category
                    .Where(x =>
                        (!String.IsNullOrEmpty(slug) && x.Slug == slug) ||
                        (String.IsNullOrEmpty(slug) && x.CategoryLevel == 0)
                    )
                    .Select(x => new ProductCategoryModel()
                    {
                        id = x.ID,
                        name = x.CategoryName,
                        slug = x.Slug,
                        description = x.CategoryDescription
                    })
                    .FirstOrDefault();

                return parent;
            }
        }
        #endregion
    }
}
