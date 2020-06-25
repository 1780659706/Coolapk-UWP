﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace CoolapkUWP.Helpers
{
    public enum ImageType
    {
        SmallImage,
        OriginImage,
        SmallAvatar,
        BigAvatar,
        Icon
    }

    internal static class ImageCacheHelper
    {
        private static readonly BitmapImage whiteNoPicMode = new BitmapImage(new Uri("ms-appx:/Assets/img_placeholder.png")) { DecodePixelHeight = 100, DecodePixelWidth = 100 };
        private static readonly BitmapImage darkNoPicMode = new BitmapImage(new Uri("ms-appx:/Assets/img_placeholder_night.png")) { DecodePixelHeight = 100, DecodePixelWidth = 100 };
        private static readonly Dictionary<ImageType, StorageFolder> folders = new Dictionary<ImageType, StorageFolder>();
        public static BitmapImage NoPic { get => SettingsHelper.Get<bool>(SettingsHelper.IsDarkMode) ? darkNoPicMode : whiteNoPicMode; }

        private static async Task<StorageFolder> GetFolderAsync(ImageType type)
        {
            StorageFolder folder;
            if (folders.ContainsKey(type))
            {
                folder = folders[type];
            }
            else
            {
                folder = await ApplicationData.Current.LocalCacheFolder.TryGetItemAsync(type.ToString()) as StorageFolder;
                if (folder is null)
                {
                    folder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(type.ToString(), CreationCollisionOption.OpenIfExists);
                }
                if (!folders.ContainsKey(type))
                {
                    folders.Add(type, folder);
                }
            }
            return folder;
        }

        public static async Task<BitmapImage> GetImageAsync(ImageType type, string url, bool showMessage = false)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }
            else if (url.IndexOf("ms-appx") == 0)
            {
                return new BitmapImage(new Uri(url));
            }
            else
            {
                var fileName = DataHelper.GetMD5(url);
                var folder = await GetFolderAsync(type);
                var item = await folder.TryGetItemAsync(fileName);
                if (type == ImageType.SmallImage || type == ImageType.SmallAvatar)
                {
                    url += ".s.jpg";
                }

                if (item is null)
                {
                    StorageFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
                    try { await DownloadImageAsync(file, url, showMessage); }
                    catch (FileLoadException) { return GetLocalImageAsync(file.Path); }
                    return GetLocalImageAsync(file.Path);
                }
                else
                {
                    return item is StorageFile file ? GetLocalImageAsync(file.Path) : null;
                }
            }
        }

        public static async Task<string> GetImagePathAsync(ImageType type, string url, bool showMessage = false)
        {
            if (url.IndexOf("ms-appx") == 0)
            {
                return url;
            }
            else if (string.IsNullOrEmpty(url) || SettingsHelper.Get<bool>(SettingsHelper.IsNoPicsMode))
            {
                return SettingsHelper.Get<bool>(SettingsHelper.IsDarkMode) ? "ms-appx:/Assets/img_placeholder_night.png" : "ms-appx:/Assets/img_placeholder.png";
            }
            else
            {
                string fileName = DataHelper.GetMD5(url);
                StorageFolder folder = await GetFolderAsync(type);
                var item = await folder.TryGetItemAsync(fileName);
                if (type == ImageType.SmallImage)
                {
                    url += ".s.jpg";
                }

                if (item is null)
                {
                    StorageFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
                    try { await DownloadImageAsync(file, url, showMessage); }
                    catch (FileLoadException) { return file.Path; }
                    return file.Path;
                }
                else
                {
                    return item is StorageFile file ? file.Path : null;
                }
            }
        }

        private static BitmapImage GetLocalImageAsync(string filename) =>
            (filename is null || SettingsHelper.Get<bool>(SettingsHelper.IsNoPicsMode)) ? NoPic : new BitmapImage(new Uri(filename));

        private static async Task DownloadImageAsync(StorageFile file, string url, bool showMessage)
        {
            if (!SettingsHelper.Get<bool>(SettingsHelper.IsNoPicsMode))
            {
                try
                {
                    //if (showMessage) UIHelper.ShowProgressRing();
                    using (Stream stream = await new HttpClient().GetStreamAsync(url))
                    using (Stream fs = await file.OpenStreamForWriteAsync())
                    {
                        await stream.CopyToAsync(fs);
                    }
                }
                catch (HttpRequestException) { UIHelper.ShowMessage("图片加载失败"); }
                //finally { if (showMessage) UIHelper.HideProgressRing(); }
            }
        }

        public static async Task CleanCacheAsync()
        {
            for (int i = 0; i < 5; i++)
            {
                var type = (ImageType)i;
                await (await GetFolderAsync(type)).DeleteAsync();
                await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(type.ToString());
            }
        }
    }
}