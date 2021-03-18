using ANNwpsync.Models;
using ANNwpsync.Models.SQLServer;
using ANNwpsync.Services;
using ANNwpsync.Services.FactoryPattern;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WooCommerce.NET.WordPress.v2;
using WooCommerceNET;

namespace ANNwpsync.Controllers
{
    [ApiController]
    [Route("api/v1/wordpress")]
    public class PostController : ControllerBase
    {
        #region Parameters
        private readonly ILogger<PostController> _logger;
        private readonly PostService _service;
        #endregion

        public PostController(ILogger<PostController> logger)
        {
            _logger = logger;
            _service = ANNFactoryService.getInstance<PostService>();
        }

        #region Private
        private CheckHeaderRequestModel _checkHeaderRequest(IHeaderDictionary headers)
        {
            var result = new CheckHeaderRequestModel();

            if (!headers.ContainsKey("domain"))
            {
                result.statusCode = StatusCodes.Status400BadRequest;
                result.success = false;
                result.message = "Thiếu domain";

                return result;
            }

            var configuration = new ConfigurationBuilder()
                 .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                 .AddJsonFile("appsettings.json")
                 .Build();

            var domain = headers.Where(x => x.Key == "domain").Select(x => x.Value).FirstOrDefault();
            var domainSetting = configuration.GetSection(domain).Get<DomainSettingModel>();

            var ANNconfig = configuration.GetSection("ANNconfig").Get<ANNConfigModel>();

            if (String.IsNullOrEmpty(domain))
            {
                result.statusCode = StatusCodes.Status400BadRequest;
                result.success = false;
                result.message = "Domain không được rỗng";

                return result;
            }
            if (domainSetting == null)
            {
                result.statusCode = StatusCodes.Status500InternalServerError;
                result.success = false;
                result.message = String.Format("{0} chưa được cài đặt", domain);

                return result;
            }

            RestAPI restAPI = new RestAPI(String.Format("https://{0}/wp-json/jwt-auth/v1/token", domain), domainSetting.username, domainSetting.password);

            //RestAPI restAPI = new RestAPI(String.Format("https://{0}/wp-json/wp/v2/", domain), domainSetting.wordpress_key, domainSetting.wordpress_secret, false);
            //restAPI.oauth_token = domainSetting.wordpress_oauth_token;
            //restAPI.oauth_token_secret = domainSetting.wordpress_oauth_token_secret;

            WPObject wpObject = new WPObject(restAPI);

            result.domain = domain;
            result.success = true;
            result.message = String.Empty;
            result.rootPath = ANNconfig.root_path;
            result.wp = new Models.Wordpress()
            {
                restAPI = restAPI,
                wpObject = wpObject
            };
            return result;
        }
        private async Task<Posts> _handleWPPost(PostClone postClone, WPObject wpObject, string domain, string rootFolder)
        {
            #region Category List
            var categories = new List<int>();
            var wpPostCategory = await wpObject.Categories.GetAll(new Dictionary<string, string>() {
                { "per_page", "100" } });
            if (wpPostCategory.Count > 0)
            {
                int wpPostCategoryID = wpPostCategory.Where(x => x.name == postClone.CategoryName).Select(x => x.id).FirstOrDefault();
                if (wpPostCategoryID > 0)
                {
                    categories.Add(wpPostCategoryID);
                }
                else
                {
                    Categories newCategory = new Categories()
                    {
                        name = postClone.CategoryName,
                        description = postClone.CategoryName,
                        parent = 0,
                    };
                    var createCategory = await wpObject.Categories.Add(newCategory);
                    categories.Add(createCategory.id);
                }
            }
            #endregion

            #region Thumbnail
            int featured_media = 0;
            Media wpPostThumbnail = new Media();
            if (!String.IsNullOrEmpty(postClone.Thumbnail))
            {
                string thumbnailFileName = Path.GetFileName(postClone.Thumbnail);
                string filePath = rootFolder + postClone.Thumbnail.Replace("/", @"\");
                if (System.IO.File.Exists(filePath))
                {
                    wpPostThumbnail = await wpObject.Media.Add(thumbnailFileName, filePath);
                    featured_media = wpPostThumbnail.id;
                }
            }
            #endregion

            #region Content
            string content = postClone.Content;
            if (!String.IsNullOrEmpty(wpPostThumbnail.source_url))
            {
                content = String.Format("<p><img src='{0}' alt='{1}' /></p>", wpPostThumbnail.source_url, postClone.Title) + content;
            }

            var postImage = _service.getPostImageByPostID(postClone.PostPublicID);
            if (postImage.Count > 0)
            {
                foreach (var item in postImage)
                {
                    string imageFileName = Path.GetFileName(item.Image);
                    string filePath = rootFolder + item.Image.Replace("/", @"\");
                    if (System.IO.File.Exists(filePath))
                    {
                        var wpPostImage = await wpObject.Media.Add(imageFileName, filePath);
                        content += String.Format("<p><img src='/wp-content/uploads/{0}' alt='{1}' /></p>", System.IO.Path.GetFileName(wpPostImage.source_url), postClone.Title);
                    }
                }
            }
            #endregion

            return new Posts()
            {
                title = postClone.Title,
                content = content,
                excerpt = postClone.Summary,
                featured_media = featured_media,
                categories = categories,
                status = "publish"
            };
        }
        #endregion

        [HttpGet]
        [Route("post")]
        public async Task<IActionResult> getAll()
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);

            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);

            var wpObject = checkHeader.wp.wpObject;
            #endregion

            var posts = await wpObject.Post.GetAll();

            return Ok(posts);
        }

        #region Thực hiện post bài viết lên web
        /// <summary>
        /// Thực hiện post bài viết
        /// </summary>
        /// <param name="postCloneID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("post/{postCloneID:int}")]
        public async Task<IActionResult> postPost(int postCloneID)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);
            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);
            var wpObject = checkHeader.wp.wpObject;
            #endregion

            #region Kiểm tra tồn tại clone trong data gốc
            var postClone = _service.getCloneByID(postCloneID);

            if (postClone == null)
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy clone post" });
            #endregion

            #region Thực hiện get clone trên web
            if (postClone.PostWebID != 0)
            {
                var wpPost = await wpObject.Post.Get(postClone.PostWebID);
                if (wpPost != null)
                {
                    return BadRequest(new ResponseModel() { success = false, message = "Bài viết này đã tồn tại trên web" });
                }
            }
            #endregion

            #region Thực hiện đồng bộ post clone
            try
            {
                Posts newPost = await _handleWPPost(postClone, wpObject, checkHeader.domain, checkHeader.rootPath);
                Posts createPost = await wpObject.Post.Add(newPost);

                postClone.PostWebID = createPost.id;
                _service.Update(postClone);

                return Ok(createPost);
            }
            catch (WebException e)
            {
                var wcError = JsonConvert.DeserializeObject<WCErrorModel>(e.Message);

                return StatusCode(StatusCodes.Status500InternalServerError, wcError);
            }
            #endregion
        }
        #endregion
        #region Up to the top
        /// <summary>
        /// Thực hiện up top product
        /// </summary>
        /// <param name="postCloneID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("post/{postCloneID:int}/uptop")]
        public async Task<IActionResult> upTopPost(int postCloneID)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);
            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);
            var wpObject = checkHeader.wp.wpObject;
            #endregion

            #region Kiểm tra tồn tại clone trong data gốc
            var postClone = _service.getCloneByID(postCloneID);

            if (postClone == null)
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy clone post" });
            #endregion

            #region Thực hiện get clone trên web
            var wpPost = await wpObject.Post.Get(postClone.PostWebID);
            if (wpPost == null)
            {
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy bài viết trên web" });
            }
            #endregion

            string newTime = DateTime.Now.AddHours(-8).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
            var updatePost = await wpObject.Post.Update(wpPost.id, new Posts { date_gmt = newTime, modified_gmt = newTime });

            return Ok(updatePost);
        }
        #endregion
        #region Renew post
        /// <summary>
        /// Thực hiện renew product
        /// </summary>
        /// <param name="postCloneID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("post/{postCloneID:int}/renew")]
        public async Task<IActionResult> renewPost(int postCloneID)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);
            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);
            var wpObject = checkHeader.wp.wpObject;
            #endregion

            #region Kiểm tra tồn tại clone trong data gốc
            var postClone = _service.getCloneByID(postCloneID);

            if (postClone == null)
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy clone post" });
            #endregion

            #region Thực hiện get clone trên web
            var wpPost = await wpObject.Post.Get(postClone.PostWebID);
            if (wpPost == null)
            {
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy bài viết trên web" });
            }
            #endregion

            #region Thực hiện làm mới bài viết cũ trên web
            try
            {
                Posts editedPost = await _handleWPPost(postClone, wpObject, checkHeader.domain, checkHeader.rootPath);
                editedPost.modified_gmt = DateTime.Now.AddHours(-8).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
                Posts updatePost = await wpObject.Post.Update(wpPost.id, editedPost);

                return Ok(updatePost);
            }
            catch (WebException e)
            {
                var wcError = JsonConvert.DeserializeObject<WCErrorModel>(e.Message);

                return StatusCode(StatusCodes.Status500InternalServerError, wcError);
            }
            #endregion
        }
        #endregion
        #region Delete post
        /// <summary>
        /// Delete post
        /// </summary>
        /// <param name="postCloneID"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("post/{postCloneID:int}/delete")]
        public async Task<IActionResult> deletePost(int postCloneID)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);
            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);
            var wpObject = checkHeader.wp.wpObject;
            #endregion

            #region Kiểm tra tồn tại clone trong data gốc
            var postClone = _service.getCloneByID(postCloneID);

            if (postClone == null)
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy clone post" });
            #endregion

            #region Thực hiện get clone trên web
            var wpPost = await wpObject.Post.Get(postClone.PostWebID);
            if (wpPost == null)
            {
                return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy bài viết trên web" });
            }
            #endregion

            await wpObject.Post.Delete(wpPost.id, true);
            postClone.PostWebID = 0;
            _service.Update(postClone);

            return Ok(wpPost);
        }
        #endregion
    }
}
