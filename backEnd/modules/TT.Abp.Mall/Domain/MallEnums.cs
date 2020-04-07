﻿using System.ComponentModel.DataAnnotations;

namespace TT.Abp.Mall.Domain
{
    public class MallEnums
    {
        public enum PartnerState
        {
            待审核 = 0,
            驳回 = -1,
            成功 = 10
        }

        public enum RealNameInfoType : byte
        {
            个人 = 0,
            企业 = 1
        }

        public enum RealNameInfoState : byte
        {
            未认证 = 0,
            个人认证 = 1,
            企业认证 = 2
        }

        public enum PayType
        {
            [Display(Name = "微信")] 微信 = 1,
            [Display(Name = "微信扫码")] 微信扫码 = 2,
            [Display(Name = "支付宝")] 支付宝 = 3,
            [Display(Name = "银联")] 银联 = 4,
            [Display(Name = "用户余额")] 用户余额 = 10
        }

        public enum OrderType
        {
            Product = 1
        }
    }
}