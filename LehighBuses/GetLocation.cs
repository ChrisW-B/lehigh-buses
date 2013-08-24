using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LehighBuses
{
    class getLocation
    {
        #region variables
        private string latitude;
        private string longitude;
        #endregion

        #region getters
        public string getLat()
        {
            return latitude;
        }
        public string getLong()
        {
            return longitude;
        }
        #endregion

        public getLocation()
        {
            getInfo();
        }

        private void getInfo()
        {
            GeoCoordinateWatcher watcher = new GeoCoordinateWatcher();
            watcher.Start();

            GeoCoordinate coord = watcher.Position.Location;

            if (coord.IsUnknown != true)
            {
                latitude = coord.Latitude.ToString();
                longitude = coord.Longitude.ToString();
            }
            else
            {
                Console.WriteLine("Unknown latitude and longitude.");
            }
        }
    }
}
