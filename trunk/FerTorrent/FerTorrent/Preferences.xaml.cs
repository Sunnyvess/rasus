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

namespace FerTorrent
{
    /// <summary>
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class Preferences : Page
    {
        public Preferences()
        {
            InitializeComponent();
            textBox1.Text = Config.MetaPath;
            textBox2.Text = Config.FilePath;
            textBox3.Text = Config.MaxIncoming.ToString();
            textBox4.Text = Config.MaxOutgoing.ToString();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Config.MetaPath=textBox1.Text;
            Config.FilePath=textBox2.Text;
            Config.MaxIncoming=Convert.ToInt32(textBox3.Text);
            Config.MaxOutgoing=Convert.ToInt32(textBox4.Text);
            Config.SaveConfig();
            this.NavigationService.GoBack();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }
    }
}
