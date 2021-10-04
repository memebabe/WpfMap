using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MapWpf
{
    public class Place
    {
        public Place(object sender)
        {
            var lon = (double)sender.GetType().GetProperty("Longitude").GetValue(sender);
            var lat = (double)sender.GetType().GetProperty("Latitude").GetValue(sender);
            this.DataObject = sender;
            this.Location = new Coordinate(lon, lat);
        }

        public Place(object sender, double lon, double lat)
        {
            this.DataObject = sender;
            this.Location = new Coordinate(lon, lat);
        }

        public object DataObject { get; set; }
        public Coordinate Location { get; set; }
        public double Longitude
        {
            get { return Location.Longitude; }
        }
        public double Latitude
        {
            get { return Location.Latitude; }
        }
    }
}
