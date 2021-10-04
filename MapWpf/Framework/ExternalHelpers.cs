namespace MapWpf.Framework
{
    public static class ExternalHelpers
    {
        public static object Clone(this System.Windows.Rect src)
        {
            return new System.Windows.Rect(src.Location, src.Size);
        }

        public static System.Windows.Point GetCenter(this System.Windows.Rect rect)
        {
            return new System.Windows.Point(
                (rect.Right - rect.Left) / 2, (rect.Bottom - rect.Top) / 2);
        }

        public static bool Contains(this System.Windows.Point pt, System.Windows.Rect rect)
        {
            return pt.X >= rect.Left && pt.X <= rect.Right
                && pt.Y >= rect.Top && pt.Y <= rect.Bottom;
        }
    }
}
