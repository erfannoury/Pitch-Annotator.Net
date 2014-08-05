using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Media.Animation;

namespace Pitch_Annotator.Net
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // UI elements
        GroupBox layersListBox;
        GroupBox imagesListBox;
        GroupBox mainBox;
        ScrollViewer mainPane;
        Canvas paneCanvas;
        ListView imagesList;
        ListView layersLists;
        Image imViewer;
        static private double listViewWidth = 300.0;


        public MainWindow()
        {
            InitializeComponent();
            this.MinHeight = 600;
            this.MinWidth = 800;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            /// Main pane UI section
            createMainPaneUI();

            /// Lists UI section
            createListsSectionUI();
        }

        private void createMainPaneUI()
        {
            mainBox = new GroupBox() { Margin = new Thickness(0, 0, listViewWidth, 0), Header = "Image Pane" };
            mainPane = new ScrollViewer() { Margin = new Thickness(0) };
            mainBox.Content = mainPane;
            mainGrid.Children.Add(mainBox);
            mainPane.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            var image = new BitmapImage(new Uri(@"C:\Users\Erfan\Pictures\Screenshots\Screenshot (1).png"));
            imViewer = new Image() { Source = image };
            paneCanvas = new Canvas() { Width = image.Width, Height = image.Height };

            
            paneCanvas.Children.Add(imViewer);

            //paneCanvas.MouseWheel += paneCanvas_MouseWheel;
            mainPane.Content = paneCanvas;
        }
        
        private void createListsSectionUI()
        {
            var h = mainGrid.ActualHeight / 2;

            imagesListBox = new GroupBox() { Width = listViewWidth, Height = h, Margin = new Thickness(0), HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Header = "Images", VerticalAlignment = System.Windows.VerticalAlignment.Top };
            imagesList = new ListView();
            imagesListBox.Content = imagesList;
            mainGrid.Children.Add(imagesListBox);


            layersListBox = new GroupBox() { Width = listViewWidth, Height = h, Margin = new Thickness(0, h, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Header = "Layers", VerticalAlignment = System.Windows.VerticalAlignment.Top };
            layersLists = new ListView();
            layersListBox.Content = layersLists;
            mainGrid.Children.Add(layersListBox);
        }

        //void paneCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        //{
        //    Console.WriteLine("Delta = {0}", e.Delta);
        //    double zoom = e.Delta > 0 ? 1.1 : 0.9;
        //    double height = imViewer.Height;
        //    double width = imViewer.Width;
        //    height += zoom;
        //    width += zoom;
        //    paneCanvas.LayoutTransform = new ScaleTransform(zoom, zoom);
        //    //imViewer.LayoutTransform = new ScaleTransform(zoom, zoom);
        //    paneCanvas.UpdateLayout(); 

        //    Console.WriteLine("Canvas Width = {0}, Height = {1}", paneCanvas.Width, paneCanvas.Height);
        //    Console.WriteLine("Image Width = {0}, Height = {1}", imViewer.ActualWidth, imViewer.ActualHeight);
        //}

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(this.IsLoaded)
            {
                var h = mainGrid.ActualHeight / 2;
                imagesListBox.Height = h;
                layersListBox.Height = h;
                layersListBox.Margin = new Thickness(0, h, 0, 0);
            }
        }
    }
}
