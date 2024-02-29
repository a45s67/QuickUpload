using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Windows.Markup;
using static System.Net.Mime.MediaTypeNames;

public class Image
{
    public string name { get; set; }
    public BitmapSource source { get; set; }
}

public class LogItem
{
    public string text { get; set; }
}

public class Logger
{
    private ObservableCollection<LogItem> log_items = new ObservableCollection<LogItem>();
    public void SetListBox(ref ListBox listBox)
    {
        listBox.ItemsSource = this.log_items;
    }
    public void AddLog(string log)
    {
        // [How to get correct timestamp in C# - Stack Overflow](https://stackoverflow.com/questions/21219797/how-to-get-correct-timestamp-in-c-sharp)
        this.log_items.Add(new LogItem() { text = log });
    }
}
public class Uploader
{
    private ObservableCollection<Image> images = new ObservableCollection<Image>();
    private Logger logger;
    ObservableCollection<Image> GetImages()
    {
        return this.images;
    }
    public void SetLogger(ref Logger logger)
    {
        this.logger = logger;
    }
    public void SetListBox(ref ListBox listBox)
    {
        listBox.ItemsSource = this.images;
    }
    public void AddImage(BitmapSource source)
    {
        this.logger.AddLog("Add image!");
        // [How to get correct timestamp in C# - Stack Overflow](https://stackoverflow.com/questions/21219797/how-to-get-correct-timestamp-in-c-sharp)
        images.Add(new Image() { name="clipboard - " + DateTime.Now.ToString("yyyyMMddHHmmssffff"), source = source });
    }
    public byte[] ToBytes(BitmapSource source)
    {
        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
        using MemoryStream stream = new MemoryStream();
        encoder.Frames.Add(BitmapFrame.Create(source));
        encoder.Save(stream);
        return stream.ToArray();
    }


    internal async void Upload(string RequestUrl)
    {
        using (var client = new HttpClient())
        {
            this.logger.AddLog($"Uploading...");
            MultipartFormDataContent form = new MultipartFormDataContent();
            foreach (var image in this.images)
            {
                form.Add(new ByteArrayContent(this.ToBytes(image.source)), "files", image.name + ".png");
            }

            HttpResponseMessage response = await client.PostAsync(RequestUrl, form);
            var result = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                this.logger.AddLog($"Upload finished. Result: {result}");
            }
            else
            {
                this.logger.AddLog($"Upload failed. Message: {result}");
            }



        }
    }

    internal void RemoveItem(object selectedItem)
    {
        Image img = (Image)selectedItem;
        this.logger.AddLog($"Remove image {img.name}");
        this.images.Remove(img);
    }
}

namespace DanbooruAgent
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Uploader uploader = new Uploader();
        private Logger logger = new Logger();
        public MainWindow()
        {
            InitializeComponent();
            KeyDown += MainWindow_KeyDown;
            this.logger.SetListBox(ref LogBox);
            this.uploader.SetListBox(ref ImageListBox);
            this.uploader.SetLogger(ref this.logger);
        }

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.V)
                {
                    BitmapSource bitmapSource = Clipboard.GetImage();
                    if (bitmapSource != null)
                    {
                        ImageBlock.Source = bitmapSource;
                        this.uploader.AddImage(bitmapSource);
                    }
                }
                else if(e.Key == Key.Enter)
                {
                    this.uploader.Upload(URLTextBox.Text);
                }
            } else if (e.Key == Key.Delete)
            {
                this.uploader.RemoveItem(ImageListBox.SelectedItem);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // imgDynamic.Source = Clipboard.GetImage();
        }

        private void ImageNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ImageListBox.SelectedItem != null)
            {
                ImageBlock.Source = (ImageListBox.SelectedItem as Image).source;
            }
        }
    }
}
