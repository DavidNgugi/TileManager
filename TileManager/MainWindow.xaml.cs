using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Threading;

namespace TileManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Zoom
        /// </summary>
        private Double zoomMax = 5;
        private Double zoomMin = 0.5;
        private Double zoomSpeed = 0.001;
        private Double zoom = 1;

        /// <summary>
        /// Collections
        /// </summary>
        private ObservableCollection<Tileset> tilesets = new ObservableCollection<Tileset>();

        /// <summary>
        /// File variables
        /// </summary>
        private string destPath = Directory.GetParent(@"../../").FullName + @"\tilesets";
        private string filename;
        private string sourcePath;
        private string sourceFile;
        private string destFile;

        /// <summary>
        /// Modes
        /// </summary>
        Mode mode;

        /// <summary>
        /// Bools
        /// </summary>
        private bool isMouseDragging = false;
        private bool isMouseDown;
        
        /// <summary>
        /// Points
        /// </summary>
        private Point anchorPoint = new Point();
        private Point scrollMousePoint = new Point();

        private double hOff = 1;
        private double vOff = 1;

        /// <summary>
        /// Threads
        /// </summary>
        Thread thDraw;
        
        /// <summary>
        /// Misc.
        /// </summary>
        Rectangle sector;
        ImageBrush imgbrush;

        Image map;
        Rectangle selectBox;
        Rectangle dragSelectBox;
        Label lblWidth;
        Label lblHeight;
        private bool isTilesetSelected;

        public MainWindow()
        {
            InitializeComponent();

            this.mode = new Mode();

            this.ReadFilesFromFolder();

            this.listTileSets.ItemsSource = tilesets;

            scroller.IsEnabled = true;

            AddToCanvas();
        }

        public void AddToCanvas() {
            // Select Box
            selectBox = new Rectangle();
            selectBox.Name = "selectBox";
            selectBox.Fill = Brushes.AliceBlue;
            selectBox.Opacity = 0.5;
            Binding bW2 = new Binding();
            Binding bH2 = new Binding();
            bW2.ElementName = "txtTileWidth";
            bW2.Path = new PropertyPath("Text");
            bH2.ElementName = "txtTileHeight";
            bH2.Path = new PropertyPath("Text");
            selectBox.SetBinding(WidthProperty, bW2);
            selectBox.SetBinding(HeightProperty, bH2);
            selectBox.Visibility = Visibility.Collapsed;

            // Drag Box
            dragSelectBox = new Rectangle();
            dragSelectBox.Name = "dragSelectBox";
            dragSelectBox.Fill = Brushes.AliceBlue;
            dragSelectBox.Opacity = 0.5;
            dragSelectBox.Stroke = Brushes.DarkBlue;
            dragSelectBox.Visibility = Visibility.Collapsed;

            //// Width Label
            lblWidth = new Label();
            lblWidth.Name = "lblWidth";
            lblWidth.Foreground = Brushes.Red;
            lblWidth.Visibility = Visibility.Collapsed;

            ////Height Label
            lblHeight = new Label();
            lblHeight.Name = "lblHeight";
            lblHeight.Foreground = Brushes.Red;
            lblHeight.Visibility = Visibility.Collapsed;

            canvas.Children.Add(selectBox);
            canvas.Children.Add(dragSelectBox);
            canvas.Children.Add(lblWidth);
            canvas.Children.Add(lblHeight);

        }

        public void CreateMap(ImageSource source) {
            map = new Image();
            map.Name = "map";
            Binding bW = new Binding();
            Binding bH = new Binding();
            bW.ElementName = "txtTileSetWidth";
            bW.Path = new PropertyPath("Text");
            bW.StringFormat = "{}{0:0}";
            bH.ElementName = "txtTileSetHeight";
            bH.Path = new PropertyPath("Text");
            bH.StringFormat = "{}{0:0}";
            map.SetBinding(WidthProperty, bW);
            map.SetBinding(HeightProperty, bH);
            map.Stretch = Stretch.Fill;
            map.Source = source;

            //AddToCanvas();
            canvas.Children.Add(map);
            
        }

        public void ReadFilesFromFolder() {
            string directoryPath = Directory.GetParent(@"../../").FullName + @"\tilesets";
            DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);

            FileInfo[] info = dirInfo.GetFiles("*.bmp");

            foreach(FileInfo f in info){
                tilesets.Add(new Tileset { Name = System.IO.Path.GetFileNameWithoutExtension(f.Name), Source = this.GetTargetPath(System.IO.Path.GetFileNameWithoutExtension(f.Name)) });
            }
            
        }
       
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            
            try{
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Filter = "Image files (*.png; *.jpeg)|*.png;*.jpeg";
                    openFileDialog.Multiselect = false;
                    openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    openFileDialog.Title = "Choose a Tileset";

                    if (!Directory.Exists(destPath))
                    {
                        Directory.CreateDirectory(destPath);
                    }

                    if (openFileDialog.ShowDialog() == true)
                    {
                        filename = openFileDialog.FileName;
                        string targetImagePath = this.GetTargetPath(System.IO.Path.GetFileNameWithoutExtension(filename));
                        Tileset t = new Tileset { Name = System.IO.Path.GetFileNameWithoutExtension(filename), Source = targetImagePath };

                        if (!tilesets.Contains(t))
                        {
                            sourcePath = System.IO.Path.GetFullPath(filename);
                            sourceFile = System.IO.Path.Combine(sourcePath, filename);
                            destFile = System.IO.Path.Combine(destPath, filename);

                            System.Drawing.Bitmap bm = System.Drawing.Image.FromFile(openFileDialog.FileName) as System.Drawing.Bitmap;
                            bm.Save(targetImagePath, ImageFormat.Bmp);

                            setTileSet(System.IO.Path.GetFileNameWithoutExtension(filename));

                            tilesets.Add(t);
                        }
                        else {
                            MessageBox.Show("A similar tileset has already been uploaded!", "Reminder", MessageBoxButton.OK);
                        }

                    }

                   
                                   
            }catch(Exception ex){
                MessageBox.Show(ex.Message, "File Openning Error", MessageBoxButton.OK);
            }
        }

        public String GetTargetPath(string fname) {
            return Directory.GetParent(@"../../").FullName + @"\tilesets\" + fname + ".bmp";
        }

        public void setTileSet(string tilesetname) {
            canvas.Children.Clear();

            string targetImagePath = this.GetTargetPath(tilesetname);

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(targetImagePath);
            bitmap.EndInit();
            txtTileSetWidth.Text = bitmap.Width.ToString();
            txtTileSetHeight.Text = bitmap.Height.ToString();
            CreateMap(bitmap);
            
            AddToCanvas();
            
        }

        private void listTileSets_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            
            if (listTileSets.SelectedItem != null) {
                Tileset tset = listTileSets.SelectedItem as Tileset;
                setTileSet(tset.Name);
                isTilesetSelected = true;
            }
        }

        private void canvas_MouseEnter_1(object sender, System.Windows.Input.MouseEventArgs e)
        {
           if(mode.Select){ selectBox.Visibility = Visibility.Visible;}
        }

        private void canvas_MouseLeftButtonDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            
            // Get mouse position onMouseDown
            anchorPoint.X = e.GetPosition(canvas).X;
            anchorPoint.Y = e.GetPosition(canvas).Y;
            isMouseDown = true;
            isMouseDragging = true;

            // Set/Reset Selection Box attributes;
            dragSelectBox.Width = 0;
            dragSelectBox.Height = 0;
            dragSelectBox.SetValue(Canvas.LeftProperty, anchorPoint.X);
            dragSelectBox.SetValue(Canvas.TopProperty, anchorPoint.Y);
        }

        private void canvas_MouseLeftButtonUp_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
            isMouseDown = false;
            isMouseDragging = false;
            dragSelectBox.Visibility = Visibility.Collapsed;
            lblWidth.Visibility = Visibility.Collapsed;
            lblHeight.Visibility = Visibility.Collapsed;
            if (mode.Select) { selectBox.Visibility = Visibility.Visible; }
            canvas.ReleaseMouseCapture();
        }

        private void canvas_MouseLeave_1(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
            selectBox.Visibility = Visibility.Collapsed;
            dragSelectBox.Visibility = Visibility.Collapsed;
            lblWidth.Visibility = Visibility.Collapsed;
            lblHeight.Visibility = Visibility.Collapsed;
            canvas.ReleaseMouseCapture();
        }

        private void canvas_MouseMove_1(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(canvas);
            if (mode.Select == true)
            {
                selectBox.SetValue(Window.LeftProperty, mousePos.X - selectBox.Width / 2);
                selectBox.SetValue(Window.TopProperty, mousePos.Y - selectBox.Height / 2);
            }

            if (isMouseDown && isMouseDragging && !mode.Pan)
            {
                double x = 0.0, y = 0.0, width = 0, height = 0;
                Point point2 = e.GetPosition(canvas);

                selectBox.Visibility = Visibility.Collapsed;

                // Set all case values correctly

                // left
                if (point2.X < anchorPoint.X)
                {
                    x = point2.X;
                    width = anchorPoint.X - point2.X;
                }
                else
                {
                    x = anchorPoint.X;
                    width = point2.X - anchorPoint.X;
                }

                //up
                if (point2.Y < anchorPoint.Y)
                {
                    y = point2.Y;
                    height = anchorPoint.Y - point2.Y;
                }
                else
                {
                    y = anchorPoint.Y;
                    height = point2.Y - anchorPoint.Y;
                }

                if (width < 0) { width = 0; }
                if (height < 0) { height = 0; }



                // Interpolation params
                double flGoalX = x;
                double flGoalY = y;
                double flcurrentLeft = Canvas.GetLeft(dragSelectBox);
                double flcurrentTop = Canvas.GetTop(dragSelectBox);
                double dt = 30;

                // Set the box Position, width and height
                dragSelectBox.SetValue(Canvas.LeftProperty, Lerp(flGoalX, flcurrentLeft, dt * 20));
                dragSelectBox.SetValue(Canvas.TopProperty, Lerp(flGoalY, flcurrentTop, dt * 20));

                dragSelectBox.Width = width;
                dragSelectBox.Height = height;

                double lblWidthPosX = Canvas.GetLeft(dragSelectBox) + width/2 - 10;
                double lblWidthPosY = Canvas.GetTop(dragSelectBox) - 20;
                double lblHeightPosX = Canvas.GetLeft(dragSelectBox) - 30;
                double lblHeightPosY = Canvas.GetTop(dragSelectBox) + height/2 - 10;

                lblWidth.Content = Math.Round(Math.Abs(width), 0).ToString() + "px";
                lblHeight.Content = Math.Round(Math.Abs(height), 0).ToString() + "px";

                lblWidth.SetValue(Canvas.LeftProperty, lblWidthPosX);
                lblWidth.SetValue(Canvas.TopProperty, lblWidthPosY);
                lblHeight.SetValue(Canvas.LeftProperty, lblHeightPosX);
                lblHeight.SetValue(Canvas.TopProperty, lblHeightPosY);

                // Show the box
                if (dragSelectBox.Visibility != Visibility.Visible) { dragSelectBox.Visibility = Visibility.Visible;
                lblWidth.Visibility = Visibility.Visible; lblHeight.Visibility = Visibility.Visible; ;
                }

            }
            

        }

        private void canvas_MouseWheel_1(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            
            if (mode.Zoom == true)
            {
                //scroller.CanContentScroll = false;
                zoom += zoomSpeed * e.Delta; // Ajust zooming speed (e.Delta = Mouse spin value )
                if (zoom < zoomMin) { zoom = zoomMin; } // Limit Min Scale
                if (zoom > zoomMax) { zoom = zoomMax; } // Limit Max Scale

                Point mousePos = e.GetPosition(canvas);

                if (zoom > 1)
                {
                    canvas.RenderTransform = new ScaleTransform(zoom, zoom, mousePos.X, mousePos.Y); // transform Canvas size from mouse position
                }
                else
                {
                    canvas.RenderTransform = new ScaleTransform(zoom, zoom); // transform Canvas size
                }

            }


            
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            
            Button b = sender as Button;

            switch(b.Name){
                case "btnSelect":
                    mode.Select = (mode.Select) ? false : true;
                    b.Background = (mode.Select) ? Brushes.Blue : Brushes.LightGray;
                    b.Foreground = (mode.Select) ? Brushes.White : Brushes.Black;
                    break;
                case "btnPan":
                    mode.Pan = (mode.Pan) ? false : true;
                    b.Background = (mode.Pan) ? Brushes.Blue : Brushes.LightGray;
                    b.Foreground = (mode.Pan) ? Brushes.White : Brushes.Black;
                    break;
                case "btnZoom":
                    mode.Zoom = (mode.Zoom) ? false : true;
                    b.Background = (mode.Zoom) ? Brushes.Blue : Brushes.LightGray;
                    b.Foreground = (mode.Zoom) ? Brushes.White : Brushes.Black;
                    break;
                case "btnCancel":
                    mode.Reset();

                    btnSelect.Background = Brushes.LightGray;
                    btnSelect.Foreground = Brushes.Black;

                    btnPan.Background = Brushes.LightGray;
                    btnPan.Foreground = Brushes.Black;

                    btnZoom.Background = Brushes.LightGray;
                    btnZoom.Foreground = Brushes.Black;
                    break;
            }
        }

        public double Lerp(double flgoal, double flcurrent, double dt)
        {
            double fldifference = flgoal - flcurrent;
            if (fldifference > dt)
            {
                return flcurrent + dt;
            }
            else if (fldifference < -dt)
            {
                return flcurrent - dt;
            }

            return flgoal;

        }

        private void scroller_PreviewMouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            if (mode.Pan) {
                scrollMousePoint = e.GetPosition(scroller);
                hOff = scroller.HorizontalOffset;
                vOff = scroller.VerticalOffset;
                scroller.CaptureMouse();
            }
        }

        private void scroller_PreviewMouseMove_1(object sender, MouseEventArgs e)
        {
            if (mode.Pan && scroller.IsMouseCaptured)
            {
                this.Cursor = Cursors.Arrow;
                scroller.ScrollToHorizontalOffset(hOff + (scrollMousePoint.X - e.GetPosition(scroller).X));
                scroller.ScrollToVerticalOffset(vOff + (scrollMousePoint.Y - e.GetPosition(scroller).Y));
            }
        }

        private void scroller_PreviewMouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            scrollMousePoint = e.GetPosition(scroller);
            this.Cursor = Cursors.Arrow;
            scroller.ReleaseMouseCapture();
        }

        private void ThreadDrawInBackground()
        {
            thDraw.IsBackground = true;
            if (!thDraw.IsAlive) { thDraw.Start(); Console.WriteLine("Starting Draw Thread..."); }
        }

        private void stopDrawThread()
        {
            if (thDraw.IsAlive) { thDraw.Abort(); Console.WriteLine("Stoping Draw Thread..."); }
        }

        private void btnCreateTileMap_Click_1(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(txtTileSetWidth.Text) == true || Convert.ToDouble(txtTileSetWidth.Text) < 0){
                MessageBox.Show("Set a valid Tileset width!", "Reminder", MessageBoxButton.OK);
            }
            else if(string.IsNullOrEmpty(txtTileHeight.Text) || Convert.ToDouble(txtTileSetHeight.Text) < 0){
                MessageBox.Show("Set a valid Tileset height!", "Reminder", MessageBoxButton.OK);
            }
            else if (string.IsNullOrEmpty(txtTileWidth.Text) || Convert.ToDouble(txtTileWidth.Text) < 0)
            {
                MessageBox.Show("Set a valid Tile width!", "Reminder", MessageBoxButton.OK);
            }
            else if(string.IsNullOrEmpty(txtTileHeight.Text) || Convert.ToDouble(txtTileHeight.Text) < 0){
                MessageBox.Show("Set a valid Tile height!", "Reminder", MessageBoxButton.OK);
            }
            else if (Convert.ToDouble(txtHorizontalTilePadding.Text) < 0)
            {
                MessageBox.Show("Invalid Horizontal Tile padding!", "Reminder", MessageBoxButton.OK);
            }
            else if (Convert.ToDouble(txtVerticalTilePadding.Text) < 0)
            {
                MessageBox.Show("Invalid Vertical Tile padding!", "Reminder", MessageBoxButton.OK);
            }
            else {
                // Generate Tilemap
                if (isTilesetSelected)
                {
                    GenerateTilemap(map.Source, Convert.ToDouble(txtTileSetWidth.Text), Convert.ToDouble(txtTileSetHeight.Text),
                        Convert.ToDouble(txtTileWidth.Text), Convert.ToDouble(txtTileHeight.Text), Convert.ToDouble(txtHorizontalTilePadding.Text),
                        Convert.ToDouble(txtVerticalTilePadding.Text));
                }
            }
        }

        public void GenerateTilemap(ImageSource source, double tsetwidth, double tsetheight, double twidth, double theight, 
            double hPadding, double vPadding)
        {
     
            canvas.Children.Clear();

            hPadding = (hPadding < 0) ? 1 : hPadding;
            vPadding = (vPadding < 0) ? 1 : vPadding;

            for (double x = 0; x < tsetwidth; x += twidth)
            {
                for (double y = 0; y < tsetheight; y += theight)
                {
                    imgbrush = new ImageBrush();
                    imgbrush.ImageSource = source;
                    imgbrush.ViewboxUnits = BrushMappingMode.Absolute;
                    imgbrush.Viewbox = new Rect { X = x + hPadding, Y = y + vPadding, Width = twidth, Height = theight };
                    sector = new Rectangle();
                    sector.Width = twidth;
                    sector.Height = theight;
                    sector.SetValue(Canvas.LeftProperty, x );
                    sector.SetValue(Canvas.TopProperty, y );
                    sector.Fill = imgbrush;
                    sector.Stroke = Brushes.Blue;
                    sector.Stroke = Brushes.Transparent;
                    sector.StrokeThickness = 0;
                    sector.MouseEnter += sector_MouseEnter;
                    sector.MouseLeave += sector_MouseLeave;
                    ////Add to Canvas 
                    canvas.Children.Add(sector);
                }
            }

            AddToCanvas();
            
        }

        void sector_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
            Rectangle s = sender as Rectangle;
            s.Stroke = Brushes.Blue;
            s.Opacity = 0.7;
            s.StrokeThickness = 1;
        }

        void sector_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
            Rectangle s = sender as Rectangle;
            s.Stroke = Brushes.Transparent;
            s.Opacity = 1;
            s.StrokeThickness = 0;
        }

        private void btnImportTilemap_Click_1(object sender, RoutedEventArgs e)
        {
            //
        }

        private void btnSaveTileMap_Click_1(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = ".xml";
            saveFileDialog.Filter = "XML files (*.xml; *.dat)|*.xml;*.dat";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            saveFileDialog.Title = "Save Tilemap";
            if(saveFileDialog.ShowDialog() == true){
                
            }

        }


      
    }

}
