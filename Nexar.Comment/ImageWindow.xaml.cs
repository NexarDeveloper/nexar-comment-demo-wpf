using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Nexar.Comment
{
    /// <summary>
    /// Interaction logic for ImageWindow.xaml
    /// </summary>
    public partial class ImageWindow : Window
    {
        public ImageWindow(string url)
        {
            InitializeComponent();

            var resourceUri = new Uri(url, UriKind.Absolute);
            MyImage.Source = new BitmapImage(resourceUri);
        }
    }
}
