using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ya.Music.Downloader.Yandex
{
    abstract class Music
    {
        public abstract void Download();


        public int GetNumFiles()
        {
            return 0;
        }
        public int GetMbSize()
        {
            return 0;
        }
    }
}
