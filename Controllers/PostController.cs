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
using WooCommerceNET.WooCommerce.v3;

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

            RestAPI restAPI = new RestAPI(String.Format("https://{0}/wp-json/wp/v2/", domain), domainSetting.wordpress_key, domainSetting.wordpress_secret);
            restAPI.oauth_token = domainSetting.wordpress_oauth_token;
            restAPI.oauth_token_secret = domainSetting.wordpress_oauth_token_secret;

            WPObject wpObject = new WPObject(restAPI);

            result.domain = domain;
            result.success = true;
            result.message = String.Empty;
            result.wp = new Models.Wordpress()
            {
                restAPI = restAPI,
                wpObject = wpObject
            };
            return result;
        }
        private async Task<Posts> _handleWPPost(PostClone postClone, WPObject wpObject, string domain)
        {
            #region Category List
            var categories = new List<int>();
            var wpPostCategory = await wpObject.Categories.GetAll(new Dictionary<string, string>() { { "search", postClone.CategoryName } });
            if (wpPostCategory.Count > 0)
            {
                var wpPostCategoryID = wpPostCategory.Select(x => x.id).FirstOrDefault();
                categories.Add(wpPostCategoryID);
            }
            #endregion

            var test = await wpObject.Media.GetAll();
            #region Thumbnail
            var wpPostThumbnail = await wpObject.Media.Add("post-323-90524679-102448881403878-224407102003609600-o.jpg", @"http://hethongann.com/uploads/images/posts/post-323-90524679-102448881403878-224407102003609600-o.jpg");
            #endregion

            return new Posts()
            {
                title = postClone.Title,
                content = postClone.Content,
                excerpt = postClone.Summary,
                featured_media = 0,
                categories = categories,
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

        //[HttpGet]
        //[Route("post/{postPublicID:int}")]
        //public async Task<IActionResult> getPost(int postPublicID)
        //{
        //    #region Kiểm tra điều kiện header request
        //    var checkHeader = _checkHeaderRequest(Request.Headers);

        //    if (!checkHeader.success)
        //        return StatusCode(checkHeader.statusCode, checkHeader.message);

        //    var wpObject = checkHeader.wp.wpObject;
        //    #endregion
        //    var post = _service.ge(postPublicID, checkHeader.domain);

        //    if (post == null)
        //    {
        //        return BadRequest(new ResponseModel() { success = false, message = "Không tìm thấy bài viết trên hệ thống" });
        //    }
        //    var wpPost = await wpObject.Post.Get(post.PostWordpressID);

        //    return Ok(wpPost);
        //}
        [HttpPost]
        [Route("post/{postCloneID:int}")]
        public async Task<IActionResult> postProduct(int postCloneID)
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

            #region Thực hiện đồng bộ post clone
            try
            {
                Posts newPost = await _handleWPPost(postClone, wpObject, checkHeader.domain);
                Posts wpPost = await wpObject.Post.Add(newPost);

                return Ok(wpPost);
            }
            catch (WebException e)
            {
                var wcError = JsonConvert.DeserializeObject<WCErrorModel>(e.Message);

                return StatusCode(StatusCodes.Status500InternalServerError, wcError);
            }
            #endregion
        }
    }
}
