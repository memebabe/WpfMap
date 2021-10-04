using System;
using System.Collections.Generic;
using MapWpf.Google;
using System.Windows;

namespace MapWpf
{
    public class Coordinate : IComparable, ICloneable
    {
        public static readonly Coordinate Empty = new Coordinate();

        public double Longitude { get; set; }

        public double Latitude { get; set; }
        
        private Coordinate()
        {
            Latitude = 0;
            Longitude = 0;
        }

        public Coordinate(double pLongitude, double pLatitude)
        {
            Longitude = pLongitude;
            Latitude = pLatitude;
        }
        
        public Coordinate(decimal pLongitude, decimal pLatitude)
        {
            Longitude = (double)pLongitude;
            Latitude = (double)pLatitude;
        }

        #region ICloneable Members
        public object Clone()
        {
            return new Coordinate(Longitude, Latitude);
        }
        #endregion

        #region IComparable Members
        public int CompareTo(Object obj)
        {
            var coords = (Coordinate)obj;
            if (coords.Latitude < Latitude)
                return -1;
            if (coords.Latitude > Latitude)
                return 1;
            if (coords.Longitude < Longitude)
                return -1;
            if (coords.Longitude > Longitude)
                return 1;
            return 0;
        }
        #endregion


        public override string ToString()
        {
            return (String.Format("E{0:F5} N{1:F5}", Longitude, Latitude));
        }

        public static Coordinate operator +(Coordinate coordinate, GoogleCoordinate addon)
        {
            return new GoogleCoordinate(coordinate, addon.Level) + addon;
        }

        public static Coordinate operator -(Coordinate coordinate, GoogleCoordinate addon)
        {
            return new GoogleCoordinate(coordinate, addon.Level) - addon;
        }

        public static Coordinate operator +(Coordinate coordinate, Coordinate addon)
        {
            return new Coordinate(coordinate.Longitude + addon.Longitude, coordinate.Latitude + addon.Latitude);
        }

        public static Coordinate operator -(Coordinate coordinate, Coordinate sub)
        {
            return new Coordinate(coordinate.Longitude - sub.Longitude, coordinate.Latitude - sub.Latitude);
        }

        private GoogleCoordinate GetLeftTopGoogle(int screenWidth, int screenHeight, int level)
        {
            return new GoogleCoordinate(
                GoogleMapUtilities.GetGoogleX(this, level) - ((screenWidth + 1) / 2 - 1),
                GoogleMapUtilities.GetGoogleY(this, level) - ((screenHeight + 1) / 2 - 1),
                level);
        }

        private GoogleCoordinate GetRightBottomGoogle(int screenWidth, int screenHeight, int level)
        {
            return new GoogleCoordinate(
                GoogleMapUtilities.GetGoogleX(this, level) + ((screenWidth - 1) / 2 + 1),
                GoogleMapUtilities.GetGoogleY(this, level) + ((screenHeight - 1) / 2 + 1),
                level);
        }

        public GoogleRectangle GetScreenViewFromCenter(int screenWidth, int screenHeight, int level)
        {
            return new GoogleRectangle(GetLeftTopGoogle(screenWidth, screenHeight, level), GetRightBottomGoogle(screenWidth, screenHeight, level));
        }

        public Point GetScreenPoint(GoogleRectangle screenView)
        {
            return new GoogleCoordinate(this, screenView.Level).GetScreenPoint(screenView);
        }

        public static Coordinate GetCoordinateFromScreen(GoogleRectangle screenView, Point point)
        {
            return screenView.LeftTop + new GoogleCoordinate((long)point.X, (long)point.Y, screenView.Level);
        }

        public static Coordinate GetCoordinateFromScreen(GoogleRectangle screenView, long pX, long pY)
        {
            return screenView.LeftTop + new GoogleCoordinate(pX, pY, screenView.Level);
        }

        public GoogleBlock GetGoogleBlock(int level)
        {
            return new GoogleCoordinate(this, level);
        }

        public double Distance(Coordinate coordinate)
        {
            return EarthUtilities.GetLength(this, coordinate);
        }

        public override bool Equals(object obj)
        {
            Coordinate c = (Coordinate)obj;
            return this.Longitude == c.Longitude &&
                this.Latitude == c.Latitude;
        }
    }
}
