using ANNwpsync.Models;
using ANNwpsync.Services;
using ANNwpsync.Services.FactoryPattern;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        #endregion

        public PostController(ILogger<PostController> logger)
        {
            _logger = logger;
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

        [HttpGet]
        [Route("post/{postID:int}")]
        public async Task<IActionResult> getPost(int postID)
        {
            #region Kiểm tra điều kiện header request
            var checkHeader = _checkHeaderRequest(Request.Headers);

            if (!checkHeader.success)
                return StatusCode(checkHeader.statusCode, checkHeader.message);

            var wpObject = checkHeader.wp.wpObject;
            #endregion

            var posts = await wpObject.Post.Get(postID);

            return Ok(posts);
        }
    }
}
