using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Ya.Music.Downloader.Yandex
{
    class Playlist : Music
    {
        string user;
        int id;

        public Playlist(string url)
        {
            id = Convert.ToInt32(GetIdFromUrl(url, YaType.Playlists));
            user = GetIdFromUrl(url, YaType.Users);
        }

        public async override void Download()
        {
            var url = baseUrl + "/handlers/playlist.jsx?owner="+user + "&kinds=" + id;
            string str = await web.DownloadStringTaskAsync(url);
            // На данный момент яндекс может усомниться в нас и отдаст капчу. Её нужно обработать.
            if (!TryAnswerCaptcha(ref str))
            {
                MessageBox.Show("Вы отменили запрос на скачивание трека из-за отказа от распознавания капчи", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            JToken dataSet = JObject.Parse(str).First.First;
            var data = dataSet.SelectToken("tracks");
            string storage = data.SelectToken("storageDir").ToString();
            str = await web.DownloadStringTaskAsync("http://storage.music.yandex.ru/download-info/" + storage + "/2.mp3");

        }


    }
}
