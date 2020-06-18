﻿using System;
using System.Security.Cryptography;
using System.Text;

namespace TT.Extensions
{
    public static class StringExt
    {
        public static string GetMd5(this string str)
        {
            //创建MD5哈稀算法的默认实现的实例
            MD5 md5 = MD5.Create();
            //将指定字符串的所有字符编码为一个字节序列
            byte[] buffer = Encoding.Default.GetBytes(str);
            //计算指定字节数组的哈稀值
            byte[] bufferMd5 = md5.ComputeHash(buffer);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bufferMd5.Length; i++)
            {
                //x:表示将十进制转换成十六进制
                sb.Append(bufferMd5[i].ToString("x2"));
            }

            return sb.ToString();
        }

        public static bool IsNullOrEmptyOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str) || string.IsNullOrEmpty(str);
        }

        public static Random random = new Random();

        /// <summary>
        /// 取随机数
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string BuildRandomStr(int length)
        {
            int num;

            lock (random)
            {
                num = random.Next();
            }

            string str = num.ToString();

            if (str.Length > length)
            {
                str = str.Substring(0, length);
            }
            else if (str.Length < length)
            {
                int n = length - str.Length;
                while (n > 0)
                {
                    str = str.Insert(0, "0");
                    n--;
                }
            }

            return str;
        }

        public static string GetSha1(this string encypStr)
        {
            var hash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(encypStr));
            var stringBuilder = new StringBuilder();
            foreach (var num in hash)
            {
                stringBuilder.Append($"{num:x2}");
            }

            return $"{stringBuilder}";
        }

        public static string GetTimestamp()
        {
            return $"{Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds)}";
        }
    }
}