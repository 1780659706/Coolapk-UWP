﻿using CoolapkUWP.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.UI.Xaml.Media.Imaging;
using static CoolapkUWP.Helpers.NetworkHelper;
using SymbolIcon = Windows.UI.Xaml.Controls.SymbolIcon;
using Visibility = Windows.UI.Xaml.Visibility;

namespace CoolapkUWP.Helpers
{
    /// <summary>
    ///     程序支持的能从服务器中获取的数据的类型。
    /// </summary>
    internal enum DataUriType
    {
        CheckLoginInfo,
        CreateFeed,
        CreateFeedReply,
        CreateReplyReply,
        GetAnswers,
        GetDyhDetail,
        GetDyhFeeds,
        GetFeedDetail,
        GetFeedReplies,
        GetHotReplies,
        GetIndexPage,
        GetIndexPageNames,
        GetLikeList,
        GetMyPageCard,
        GetNotifications,
        GetNotificationNumbers,
        GetReplyReplies,
        GetSearchWords,
        GetShareList,
        GetTagDetail,
        GetTagFeeds,
        GetUserFeeds,
        GetUserHistory,
        GetUserList,
        GetUserRecentHistory,
        GetUserSpace,
        GetUserProfile,
        OperateFollow,
        OperateLike,
        OperateUnfollow,
        OperateUnlike,
        RequestValidate,
        SearchFeeds,
        SearchTags,
        SearchUsers,
        SearchWords,
    }

    /// <summary>
    ///     提供数据处理的方法。
    /// </summary>
    internal static class DataHelper
    {
        private static readonly System.Collections.Generic.Dictionary<string, object> responseCache = new System.Collections.Generic.Dictionary<string, object>();

        public static string GetMD5(string inputString)
        {
            CryptographicHash objHash = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5).CreateHash();
            objHash.Append(CryptographicBuffer.ConvertStringToBinary(inputString, BinaryStringEncoding.Utf8));
            Windows.Storage.Streams.IBuffer buffHash1 = objHash.GetValueAndReset();
            return CryptographicBuffer.EncodeToHexString(buffHash1);
        }

        private static string GetUriStringTemplate(DataUriType type)
        {
            switch (type)
            {
                case DataUriType.CheckLoginInfo: return "/account/checkLoginInfo";
                case DataUriType.CreateFeed: return "/feed/createFeed";
                case DataUriType.CreateFeedReply: return "/feed/reply?id={0}&type=feed";
                case DataUriType.CreateReplyReply: return "/feed/reply?id={0}&type=reply";
                case DataUriType.GetAnswers: return "/question/answerList?id={0}&sort={1}&page={2}{3}{4}";
                case DataUriType.GetDyhDetail: return "/dyh/detail?dyhId={0}";
                case DataUriType.GetDyhFeeds: return "/dyhArticle/list?dyhId={0}&type={1}&page={2}{3}{4}";
                case DataUriType.GetFeedDetail: return "/feed/detail?id={0}";
                case DataUriType.GetFeedReplies: return "/feed/replyList?id={0}&listType={1}&page={2}{3}&discussMode=1&feedType=feed&blockStatus=0&fromFeedAuthor={4}";
                case DataUriType.GetHotReplies: return "/feed/hotReplyList?id={0}&page={1}{2}&discussMode=1";
                case DataUriType.GetIndexPage: return "{0}{1}page={2}";
                case DataUriType.GetIndexPageNames: return "/main/init";
                case DataUriType.GetLikeList: return "/feed/likeList?id={0}&listType=lastupdate_desc&page={1}{2}";
                case DataUriType.GetMyPageCard: return "/account/loadConfig?key=my_page_card_config";
                case DataUriType.GetNotifications: return "/notification/{0}?page={1}{2}{3}";
                case DataUriType.GetNotificationNumbers: return "/notification/checkCount";
                case DataUriType.GetReplyReplies: return "/feed/replyList?id={0}&listType=&page={1}{2}&discussMode=0&feedType=feed_reply&blockStatus=0&fromFeedAuthor=0";
                case DataUriType.GetSearchWords: return "/search/suggestSearchWordsNew?searchValue={0}&type=app";
                case DataUriType.GetShareList: return "/feed/forwardList?id={0}&type=feed&page={1}";
                case DataUriType.GetTagDetail: return "/topic/newTagDetail?tag={0}";
                case DataUriType.GetTagFeeds: return "/topic/tagFeedList?tag={0}&page={1}{2}{3}&listType={4}&blockStatus=0";
                case DataUriType.GetUserFeeds: return "/user/feedList?uid={0}&page={1}{2}{3}";
                case DataUriType.GetUserHistory: return "/user/hitHistoryList?page={0}{1}{2}";
                case DataUriType.GetUserList: return "/user/{0}?uid={1}&page={2}{3}{4}";
                case DataUriType.GetUserRecentHistory: return "/user/recentHistoryList?page={0}{1}{2}";
                case DataUriType.GetUserSpace: return "/user/space?uid={0}";
                case DataUriType.GetUserProfile: return "/user/profile?uid={0}";
                case DataUriType.OperateFollow: return "/user/follow?uid={0}";
                case DataUriType.OperateLike: return "/feed/like{0}?id={1}&detail=0";
                case DataUriType.OperateUnfollow: return "/user/follow?uid={0}";
                case DataUriType.OperateUnlike: return "/feed/unlike{0}?id={1}&detail=0";
                case DataUriType.RequestValidate: return "/account/requestValidate";
                case DataUriType.SearchFeeds: return "/search?type=feed&feedType={0}&sort={1}&searchValue={2}&page={3}{4}&showAnonymous=-1";
                case DataUriType.SearchTags: return "/search?type=feedTopic&searchValue={0}&page={1}{2}&showAnonymous=-1";
                case DataUriType.SearchUsers: return "/search?type=user&searchValue={0}&page={1}{2}&showAnonymous=-1";
                case DataUriType.SearchWords: return "/search/suggestSearchWordsNew?searchValue={0}&type=app";
                default: throw new ArgumentException($"{typeof(DataUriType).FullName}值错误");
            }
        }

        public static async Task<BitmapImage> GetImageAsync(string uri)
        {
            var folder = await ImageCacheHelper.GetFolderAsync(ImageType.Captcha);
            var file = await folder.CreateFileAsync(GetMD5(uri));
            return await DownloadImageAsync(new Uri(uri), file);
        }

        public static async Task<JToken> PostDataAsync(DataUriType type, Windows.Web.Http.IHttpContent content, params object[] args)
        {
            var uri = string.Format(GetUriStringTemplate(type), args);
            var json = await PostAsync(uri, content);
            var o = JObject.Parse(json);
            JToken token = null;
            if (!string.IsNullOrEmpty(json) &&
                !o.TryGetValue("data", out token) &&
                o.TryGetValue("message", out _))
                throw new CoolapkMessageException(o);
            else return token;
        }

        /// <summary>
        ///     从服务器中获取数据。
        /// </summary>
        /// <param name="type">
        ///     要获取的数据的类型。
        /// </param>
        /// <param name="args">
        ///     一个参数数组，其中的内容用于替换格式符号。
        /// </param>
        public static async Task<JToken> GetDataAsync(DataUriType type, bool forceRefresh, params object[] args)
        {
            string uri = string.Format(GetUriStringTemplate(type), args);
            string json;
            if (forceRefresh || !responseCache.ContainsKey(uri))
            {
                json = await GetJson(uri);
                if (responseCache.ContainsKey(uri))
                {
                    responseCache[uri] = json;

                    int i = uri.IndexOf("page=");
                    if (i != -1)
                    {
                        string u = uri.Substring(i);

                        var needDelete = (from item in responseCache
                                          where item.Key != uri
                                          where item.Key.IndexOf(u, StringComparison.Ordinal) == 0
                                          select item).ToArray();
                        foreach (var item in needDelete)
                        {
                            responseCache.Remove(item.Key);
                        }
                    }
                }
                else
                {
                    responseCache.Add(uri, json);
                }
            }
            else
            {
                json = responseCache[uri] as string;
            }
            var o = JObject.Parse(json);
            JToken token = null;
            if (!string.IsNullOrEmpty(json) &&
                !o.TryGetValue("data", out token) &&
                o.TryGetValue("message", out _))
                throw new CoolapkMessageException(o);
            else return token;
        }

        public static async Task<T> GetDataAsync<T>(DataUriType dataUriType, bool forceRefresh = false, params object[] args)
        {
            string uri = string.Format(GetUriStringTemplate(dataUriType), args);
            string json = string.Empty;
            T result = default;

            if (forceRefresh || !responseCache.ContainsKey(uri))
            {
                json = await GetJson(uri);
                result = await Task.Run(() => JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                {
                    Error = (_, e) =>
                    {
                        if (!string.IsNullOrEmpty(json))
                        {
                            var o = JsonConvert.DeserializeObject<Models.Json.MessageModel.Rootobject>(json, new JsonSerializerSettings { Error = (__, ee) => ee.ErrorContext.Handled = true });
                            if (o != null)
                                throw new CoolapkMessageException(o.Message);
                        }
                        e.ErrorContext.Handled = true;
                    }
                }));
                if (responseCache.ContainsKey(uri))
                {
                    responseCache[uri] = result;

                    int i = uri.IndexOf("page=");
                    if (i != -1)
                    {
                        string u = uri.Substring(i);

                        var needDelete = (from item in responseCache
                                          where item.Key != uri
                                          where item.Key.IndexOf(u, StringComparison.Ordinal) == 0
                                          select item).ToArray();
                        foreach (var item in needDelete)
                        {
                            responseCache.Remove(item.Key);
                        }
                    }
                }
                else
                {
                    responseCache.Add(uri, result);
                }
            }
            else
            {
                result = (T)responseCache[uri];
            }

            return result;
        }

        static readonly DateTime unixDate = new DateTime(1970, 1, 1).ToLocalTime();

        /// <summary>
        ///     转换Unix时间戳至可读时间。
        /// </summary>
        /// <param name="timestr">
        ///     Unix时间戳。
        /// </param>
        public static string ConvertTime(double timestr)
        {
            DateTime time = unixDate.Add(new TimeSpan(Convert.ToInt64(timestr) * 1000_0000));
            TimeSpan temp = DateTime.Now.Subtract(time);

            if (temp.TotalDays > 30)
            {
                return $"{time.Year}/{time.Month}/{time.Day}";
            }
            else if (temp.Days > 0)
            {
                return $"{temp.Days}天前";
            }
            else if (temp.Hours > 0)
            {
                return $"{temp.Hours}小时前";
            }
            else if (temp.Minutes > 0)
            {
                return $"{temp.Minutes}分钟前";
            }
            else
            {
                return "刚刚";
            }
        }

        public static string ConvertTimeToUnix(DateTime time) => $"{time.Subtract(unixDate).TotalSeconds:F0}";

        public static async Task MakeLikeAsync(ICanChangeLike like, Windows.UI.Core.CoreDispatcher dispatcher, SymbolIcon like1, SymbolIcon like2)
        {
            if (like == null) { return; }
            bool isReply = like is FeedReplyModel;
            bool isLike = false;
            JObject o;
            if (like.Liked)
            {
                o = (JObject)await GetDataAsync(DataUriType.OperateUnlike, true, isReply ? "Reply" : string.Empty, like.Id);
            }
            else
            {
                o = (JObject)await GetDataAsync(DataUriType.OperateLike, true, isReply ? "Reply" : string.Empty, like.Id);
                isLike = true;
            }

            await dispatcher?.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                like.Liked = isLike;
                if (isReply)
                {
                    like.Likenum = o.ToString().Replace("\"", string.Empty);
                }
                else if (o != null)
                {
                    like.Likenum = o.Value<int>("count").ToString();
                }

                if (like1 != null)
                {
                    like1.Visibility = isLike ? Visibility.Visible : Visibility.Collapsed;
                }
                if (like1 != null)
                {
                    like2.Visibility = isLike ? Visibility.Collapsed : Visibility.Visible;
                }
            });
        }
    }
}