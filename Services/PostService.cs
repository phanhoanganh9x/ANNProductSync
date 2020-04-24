using ANNwpsync.Models;
using ANNwpsync.Models.Common;
using ANNwpsync.Models.SQLServer;
using ANNwpsync.Services.FactoryPattern;
using ANNwpsync.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNwpsync.Services
{
    public class PostService: IANNService
    {


        #region Lấy thông tin bài viết

        #region Lấy thông tin bài viết app theo ID
        /// <summary>
        /// Lấy thông tin bài viết app theo ID
        /// </summary>
        /// <param name="postID"></param>
        /// <returns></returns>
        public PostPublicModel getPostPublicByID(int postPublicID)
        {
            using (var con = new SQLServerContext())
            {
                // Kiểm tra có bài viết không
                var data = con.PostPublic.Where(x => x.ID == postPublicID);

                if (data.FirstOrDefault() == null)
                    return null;

                // Lấy ID bài viết
                var id = data.FirstOrDefault().ID;

                // Xuất thông tin cơ bản của sản phẩm
                var posts = data.Where(x => x.CategoryID != 0)
                    .Join(
                        con.PostPublicCategory,
                        post => post.CategoryID,
                        cat => cat.ID,
                        (p, c) => new PostPublicModel
                        {
                            id = p.ID,
                            categoryID = p.CategoryID,
                            categoryName = c.Name,
                            title = p.Title,
                            content = p.Content,
                            thumbnail = p.Thumbnail,
                            summary = p.Summary,
                            createdDate = p.CreatedDate,
                            modifiedDate = p.ModifiedDate
                        }
                    )
                    .OrderBy(x => x.id)
                    .ToList();

                

                return posts.FirstOrDefault();
            }
        }
        #endregion
        #region Lấy thông tin bài viết wordpress theo ID
        /// <summary>
        /// Lấy thông tin bài viết app theo ID
        /// </summary>
        /// <param name="postPublicID"></param>
        /// <returns></returns>
        public PostWordpress getPostWordpressByPostPublicID(int postPublicID, string webWordpress)
        {
            using (var con = new SQLServerContext())
            {
                // Kiểm tra có bài viết không
                var data = con.PostWordpress.Where(x => x.PostPublicID == postPublicID && x.WebWordpress == webWordpress);

                if (data.FirstOrDefault() == null)
                    return null;

                //// Lấy ID bài viết
                //var id = data.FirstOrDefault().ID;

                //// Xuất thông tin cơ bản của sản phẩm
                //var posts = data.Where(x => x.CategoryID != 0)
                //    .Join(
                //        con.PostPublicCategory,
                //        post => post.CategoryID,
                //        cat => cat.ID,
                //        (p, c) => new PostWordpressModel
                //        {
                //            id = p.ID,
                //            postPublicID = p.PostPublicID,
                //            webWordpress = p.WebWordpress,
                //            postWordpressID = p.PostWordpressID,
                //            categoryID = p.CategoryID,
                //            categoryName = c.Name,
                //            title = p.Title,
                //            content = p.Content,
                //            thumbnail = p.Thumbnail,
                //            summary = p.Summary,
                //            createdDate = p.CreatedDate,
                //            modifiedDate = p.ModifiedDate
                //        }
                //    )
                //    .OrderBy(x => x.id)
                //    .ToList();



                return data.FirstOrDefault();
            }
        }
        #endregion
        #endregion
    }
}
