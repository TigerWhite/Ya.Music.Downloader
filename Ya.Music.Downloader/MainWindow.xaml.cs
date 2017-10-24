using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ya.Music.Downloader
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!editUrl.Text.Contains("music.yandex.ru"))
            {
                MessageBox.Show("Неверная ссылка для парсинга", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var music = Yandex.Music.CreateMusic(editUrl.Text);
            if(music == null)
            {
                MessageBox.Show("Указанная ссылка не ведет к музыкальным файлам", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            music.web.DownloadFileCompleted += (object send, AsyncCompletedEventArgs ev) => { MessageBox.Show("Файл скачан", "Готово", MessageBoxButton.OK, MessageBoxImage.Information); statusLabel.Content = "";  };
            music.web.DownloadProgressChanged += DownloadProgressChanged;
            music.Download();
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var client = (WebClient)sender;
            var header = client.ResponseHeaders[ HttpResponseHeader.ContentType];
            if (!header.Contains("audio"))
                return;

            statusLabel.Dispatcher.Invoke(() =>
            {
                statusLabel.Content = e.ProgressPercentage.ToString() + " %";
            });
            
        }
    }
}
