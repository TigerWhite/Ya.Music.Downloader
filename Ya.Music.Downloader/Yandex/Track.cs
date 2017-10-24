using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace Ya.Music.Downloader.Yandex
{
    class Track : Music
    {
        // Данная строка собирается яндексом как соль, но при этом всегда одинакова. 
        private const string magicHash = "XGRlBW9FXlekgbPrRHuSiA";


        int trackID;
        int albumID;

        public Track(string url)
        {
            trackID = Convert.ToInt32(GetIdFromUrl(url, YaType.Track));
            albumID = Convert.ToInt32(GetIdFromUrl(url, YaType.Album));
        }
        public Track(int album, int track)
        {
            trackID = track;
            albumID = album;
        }

        public async override void Download()
        {
            var fileUrl = await Task.Run(GetDownloadUrl);
            if (fileUrl == "")
                return;

            var dialog = new SaveFileDialog();
            dialog.DefaultExt = ".mp3";
            dialog.Filter = "Музыка (*.mp3)|*.mp3";
            var result = dialog.ShowDialog();
            if (result == null || result == false)
                return;

            
            await web.DownloadFileTaskAsync(fileUrl, dialog.FileName);

        }

        async Task<string> GetDownloadUrl()
        {
            var url = baseUrl + "/handlers/track.jsx?track=" + trackID + "%3A" + albumID;
            string str = await web.DownloadStringTaskAsync(url);

            // На данный момент яндекс может усомниться в нас и отдаст капчу. Её нужно обработать.
            if (!TryAnswerCaptcha(ref str))
            {
                MessageBox.Show("Вы отменили запрос на скачивание трека из-за отказа от распознавания капчи", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
                return "";
            }

            JToken dataSet = JObject.Parse(str);
            var data = dataSet.SelectToken("track");
            string storage = data.SelectToken("storageDir").ToString();
            str = await web.DownloadStringTaskAsync("http://storage.music.yandex.ru/download-info/" + storage + "/2.mp3");

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(str);
            var info = xml["download-info"];

            string secret = Yandex.Tools.GetMd5Hash(magicHash + info["path"].InnerText.Substring(1) + info["s"].InnerText);
            return "http://" + info["host"].InnerText + "/get-mp3/" + secret + "/" + info["ts"].InnerText + info["path"].InnerText;
        }
    }
}
