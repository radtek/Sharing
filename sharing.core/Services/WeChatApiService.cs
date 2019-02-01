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
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using Sharing.Core;
    using System.Collections.Generic;
    using System.Linq;
    using System.Collections;

    public class WeChatApiService : IWeChatApi
    {

        private readonly IMemoryCache cache;
        private readonly IRandomGenerator generator;
        public WeChatApiService(IMemoryCache cache,IRandomGenerator generator)
        {
            this.cache = cache;
            this.generator = generator;
        }
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(WeChatApiService));
        //private readonly IServiceProvider provider = SharingConfigurations.CreateServiceCollection(null).BuildServiceProvider();
        /// <summary>
        /// 获取WeChat api token
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        public string GetToken(string appid, string secret)
        {
            return GetToken(appid, secret, false);
            //return this.cache.GetOrCreate<string>(
            //    string.Format("Token_{0}", appid),
            //    (entity) =>
            //    {
            //        var url = string.Format("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}",
            //            appid, secret);
            //        var token = url.GetUriJsonContent<AccessTokenWxResponse>();
            //        entity.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(token.Expiresin);
            //        return token.Token;
            //    });
        }
        private string GetToken(string appid, string secret, bool forApiTicket=false)
        {
            return this.cache.GetOrCreate<string>(
                string.Format("Token_{0}_{1}", appid, forApiTicket ? "yes" : "no"),
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
            return url.GetUriJsonContent<JObject>((http) =>
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

        }


        public WxPayParameter Unifiedorder(WxPayData data, string mchid)
        {
            var request = "https://api.mch.weixin.qq.com/pay/unifiedorder";
            var order = request.GetUriContentDirectly((http) =>
            {
                if (request.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    ServicePointManager.ServerCertificateValidationCallback =
                            new RemoteCertificateValidationCallback(CheckValidationResult);
                }
                http.Timeout = 30 * 1000;
                ServicePointManager.DefaultConnectionLimit = 200;
                http.UserAgent = string.Format("WXPaySDK/{3} ({0}) .net/{1} {2}",
                    Environment.OSVersion, Environment.Version, mchid,
                    typeof(WxPayParameter).Assembly.GetName().Version);
                http.Method = "POST";
                http.ContentType = "text/xml";
                using (var stream = http.GetRequestStream())
                {
                    var body = data.SerializeToXml();
                    var buffers = UTF8Encoding.UTF8.GetBytes(body);
                    stream.Write(buffers, 0, buffers.Length);
                    stream.Flush();
                }
                return http;
            }).DeserializeFromXml<WeChatUnifiedorderResponse>();
            return new WxPayParameter(order,this.generator);
        }


        public static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            //直接确认，否则打不开    
            return true;
        }

        public string GenerateSignForApplyMCard(
            IWxApp official,
            IWxApp miniprogram,
            string cardid,
            long timestamp,
            string nonce_str)
        {
            //https://mp.weixin.qq.com/debug/cgi-bin/sandbox?t=cardsign
            //卡券签名算法  https://mp.weixin.qq.com/wiki?t=resource/res_main&id=mp1421141115
            var api_ticket = GetApiTicket(miniprogram.AppId, this.GetToken(official.AppId, official.Secret));
            ArrayList AL = new ArrayList();
            AL.Add(api_ticket);
            AL.Add(timestamp);
            AL.Add(nonce_str);
            AL.Add(cardid);
            AL.Sort(new DictionarySort());

            //var perpare = string.Format("{0}{1}{2}{3}", timestamp, nonce_str,api_ticket, cardid );
            var perpare = string.Join(string.Empty, AL.ToArray());
            return perpare.GetSHA1Crypto();

        }

        private string GetApiTicket(string appid, string token)
        {
            string cacheKey = string.Format("ticket-{0}", appid);
            return this.cache.GetOrCreate<string>(cacheKey,
                (entity) =>
                {
                    //https://api.weixin.qq.com/cgi-bin/ticket/getticket?type=jsapi&access_token=$accessToken
                    //https://api.weixin.qq.com/cgi-bin/ticket/getticket?access_token=ACCESS_TOKEN&type=wx_card
                    var url = string.Format("https://api.weixin.qq.com/cgi-bin/ticket/getticket?access_token={0}&type=wx_card", token);
                    var response = url.GetUriJsonContent<TicketWxResponse>();
                    entity.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(response.Expiresin);
                    return response.Ticket;
                });
        }

        public QueryWxUserCardResponse QueryWxUserMCards(
            IWxApp app,
            IWxUserOpenId wxuser,
            IWxMCardId mcard = null)
        {
            var url = string.Format("https://api.weixin.qq.com/card/user/getcardlist?access_token={0}"
                , GetToken(app.AppId, app.Secret));
            return url.GetUriJsonContent<QueryWxUserCardResponse>((http) =>
            {
                http.Method = "POST";
                http.ContentType = "application/json; encoding=utf-8";
                var data = new
                {
                    openid = wxuser.OpenId,
                    card_id = (mcard == null || string.IsNullOrWhiteSpace(mcard.CardId))
                    ? null
                    : mcard.CardId
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

        public DecryptCodeWxResponse DecryptMCardUserCode(IWxApp app, string encryptedData)
        {
            string url = string.Format("https://api.weixin.qq.com/card/code/decrypt?access_token={0}", GetToken(app.AppId, app.Secret));
            return url.GetUriJsonContent<DecryptCodeWxResponse>((http) =>
            {
                http.Method = "POST";
                http.ContentType = "application/json; encoding=utf-8";
                var data = new
                {
                    encrypt_code = encryptedData
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

        public CreateCouponWxResponse SaveOrUpdateCardCoupon(IWxApp official, JObject jObject)
        {

            var url = string.Format(string.IsNullOrEmpty(jObject.ParseCardId())
                ? "https://api.weixin.qq.com/card/create?access_token={0}"
                : "https://api.weixin.qq.com/card/update?access_token={0}",
                GetToken(official.AppId, official.Secret));
            return url.GetUriJsonContent<CreateCouponWxResponse>((http) =>
            {
                http.Method = "POST";
                http.ContentType = "application/json;encoding=utf-8";
                using (var stream = http.GetRequestStream())
                {
                    var buffers = UTF8Encoding.UTF8.GetBytes(jObject.ToString());
                    stream.Write(buffers, 0, buffers.Length);
                    stream.Flush();
                }
                return http;
            });
        }



        public NormalWxResponse DeleteCardCoupon(IWxApp official, IWxMCardId cardId)
        {
            var url = string.Format("https://api.weixin.qq.com/card/delete?access_token={0}", GetToken(official.AppId, official.Secret));
            return url.GetUriJsonContent<NormalWxResponse>((http) =>
            {
                var data = new { card_id = cardId.CardId };
                http.Method = "POST";
                http.ContentType = "application/json; encoding=utf-8";
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
    }
}
