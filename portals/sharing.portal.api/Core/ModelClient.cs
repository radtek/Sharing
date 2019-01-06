﻿

namespace Sharing.Portal.Api
{
    using Sharing.Core;
    using Sharing.Portal.Api.Models;
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Sharing.WeChat.Models;
    using Sharing.Core.Entities;
    using Sharing.Core.Models;
    using System.Collections.Generic;
    using System.Linq;
    public class ModelClient
    {
        private readonly IServiceProvider provider = Utility.CreateServiceProvider();
        public WeChatUserModel Register(RegisterWeChatUserContext context)
        {
            var weChatApi = provider.GetService<IWeChatApi>();
            var wxUserService = provider.GetService<IWxUserService>();
            var info = weChatApi.Decrypt<WeChatUserInfo>(context.Data, context.IV, context.SessionKey);
            var membership = wxUserService.Register(new Core.Models.RegisterWxUserContext()
            {
                AppType = AppTypes.Miniprogram,
                Info = info,
                InvitedBy = context.InvitedBy,
                WxApp = new WxApp()
                {
                    AppId = context.AppId
                }
            });
            return new WeChatUserModel()
            {
                Mobile = membership.Mobile,
                OpenId = membership.OpenId,
                UnionId = info.UnionId
            };
        }
        public SessionWxResponse GetSession(JSCodeApiToken token)
        {
            var service = provider.GetService<IWeChatApi>();
            return service.GetSession(token);
        }
        public IList<MCardModel> GetMCardModels(string mcode)
        {
            var queryString = @"
SELECT 
    CardId,
	merchant.BrandName,
	mcard.Title,
	mcard.Prerogative,
	mcard.LogoUrl
FROM sharing_mcard AS mcard
	INNER JOIN sharing_merchant AS merchant 
		ON mcard.MerchantId=merchant.Id
WHERE merchant.MCode=mcode";
            using (var database = SharingConfigurations.GenerateDatabase(false))
            {
                return database.SqlQuery<MCardModel>(queryString, new { mcode = mcode }).ToList();
            }
        }

        public MyMCardModel GetMyMCard(string appid, string openid, string cardid)
        {
            var queryString = @"
SELECT 
	mcard.BrandName,
    mcard.Title,
    IFNULL(ucard.WxUserId,0) AS Ready,
    mcard.CardId,
    mcard.Prerogative,
    ucard.UserCode,
    ucard.Money,
    ucard.RewardMoney,
    (
		SELECT Address FROM  `sharing_merchant` AS merchant WHERE merchant.Id = mcard.MerchantId
        LIMIT 1
    ) AS Address
FROM `sharing_wxusercard` AS ucard
	RIGHT JOIN `sharing_mcard` AS mcard
		ON ucard.CardId = mcard.CardId AND mcard.CardId =@pCardid
	LEFT JOIN  `sharing_wxuser` AS wxuser
		ON ucard.WxUserId = wxuser.Id AND wxuser.AppId=@pAppid AND wxuser.OpenId=@pOpenId
WHERE mcard.CardId =@pCardid
";
            using (var database = SharingConfigurations.GenerateDatabase(false))
            {
                return database.SqlQuerySingleOrDefault<MyMCardModel>(queryString, new
                {
                    pAppid = appid,
                    pOpenId = openid,
                    pCardid = cardid
                });
            }
        }
    }
}