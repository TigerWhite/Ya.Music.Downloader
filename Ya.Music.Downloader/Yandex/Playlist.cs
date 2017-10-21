using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ya.Music.Downloader.Yandex;

namespace Ya.Music.Downloader.Yandex
{
    class Playlist : Music
    {
        string user;
        int id;

        public Playlist(string url)
        {
            id = Convert.ToInt32(Yandex.Tools.GetIdFromUrl(url, YaType.Playlists));
            user = Yandex.Tools.GetIdFromUrl(url, YaType.Users);

           
        }

        public override void Download()
        {
            //`${ this.baseUrl}/ handlers / playlist.jsx ? owner =${ username}            &kinds =${ playlistId}`
        }


    }
}
