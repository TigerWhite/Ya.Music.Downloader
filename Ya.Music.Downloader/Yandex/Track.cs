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
            trackID = Convert.ToInt32(Yandex.Tools.GetIdFromUrl(url, YaType.Track));
            albumID = Convert.ToInt32(Yandex.Tools.GetIdFromUrl(url, YaType.Album));
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

            Process.Start(new ProcessStartInfo(fileUrl));
        }

        async Task<string> GetDownloadUrl()
        {
            var url = "https://music.yandex.ru/handlers/track.jsx?track=" + trackID + "%3A" + albumID;
            WebClient web = new WebClient();
            if (Yandex.Tools.cookie != "")
                web.Headers[HttpRequestHeader.Cookie] = Yandex.Tools.cookie;
            string str = await web.DownloadStringTaskAsync(url);

            // На данный момент яндекс может усомниться в нас и отдаст капчу. Её нужно обработать.
            if (!Yandex.Tools.TryAnswerCaptcha(ref str))
            {
                Captcha.inCycle = true; // Восстановим значение по умолчанию, потмоу что мы можем выбрать другую ссылку
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
