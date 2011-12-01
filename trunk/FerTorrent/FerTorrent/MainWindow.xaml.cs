using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace FerTorrent
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //StreamReader reader = new StreamReader(new FileStream(Config.MetaPath + imefajla,FileMode.Open,FileAccess.Read));
            //benkoder itd
            //check exist
            //download/upload


        }

        private void preferences_Click(object sender, RoutedEventArgs e)
        {
            Preferences pref = new Preferences();
            this.frame1.Navigate(pref);
        }
        private void exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();        
        }

        private void open_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dia = new Microsoft.Win32.OpenFileDialog();
            dia.CheckFileExists = true;
            dia.CheckPathExists = true;
            dia.DefaultExt = ".torrent";
            dia.Multiselect = false;
            dia.Filter = "Torrent files |*.torrent";
            if (dia.ShowDialog() == true)
            {
                StreamReader reader = new StreamReader(dia.OpenFile());
                //bekoder bla bla
                //check exist
                //start download/upload
            }

        }


    }
}
