using MapWpf.Framework;
using MapWpf.Google;
using MapWpf.Layers;
using MapWpf.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MapWpf
{
    /// <summary>
    /// Interaction logic for MapUC.xaml
    /// </summary>
    public partial class MapUC : System.Windows.Controls.UserControl, INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "non pass")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PlaceMouseDownEventHandle PlaceMouseDown;

        public BitmapImage PlaceImage
        {
            get { return MapLayer.PlaceImage; }
            set { MapLayer.PlaceImage = value; }
        }

        public int Level
        {
            get
            {
                return MapLayer.Level;
            }
            set
            {
                if (value != MapLayer.Level)
                {
                    MapLayer.Level = value;
                    OnPropertyChanged();
                }
            }
        }

        public Coordinate CenterCoordinate
        {
            get
            {
                return MapLayer.CenterCoordinate;
            }
            set
            {
                MapLayer.CenterCoordinate = value;
            }
        }



        public MapLayer MapLayer;
        private Point _tempMousePoint;
        private Coordinate _tempCenterCoordinate;
        private Vector _movedVector = new Vector(0, 0);
        public DispatcherTimer _timerToRefreshMap;

        public DrawingImage DrawingImage
        {
            get { return MapLayer.DrawingImage; }
        }

        private Place _selectedPlace;
        public Place SelectedPlace
        {
            get { return _selectedPlace; }
            set
            {
                _selectedPlace = value;
                OnPropertyChanged();
            }

        }

        public MapUC()
        {
            InitializeComponent();
            this.DataContext = this;

            var center = Settings.CenterMapBound;
            MapLayer = new MapLayer((int)this.ActualWidth, (int)this.ActualHeight, center, Settings.Default.StartZoomLevel);
            MapLayer.PropertyChanged += _mapLayer_PropertyChanged;
            MapLayer.Background = Brushes.White;

            _timerToRefreshMap = new DispatcherTimer();
            _timerToRefreshMap.Tick += _timerToRefreshMap_Tick;

            //Đà Nẵng
            this.CenterCoordinate = new Coordinate(108.224851, 16.067610);

            this.Level = 15;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void _timerToRefreshMap_Tick(object sender, EventArgs e)
        {
            _timerToRefreshMap.Stop();
            MapLayer.IsMoving = false;

            _tempCenterCoordinate = null;
            _movedVector = new Vector(0, 0);

            MapLayer.Update();
        }


        private void _mapLayer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "DrawingImage":
                    OnPropertyChanged("DrawingImage");
                    break;
            }
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MapLayer.Resize((int)ActualWidth, (int)ActualHeight);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void btnZoomIn_click(object sender, RoutedEventArgs e)
        {
            var newZoom = Level - 1;
            if (newZoom < Settings.Default.MinZoomLevel)
                newZoom = Settings.Default.MinZoomLevel;
            Level = newZoom;
        }

        private void btnZoomOut_click(object sender, RoutedEventArgs e)
        {
            var newZoom = Level + 1;
            if (newZoom > Settings.Default.MaxZoomLevel)
                newZoom = Settings.Default.MaxZoomLevel;
            Level = newZoom;
        }

        public Coordinate GetCoordinateFromPoint(Point point)
        {
            return MapWpf.Coordinate.GetCoordinateFromScreen(MapLayer.ScreenView, point);
        }

        public void AddPlace(object place)
        {
            MapLayer.Places.Add(new Place(place));
        }

        public void AddPlace(object place, double lon, double lat)
        {
            MapLayer.Places.Add(new Place(place, lon, lat));
        }

        public void RemovePlace(object place)
        {
            var placeObj = MapLayer.Places.Where(p => p.DataObject.Equals(place)).FirstOrDefault();
            if (placeObj != null)
                MapLayer.Places.Remove(placeObj);
        }

        public void ClearPlaces()
        {
            MapLayer.Places.Clear();
        }

        public Point FindPlacePosition(object place)
        {
                var placeObject = MapLayer.Places.Where(p => p.DataObject == place).FirstOrDefault();
                return placeObject.Location.GetScreenPoint(MapLayer.ScreenView);
        }

        public void Move(Vector moveVector)
        {
            MapLayer.IsMoving = true;

            _timerToRefreshMap.Stop();
            _timerToRefreshMap.Interval = TimeSpan.FromMilliseconds(300);
            _timerToRefreshMap.Start();

            MapLayer.Move(moveVector);
            _movedVector += moveVector;

            // Move temp center
            if (_tempCenterCoordinate == null)
                _tempCenterCoordinate = CenterCoordinate;
            MapLayer.SetCenterCoordinateNoUpdate(_tempCenterCoordinate +
                new GoogleCoordinate(-(int)_movedVector.X, -(int)_movedVector.Y, Level));

        }

        public void Dispose()
        {
            MapLayer.Terminating = true;
        }

        #region mouse events

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _tempMousePoint = e.GetPosition(sender as Image);

                SelectedPlace = (Place)MapLayer.FindMapObject(_tempMousePoint);
                if (SelectedPlace != null &&
                    PlaceMouseDown != null)
                    PlaceMouseDown(SelectedPlace.DataObject, _tempMousePoint);
            }
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (MapLayer.Terminating) return;

                Point currPoint = e.GetPosition(sender as Image);
                this.Move(currPoint - _tempMousePoint);
                _tempMousePoint = currPoint;
            }
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (MapLayer.IsMoving)
            {
                MapLayer.IsMoving = false;
            }
        }

        private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta != 0)
            {
                var newZoom = Level;
                newZoom += (e.Delta > 0) ? 1 : -1;
                if (newZoom < Settings.Default.MinZoomLevel)
                    newZoom = Settings.Default.MinZoomLevel;
                else if (newZoom > Settings.Default.MaxZoomLevel)
                    newZoom = Settings.Default.MaxZoomLevel;
                Level = newZoom;
            }
        }

        #endregion mouse events
    }

    public delegate void PlaceMouseDownEventHandle(object place, Point mousePos);

}
