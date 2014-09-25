using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
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
        /// A transparent red color used for the painter cursor on the inkcanvas
        /// </summary>
        private readonly SolidColorBrush transparentRed;

        /// <summary>
        /// A transparent white color used for the eraser cursor on the inkcanvas
        /// </summary>
        private readonly SolidColorBrush transparentWhite;

        /// <summary>
        /// Default size for the canvas cursor size
        /// </summary>
        private const int CanvasCursorDefaultSize = 20;

        /// <summary>
        /// This ellipse is used
        /// </summary>
        private Ellipse inkCanvasEllipse;

        /// <summary>
        /// A boolean to show whether currently in the brushing mode, eraser is enabled or not
        /// </summary>
        private bool IsEraserMode = false;
        
        /// <summary>
        /// This LineClipper object is for clipping the lines annotated by user to be inside the bounds of image
        /// </summary>
        private LineClipper Clipper;

        /// <summary>
        /// This additional margin allows panning beyond the image border
        /// </summary>
        static int CanvasGridMargin = 50;

        /// <summary>
        /// This is the selected endpoint that user can move using the keyboard arrow keys
        /// Line: a reference to the line that we want to update one of its endpoint's location
        /// int: {1,2} which determines which endpoints to update
        /// </summary>
        Tuple<Line, int> SelectedLineEndpoint;

        /// <summary>
        /// When user wants to move an endpoint of a line, increase or decrease the corresponding coordinate by LineEndpointDelta
        /// </summary>
        static double LineEndpointDelta = 0.5f;

        /// <summary>
        /// This is a reference to the last selected image item from the layersList ListView
        /// </summary>
        LineEntry LastSelectedLineItem;

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
        /// This list will contain all the lineEntries added to the image
        /// </summary>
        List<LineEntry> lineEntries;

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
        /// The line that user is currently drawing
        /// </summary>
        private LineEntry currLineEntry;

        /// <summary>
        /// This is the constructor for the Main window of the appliation
        /// </summary>
        /// <param name="imFolder"> This is the address to the folder which contains all the images that are going to be annotated.</param>
        /// <param name="gtFolder"> This is the address to the folder which contains/will contain all the annotation data of the image files located in imFolder.</param>
        public MainWindow(string imFolder, string annotFolder)
        {
            InitializeComponent();

            transparentRed = new SolidColorBrush(Colors.IndianRed);
            transparentRed.Opacity = 0.5;
            transparentWhite = new SolidColorBrush(Colors.White);
            transparentWhite.Opacity = 0.5;

            lineEntries = new List<LineEntry>();
            imageEntries = new List<ImageEntry>();
            CurrentImageEntry = null;
            ImagesFolderAddress = imFolder;
            AnnotationFolderAddress = annotFolder;
            PopulateImageEntries();
        }

        /// <summary>
        /// Event raised when the Window has loaded.
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            createListsSectionUI();
            UpdateImageListViewer();
            //HelpTextWindow helpTextWindow = new HelpTextWindow();
            //helpTextWindow.Left = this.Left + this.Width + 5;
            //helpTextWindow.Top = this.Top;
            //helpTextWindow.Owner = this;
            //helpTextWindow.Show();

            inkCanvasEllipse = new Ellipse() {Width = CanvasCursorDefaultSize, Height = CanvasCursorDefaultSize, Fill = transparentRed, Visibility = Visibility.Hidden};
            inkcanvas.Visibility = Visibility.Hidden;
            inkcanvas.Background = new SolidColorBrush(Colors.Transparent);
            inkcanvas.DefaultDrawingAttributes.Width = CanvasCursorDefaultSize;
            inkcanvas.DefaultDrawingAttributes.Height = CanvasCursorDefaultSize;
            inkcanvas.DefaultDrawingAttributes.Color = Colors.IndianRed;
            inkcanvas.DefaultDrawingAttributes.IsHighlighter = true;
            inkcanvas.UseCustomCursor = true;
            inkcanvas.Children.Add(inkCanvasEllipse);

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
            layersLists.SelectionChanged += layersLists_SelectionChanged;
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
                currLineEntry = new LineEntry(origContentMouseDownPoint.X, origContentMouseDownPoint.Y, origContentMouseDownPoint.X, origContentMouseDownPoint.Y);
                currLineEntry.line.MouseDown += Line_MouseDown;
                currLineEntry.line.MouseUp += Line_MouseUp;
                currLineEntry.line.MouseMove += Line_MouseMove;
                canvas.Children.Add(currLineEntry.line);
                lineEntries.Add(currLineEntry);
                updateLayersListView(currLineEntry);
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
                    Point curContentMousePoint = e.GetPosition(theGrid);
                    var marginalizedOrigContentMouseDownPoint = new Point(origContentMouseDownPoint.X + CanvasGridMargin, origContentMouseDownPoint.Y + CanvasGridMargin);
                    InitDragZoomRect(marginalizedOrigContentMouseDownPoint, curContentMousePoint);
                }

                e.Handled = true;
            }
            else if (mouseHandlingMode == MouseHandlingMode.DragZooming)
            {
                //
                // When in drag zooming mode continously update the position of the rectangle
                // that the user is dragging out.
                //
                Point curContentMousePoint = e.GetPosition(theGrid);
                var marginalizedOrigContentMouseDownPoint = new Point(origContentMouseDownPoint.X + CanvasGridMargin, origContentMouseDownPoint.Y + CanvasGridMargin);
                SetDragZoomRect(marginalizedOrigContentMouseDownPoint, curContentMousePoint);

                e.Handled = true;
            }
            else if (mouseHandlingMode == MouseHandlingMode.LineDrawing)
            {
                //
                // When the user is drawing the line
                //
                Point curLoc = e.GetPosition(canvas);
                currLineEntry.line.X2 = curLoc.X;
                currLineEntry.line.Y2 = curLoc.Y;

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
                Point curContentMousePoint = e.GetPosition(theGrid);
                ZoomIn(curContentMousePoint);

            }
            else if (e.Delta < 0)
            {
                Point curContentMousePoint = e.GetPosition(theGrid);
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
                Point doubleClickPoint = e.GetPosition(theGrid);
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
        }

        /// <summary>
        /// Event raised when mouse cursor is released over a Line.
        /// </summary>
        void Line_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (mouseHandlingMode != MouseHandlingMode.DraggingLines)
                return;

            Line line = (Line)sender;
            line.ReleaseMouseCapture();
            mouseHandlingMode = MouseHandlingMode.None;

            e.Handled = true;
        }

        /// <summary>
        /// Event raised when mouse cursor is clicked down when over a Line.
        /// </summary>
        void Line_MouseDown(object sender, MouseButtonEventArgs e)
        {
            canvas.Focus();
            Keyboard.Focus(canvas);

            // When the shift key is held down special zooming logic is executed in content_MouseDown,
            // so don't handle mouse input here.
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                return;
            }

            // We are in some other mouse handling mode, don't do anything.
            if (mouseHandlingMode != MouseHandlingMode.None)
            {
                return;
            }

            /// Edit line, only if `Alt` key is pressed
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == 0)
            {
                return;
            }

            mouseHandlingMode = MouseHandlingMode.DraggingLines;
            origContentMouseDownPoint = e.GetPosition(canvas);

            // Copy the old line, then remove it, work with the temporary line and then add it to the list
            Line line = (Line)sender;
            line.CaptureMouse();

            /// Logic to update the endpoint that then user can adjust its location using keyboard arrow keys

            Point curContentPoint = e.GetPosition(canvas);

            // calculate length of the line
            double length = Math.Sqrt((line.X1 - line.X2) * (line.X1 - line.X2) + (line.Y1 - line.Y2) * (line.Y1 - line.Y2));

            // calculate cursor distance to first endpoint of the line
            double dist1 = Math.Sqrt((line.X1 - curContentPoint.X) * (line.X1 - curContentPoint.X) + (line.Y1 - curContentPoint.Y) * (line.Y1 - curContentPoint.Y));
            // calculate cursor distance to second encpoint of the line
            double dist2 = Math.Sqrt((line.X2 - curContentPoint.X) * (line.X2 - curContentPoint.X) + (line.Y2 - curContentPoint.Y) * (line.Y2 - curContentPoint.Y));

            // mouse cursor is close enough to the first endpoint, so first endpoint will be translated
            if (dist1 / length <= LineLengthRatio)
            {
                SelectedLineEndpoint = new Tuple<Line, int>(line, 1);
            }
            // mouse cursor is close enough to the second endpoint, so second endpoint will be translated
            else if (dist2 / length <= LineLengthRatio)
            {
                SelectedLineEndpoint = new Tuple<Line, int>(line, 2);
            }

            /// Logic to update the endpoint that then user can adjust its location using keyboard arrow keys

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
        /// This will locate all the image files in the specified folder and try to find their respective annotation files
        /// </summary>
        private void PopulateImageEntries()
        {
            imageEntries.Clear();
            foreach (var file in Directory.EnumerateFiles(ImagesFolderAddress, "*", SearchOption.AllDirectories))
            {
                if (ValidImageExtensions.Contains(System.IO.Path.GetExtension(file)))
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
            foreach (var ent in imageEntries)
            {
                imagesList.Items.Add(ent.GetListViewItem());
            }
        }

        /// <summary>
        /// Event raised when selection of the images list view changes
        /// </summary>
        void imagesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /// Retunrn the focus to the canvas
            canvas.Focus();
            Keyboard.Focus(canvas);

            bool saved = SaveCurrentImageOutput();
            string imName;
            if (imagesList.SelectedItem != null)
            {
                imName = (string)((Label)(((Grid)imagesList.SelectedItem).Children[0])).Content;
                CurrentImageEntry = imageEntries.Find(a => a.ImageName == imName);
                DisplayImageAsCanvasBackground();
                DisplayNewAnnotation();
                DisplayInkCanvas();
            }
            if (LastSelectedImageItem != null)
            {
                var g = (Grid)LastSelectedImageItem;
                g.Background = saved ? Brushes.LightGreen : Brushes.PaleVioletRed;

            }
            LastSelectedImageItem = imagesList.SelectedItem;

        }

        /// <summary>
        /// If current image entry had InkStrokeFile saved before, it will load the strokes file and insert those strokes inside the InkCanvas
        /// </summary>
        private void DisplayInkCanvas()
        {
            inkcanvas.Strokes.Clear();
            if (CurrentImageEntry.HasInkStrokeFile)
            {
                using (FileStream fs = new FileStream(CurrentImageEntry.InkStrokeFileAddress, FileMode.Open, FileAccess.Read))
                {
                    StrokeCollection strokes = new StrokeCollection(fs);
                    inkcanvas.Strokes = strokes;
                    fs.Close();
                }
            }
        }

        /// <summary>
        /// Displays the selected image in the viewport. To display the image, it is set as the canvas's background.
        /// </summary>
        private void DisplayImageAsCanvasBackground()
        {
            BitmapImage im = new BitmapImage(new Uri(CurrentImageEntry.ImageAddress));
            this.theGrid.Height = im.Height + 2 * CanvasGridMargin;
            this.theGrid.Width = im.Width + 2 * CanvasGridMargin;
            canvas.Background = new ImageBrush(im);
            canvas.Margin = new Thickness(CanvasGridMargin);

            inkcanvas.Height = im.Height;
            inkcanvas.Width = im.Width;

            /// Update the LineClipper object so that when saving the output, lines be clipped to the bounds of the image
            Clipper = new LineClipper(im.Width, im.Height);
        }

        /// <summary>
        /// When a new image is selected, lineEntries displayed on the canvas will be deleted and new lineEntries from the currently selected image's annotation file will be displayed
        /// </summary>
        private void DisplayNewAnnotation()
        {
            // First delete all the lineEntries displayed on the canvas
            canvas.Children.Clear();
            // then clear the lineEntries List containing all the annotated lineEntries
            lineEntries.Clear();

            layersLists.Items.Clear();

            // if the currently selected image already has annotation file, add all the lineEntries to the proper lists
            if (CurrentImageEntry.HasAnnotation)
            {
                using (var streamreader = new StreamReader(CurrentImageEntry.AnnotationAddress))
                {
                    CsvReader csv = new CsvReader(streamreader, new CsvHelper.Configuration.CsvConfiguration() { HasHeaderRecord = false });
                    while (csv.Read())
                    {
                        var lineEntry = new LineEntry(csv.GetField<float>(0), csv.GetField<float>(1), csv.GetField<float>(2), csv.GetField<float>(3));
                        lineEntry.line.MouseDown += Line_MouseDown;
                        lineEntry.line.MouseUp += Line_MouseUp;
                        lineEntry.line.MouseMove += Line_MouseMove;
                        lineEntries.Add(lineEntry);
                        updateLayersListView(lineEntry);
                        canvas.Children.Add(lineEntry.line);
                    }
                }
            }
        }

        /// <summary>
        /// This will save the output for the current image entry that user was working on
        /// </summary>
        private bool SaveCurrentImageOutput()
        {
            outputsavedLbl.Visibility = System.Windows.Visibility.Visible;

            // This thread will hide the output saved label after one second
            Thread lblThread = new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(1000);
                try
                {
                    if (this.IsVisible)
                    {
                        this.Dispatcher.Invoke(() =>
                            {
                                outputsavedLbl.Visibility = System.Windows.Visibility.Hidden;
                            });
                    }
                }
                catch (Exception e)
                {
                    if (CurrentImageEntry != null)
                    {
                        Console.WriteLine("Exception occured when saving image {0}'s  output.", CurrentImageEntry.ImageName);
                        Console.WriteLine("Exception message:\n{0}", e.Message);
                    }
                }
            }));

            lblThread.Start();


            if (CurrentImageEntry == null)
                return false;
            var outpath = CurrentImageEntry.AnnotationAddress;
            using (var streamwriter = new StreamWriter(CurrentImageEntry.AnnotationAddress, false))
            {
                var csvwriter = new CsvWriter(streamwriter);
                foreach (var lineEnt in lineEntries)
                {
                    if (Clipper != null)
                        Clipper.Clip(lineEnt.line);
                    csvwriter.WriteField<double>(lineEnt.line.X1);
                    csvwriter.WriteField<double>(lineEnt.line.Y1);
                    csvwriter.WriteField<double>(lineEnt.line.X2);
                    csvwriter.WriteField<double>(lineEnt.line.Y2);
                    csvwriter.NextRecord();
                }
            }

            // Save stroke output if there is at least one
            if (inkcanvas.Strokes.Count > 0)
            {
                // Save the InkStrokeFile
                using (
                    var fs = new FileStream(CurrentImageEntry.InkStrokeFileAddress, FileMode.Create, FileAccess.Write)
                    )
                {
                    inkcanvas.Strokes.Save(fs);
                    fs.Close();
                }

                // Save the InkStroke as bitmap file
                var leftMargin = int.Parse(inkcanvas.Margin.Left.ToString());
                var rtBitmap = new RenderTargetBitmap((int)inkcanvas.ActualWidth, (int)inkcanvas.ActualHeight, 0,0,PixelFormats.Default);
                rtBitmap.Render(inkcanvas);
                var pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(rtBitmap));
                using (var fs = new FileStream(CurrentImageEntry.InkStrokeBitmap, FileMode.Create, FileAccess.Write))
                {
                    pngEncoder.Save(fs);
                }
            }
            else if (inkcanvas.Strokes.Count == 0)
            {
                File.Delete(CurrentImageEntry.InkStrokeFileAddress);
                File.Delete(CurrentImageEntry.InkStrokeBitmap);
            }

            if (lineEntries.Count == 0)
            {
                File.Delete(CurrentImageEntry.AnnotationAddress);
                return false;
            }

            return true;
        }

        /// <summary>
        /// When a new line is added, a line is modified or deleted, this will be called to update the list view showing layers
        /// </summary>
        private void updateLayersListView(LineEntry lineEntry)
        {
            layersLists.Items.Add(lineEntry);
            layersLists.SelectedItem = lineEntry;
        }

        /// <summary>
        /// This is executed when a layer in line layers list is being deleted
        /// </summary>
        private void DeleteLineLayer_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (layersLists.SelectedItem != null)
            {
                int idx = layersLists.SelectedIndex;
                var item = layersLists.SelectedItem as LineEntry;
                canvas.Children.Remove(item.line);
                lineEntries.Remove(item);
                layersLists.Items.Remove(item);
                layersLists.SelectedIndex = idx >= layersLists.Items.Count ? layersLists.Items.Count - 1 : idx;
            }
        }

        /// <summary>
        /// This event is raised when a new item in the layerList is selected or the selection changes
        /// </summary>
        void layersLists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /// Return the focus to the canvas
            canvas.Focus();
            Keyboard.Focus(canvas);

            if (LastSelectedLineItem != null)
            {
                LastSelectedLineItem.line.Stroke = Brushes.Red;
                LastSelectedLineItem.IsItemSelected = false;
            }
            if (layersLists.SelectedItem != null)
            {
                var item = layersLists.SelectedItem as LineEntry;
                item.line.Stroke = Brushes.Orange;
                item.IsItemSelected = true;
                LastSelectedLineItem = item;
            }
        }

        /// <summary>
        /// This event is executed when user wants to adjust one end of a line using keyboard. This will move the endpoint one pixel (or less) down.
        /// </summary>
        private void MoveDownOnePixel_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedLineEndpoint != null)
            {
                if (SelectedLineEndpoint.Item2 == 1)
                {
                    SelectedLineEndpoint.Item1.SetValue(Line.Y1Property, SelectedLineEndpoint.Item1.Y1 + LineEndpointDelta);
                }
                else if (SelectedLineEndpoint.Item2 == 2)
                {
                    SelectedLineEndpoint.Item1.SetValue(Line.Y2Property, SelectedLineEndpoint.Item1.Y2 + LineEndpointDelta);
                }
            }
        }

        /// <summary>
        /// This event is executed when user wants to move one endpoint of a line one (or less) pixel to the left.
        /// </summary>
        private void MoveLeftOnePixel_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedLineEndpoint != null)
            {
                if (SelectedLineEndpoint.Item2 == 1)
                {
                    SelectedLineEndpoint.Item1.SetValue(Line.X1Property, SelectedLineEndpoint.Item1.X1 - LineEndpointDelta);
                }
                else if (SelectedLineEndpoint.Item2 == 2)
                {
                    SelectedLineEndpoint.Item1.SetValue(Line.X2Property, SelectedLineEndpoint.Item1.X2 - LineEndpointDelta);
                }
            }
        }

        /// <summary>
        /// This event is executed when user wants to move one endpoint of a line one (or less) pixel to the right.
        /// </summary>
        private void MoveRightOnePixel_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedLineEndpoint != null)
            {
                if (SelectedLineEndpoint.Item2 == 1)
                {
                    SelectedLineEndpoint.Item1.SetValue(Line.X1Property, SelectedLineEndpoint.Item1.X1 + LineEndpointDelta);
                }
                else if (SelectedLineEndpoint.Item2 == 2)
                {
                    SelectedLineEndpoint.Item1.SetValue(Line.X2Property, SelectedLineEndpoint.Item1.X2 + LineEndpointDelta);
                }
            }
        }

        /// <summary>
        /// This event is executed when user wants to move one endpoint of a line one (or less) pixel up.
        /// </summary>
        private void MoveUpOnePixel_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SelectedLineEndpoint != null)
            {
                if (SelectedLineEndpoint.Item2 == 1)
                {
                    SelectedLineEndpoint.Item1.SetValue(Line.Y1Property, SelectedLineEndpoint.Item1.Y1 - LineEndpointDelta);
                }
                else if (SelectedLineEndpoint.Item2 == 2)
                {
                    SelectedLineEndpoint.Item1.SetValue(Line.Y2Property, SelectedLineEndpoint.Item1.Y2 - LineEndpointDelta);
                }
            }
        }

        /// <summary>
        /// This event is raised when the main window is being closed. This event is used to save the output of the current image, in case it hasn't been saved before.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Console.WriteLine("Image Output saved.");
            SaveCurrentImageOutput();
        }

        /// <summary>
        /// This event is raised when user presses `Ctrl + S` to save the output of the current image
        /// </summary>
        private void SaveOutput_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var stat = SaveCurrentImageOutput();
            if (imagesList.SelectedItem != null)
            {
                if (stat == true)
                {
                    ((Grid)imagesList.SelectedItem).Background = Brushes.LightGreen;
                }
                else
                {
                    ((Grid)imagesList.SelectedItem).Background = Brushes.PaleVioletRed;
                }

            }
        }

        /// <summary>
        /// This event is raised when user presses `B` or the `Toggle Brushing` toggle button to switch to or back from brushing mode
        /// </summary>
        private void ToggleBrushingMode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (inkcanvas.IsVisible == true)
            {
                if ((bool) toggleBrushing.IsChecked)
                    toggleBrushing.IsChecked = false;
                inkcanvas.Visibility = Visibility.Hidden;
                IsEraserMode = false;
                toggleEraser.IsChecked = false;
                inkcanvas.EditingMode = InkCanvasEditingMode.Ink;
                inkCanvasEllipse.Fill = transparentRed;
            }
            else
            {
                if ((bool) !toggleBrushing.IsChecked)
                    toggleBrushing.IsChecked = true;
                inkcanvas.Visibility = Visibility.Visible;
            }
        }
        /// <summary>
        /// Since setting binding to Width and Height subproperty of DefaultDrawingAttributes property of InkCanvas wasn't possible, instead we opt to
        /// using the event to change the stroke tip's size
        /// </summary>
        private void BrushSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (inkcanvas != null)
            {
                var val = BrushSlider.Value;
                inkcanvas.DefaultDrawingAttributes.Width = val;
                inkcanvas.DefaultDrawingAttributes.Height = val;
                inkCanvasEllipse.Height = val;
                inkCanvasEllipse.Width = val;
            }
        }

        /// <summary>
        /// This event is raised when user presses `E` or the `Toggle Eraser` toggle button to switch to or back from erasing mode when in brushing mode
        /// </summary>
        private void ToggleEraserMode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if ((bool) toggleBrushing.IsChecked)
            {
                if (!IsEraserMode)
                {
                    toggleEraser.IsChecked = true;
                    inkcanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                    inkCanvasEllipse.Fill = transparentWhite;

                    IsEraserMode = !IsEraserMode;
                }
                else
                {
                    toggleEraser.IsChecked = false;
                    inkcanvas.EditingMode = InkCanvasEditingMode.Ink;
                    IsEraserMode = !IsEraserMode;
                    inkCanvasEllipse.Fill = transparentRed;
                }
            }
            else
            {
                toggleEraser.IsChecked = false;
            }
        }

        /// <summary>
        /// This event is raised when mouse cursor enters InkCanvas. This event is used to hide the mouse cursor and instead 
        /// display a circle to better show the size of the stroke drawing tip.
        /// </summary>
        private void Inkcanvas_OnMouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.SetCursor(Cursors.None);
            var position = e.GetPosition(inkcanvas);
            var radius = inkCanvasEllipse.Height/2;
            inkCanvasEllipse.Margin = new Thickness(position.X - radius, position.Y - radius, 0, 0);
            inkCanvasEllipse.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// This event is raised when mouse cursor leaves the inkcanvas. This event is used to hide the cursor as the stroke
        /// drawing tip and display the default mouse cursor.
        /// </summary>
        private void Inkcanvas_OnMouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.SetCursor(Cursors.Arrow);
            inkCanvasEllipse.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// This event is raised when mouse moves inside the InkCanvas. It is used for updating the location of the circle
        /// which is used instead of the mouse cursor to display the stroke drawing tip.
        /// </summary>
        private void Inkcanvas_OnMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(inkcanvas);
            var radius = inkCanvasEllipse.Height/2;
            inkCanvasEllipse.Margin = new Thickness(position.X - radius, position.Y - radius, 0, 0);
        }
    }
}
