using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Video;
using AForge.Video.DirectShow;

namespace Deteccion
{
    public partial class MainWindow : Window
    {
        FilterInfoCollection cams;
        VideoCaptureDevice currentCam;
        Bitmap fileToChange;
        bool takeImages = false;
        System.Windows.Visibility visible = System.Windows.Visibility.Collapsed;
        Gestures GESTURE = Gestures.Nothing;
        int nroIm = 0;
        bool stopProcessing = false;
        string conta = "";


        private DispatcherTimer thumbDownTimer;
        private string memeImagePath = "C:\\Users\\Acer\\Documents\\Pdi\\Deteccion\\img\\meme.png";

        //codigo para emoji
        private string _emoji;

        public MainWindow()
        {
            InitializeComponent();
            cams = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo VideoCaptureDevice in cams)
            {
                cmbCameras.Items.Add(VideoCaptureDevice.Name);
            }
            if (cmbCameras.Items.Count > 0)
            {
                cmbCameras.SelectedIndex = 0;
            }
            Loaded += MainWindow_Loaded;

            // Inicializar el temporizador
            thumbDownTimer = new DispatcherTimer();
            thumbDownTimer.Interval = TimeSpan.FromSeconds(5);
            thumbDownTimer.Tick += ThumbDownTimer_Tick;
        } 

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (currentCam != null)
            {
                currentCam.Stop();
            }

            currentCam = new VideoCaptureDevice(cams[cmbCameras.SelectedIndex].MonikerString);
            currentCam.NewFrame += new NewFrameEventHandler(MyNewFrame);

            currentCam.VideoResolution = currentCam.VideoCapabilities[1];
            currentCam.Start();

            takeImages = true;
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessMyImage();
            }
            catch (Exception ex) { }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) { }

        void MyNewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap myaux = (Bitmap)eventArgs.Frame.Clone();
            BitmapImage bim = ToBitmapImage(myaux);

            fileToChange = myaux;

            if (takeImages)
            {
                MyDetector();
                takeImages = false;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                imgVideo.Source = bim;
                gestureResult.Text = GESTURE.ToString();
                contador.Text = conta;
            }));
        }

        public static BitmapImage ToBitmapImage(System.Drawing.Image bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        public static Bitmap BitmapImage2Bitmap(BitmapImage img)
        {
            using (var memory = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(img));
                enc.Save(memory);
                Bitmap btm = new Bitmap(memory);
                return new Bitmap(btm);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                currentCam.Stop();
            }
            catch (Exception) { }
        }

        Thread hilo;

        void MyDetector()
        {
            ThreadStart delegado = new ThreadStart(ProcessMyImage);
            hilo = new Thread(delegado);
            hilo.Start();
        }

        private async void ProcessMyImage()
        {
            Dispatcher.Invoke(() => visible = System.Windows.Visibility.Visible);

            Dispatcher.Invoke(() => conta = "3 ");
            Thread.Sleep(1000);

            Dispatcher.Invoke(() => conta = "2 ");
            Thread.Sleep(1000);

            Dispatcher.Invoke(() => conta = "1 ");
            Thread.Sleep(1000);

            Dispatcher.Invoke(() => conta = "0 ");
            Dispatcher.Invoke(() => gestureResult.Text = "Procesando Gesto");

            Thread.Sleep(1000);
            if (stopProcessing) return;

            using (HttpClient client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            {
                try
                {
                    ContrastCorrection he = new ContrastCorrection();
                    he.ApplyInPlace(fileToChange);
                    byte[] imageBytes = ConvertBitmapToBytes(fileToChange);
                    var imageContent = new ByteArrayContent(imageBytes);
                    content.Add(imageContent, "image", "image.jpg");

                    HttpResponseMessage response = await client.PostAsync(GeneralTools.ApiCpnnection, content);
                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        Dispatcher.Invoke(() => MessageBox.Show("La API requiere autenticación. Por favor, proporciona las credenciales necesarias."));
                        return;
                    }
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    GESTURE = GetGesture(responseBody);
                    Dispatcher.Invoke(() => MAKEACTION(GESTURE));
                }
                catch (HttpRequestException e)
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"Error al hacer la solicitud HTTP: {e.Message}"));
                }
            }
            Dispatcher.Invoke(() => visible = System.Windows.Visibility.Collapsed);

            if (!stopProcessing) ProcessMyImage();
        }

        public static byte[] ConvertBitmapToBytes(Bitmap bitmap)
        {
            try
            {
                Bitmap aux = new Bitmap(bitmap);
                using (MemoryStream stream = new MemoryStream())
                {
                    aux.Save(stream, ImageFormat.Bmp);
                    return stream.ToArray();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }


        Gestures GetGesture(string gest)
        {
            Console.WriteLine($"Respuesta de la API: {gest}");

            gest = gest.ToLower();
            if (gest.Contains("thumbs up") || gest.Contains("up") || gest.Contains("👍"))
            {
                return Gestures.ThumbsUp;
            }
            if (gest.Contains("thumbs down") || gest.Contains("down") || gest.Contains("👎"))
            {
                return Gestures.ThumbsDown;
            }
            if (gest.Contains("peace") || gest.Contains("victory") || gest.Contains("✌️"))
            {
                return Gestures.Peace;
            }
            if (gest.Contains("point up") || gest.Contains("☝️"))
            {
                return Gestures.PointUp;
            }
            if (gest.Contains("two fingers") || gest.Contains("2 fingers") || gest.Contains("two"))
            {
                return Gestures.TwoFingers;
            }
            if (gest.Contains("open palm") || gest.Contains("open") || gest.Contains("palm"))
            {
                return Gestures.OpenPalm;
            }
            if (gest.Contains("close")) 
            {
                return Gestures.Close; 
            }

            return Gestures.Nothing;
        }



        async void MAKEACTION(Gestures gesture)
        {
            switch (gesture)
            {
                case Gestures.ThumbsUp:
                        Dispatcher.Invoke(() => contador.Text = "Mostrando meme...");
                        await Task.Delay(2000);
                        ShowMemeImage(memeImagePath);
                    break;
                case Gestures.ThumbsDown:
                    OpenApplication("mspaint.exe"); //Paint
                    break;
                case Gestures.Peace:
                    System.Threading.Thread.Sleep(2000);
                    ShowSystemInfo();
                    break;
                case Gestures.TwoFingers:
                    OpenApplication("C:\\Program Files\\PuTTY\\putty.exe"); //Putty
                    break;
                case Gestures.OpenPalm:
                    saludos sal = new saludos();
                    sal.Show();

                    break;
                case Gestures.Close:
                        thumbDownTimer.Start();
                    break;
                case Gestures.None:
                case Gestures.Nothing:
                default:    
                    break;
            }
        }

        private void ThumbDownTimer_Tick(object sender, EventArgs e)
        {
            thumbDownTimer.Stop();
            SaveImage(fileToChange);
        }

        private void SaveImage(Bitmap bitmap)
        {
            try
            {
                string filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "capturedImage.png");
                bitmap.Save(filePath, ImageFormat.Png);
                Dispatcher.Invoke(() => MessageBox.Show($"Imagen guardada en: {filePath}"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Error al guardar la imagen: {ex.Message}"));
            }
        }


        //Ejecucion de procesos

        private void ShowMemeImage(string imagePath)
        {
            try
            {
                BitmapImage memeImage = new BitmapImage();
                memeImage.BeginInit();
                memeImage.UriSource = new Uri(imagePath, UriKind.Absolute);
                memeImage.CacheOption = BitmapCacheOption.OnLoad;
                memeImage.EndInit();
        
                Dispatcher.Invoke(() =>
                {
                    memeImageControl.Source = memeImage;
                    memeImageControl.Visibility = Visibility.Visible; // Mostrar el control Image si está oculto
                });
        
                // Crear un DispatcherTimer para ocultar la imagen después de 5 segundos
                DispatcherTimer hideTimer = new DispatcherTimer();
                hideTimer.Interval = TimeSpan.FromSeconds(5);
                hideTimer.Tick += (sender, args) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        memeImageControl.Visibility = Visibility.Collapsed;
                    });
                    hideTimer.Stop();
                };
                hideTimer.Start();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Error al cargar la imagen del meme: {ex.Message}"));
            }
        }



        void OpenApplication(string applicationPath, string arguments = "")
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(applicationPath);
                if (!string.IsNullOrEmpty(arguments))
                {
                    startInfo.Arguments = arguments;
                }
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir la aplicación: {ex.Message}");
            }
        }

        public static void ShowSystemInfo()
        {
            string hostName = Dns.GetHostName();
            string localIP = GetLocalIPAddress();
            string publicIP = GetPublicIPAddress();
            double totalRAM = GetTotalMemoryInGB();
            double totalStorage = GetTotalStorageInGB();
            double availableStorage = GetAvailableStorageInGB();

            string message = $"Nombre del Host: {hostName}\n" +
                             $"IP Local: {localIP}\n" +
                             $"IP Pública: {publicIP}\n" +
                             $"Memoria RAM Total: {totalRAM:F2} GB\n" +
                             $"Almacenamiento Total: {totalStorage:F2} GB\n" +
                             $"Almacenamiento Disponible: {availableStorage:F2} GB";

            MessageBox.Show(message, "Información del Sistema");
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No se encontró una IP local.");
        }

        private static string GetPublicIPAddress()
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadString("https://api.ipify.org");
            }
        }

        private static double GetTotalMemoryInGB()
        {
            var query = new ObjectQuery("SELECT * FROM Win32_ComputerSystem");
            using (var searcher = new ManagementObjectSearcher(query))
            {
                foreach (var item in searcher.Get())
                {
                    double totalMemoryInKB = Convert.ToDouble(item["TotalPhysicalMemory"]);
                    return totalMemoryInKB / (1024 * 1024 * 1024);
                }
            }
            return 0;
        }

        private static double GetAvailableStorageInGB()
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
            return drives.Sum(d => d.AvailableFreeSpace) / (1024 * 1024 * 1024);
        }

        private static double GetTotalStorageInGB()
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
            return drives.Sum(d => d.TotalSize) / (1024 * 1024 * 1024);
        }

    }

    public enum Gestures
    {
        ThumbsUp,
        ThumbsDown,
        CallMe,
        Peace,
        PointUp,
        TwoFingers,
        ThreeFingers,
        FourFingers,
        OpenPalm,
        Nothing,
        Close,
        None
    }
    





    public static class GeneralTools
    {
        public static string ApiCpnnection = "https://mygestures.onrender.com/recognize_gesture";
    }

    

}
