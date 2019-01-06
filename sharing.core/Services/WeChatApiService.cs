﻿


namespace Sharing.Core.Services
{
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.DependencyInjection;
    using Sharing.WeChat.Models;
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    public class WeChatApiService : IWeChatApi
    {

        private readonly IServiceProvider provider = SharingConfigurations.CreateServiceCollection(null).BuildServiceProvider();
        /// <summary>
        /// 获取WeChat api token
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        public string GetToken(string appid, string secret)
        {
            return provider.GetService<IMemoryCache>().GetOrCreate<string>(
                string.Format("Token_{0}", appid),
                (entity) =>
                {
                    var url = string.Format("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}",
                        appid, secret);
                    var token = url.GetUriJsonContent<AccessTokenWxResponse>();
                    entity.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(token.Expiresin);
                    return token.Token;
                });
        }
        /// <summary>
        /// 解密数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="encryptedData"></param>
        /// <param name="iv"></param>
        /// <param name="sessionKey"></param>
        /// <returns></returns>
        public T Decrypt<T>(string encryptedData, string iv, string sessionKey)
        {
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();

            //设置解密器参数  
            aes.Mode = CipherMode.CBC;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.PKCS7;
            //格式化待处理字符串  
            byte[] byte_encryptedData = Convert.FromBase64String(encryptedData);
            byte[] byte_iv = Convert.FromBase64String(iv);
            byte[] byte_sessionKey = Convert.FromBase64String(sessionKey);

            aes.IV = byte_iv;
            aes.Key = byte_sessionKey;
            //根据设置好的数据生成解密器实例  
            ICryptoTransform transform = aes.CreateDecryptor();

            //解密  
            byte[] final = transform.TransformFinalBlock(byte_encryptedData, 0, byte_encryptedData.Length);

            //生成结果  
            string result = Encoding.UTF8.GetString(final);

            //反序列化结果，生成用户信息实例  
            return result.DeserializeToObject<T>();
        }

        public SessionWxResponse GetSession(JSCodeApiToken token)
        {
            var url = string.Format("https://api.weixin.qq.com/sns/jscode2session?appid={0}&js_code={1}&secret={2}&grant_type=authorization_code",
                 token.AppId, token.Code, token.Secret);
            return url.GetUriJsonContent<SessionWxResponse>();
        }


        public QueryCardCouponWxResponse QueryMCard(IWxApp official)
        {
            var url = string.Format("https://api.weixin.qq.com/card/batchget?access_token={0}", GetToken(official.AppId, official.Secret));
            return url.GetUriJsonContent<QueryCardCouponWxResponse>((http) =>
           {
               http.Method = "POST";
               http.ContentType = "application/json; encoding=utf-8";
               var data = new
               {
                   offset = 0,
                   count = 10,
                   //status_list
                   /*
                    * 支持开发者拉出指定状态的卡券列表 
                    * “CARD_STATUS_NOT_VERIFY”, 待审核 ； 
                    * “CARD_STATUS_VERIFY_FAIL”, 审核失败； 
                    * “CARD_STATUS_VERIFY_OK”， 通过审核； 
                    * “CARD_STATUS_DELETE”，  卡券被商户删除； 
                    * “CARD_STATUS_DISPATCH” 在公众平台投放过的卡券；
                    */
                   status_list = new string[] { "CARD_STATUS_VERIFY_OK", "CARD_STATUS_DISPATCH" }
               };
               using (var stream = http.GetRequestStream())
               {
                   var body = data.SerializeToJson();
                   var buffers = UTF8Encoding.UTF8.GetBytes(body);
                   stream.Write(buffers, 0, buffers.Length);
                   stream.Flush();
               }
               return http;
           });
        }

        public JObject QueryMCardDetails(IWxApp official, IWxCardKey card)
        {
            var url = string.Format("https://api.weixin.qq.com/card/get?access_token={0}", GetToken(official.AppId, official.Secret));
            var result= url.GetUriJsonContent<JObject>((http) =>
            {
                http.Method = "POST";
                http.ContentType = "application/json; encoding=utf-8";
                var data = new
                {
                    card_id = card.CardId
                };
                using (var stream = http.GetRequestStream())
                {
                    var body = data.SerializeToJson();
                    var buffers = UTF8Encoding.UTF8.GetBytes(body);
                    stream.Write(buffers, 0, buffers.Length);
                    stream.Flush();
                }
                return http;
            });        
            return result;
        }
    }
}