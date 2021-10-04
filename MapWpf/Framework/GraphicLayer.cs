using System.Threading;
using MapWpf.Google;
using MapWpf.Properties;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Collections.Generic;
using System.ComponentModel;

namespace MapWpf.Framework
{
    public class GraphicLayer
    {
        private readonly BitmapImage[] _offScreen = { null, null };
        
        private readonly SemaphoreSlim _lockDc = new SemaphoreSlim(1, 1);

        private Coordinate _centerCoordinate;
        private int _level = Settings.Default.StartZoomLevel;

        public bool Terminating { get; set; }

        public Coordinate CenterCoordinate
        {
            get
            {
                return (Coordinate)_centerCoordinate.Clone();
            }
            set
            {
                SetCenterCoordinate(value, _level);
            }
        }
        public int Level
        {
            get
            {
                return _level;
            }
            set
            {
                SetCenterCoordinate(_centerCoordinate, value);
            }
        }
        
        private GoogleRectangle _screenView;
        public GoogleRectangle ScreenView
        {
            get
            {
                return _screenView == null ? null:(GoogleRectangle)_screenView.Clone();
            }
        }
        private CoordinateRectangle _coordinateView;
        protected CoordinateRectangle CoordinateView
        {
            get
            {
                return (CoordinateRectangle)_coordinateView.Clone();
            }
        }

        public int Width { get; private set; }

        public int Height { get; private set; }
        
        public GraphicLayer(int pWidth, int pHeight, Coordinate centerCoordinate, int pLevel)
        {
            Width = pWidth;
            Height = pHeight;

            _centerCoordinate = centerCoordinate;
            _level = pLevel;
            
            Resize(false);
        }
        
        protected virtual bool SetCenterCoordinate(Coordinate center, int level)
        {
            if (_level != level
                || new GoogleCoordinate(_centerCoordinate, level).CompareTo(
                new GoogleCoordinate(center, level)) != 0)
            {
                _centerCoordinate = center;
                _level = level;
                TranslateCoords();
                Update(new Rect(0, 0, Width, Height));
                return true;
            }
            return false;
        }

        public virtual void SetCenterCoordinateNoUpdate(Coordinate center)
        {
            _centerCoordinate = center;
            TranslateCoords();
        }

        private void Resize(bool bUpdate)
        {
            bool isUpdate = false;
            try
            {
                _lockDc.Wait(); Log("lock");

                if (Width > 0 && Height > 0 && !Terminating)
                {
                    int stride = Width / 8;
                    
                    List<Color> colors = new List<System.Windows.Media.Color>();
                    colors.Add(System.Windows.Media.Colors.Red);
                    colors.Add(System.Windows.Media.Colors.Blue);
                    colors.Add(System.Windows.Media.Colors.Green);
                    BitmapPalette myPalette = new BitmapPalette(colors);

                    for (var i = 0; i <= 1; i++)
                    {
                        byte[] pixels = new byte[Height * stride];
                        
                        _offScreen[i] = new BitmapImage();
                    }

                    TranslateCoords();

                    isUpdate = true;
                }
                else
                {
                    _offScreen[0] = null;
                    _offScreen[1] = null;
                }
            }
            finally
            {
                _lockDc.Release(); Log("unlock");
            }
            if (bUpdate && isUpdate)
                Update(new Rect(0, 0, Width, Height));
        }

        public virtual void Resize(int pWidth, int pHeight)
        {
            if (pWidth != Width || pHeight != Height)
            {
                Width = pWidth;
                Height = pHeight;
                Resize(true);
            }
        }

        virtual protected void TranslateCoords()
        {
            _screenView = _centerCoordinate.GetScreenViewFromCenter(Width, Height, _level);
            _coordinateView = ScreenView;
        }
        
        public void Update()
        {
            Update(Rect.Empty);
        }

        public void Update(Rect clipRect)
        {
            if (!Terminating)
            {
                DrawLayer(new Rect(0, 0, Width, Height));
            }
        }

        virtual protected void DrawLayer(Rect clipRect)
        {
        }
        
        private void Log(string msg = "", [System.Runtime.CompilerServices.CallerMemberName] string methodName = "none passed")
        {
            Logger.Log($"{this.GetType().Name}\t{methodName}", msg);
        }
    }
}
