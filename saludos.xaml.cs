using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfAnimatedGif;

namespace Deteccion
{
    public partial class saludos : Window
    {
        private DispatcherTimer timer;

        public saludos()
        {
            InitializeComponent();

            try
            {
                // Cargar la imagen GIF desde una carpeta local
                string gifPath = "C:\\Users\\Acer\\Documents\\Pdi\\Deteccion\\img\\emo.gif";
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(gifPath, UriKind.Absolute);
                image.EndInit();
                ImageBehavior.SetAnimatedSource(img, image);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la imagen: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Inicializar el DispatcherTimer
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5); // Intervalo de 5 segundos
            timer.Tick += Timer_Tick; // Evento que se ejecutará cuando termine el intervalo
            timer.Start(); // Iniciar el timer
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Detener el timer
            timer.Stop();

            // Cerrar la ventana actual
            Close();
        }
    }
}
