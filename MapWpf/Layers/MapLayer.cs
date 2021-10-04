using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using MapWpf.Framework;
using MapWpf.Google;
using System.Linq;
using MapWpf.Properties;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using MapWpf.Arrowheads;
using System.Windows.Threading;
using System.Collections.ObjectModel;

namespace MapWpf.Layers
{
    public class MapLayer : GraphicLayer, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyname = "non pass")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));

            switch (propertyname)
            {
                case "DisplayRect":
                case "Background":
                    UpdateDisplayRegion();
                    break;
            }
        }

        public Dispatcher Dispatcher;

        private DrawingGroup _mapImagesGroup;
        private DrawingGroup _placeImagesGraoup;
        private DrawingGroup _linesGraoup;

        public DrawingImage DrawingImage
        {
            get;
            private set;
        }

        private Rect _displayRect;
        public Rect DisplayRect
        {
            get { return _displayRect; }
            set
            {
                _displayRect = value;
                OnPropertyChanged();
            }
        }

        private Brush _background = Brushes.Black;
        public Brush Background
        {
            get { return _background; }
            set
            {
                _background = value;
                OnPropertyChanged();
            }
        }

        private static readonly SortedDictionary<GoogleBlock, ImageDrawing> MapCache = new SortedDictionary<GoogleBlock, ImageDrawing>();
        private static readonly SortedDictionary<Coordinate, ImageDrawing> PlaceImageCache = new SortedDictionary<Coordinate, ImageDrawing>();
        
        public BitmapImage PlaceImage { get; set; }

        private bool _isMoving;
        public bool IsMoving
        {
            get { return _isMoving; }
            set
            {
                if (_isMoving != value)
                {
                    _isMoving = value;
                }
            }
        }

        public ObservableCollection<Place> Places;

        public MapLayer(int width, int height, Coordinate centerCoordinate, int level)
            : base(width, height, centerCoordinate, level)
        {
            Log();
            this.Dispatcher = Dispatcher.FromThread(System.Threading.Thread.CurrentThread);

            var imageContent = new DrawingGroup();
            _mapImagesGroup = new DrawingGroup();
            _placeImagesGraoup = new DrawingGroup();
            _linesGraoup = new DrawingGroup(); ;

            imageContent.Children.Add(new GeometryDrawing(Background, null, new RectangleGeometry(DisplayRect)));
            imageContent.Children.Add(_mapImagesGroup);
            imageContent.Children.Add(_linesGraoup);
            imageContent.Children.Add(_placeImagesGraoup);

            DrawingImage = new DrawingImage(imageContent);

            Places = new ObservableCollection<Place>();
            Places.CollectionChanged += Places_CollectionChanged;
        }

        private void Places_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    Place prevPlace = Places.Count > 1 ? Places[Places.Count - 2] : null;
                    foreach (var newItem in e.NewItems)
                    {
                        DrawPlace(newItem as Place);
                        if (prevPlace != null)
                            DrawArrow(
                                prevPlace.Location.GetScreenPoint(ScreenView),
                                (newItem as Place).Location.GetScreenPoint(ScreenView),
                                Brushes.Blue, 3);
                        prevPlace = newItem as Place;
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var removedItem in e.OldItems)
                    {
                        RemovePlaces(removedItem as Place);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    ClearPlaces();
                    ClearArrows();
                    break;
            }
        }

      

        override protected void DrawLayer(Rect clipRect)
        {
            if (ScreenView == null)
                return;

            System.Threading.Tasks.Task.Run(() =>
            {
                DrawMapImages(ScreenView.BlockView);
                DrawPlaces();
            });
        }

        public double DistancePoint(Point point1, Point point2)
        {
            try
            {
                double xdis = point1.X - point2.X;
                double ydis = point1.Y - point2.Y;
                return Math.Sqrt(xdis * xdis + ydis * ydis);
            }
            catch
            {
                return Double.MaxValue;
            }
        }

        public object FindMapObject(Point location)
        {
            Log();
            Rect rect;
            var localScreenView = (GoogleRectangle)ScreenView.Clone();
            // TODO something
            //foreach (var point in NodesClone.ToList())
            //{
            //    if (localScreenView.PointContains(point) == ATVD_Map.Types.InterseptResult.Contains)
            //    {
            //        Point nodeLocation = point.GetScreenPoint(ScreenView);
            //        rect = new Rect(
            //                nodeLocation.X - Resources.place_cam.Width / 2,
            //                nodeLocation.Y - Resources.place_cam.Height,
            //                Resources.place_cam.Width,
            //                Resources.place_cam.Height);

            //        if (rect.Contains(location))
            //        {
            //            return point;
            //        }
            //    }
            //}

            foreach (var place in Places)
            {
                if (localScreenView.PointContains(place.Location) == MapWpf.Types.InterseptResult.Contains)
                {
                    Point nodeLocation = place.Location.GetScreenPoint(ScreenView);
                    rect = new Rect(
                            nodeLocation.X - PlaceImage.Width / 2,
                            nodeLocation.Y - PlaceImage.Height,
                            PlaceImage.Width,
                            PlaceImage.Height);

                    if (rect.Contains(location))
                    {
                        return place;
                    }
                }
            }
            return null;
        }

        protected override bool SetCenterCoordinate(Coordinate center, int level)
        {
            Log(center.ToString());
            if (level != this.Level)
            {
                this.ClearMapImages();
                this.ClearArrows();
            }

            var res = base.SetCenterCoordinate(center, level);
            return res;
        }

        public override void Resize(int pWidth, int pHeight)
        {
            DisplayRect = new Rect(0, 0, pWidth, pHeight);
            base.Resize(pWidth, pHeight);
        }

        public void Move(Vector vector)
        {
            ClearArrows();
            MoveMap(vector);
            MovePlaces(vector);
            OnPropertyChanged("DrawingImage");
        }

        #region Load map images

        private BitmapImage DownloadImageFromGoogle(GoogleBlock block, bool getBitmap)
        {
            try
            {
                var oRequest = GoogleMapUtilities.CreateGoogleWebRequest(block);
                var oResponse = (HttpWebResponse)oRequest.GetResponse();

                var bmpStream = new MemoryStream();
                var oStream = oResponse.GetResponseStream();
                if (oStream != null) oStream.CopyTo(bmpStream);
                oResponse.Close();
                if (bmpStream.Length > 0)
                {
                    WriteImageToFile(block, bmpStream);
                    if (getBitmap)
                    {
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = bmpStream;
                        bitmapImage.EndInit();
                        return bitmapImage;
                    }
                    else
                        return null;
                }
            }
            catch (Exception ex)
            {
                //do nothing
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
            return null;
        }

        private BitmapImage DownloadImageFromFile(GoogleBlock block)
        {
            try
            {
                var fileName = Settings.GetMapFileName(block);
                if (File.Exists(fileName))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(fileName);
                    bitmapImage.EndInit();
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                //do nothing
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
            return null;
        }

        private void WriteImageToFile(GoogleBlock block, Stream bmpStream)
        {
            var fileName = Settings.GetMapFileName(block);
            try
            {
                if (!File.Exists(fileName))
                {
                    var path = System.IO.Path.GetDirectoryName(fileName) ?? "";
                    var destdir = new DirectoryInfo(path);
                    if (!destdir.Exists)
                    {
                        destdir.Create();
                    }
                    var fileStream = File.Create(fileName);
                    try
                    {
                        bmpStream.Seek(0, SeekOrigin.Begin);
                        bmpStream.CopyTo(fileStream);
                    }
                    finally
                    {
                        fileStream.Flush();
                        fileStream.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                //do nothing
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
        }

        #endregion Load map images

        #region drawing map

        private void UpdateDisplayRegion()
        {
            DrawingImage.Dispatcher.Invoke(() =>
            {
                DrawingGroup drawingGroup = DrawingImage.Drawing as DrawingGroup;
                if (drawingGroup.Children.Count == 0)
                    drawingGroup.Children.Add(new GeometryDrawing(Background, null, new RectangleGeometry(DisplayRect)));
                else
                    drawingGroup.Children[0] = new GeometryDrawing(Background, null, new RectangleGeometry(DisplayRect));
                drawingGroup.ClipGeometry = new RectangleGeometry(DisplayRect);
            });
            OnPropertyChanged("DrawingImage");
        }

        private void DrawMapImages(Rect localBlockView)
        {
            //System.Threading.Tasks.Task.Run(() =>
            //{
            //Clear();
            for (var x = localBlockView.Left; x <= localBlockView.Right; x++)
            {
                for (var y = localBlockView.Top; y <= localBlockView.Bottom; y++)
                {
                    if (IsMoving)
                        break;
                    var block = new GoogleBlock((int)x, (int)y, Level);
                    DrawMapImage(block);
                }
            }
            //});
        }

        private void DrawMapImage(GoogleBlock block)
        {
            if (Terminating) return;
            
            if (block.Level == Level && block.Pt.Contains(ScreenView.BlockView))
            {
                // If the the block was exists then break
                if (MapCache.ContainsKey(block))
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        lock (MapCache)
                        {
                            var rect = ((GoogleRectangle)block).GetScreenRect(ScreenView);
                            MapCache[block].Rect = rect;
                        }
                    });
                    return;
                }
                else
                {
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            var rect = ((GoogleRectangle)block).GetScreenRect(ScreenView);
                            var bmp = DownloadImageFromFile(block) ?? DownloadImageFromGoogle(block, true);
                            ImageDrawing newImage = new ImageDrawing(bmp, rect);
                            //DrawingGroup drawingGroup = DrawingImage.Drawing as DrawingGroup;
                            lock (MapCache)
                            {
                                if (MapCache.ContainsKey(block))
                                {
                                    _mapImagesGroup.Children.Remove(MapCache[block]);
                                    //Log("return");
                                    //return;
                                }

                                MapCache[block] = newImage;
                            }
                            _mapImagesGroup.Children.Add(newImage);
                        }, System.Windows.Threading.DispatcherPriority.Background);
                    });
                    OnPropertyChanged("DrawingImage");
                }
            }
        }

        public void ClearMapImages()
        {
            DrawingImage.Dispatcher.Invoke(() =>
            {
                lock (MapCache)
                {
                    foreach (var mapItem in MapCache.ToArray())
                    {
                        _mapImagesGroup.Children.Remove(mapItem.Value);
                        MapCache.Remove(mapItem.Key);
                    }
                }
            });
        }

        private void MoveMap(Vector vector)
        {
            DrawingImage.Dispatcher.Invoke(() =>
            {
                foreach (var mapItem in MapCache.ToArray())
                {
                    var rect = mapItem.Value.Rect;
                    rect = new Rect(
                        rect.X + vector.X,
                        rect.Y + vector.Y,
                        rect.Width,
                        rect.Height);

                    // If the block is out of display rect then remove it
                    // Else move it to new position
                    if (!rect.IntersectsWith(DisplayRect))
                    {
                        lock (MapCache)
                        {
                            _mapImagesGroup.Children.Remove(mapItem.Value);
                            MapCache.Remove(mapItem.Key);
                        }
                    }
                    else
                    {
                        mapItem.Value.Rect = rect;
                    }
                }
            });
        }

        #endregion drawing map

        #region draw places

        private void DrawPlaces()
        {
            if (Terminating) return;

            Place prevPlace = null;
            if (Places.Count > 0)
            {
                foreach (var place in Places)
                {
                    DrawPlaceImage(place.Location);
                    if (prevPlace != null)
                        DrawArrow(
                            prevPlace.Location.GetScreenPoint(ScreenView),
                            place.Location.GetScreenPoint(ScreenView),
                            Brushes.Blue, 3);
                    prevPlace = place;
                }
            }
        }

        private void DrawPlace(Place placeObject)
        {
            DrawPlaceImage(placeObject.Location);
        }

        private void RemovePlaces(Place placeObject)
        {
            if (PlaceImageCache.ContainsKey(placeObject.Location))
            {
                _placeImagesGraoup.Children.Remove(PlaceImageCache[placeObject.Location]);
                PlaceImageCache.Remove(placeObject.Location);
            }
        }

        private void ClearPlaces()
        {
            DrawingImage.Dispatcher.Invoke(() =>
            {
                _placeImagesGraoup.Children.Clear();
                PlaceImageCache.Clear();
            });
        }

        private void DrawPlaceImage(Coordinate pos)
        {
            this.Dispatcher.Invoke(() =>
            {
                Point location = pos.GetScreenPoint((GoogleRectangle)ScreenView.Clone());
                Rect rect = new Rect(
                    location.X - PlaceImage.Width / 2,
                    location.Y - PlaceImage.Height,
                    PlaceImage.Width,
                    PlaceImage.Height);

                // If the the block was exists then break
                if (PlaceImageCache.ContainsKey(pos))
                {
                    DrawingImage.Dispatcher.Invoke(() =>
                    {
                        DrawingGroup drawingGroup = DrawingImage.Drawing as DrawingGroup;
                        PlaceImageCache[pos].Rect = rect;
                    });
                    return;
                }
                else
                {
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        DrawingImage.Dispatcher.Invoke(() =>
                        {
                            ImageDrawing newImage = new ImageDrawing(PlaceImage, rect);
                            //DrawingGroup drawingGroup = DrawingImage.Drawing as DrawingGroup;
                            lock (PlaceImageCache)
                            {
                                if (PlaceImageCache.ContainsKey(pos))
                                {
                                    _placeImagesGraoup.Children.Remove(PlaceImageCache[pos]);
                                }

                                PlaceImageCache[pos] = newImage;
                            }
                            _placeImagesGraoup.Children.Add(newImage);
                        }, System.Windows.Threading.DispatcherPriority.Background);
                    });
                }
            });
        }

        private void DrawArrow(Point p1, Point p2, Brush brush, int thickness)
        {
            if (Terminating) return;

            this.Dispatcher.Invoke(() =>
            {
                ArrowLine arrow = new ArrowLine();
                arrow.ArrowEnds = ArrowEnds.End;
                arrow.Stroke = brush;
                arrow.StrokeThickness = thickness;
                arrow.P1 = p1;
                arrow.P2 = p2;

                var arr = new GeometryDrawing(brush, new Pen(brush, thickness), arrow.GetGeometry());
                _linesGraoup.Children.Add(arr);
            });
            OnPropertyChanged("DrawingImage");
        }

        private void ClearArrows()
        {
            DrawingImage.Dispatcher.Invoke(() =>
            {
                foreach (var child in _linesGraoup.Children.ToArray())
                {
                    _linesGraoup.Children.Remove(child);
                }
            });
        }

        private void MovePlaces(Vector vector)
        {
            DrawingImage.Dispatcher.Invoke(() =>
            {
                foreach (var placeImage in PlaceImageCache.ToArray())
                {
                    var rect = placeImage.Value.Rect;
                    rect = new Rect(
                        rect.X + vector.X,
                        rect.Y + vector.Y,
                        rect.Width,
                        rect.Height);


                    placeImage.Value.Rect = rect;
                }
            });
        }

        #endregion draw places

        private void Log(string msg = "", [System.Runtime.CompilerServices.CallerMemberName] string methodName = "none passed")
        {
            Logger.Log($"{this.GetType().Name}\t{methodName}", msg);
        }
    }
}
