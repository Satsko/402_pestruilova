using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Threading;
using System.ComponentModel;
//using EmotionsLibrary;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Reflection.Metadata;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Windows.Markup;
using System.Net;
using Contracts;
using System.Net.NetworkInformation;
using System.Windows.Media.Animation;
using System.Runtime.ConstrainedExecution;
using System.Security.Policy;
using System.Threading.Tasks.Dataflow;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged(string propertyName) =>
           PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        CancellationTokenSource source;
        string[] files;
        private string url = "http://localhost:5227";
        private AsyncRetryPolicy _retryPolicy;
        private int maxRetries = 3;
        List<int> IDs = new List<int>();

        public ObservableCollection<ImageInfo> neutral = new ObservableCollection<ImageInfo>();
        public ObservableCollection<ImageInfo> happiness = new ObservableCollection<ImageInfo>();
        public ObservableCollection<ImageInfo> surprise = new ObservableCollection<ImageInfo>();
        public ObservableCollection<ImageInfo> sadness = new ObservableCollection<ImageInfo>();
        public ObservableCollection<ImageInfo> anger = new ObservableCollection<ImageInfo>();
        public ObservableCollection<ImageInfo> disgust = new ObservableCollection<ImageInfo>();
        public ObservableCollection<ImageInfo> fear = new ObservableCollection<ImageInfo>();
        public ObservableCollection<ImageInfo> contempt = new ObservableCollection<ImageInfo>();
        private SemaphoreSlim semaphore;
        public MainWindow()
        {
            InitializeComponent();
            lst_neutral.ItemsSource = neutral;
            lst_happiness.ItemsSource = happiness;
            lst_surprise.ItemsSource = surprise;
            lst_sadness.ItemsSource = sadness;
            lst_anger.ItemsSource = anger;
            lst_disgust.ItemsSource = disgust;
            lst_fear.ItemsSource = fear;
            lst_contempt.ItemsSource = contempt;
            Check.ItemsSource = IDs;
            semaphore = new SemaphoreSlim(1, 1);
            source = new CancellationTokenSource();
            _retryPolicy = Policy.Handle<HttpRequestException>().WaitAndRetryAsync(maxRetries, times =>
               TimeSpan.FromMilliseconds(3000));
        }

        private void Open_btn_click(object sender, RoutedEventArgs e)
        {
            var dlg = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            dlg.Multiselect = true;
            var result = dlg.ShowDialog();
            if (result == true)
            {
                files = dlg.FileNames;
            }
            Check.ItemsSource = files;
        }
        private async void Analysis_btn_click(object sender, RoutedEventArgs e)
        {
            Add_btn.IsEnabled = false;
            Analysis_btn.IsEnabled = false;
            Clear_btn.IsEnabled = false;
            AllID_btn.IsEnabled = false;
            ProgressBar.Maximum = files.Length;
            for (int i = 0; i < files.Length && !source.IsCancellationRequested; i++)
            {
                try
                {
                    await _retryPolicy.ExecuteAsync(async () =>
                    {
                        var httpClient = new HttpClient();
                        httpClient.BaseAddress = new Uri($"{url}/images");
                        byte[] bytesFile = await File.ReadAllBytesAsync(files[i], source.Token);
                        var img_data = new ImgPost(bytesFile, files[i]);
                        httpClient.DefaultRequestHeaders.Accept.Clear();
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        var response = await HttpClientJsonExtensions.PostAsJsonAsync(httpClient, "", img_data, source.Token);
                        if (response.IsSuccessStatusCode)
                        {
                            //System.Windows.Forms.MessageBox.Show("Success");
                            ProgressBar.Value+=1;
                            if (response.StatusCode == HttpStatusCode.Created)
                            {
                                System.Windows.Forms.MessageBox.Show("Created");
                            }
                            else if (response.StatusCode == HttpStatusCode.NoContent)
                                throw new OperationCanceledException("No images");
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show("Not Success: " + response.ReasonPhrase);
                        }
                    });
                }
                catch (OperationCanceledException ex)
                {
                    source = new CancellationTokenSource();
                    ProgressBar.Foreground = Brushes.OrangeRed;
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }
                catch (Exception ex)
                {
                    source = new CancellationTokenSource();
                    ProgressBar.Foreground = Brushes.OrangeRed;
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }
            }
            Add_btn.IsEnabled = true;
            Analysis_btn.IsEnabled = true;
            Clear_btn.IsEnabled = true;
            AllID_btn.IsEnabled = true;
        }
        private async void Clear_btn_click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clear_Collections();
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.DeleteAsync($"{url}/images");
                    if (response.IsSuccessStatusCode)
                    {
                        System.Windows.Forms.MessageBox.Show("Images are deleted");
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show(response.ReasonPhrase);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }
        private void Cancel_btn_click(object sender, RoutedEventArgs e)
        {
            source.Cancel();
        }
        private async void AllID_btn_click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync($"{url}/images");
                    Clear_Collections();
                    if (response.IsSuccessStatusCode)
                    {
                        IDs = await response.Content.ReadFromJsonAsync<List<int>>();
                        Check.ItemsSource = IDs;
                        if (IDs.Count ==0)
                        {
                            System.Windows.Forms.MessageBox.Show("No images");
                        }
                        foreach (int id in IDs)
                        {
                            var imagedata = await httpClient.GetAsync($"{url}/images/{id}");
                            ImgData idata = await imagedata.Content.ReadFromJsonAsync<ImgData>();
                            string max_em = Max_Emotion(idata);
                            ImageInfo new_item = new ImageInfo(idata.path, idata.emotions);
                            if (max_em == "neutral")
                            {
                                neutral.Add(new_item);
                            }
                            else if (max_em == "happiness")
                            {
                                happiness.Add(new_item);
                            }
                            else if (max_em == "surprise")
                            {
                                surprise.Add(new_item);
                            }
                            else if (max_em == "sadness")
                            {
                                sadness.Add(new_item);
                            }
                            else if (max_em == "anger")
                            {
                                anger.Add(new_item);
                            }
                            else if (max_em == "disgust")
                            {
                                disgust.Add(new_item);
                            }
                            else if (max_em == "fear")
                            {
                                fear.Add(new_item);
                            }
                            else if (max_em == "contempt")
                            {
                                contempt.Add(new_item);
                            }
                        }
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show(response.ReasonPhrase);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }
        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void Clear_Collections()
        {
            neutral = new ObservableCollection<ImageInfo>();
            happiness = new ObservableCollection<ImageInfo>();
            surprise = new ObservableCollection<ImageInfo>();
            sadness = new ObservableCollection<ImageInfo>();
            anger = new ObservableCollection<ImageInfo>();
            disgust = new ObservableCollection<ImageInfo>();
            fear = new ObservableCollection<ImageInfo>();
            contempt = new ObservableCollection<ImageInfo>();
            lst_neutral.ItemsSource = neutral;
            lst_happiness.ItemsSource = happiness;
            lst_surprise.ItemsSource = surprise;
            lst_sadness.ItemsSource = sadness;
            lst_anger.ItemsSource = anger;
            lst_disgust.ItemsSource = disgust;
            lst_fear.ItemsSource = fear;
            lst_contempt.ItemsSource = contempt;
            IDs = new List<int>();
            Check.ItemsSource = IDs;
        }
        private string Max_Emotion(ImgData i)
        {
            if (i.emotions == null) { return ""; }
            if (i.emotions.Count == 0) { return ""; }
            return i.emotions[0].Item1;
        }

    }
}

