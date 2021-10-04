using System;
using MapWpf.Types;
using System.Windows;

namespace MapWpf.Google
{
    public class GoogleRectangle : ICloneable, IComparable
    {
        public static readonly GoogleRectangle Empty = new GoogleRectangle();

        public GoogleCoordinate LeftTop
        {
            get
            {
                return new GoogleCoordinate(Left, Top, Level);
            }
        }

        public GoogleCoordinate RightBottom
        {
            get
            {
                return new GoogleCoordinate(Right, Bottom, Level);
            }
        }

        public long Left { get; private set; }

        public long Right { get; private set; }

        public long Top { get; private set; }

        public long Bottom { get; private set; }

        public int Level { get; private set; }

        private GoogleRectangle()
        {
            Left = 0;
            Right = 0;
            Top = 0;
            Bottom = 0;
            Level = 0;
        }

        public GoogleRectangle(long left, long top, long right, long bottom, int level)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
            Level = level;
        }

        public GoogleRectangle(GoogleCoordinate pLeftTop, GoogleCoordinate pRightBottom)
        {
            if (pRightBottom.Level != pLeftTop.Level)
            {
                pRightBottom = new GoogleCoordinate(pRightBottom, pLeftTop.Level);
            }
            Left = pLeftTop.X;
            Top = pLeftTop.Y;
            Right = pRightBottom.X;
            Bottom = pRightBottom.Y;
            Level = pLeftTop.Level;
        }

        public GoogleRectangle(CoordinateRectangle coordinateRect, int level)
        {
            var pLeftTop = new GoogleCoordinate(coordinateRect.LeftTop, level);
            var pRightBottom = new GoogleCoordinate(coordinateRect.RightBottom, level);
            Left = pLeftTop.X;
            Top = pLeftTop.Y;
            Right = pRightBottom.X;
            Bottom = pRightBottom.Y;
            Level = level;
        }

        public GoogleRectangle(CoordinatePoligon coordinatePoligon, int level)
        {
            for (var i = 0; i < coordinatePoligon.Count; i++)
            {
                var pt = new GoogleCoordinate(coordinatePoligon.Coordinates[i], level);
                if (i == 0)
                {
                    Left = pt.X;
                    Right = pt.X;
                    Top = pt.Y;
                    Bottom = pt.Y;
                }
                else
                {
                    if (pt.X < Left) Left = pt.X;
                    if (pt.X > Right) Right = pt.X;
                    if (pt.Y < Top) Top = pt.Y;
                    if (pt.Y > Bottom) Bottom = pt.Y;
                }
            }

            Level = level;
        }

        #region ICloneable Members
        public object Clone()
        {
            return new GoogleRectangle(Left, Top, Right, Bottom, Level);
        }
        #endregion

        #region IComparable Members
        public int CompareTo(Object obj)
        {
            var rectangle = (GoogleRectangle)obj;
            var res = LeftTop.CompareTo(rectangle.LeftTop);
            if (res != 0) return res;
            return RightBottom.CompareTo(rectangle.RightBottom);
        }
        #endregion

        public static implicit operator CoordinateRectangle(GoogleRectangle google)
        {
            return new CoordinateRectangle(google.LeftTop, google.RightBottom);
        }

        /// <summary>
        /// Google bitmap block count for (X, Y)
        /// </summary>
        public Rect BlockView
        {
            get
            {
                return new Rect(
                    new Point((int)(Left / GoogleBlock.BlockSize),
                        (int)(Top / GoogleBlock.BlockSize)),
                    new Point((int)((Right - GoogleBlock.BlockSize) / GoogleBlock.BlockSize) + 1,
                        (int)((Bottom - GoogleBlock.BlockSize) / GoogleBlock.BlockSize) + 1));
            }
        }

        public Rect GetScreenRect(GoogleRectangle screenView)
        {
            if (Level != screenView.Level)
            {
                screenView = new GoogleRectangle(
                    new CoordinateRectangle(screenView.LeftTop, screenView.RightBottom), Level);
            }

            var pt1 = LeftTop.GetScreenPoint(screenView);
            var pt2 = RightBottom.GetScreenPoint(screenView);

            return new Rect(pt1, pt2);
        }

        public InterseptResult PointContains(Coordinate point)
        {
            return ((CoordinateRectangle)this).PointContains(point);
        }

        public InterseptResult RectangleContains(CoordinateRectangle rectangle)
        {
            return ((CoordinateRectangle)this).RectangleContains(rectangle);
        }

        public InterseptResult LineContains(CoordinateRectangle line)
        {
            return ((CoordinateRectangle)this).LineContains(line);
        }

        public InterseptResult PoligonContains(CoordinatePoligon poligon)
        {
            return ((CoordinateRectangle)this).PoligonContains(poligon);
        }
    }
}
