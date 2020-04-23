using ANNProductSync.Models;
using ANNProductSync.Services;
using ANNProductSync.Services.FactoryPattern;
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

namespace ANNProductSync.Controllers
{
    [ApiController]
    [Route("api/v1/post")]
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

            //if (!headers.ContainsKey("domain"))
            //{
            //    result.statusCode = StatusCodes.Status400BadRequest;
            //    result.success = false;
            //    result.message = "Thiếu domain WooCommerce";

            //    return result;
            //}

            //var configuration = new ConfigurationBuilder()
            //     .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            //     .AddJsonFile("appsettings.json")
            //     .Build();

            //var domain = headers.Where(x => x.Key == "domain").Select(x => x.Value).FirstOrDefault();
            //var domainSetting = configuration.GetSection(domain).Get<DomainSettingModel>();

            //if (String.IsNullOrEmpty(domain))
            //{
            //    result.statusCode = StatusCodes.Status400BadRequest;
            //    result.success = false;
            //    result.message = "Domain WooCommerce không được rỗng";

            //    return result;
            //}
            //if (domainSetting == null)
            //{
            //    result.statusCode = StatusCodes.Status500InternalServerError;
            //    result.success = false;
            //    result.message = String.Format("{0} chưa được cài đặt", domain);

            //    return result;
            //}

            RestAPI rest = new RestAPI("https://annshop.vn/wp-json/wp/v2/", "wcjxmCc1VXAJ", "WNddYdw1oLqVpuNy5MsrQ8TpYWzM7PCAEXr9k7f2MKltMiqh");
            rest.oauth_token = "bZqkUIRptTUDKDUfSDtAkONr";
            rest.oauth_token_secret = "4xgJ80NmiRCO2T2pbYfqnHOPjLQeQYohLVdgzbT7VI9WJf2u";

            //var restAPI = new RestAPI(String.Format("https://{0}/wp-json/jwt-auth/v1/token", domain), "orj0le", "@HoangAnhPhan828327");
            WPObject wpObject = new WPObject(rest);

            result.domain = "annshop.vn";
            result.success = true;
            result.message = String.Empty;
            result.wp = new Models.Wordpress()
            {
                restAPI = rest,
                wpObject = wpObject
            };
            return result;
        }
        #endregion

        [HttpGet]
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
    }
}
