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
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using ZoomAndPan;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.IO;
using CsvHelper;

namespace PitchAnnotator
{
    /// <summary>
    /// This is a Window that uses ZoomAndPanControl to zoom and pan around some content.
    /// This demonstrates how to use application specific mouse handling logic with ZoomAndPanControl.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// This is a reference to the last selected image item from the imagesList ListView
        /// </summary>
        object LastSelectedImageItem;

        /// <summary>
        /// This shows the current image entry that user is working on or null if application has just started and no image is selected yet.
        /// </summary>
        private ImageEntry CurrentImageEntry;

        /// <summary>
        /// This lists all the valid image extensions
        /// </summary>
        public static string[] ValidImageExtensions = new[] { ".jpeg", ".jpg", ".png" };

        /// <summary>
        /// This list contains all the image entries; all the images in the images folder and information about their respective annotation files
        /// </summary>
        List<ImageEntry> imageEntries;

        /// <summary>
        /// This is the address of the folder which contains all the image files that are going to be annotated.
        /// </summary>
        string ImagesFolderAddress { get; set; }

        /// <summary>
        /// This is the address of the folder which contains/will contain annotation files for images that are going to be annotated.
        /// </summary>
        string AnnotationFolderAddress { get; set; }

        /// <summary>
        /// To be in the vicinity of the dragging endpoint, ratio of the mouse cursor distance to the desired line endpoint to the line length should be less than this constant
        /// </summary>
        private static double LineLengthRatio = 0.2;

        /// <summary>
        /// This list will contain all the lines added to the image
        /// </summary>
        List<Line> lines;

        /// <summary>
        /// This GroupBox element surrounds the ListBox showing the layers on the image
        /// </summary>
        GroupBox layersListBox;

        /// <summary>
        /// This GroupBox element surrounds the ListBox showing the images in the selected folder
        /// </summary>
        GroupBox imagesListBox;

        /// <summary>
        /// This List shows the images in the selected folder
        /// </summary>
        ListView imagesList;

        /// <summary>
        /// This List shows the layers on the image
        /// </summary>
        ListView layersLists;

        /// <summary>
        /// This is the constant width of the layers view section
        /// </summary>
        static private double listViewWidth = 300.0;


        /// <summary>
        /// Specifies the current state of the mouse handling logic.
        /// </summary>
        private MouseHandlingMode mouseHandlingMode = MouseHandlingMode.None;

        /// <summary>
        /// The point that was clicked relative to the ZoomAndPanControl.
        /// </summary>
        private Point origZoomAndPanControlMouseDownPoint;

        /// <summary>
        /// The point that was clicked relative to the content that is contained within the ZoomAndPanControl.
        /// </summary>
        private Point origContentMouseDownPoint;

        /// <summary>
        /// Records which mouse button clicked during mouse dragging.
        /// </summary>
        private MouseButton mouseButtonDown;

        /// <summary>
        /// Saves the previous zoom rectangle, pressing the backspace key jumps back to this zoom rectangle.
        /// </summary>
        private Rect prevZoomRect;

        /// <summary>
        /// Save the previous content scale, pressing the backspace key jumps back to this scale.
        /// </summary>
        private double prevZoomScale;

        /// <summary>
        /// Set to 'true' when the previous zoom rect is saved.
        /// </summary>
        private bool prevZoomRectSet = false;

        /// <summary>
        /// This is the location where user starts to draw a new line
        /// </summary>
        private Point initialMouseLoc;

        /// <summary>
        /// The line that user is currently drawing
        /// </summary>
        private Line currLine;

        /// <summary>
        /// This is the constructor for the Main window of the appliation
        /// </summary>
        /// <param name="imFolder"> This is the address to the folder which contains all the images that are going to be annotated.</param>
        /// <param name="gtFolder"> This is the address to the folder which contains/will contain all the annotation data of the image files located in imFolder.</param>
        //public MainWindow(string imFolder, string gtFolder)
        public MainWindow()
        {
            InitializeComponent();
            lines = new List<Line>();
            imageEntries = new List<ImageEntry>();
            CurrentImageEntry = null;

            /// TEST
            ImagesFolderAddress = @"E:\Code Vault\Github\Pitch-Annotator.Net\dataset\images\test";
            AnnotationFolderAddress = @"E:\Code Vault\Github\Pitch-Annotator.Net\dataset\groundTruth\test";
            /// TEST

            PopulateImageEntries();
        }

        /// <summary>
        /// Event raised when the Window has loaded.
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            createListsSectionUI();
            //HelpTextWindow helpTextWindow = new HelpTextWindow();
            //helpTextWindow.Left = this.Left + this.Width + 5;
            //helpTextWindow.Top = this.Top;
            //helpTextWindow.Owner = this;
            //helpTextWindow.Show();
            UpdateImageListViewer();
        }

        /// <summary>
        /// This creates the lists section UI and adds the ui elements into the window
        /// </summary>
        private void createListsSectionUI()
        {
            var h = mainGrid.ActualHeight / 2;

            imagesListBox = new GroupBox() { Width = listViewWidth, Height = h, Margin = new Thickness(0), HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Header = "Images", VerticalAlignment = System.Windows.VerticalAlignment.Top };
            imagesList = new ListView() { SelectionMode = SelectionMode.Single };
            imagesListBox.Content = imagesList;
            imagesList.SelectionChanged += imagesList_SelectionChanged;
            mainGrid.Children.Add(imagesListBox);


            layersListBox = new GroupBox() { Width = listViewWidth, Height = h, Margin = new Thickness(0, h, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Header = "Layers", VerticalAlignment = System.Windows.VerticalAlignment.Top };
            layersLists = new ListView() { SelectionMode = SelectionMode.Single };
            layersListBox.Content = layersLists;
            mainGrid.Children.Add(layersListBox);
        }

        /// <summary>
        /// Event raised on mouse down in the ZoomAndPanControl.
        /// </summary>
        private void zoomAndPanControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            canvas.Focus();
            Keyboard.Focus(canvas);

            mouseButtonDown = e.ChangedButton;
            origZoomAndPanControlMouseDownPoint = e.GetPosition(zoomAndPanControl);
            origContentMouseDownPoint = e.GetPosition(canvas);

            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 &&
                e.ChangedButton == MouseButton.Left)
            {
                // Ctrl + left-down initiates line drawing mode
                mouseHandlingMode = MouseHandlingMode.LineDrawing;
                currLine = new Line()
                {
                    X1 = origContentMouseDownPoint.X,
                    Y1 = origContentMouseDownPoint.Y,
                    X2 = origContentMouseDownPoint.X,
                    Y2 = origContentMouseDownPoint.Y,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2,
                    Cursor = Cursors.Cross,
                };
                currLine.MouseDown += Line_MouseDown;
                currLine.MouseUp += Line_MouseUp;
                currLine.MouseMove += Line_MouseMove;
                canvas.Children.Add(currLine);
                updateStatusBarLocationLabels(currLine.X1, currLine.Y1, currLine.X2, currLine.Y2);

            }
            else if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0 &&
                (e.ChangedButton == MouseButton.Left ||
                 e.ChangedButton == MouseButton.Right))
            {
                // Shift + left- or right-down initiates zooming mode.
                mouseHandlingMode = MouseHandlingMode.Zooming;
            }
            else if (mouseButtonDown == MouseButton.Left)
            {
                // Just a plain old left-down initiates panning mode.
                mouseHandlingMode = MouseHandlingMode.Panning;
            }

            if (mouseHandlingMode != MouseHandlingMode.None)
            {
                // Capture the mouse so that we eventually receive the mouse up event.
                zoomAndPanControl.CaptureMouse();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Event raised on mouse up in the ZoomAndPanControl.
        /// </summary>
        private void zoomAndPanControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mouseHandlingMode != MouseHandlingMode.None)
            {
                if (mouseHandlingMode == MouseHandlingMode.Zooming)
                {
                    if (mouseButtonDown == MouseButton.Left)
                    {
                        // Shift + left-click zooms in on the content.
                        ZoomIn(origContentMouseDownPoint);
                    }
                    else if (mouseButtonDown == MouseButton.Right)
                    {
                        // Shift + left-click zooms out from the content.
                        ZoomOut(origContentMouseDownPoint);
                    }
                }
                else if (mouseHandlingMode == MouseHandlingMode.DragZooming)
                {
                    // When drag-zooming has finished we zoom in on the rectangle that was highlighted by the user.
                    ApplyDragZoomRect();
                }

                else if (mouseHandlingMode == MouseHandlingMode.LineDrawing)
                {
                    // When line creation has finished, add the line to the lines list
                    lines.Add(currLine);
                    canvas.Children.Remove(currLine);
                    updateLayersListView();
                    clearStatusBarLocationLabels();
                }

                zoomAndPanControl.ReleaseMouseCapture();
                mouseHandlingMode = MouseHandlingMode.None;
                e.Handled = true;
            }
        }


        /// <summary>
        /// Event raised on mouse move in the ZoomAndPanControl.
        /// </summary>
        private void zoomAndPanControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseHandlingMode == MouseHandlingMode.Panning)
            {
                //
                // The user is left-dragging the mouse.
                // Pan the viewport by the appropriate amount.
                //
                Point curContentMousePoint = e.GetPosition(canvas);
                Vector dragOffset = curContentMousePoint - origContentMouseDownPoint;

                zoomAndPanControl.ContentOffsetX -= dragOffset.X;
                zoomAndPanControl.ContentOffsetY -= dragOffset.Y;

                e.Handled = true;
            }
            else if (mouseHandlingMode == MouseHandlingMode.Zooming)
            {
                Point curZoomAndPanControlMousePoint = e.GetPosition(zoomAndPanControl);
                Vector dragOffset = curZoomAndPanControlMousePoint - origZoomAndPanControlMouseDownPoint;
                double dragThreshold = 10;
                if (mouseButtonDown == MouseButton.Left &&
                    (Math.Abs(dragOffset.X) > dragThreshold ||
                     Math.Abs(dragOffset.Y) > dragThreshold))
                {
                    //
                    // When Shift + left-down zooming mode and the user drags beyond the drag threshold,
                    // initiate drag zooming mode where the user can drag out a rectangle to select the area
                    // to zoom in on.
                    //
                    mouseHandlingMode = MouseHandlingMode.DragZooming;
                    Point curContentMousePoint = e.GetPosition(canvas);
                    InitDragZoomRect(origContentMouseDownPoint, curContentMousePoint);
                }

                e.Handled = true;
            }
            else if (mouseHandlingMode == MouseHandlingMode.DragZooming)
            {
                //
                // When in drag zooming mode continously update the position of the rectangle
                // that the user is dragging out.
                //
                Point curContentMousePoint = e.GetPosition(canvas);
                SetDragZoomRect(origContentMouseDownPoint, curContentMousePoint);

                e.Handled = true;
            }
            else if (mouseHandlingMode == MouseHandlingMode.LineDrawing)
            {
                //
                // When the user is drawing the line
                //
                Point curLoc = e.GetPosition(canvas);
                currLine.X2 = curLoc.X;
                currLine.Y2 = curLoc.Y;
                updateStatusBarLocationLabels(currLine.X1, currLine.Y1, currLine.X2, currLine.Y2);

                e.Handled = true;
            }
        }

        /// <summary>
        /// Event raised by rotating the mouse wheel
        /// </summary>
        private void zoomAndPanControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            if (e.Delta > 0)
            {
                Point curContentMousePoint = e.GetPosition(canvas);
                ZoomIn(curContentMousePoint);

            }
            else if (e.Delta < 0)
            {
                Point curContentMousePoint = e.GetPosition(canvas);
                ZoomOut(curContentMousePoint);
            }
        }

        /// <summary>
        /// The 'ZoomIn' command (bound to the plus key) was executed.
        /// </summary>
        private void ZoomIn_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ZoomIn(new Point(zoomAndPanControl.ContentZoomFocusX, zoomAndPanControl.ContentZoomFocusY));
        }

        /// <summary>
        /// The 'ZoomOut' command (bound to the minus key) was executed.
        /// </summary>
        private void ZoomOut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ZoomOut(new Point(zoomAndPanControl.ContentZoomFocusX, zoomAndPanControl.ContentZoomFocusY));
        }

        /// <summary>
        /// The 'JumpBackToPrevZoom' command was executed.
        /// </summary>
        private void JumpBackToPrevZoom_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            JumpBackToPrevZoom();
        }

        /// <summary>
        /// Determines whether the 'JumpBackToPrevZoom' command can be executed.
        /// </summary>
        private void JumpBackToPrevZoom_CanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = prevZoomRectSet;
        }

        /// <summary>
        /// The 'Fill' command was executed.
        /// </summary>
        private void Fill_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SavePrevZoomRect();

            zoomAndPanControl.AnimatedScaleToFit();
        }

        /// <summary>
        /// The 'OneHundredPercent' command was executed.
        /// </summary>
        private void OneHundredPercent_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SavePrevZoomRect();

            zoomAndPanControl.AnimatedZoomTo(1.0);
        }

        /// <summary>
        /// Jump back to the previous zoom level.
        /// </summary>
        private void JumpBackToPrevZoom()
        {
            zoomAndPanControl.AnimatedZoomTo(prevZoomScale, prevZoomRect);

            ClearPrevZoomRect();
        }

        /// <summary>
        /// Zoom the viewport out, centering on the specified point (in content coordinates).
        /// </summary>
        private void ZoomOut(Point contentZoomCenter)
        {
            zoomAndPanControl.ZoomAboutPoint(zoomAndPanControl.ContentScale - 0.1, contentZoomCenter);
        }

        /// <summary>
        /// Zoom the viewport in, centering on the specified point (in content coordinates).
        /// </summary>
        private void ZoomIn(Point contentZoomCenter)
        {
            zoomAndPanControl.ZoomAboutPoint(zoomAndPanControl.ContentScale + 0.1, contentZoomCenter);
        }

        /// <summary>
        /// Initialise the rectangle that the use is dragging out.
        /// </summary>
        private void InitDragZoomRect(Point pt1, Point pt2)
        {
            SetDragZoomRect(pt1, pt2);

            dragZoomCanvas.Visibility = Visibility.Visible;
            dragZoomBorder.Opacity = 0.5;
        }

        /// <summary>
        /// Update the position and size of the rectangle that user is dragging out.
        /// </summary>
        private void SetDragZoomRect(Point pt1, Point pt2)
        {
            double x, y, width, height;

            //
            // Deterine x,y,width and height of the rect inverting the points if necessary.
            // 

            if (pt2.X < pt1.X)
            {
                x = pt2.X;
                width = pt1.X - pt2.X;
            }
            else
            {
                x = pt1.X;
                width = pt2.X - pt1.X;
            }

            if (pt2.Y < pt1.Y)
            {
                y = pt2.Y;
                height = pt1.Y - pt2.Y;
            }
            else
            {
                y = pt1.Y;
                height = pt2.Y - pt1.Y;
            }

            //
            // Update the coordinates of the rectangle that is being dragged out by the user.
            // The we offset and rescale to convert from content coordinates.
            //
            Canvas.SetLeft(dragZoomBorder, x);
            Canvas.SetTop(dragZoomBorder, y);
            dragZoomBorder.Width = width;
            dragZoomBorder.Height = height;
        }

        /// <summary>
        /// When the user has finished dragging out the rectangle the zoom operation is applied.
        /// </summary>
        private void ApplyDragZoomRect()
        {
            //
            // Record the previous zoom level, so that we can jump back to it when the backspace key is pressed.
            //
            SavePrevZoomRect();

            //
            // Retreive the rectangle that the user draggged out and zoom in on it.
            //
            double contentX = Canvas.GetLeft(dragZoomBorder);
            double contentY = Canvas.GetTop(dragZoomBorder);
            double contentWidth = dragZoomBorder.Width;
            double contentHeight = dragZoomBorder.Height;
            zoomAndPanControl.AnimatedZoomTo(new Rect(contentX, contentY, contentWidth, contentHeight));

            FadeOutDragZoomRect();
        }

        //
        // Fade out the drag zoom rectangle.
        //
        private void FadeOutDragZoomRect()
        {
            AnimationHelper.StartAnimation(dragZoomBorder, Border.OpacityProperty, 0.0, 0.1,
                delegate(object sender, EventArgs e)
                {
                    dragZoomCanvas.Visibility = Visibility.Collapsed;
                });
        }

        //
        // Record the previous zoom level, so that we can jump back to it when the backspace key is pressed.
        //
        private void SavePrevZoomRect()
        {
            prevZoomRect = new Rect(zoomAndPanControl.ContentOffsetX, zoomAndPanControl.ContentOffsetY, zoomAndPanControl.ContentViewportWidth, zoomAndPanControl.ContentViewportHeight);
            prevZoomScale = zoomAndPanControl.ContentScale;
            prevZoomRectSet = true;
        }

        /// <summary>
        /// Clear the memory of the previous zoom level.
        /// </summary>
        private void ClearPrevZoomRect()
        {
            prevZoomRectSet = false;
        }

        /// <summary>
        /// Event raised when the user has double clicked in the zoom and pan control.
        /// </summary>
        private void zoomAndPanControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                Point doubleClickPoint = e.GetPosition(canvas);
                zoomAndPanControl.AnimatedSnapTo(doubleClickPoint);
            }
        }

        /// <summary>
        /// Event raised when mouse cursor is moving over a Line.
        /// </summary>
        void Line_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseHandlingMode != MouseHandlingMode.DraggingLines)
            {
                return;
            }

            Point curContentPoint = e.GetPosition(canvas);
            Line line = (Line)sender;

            // calculate length of the line
            double length = Math.Sqrt((line.X1 - line.X2) * (line.X1 - line.X2) + (line.Y1 - line.Y2) * (line.Y1 - line.Y2));

            // calculate cursor distance to first endpoint of the line
            double dist1 = Math.Sqrt((line.X1 - curContentPoint.X) * (line.X1 - curContentPoint.X) + (line.Y1 - curContentPoint.Y) * (line.Y1 - curContentPoint.Y));
            // calculate cursor distance to second encpoint of the line
            double dist2 = Math.Sqrt((line.X2 - curContentPoint.X) * (line.X2 - curContentPoint.X) + (line.Y2 - curContentPoint.Y) * (line.Y2 - curContentPoint.Y));

            // mouse cursor is close enough to the first endpoint, so first endpoint will be translated
            if (dist1 / length <= LineLengthRatio)
            {
                line.X1 = curContentPoint.X;
                line.Y1 = curContentPoint.Y;
            }
            // mouse cursor is close enough to the second endpoint, so second endpoint will be translated
            else if (dist2 / length <= LineLengthRatio)
            {
                line.X2 = curContentPoint.X;
                line.Y2 = curContentPoint.Y;
            }
            else
            {
                // do nothing
                // TODO: maybe you can drag the line in this case. This feature might be added in future
            }

            updateStatusBarLocationLabels(line.X1, line.Y1, line.X2, line.Y2);
        }

        /// <summary>
        /// Event raised when mouse cursor is released over a Line.
        /// </summary>
        void Line_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mouseHandlingMode != MouseHandlingMode.DraggingLines)
                return;

            lines.Add(currLine);
            currLine.ReleaseMouseCapture();
            mouseHandlingMode = MouseHandlingMode.None;

            canvas.Children.Remove(currLine);
            updateLayersListView();

            clearStatusBarLocationLabels();

            e.Handled = true;
        }

        /// <summary>
        /// Event raised when mouse cursor is clicked down when over a Line.
        /// </summary>
        void Line_MouseDown(object sender, MouseButtonEventArgs e)
        {
            canvas.Focus();
            Keyboard.Focus(canvas);

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                //
                // When the shift key is held down special zooming logic is executed in content_MouseDown,
                // so don't handle mouse input here.
                //
                return;
            }

            if (mouseHandlingMode != MouseHandlingMode.None)
            {
                //
                // We are in some other mouse handling mode, don't do anything.
                return;
            }

            mouseHandlingMode = MouseHandlingMode.DraggingLines;
            origContentMouseDownPoint = e.GetPosition(canvas);

            // Copy the old line, then remove it, work with the temporary line and then add it to the list
            Line line = (Line)sender;
            currLine = line;
            lines.Remove(line);
            canvas.Children.Remove(line);
            canvas.Children.Add(currLine);
            currLine.CaptureMouse();

            e.Handled = true;
        }


        /// <summary>
        /// When the window size changes, update the layout for list views
        /// </summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                var h = mainGrid.ActualHeight / 2;
                imagesListBox.Height = h;
                layersListBox.Height = h;
                layersListBox.Margin = new Thickness(0, h, 0, 0);
            }
        }

        /// <summary>
        /// This will update the labels showing endpoint locations in the statis bar
        /// </summary>
        private void updateStatusBarLocationLabels(double x1, double y1, double x2, double y2)
        {
            firstPointLbl.Content = string.Format("From ({0}, {1})", x1.ToString("F3"), y1.ToString("F3"));
            secondPointLbl.Content = string.Format("To ({0}, {1})", x2.ToString("F3"), y2.ToString("F3"));
        }

        /// <summary>
        /// This will clear the content of the location labels in the status bar
        /// </summary>
        private void clearStatusBarLocationLabels()
        {
            firstPointLbl.Content = "";
            secondPointLbl.Content = "";
        }

        /// <summary>
        /// This will locate all the image files in the specified folder and try to find their respective annotation files
        /// </summary>
        private void PopulateImageEntries()
        {
            imageEntries.Clear();
            foreach(var file in Directory.EnumerateFiles(ImagesFolderAddress, "*", SearchOption.AllDirectories))
            {
                if(ValidImageExtensions.Contains(System.IO.Path.GetExtension(file)))
                    imageEntries.Add(new ImageEntry(file, AnnotationFolderAddress));
            }
        }

        /// <summary>
        /// This will update the images listviewer to take into account the latest changes or at the beginning of openning the application
        /// </summary>
        private void UpdateImageListViewer()
        {
            imagesList.Items.Clear();
            PopulateImageEntries();
            imageEntries.Sort();
            foreach(var ent in imageEntries)
            {
                imagesList.Items.Add(ent.GetListViewItem());
            }
        }

        /// <summary>
        /// Event raised when selection of the images list view changes
        /// </summary>
        void imagesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool saved = SaveCurrentImageOutput();
            string imName;
            if(imagesList.SelectedItem != null)
            {
                imName = (string)((Label)(((Grid)imagesList.SelectedItem).Children[0])).Content;
                CurrentImageEntry = imageEntries.Find(a => a.ImageName == imName);
                DisplayImageAsCanvasBackground();
                DisplayNewAnnotation();
            }
            if(LastSelectedImageItem != null)
            {
                var g = (Grid)LastSelectedImageItem;
                g.Background = saved ? Brushes.LightGreen : Brushes.PaleVioletRed;

            }
            LastSelectedImageItem = imagesList.SelectedItem;

        }
        
        /// <summary>
        /// Displays the selected image in the viewport. To display the image, it is set as the canvas's background.
        /// </summary>
        private void DisplayImageAsCanvasBackground()
        {
            BitmapImage im = new BitmapImage(new Uri(CurrentImageEntry.ImageAddress));
            this.theGrid.Height = im.Height;
            this.theGrid.Width = im.Width;
            canvas.Background = new ImageBrush(im);

        }

        /// <summary>
        /// When a new image is selected, lines displayed on the canvas will be deleted and new lines from the currently selected image's annotation file will be displayed
        /// </summary>
        private void DisplayNewAnnotation()
        {
            // First delete all the lines displayed on the canvas
            canvas.Children.Clear();
            // then clear the lines List containing all the annotated lines
            lines.Clear();

            // if the currently selected image already has annotation file, add all the lines to the proper lists
            if(CurrentImageEntry.HasAnnotation)
            {
                using (var streamreader = new StreamReader(CurrentImageEntry.AnnotationAddress))
                {
                    CsvReader csv = new CsvReader(streamreader, new CsvHelper.Configuration.CsvConfiguration() { HasHeaderRecord = false });
                    while(csv.Read())
                    {
                        var line = new Line()
                        {
                            X1 = csv.GetField<float>(0),
                            X2 = csv.GetField<float>(1),
                            Y1 = csv.GetField<float>(2),
                            Y2 = csv.GetField<float>(3),
                            Stroke = Brushes.Red,
                            StrokeThickness = 2,
                            Cursor = Cursors.Cross,
                        };
                        line.MouseDown += Line_MouseDown;
                        line.MouseUp += Line_MouseUp;
                        line.MouseMove += Line_MouseMove;
                        lines.Add(line);
                        canvas.Children.Add(line);
                    }
                }
            }
            updateLayersListView();
        }

        /// <summary>
        /// This will save the output for the current image entry that user was working on
        /// </summary>
        private bool SaveCurrentImageOutput()
        {
            if (CurrentImageEntry == null)
                return false;
            var outpath = CurrentImageEntry.AnnotationAddress;
            using (var streamwriter = new StreamWriter(CurrentImageEntry.AnnotationAddress, false))
            {
                var csvwriter = new CsvWriter(streamwriter);
                foreach (var line in lines)
                {
                    csvwriter.WriteField<double>(line.X1);
                    csvwriter.WriteField<double>(line.X2);
                    csvwriter.WriteField<double>(line.Y1);
                    csvwriter.WriteField<double>(line.Y2);
                    csvwriter.NextRecord();
                }
            }
            if (lines.Count == 0)
                return false;

            return true;
        }

        /// <summary>
        /// When a new line is added, a line is modified or deleted, this will be called to update the list view showing layers
        /// </summary>
        private void updateLayersListView()
        {
            layersLists.Items.Clear();
            foreach (var line in lines)
            {
                if (!canvas.Children.Contains(line))
                {
                    canvas.Children.Add(line);
                }
                layersLists.Items.Add(new LineListItem()
                {
                    Content = string.Format("Line ({0},{1}) - ({2},{3})",
                        line.X1.ToString("F3"), line.Y1.ToString("F3"),
                        line.X2.ToString("F3"), line.Y2.ToString("F3")),
                        LineReference = line
                });
            }
        }

        /// <summary>
        /// This is executed when a layer in line layers list is being deleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteLineLayer_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if(layersLists.SelectedItem != null)
            {
                var item = layersLists.SelectedItem as LineListItem;
                canvas.Children.Remove(item.LineReference);
                lines.Remove(item.LineReference);
                layersLists.Items.Remove(item);
            }
        }
    }
}
