using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;
using System.ComponentModel;

namespace Ya.Music.Downloader.Yandex
{
    enum YaType
    {
        Track,
        Album,
        Playlists,
        Users,
        Unknown
    }

    static class Tools
    {
        private static Captcha _winCaptcha = new Captcha();
        public static Captcha WinCaptcha { get { return _winCaptcha; } }
        public static string cookie;
        

        public static YaType ParseType(string url)
        {
            var uri = new UriBuilder(url);
            var num = uri.Uri.Segments.Length;

            switch (uri.Uri.Segments[num - 2].Replace("/", ""))
            {
                case "track":
                    return YaType.Track;
                case "album":
                    return YaType.Album;
                case "playlists":
                    return YaType.Playlists;
                default:
                    return YaType.Unknown;
            }

        }

        public static string GetIdFromUrl(string url, YaType type)
        {
            string id = "-1";
            try
            {
                var uri = new UriBuilder(url);
                var num = uri.Uri.Segments.Length;
                for (int i = 0; i < num; i++)
                {
                    var name = uri.Uri.Segments[i].Replace("/", "").ToLower();
                    var typeStr = Enum.GetName(typeof(YaType), type).ToLower();
                    if (name == typeStr)
                    {
                        id = uri.Uri.Segments[i + 1].Replace("/", "");
                        break;
                    }
                }

            }
            catch (Exception) { }
            if (id == "-1")
                throw new ArgumentException("Невозможно определить id");
            return id;
        }

        public static bool TryAnswerCaptcha(ref string str)
        {
            
            while (str.Contains("html") && Captcha.inCycle)
            {
                var keys = new Dictionary<string, string>();
                string result = str;
                var doc = new HtmlDocument();
                doc.LoadHtml(str);
                var form = doc.DocumentNode.Descendants("form").First();
                var imgUrl = form.Descendants("img").First().GetAttributeValue("src", "");
                var inputs = form.Descendants("input");
                foreach (var item in inputs)
                {
                    keys.Add(item.GetAttributeValue("name", ""), item.GetAttributeValue("value", ""));
                }

                WebClient web = new WebClient();

                var captcha = web.DownloadData(imgUrl);
                WinCaptcha.Dispatcher.Invoke(() =>
                {
                    BitmapImage biImg = new BitmapImage();
                    MemoryStream ms = new MemoryStream(captcha);
                    biImg.BeginInit();
                    biImg.StreamSource = ms;
                    biImg.EndInit();
                    WinCaptcha.captchaImage.Source = biImg as ImageSource;

                    WinCaptcha.Closing += (object sender, CancelEventArgs e) =>
                    {
                        // Если мы закрыли форму сами, т.е не хотим вводить капчу - нужно не дать циклу поаказывать новую
                        if (!Captcha.inCycle)
                            Captcha.inCycle = true; // Выполняется только если окно закрыто по конпке отправить, поэтому нам нужноне выходить из цикла
                        else Captcha.inCycle = false;
                        _winCaptcha = new Captcha();
                    };

                    WinCaptcha.captchaSubmit.Click += (object sender, RoutedEventArgs e) =>
                    {
                        string url = "https://music.yandex.ru" + form.GetAttributeValue("action", "/checkcaptcha");
                        var param = string.Join("&", keys.Select((kvp) =>
                        {
                            if (kvp.Value == "")
                                return kvp.Key + "=" + WebUtility.UrlEncode(WinCaptcha.captchaTextBox.Text);
                            return kvp.Key + "=" + WebUtility.UrlEncode(kvp.Value);
                        }));
                        url += "?" + param;
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.CookieContainer = new CookieContainer();
                        request.AllowAutoRedirect = false;
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        string redirUrl = response.Headers["Location"];
                        response.Close();

                        if (response.Cookies.Count > 0 && redirUrl != null)
                        {
                            web.Headers[HttpRequestHeader.Cookie] = response.Cookies[0].ToString();
                            result = web.DownloadString(redirUrl);
                        }
                        // Необходимо закрывать форму и показывать снова если капча неверна
                        Captcha.inCycle = false; 
                        WinCaptcha.Close();

                    };

                    WinCaptcha.ShowDialog();
                    cookie = web.Headers[HttpRequestHeader.Cookie];
                });

                str = result;
            }

            return Captcha.inCycle;
        }

        public static string GetMd5Hash(string input)
        {
            var md5Hash = MD5.Create();
            // Convert the input string to a byte array and compute the hash. 
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes 
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string. 
            return sBuilder.ToString();
        }

        public static Music CreateMusic(string text)
        {
            var type = Tools.ParseType(text);
            switch (type)
            {
                case YaType.Track:
                    return new Yandex.Track(text);
                case YaType.Playlists:
                    return new Yandex.Playlist(text);
                case YaType.Album:
                case YaType.Unknown:
                default:
                    return null;
            }
        }
    }
}
