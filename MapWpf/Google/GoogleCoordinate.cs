using System;
using System.Windows;

namespace MapWpf.Google
{
    public class GoogleCoordinate : IComparable, ICloneable
    {
        public static readonly GoogleCoordinate Empty = new GoogleCoordinate();

        public long X { get; private set; }

        public long Y { get; private set; }

        public int Level { get; private set; }

        private GoogleCoordinate()
        {
            X = 0;
            Level = 0;
            Y = 0;
        }

        public GoogleCoordinate(long pX, long pY, int pLevel)
        {
            X = pX;
            Y = pY;
            Level = pLevel;
        }

        public GoogleCoordinate(Coordinate coordinate, int level)
        {
            X = GoogleMapUtilities.GetGoogleX(coordinate, level);
            Y = GoogleMapUtilities.GetGoogleY(coordinate, level);
            Level = level;
        }

        #region ICloneable Members
        public object Clone()
        {
            return new GoogleCoordinate(X, Y, Level);
        }
        #endregion

        #region IComparable Members
        public int CompareTo(Object obj)
        {
            var coords = (GoogleCoordinate)obj;
            if (coords.Level < Level)
                return -1;
            if (coords.Level > Level)
                return 1;
            if (coords.Y < Y)
                return -1;
            if (coords.Y > Y)
                return 1;
            if (coords.X < X)
                return -1;
            if (coords.X > X)
                return 1;
            return 0;
        }
        #endregion

        public static GoogleCoordinate operator +(GoogleCoordinate google, GoogleCoordinate addon)
        {
            if (google.Level != addon.Level)
            {
                addon = new GoogleCoordinate(addon, google.Level);
            }
            return new GoogleCoordinate(google.X + addon.X, google.Y + addon.Y, google.Level);
        }

        public static GoogleCoordinate operator -(GoogleCoordinate google, GoogleCoordinate addon)
        {
            if (google.Level != addon.Level)
            {
                addon = new GoogleCoordinate(addon, google.Level);
            }
            return new GoogleCoordinate(google.X - addon.X, google.Y - addon.Y, google.Level);
        }

        public static implicit operator Coordinate(GoogleCoordinate google)
        {
            return new Coordinate(GoogleMapUtilities.GetLongitude(google), GoogleMapUtilities.GetLatitude(google));
        }

        public static implicit operator GoogleBlock(GoogleCoordinate google)
        {
            return new GoogleBlock(
                (int)(google.X / GoogleBlock.BlockSize),
                (int)(google.Y / GoogleBlock.BlockSize),
                google.Level);
        }

        public Point GetScreenPoint(GoogleRectangle screenView)
        {
            if (Level != screenView.Level)
            {
                screenView = new GoogleRectangle(new CoordinateRectangle(screenView.LeftTop, screenView.RightBottom), Level);
            }
            return new Point((int)(X - screenView.Left), (int)(Y - screenView.Top));
        }
    }
}
