﻿using System;
using System.Collections.Generic;
using System.Text;
using Sharing.Core.Entities;
using Sharing.Core.Models;
using Sharing.WeChat.Models;

namespace Sharing.Core.Services
{
    public class WeChatPayService : IWeChatPayService
    {
        public Trade PrepareUnifiedorder(TopupContext context)
        {
            var queryString = @"
    INSERT INTO 
    `sharing_trade`(WxUserId,WxOrderId,TradeId,TradeType,TradeState,Money,RealMoney,CreatedTime,Attach, Strategy)
    SELECT 
    (SELECT Id FROM `sharing_wxuser` WHERE AppId=@pAppId AND OpenId=@pOpenId LIMIT 1) AS WxUserId,
    @pWxOrderId AS WxOrderId,
    @pTradeId AS TradeId,
    'Recharge' AS TradeType,
    'Waiting' AS TradeState,
    @pMoney AS Money,
    @pRealMoney AS RealMoney,
    @pCreatedTime AS CreatedTime,
    @pAttach AS Attach,
    @pStrategy AS Strategy;
    UPDATE `sharing_trade` SET TradeId=CONCAT(@prefix , LPAD(Id,10,'0')) WHERE WxOrderId = @pWxOrderId;
    SELECT * FROM `sharing_trade` WHERE WxOrderId = @pWxOrderId LIMIT 1;

";
            var cardKey = new WeChatUserCardKey() { CardId = context.CardId, UserCode = context.Code };
            using (var database = SharingConfigurations.GenerateDatabase(true))
            {
                return database.SqlQuerySingleOrDefaultTransaction<Trade>(queryString, new
                {
                    pAppId = context.AppId,
                    pOpenId = context.OpenId,
                    pWxOrderId = Guid.NewGuid().ToString().Replace("-", string.Empty),
                    pTradeId = Guid.NewGuid().ToString().Replace("-", string.Empty),
                    pMoney = context.Money * 100,
                    pRealMoney = context.Money * 100 + (context.Money * 100 * 0.2),
                    pCreatedTime = DateTime.Now.ToUnixStampDateTime(),
                    pStrategy = "{}",
                    pAttach = cardKey.SerializeToJson(),
                    prefix = string.Format("T{0}", DateTime.Now.ToString("yyyyMMdd"))
                });
            }
        }

        public Payment GetPayment(string appid)
        {
            var queryString = "SELECT `Payment` FROM `sharing_mwechatapp` WHERE AppId=@AppId;";
            using (var database = SharingConfigurations.GenerateDatabase(false))
            {
                var app = database.SqlQuerySingleOrDefault<MWeChatApp>(queryString, new { AppId = appid });
                return app.Payment.DeserializeToObject<Payment>();
            }
        }
    }
}
