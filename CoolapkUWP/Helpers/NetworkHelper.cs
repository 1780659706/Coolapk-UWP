﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Http;

namespace CoolapkUWP.Helpers
{
    /// <summary> 提供与网络相关的方法。 </summary>
    internal static class NetworkHelper
    {
        private static readonly HttpClient mClient = new HttpClient();

        static NetworkHelper()
        {
            mClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            mClient.DefaultRequestHeaders.Add("X-Sdk-Int", "28");
            mClient.DefaultRequestHeaders.Add("X-Sdk-Locale", "zh-CN");
            mClient.DefaultRequestHeaders.Add("X-App-Id", "com.coolapk.market");
            mClient.DefaultRequestHeaders.Add("X-App-Version", "9.2.2");
            mClient.DefaultRequestHeaders.Add("X-App-Code", "1905301");
            mClient.DefaultRequestHeaders.Add("X-Api-Version", "9");
            string s = Guid.NewGuid().ToString();
            mClient.DefaultRequestHeaders.Add("X-App-Device", DataHelper.GetMD5(s + s + s) + "ady6r8"); //随便弄的
        }

        private static string GetCoolapkAppToken()
        {
            string DEVICE_ID = Guid.NewGuid().ToString();
            string t = DataHelper.ConvertTimeToUnix(DateTime.Now);
            string hex_t = "0x" + Convert.ToString(int.Parse(t), 16);
            // 时间戳加密
            string md5_t = DataHelper.GetMD5(t);
            string a = "token://com.coolapk.market/c67ef5943784d09750dcfbb31020f0ab?" + md5_t + "$" + DEVICE_ID + "&com.coolapk.market";
            string md5_a = DataHelper.GetMD5(Convert.ToBase64String(Encoding.UTF8.GetBytes(a)));
            string token = md5_a + DEVICE_ID + hex_t;
            return token;
        }

        public static async Task<string> PostAsync(Uri uri, IHttpContent content)
        {
            try
            {
                mClient.DefaultRequestHeaders.Remove("X-App-Token");
                mClient.DefaultRequestHeaders.Add("X-App-Token", GetCoolapkAppToken());
                var a = await mClient.PostAsync(uri, content);
                return await a.Content.ReadAsStringAsync();
            }
            catch { throw; }
        }

        public static async Task<BitmapImage> DownloadImageAsync(Uri uri, StorageFile file)
        {
            mClient.DefaultRequestHeaders.Remove("X-App-Token");
            mClient.DefaultRequestHeaders.Add("X-App-Token", GetCoolapkAppToken());

            var s = await mClient.GetInputStreamAsync(uri);
            using (var ss = await file.OpenStreamForWriteAsync())
            {
                await s.AsStreamForRead().CopyToAsync(ss);
            }

            return new BitmapImage(new Uri(file.Path));
        }

        /// <summary> 从指定URI中获取Json文本。 </summary>
        /// <param name="uri"> 数据在酷安服务器中的位置。 </param>
        public static async Task<string> GetJson(Uri uri)
        {
            try
            {
                //if (url != "/notification/checkCount") UIHelper.notifications?.RefreshNotificationsNum();
                mClient.DefaultRequestHeaders.Remove("X-App-Token");
                mClient.DefaultRequestHeaders.Add("X-App-Token", GetCoolapkAppToken());
                return await mClient.GetStringAsync(uri);
            }
            catch { throw; }
        }

        /// <summary> 通过用户名获取UID。 </summary>
        /// <param name="name"> 要获取UID的用户名。 </param>
        public static async Task<string> GetUserIDByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                var s = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse().GetString("UserNameError");
                UIHelper.ShowMessage(s);
                return "0";
            }
            string str = string.Empty;
            try
            {
                str = await mClient.GetStringAsync(new Uri("https://www.coolapk.com/n/" + name));
                return Windows.Data.Json.JsonObject.Parse(str)["dataRow"].GetObject()["uid"].GetNumber().ToString();
            }
            catch
            {
                var o = Windows.Data.Json.JsonObject.Parse(str).GetObject();
                if (o != null)
                {
                    UIHelper.ShowMessage(o["message"].GetString());
                    return "0";
                }
                else throw;
            }
        }
    }
}