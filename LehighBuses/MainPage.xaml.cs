using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using LehighBuses.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.IO.IsolatedStorage;
using System.Windows.Shapes;
using Microsoft.Phone.Maps.Controls;
using System.Device.Location;

namespace LehighBuses
{
    public partial class MainPage : PhoneApplicationPage
    {
        ProgressIndicator progGetData;
        dynamic store = IsolatedStorageSettings.ApplicationSettings;

        public MainPage()
        {
            InitializeComponent();

            initializeCollections();
            initializeProgressBar();
            setTileColor();
            getJson();
            
        }

        private void initializeProgressBar()
        {
            progGetData = new ProgressIndicator();
            progGetData.Text = "Updating...";
            progGetData.IsIndeterminate = true;
            progGetData.IsVisible = false;
        }
        private void startProgGetData()
        {
            SystemTray.SetIsVisible(this, true);
            SystemTray.SetOpacity(this, 0);
            progGetData.IsVisible = true;
            SystemTray.SetProgressIndicator(this, progGetData);
        
        }
        private void initializeCollections()
        {
            buses = new ObservableCollection<Bus>();
            departures = new ObservableCollection<Depart>();
            arrivals = new ObservableCollection<Arrival>();
        }
        private void setTileColor()
        {
            ShellTile tile = ShellTile.ActiveTiles.ElementAtOrDefault(0);
            IconicTileData TileData = new IconicTileData
            {
                BackgroundColor = HexToColor("#FF4C280F")
            };
            tile.Update(TileData);
        }

        public ObservableCollection<Bus> buses { get; set; }
        public ObservableCollection<Depart> departures { get; set; }
        public ObservableCollection<Arrival> arrivals { get; set; }

        public class Bus
        {
            public string name { get; set; }
            public ObservableCollection<Depart> departures { get; set; }
            public ObservableCollection<Arrival> arrivals { get; set; }

        }

        public class Depart
        {
            public string leave { get; set; }
        }

        public class Arrival
        {
            public int id { get; set; }
            public string name { get; set; }
            public string lat { get; set; }
            public string lon { get; set; }
            public string arrival { get; set; }
            public string concat { get; set; }
        }



        private void getJson()
        {
            startProgGetData();
            WebClient webClient = new WebClient();
            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(webClient_DownloadStringCompleted);
            webClient.DownloadStringAsync(new Uri("http://bus.lehigh.edu/scripts/routestoptimes.php?format=json"));
        }

        private void webClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null && e.Result != null)
            {
                setData(e.Result);
                setupMap();
            }
        }

        private void setData(string e)
        {
            //Clear out lists from last time
            buses = new ObservableCollection<Bus>();
            departures = new ObservableCollection<Depart>();
            arrivals = new ObservableCollection<Arrival>();


            buses.Clear();
            departures.Clear();
            arrivals.Clear();

            //Convert to a JObject
            JObject o = JObject.Parse(e);

            for (int x = 1; x < 5; x++)
            {
                departures = new ObservableCollection<Depart>();
                arrivals = new ObservableCollection<Arrival>();

                //Get Route Name
                string routeName = o[x.ToString()]["name"].ToString();


                //Get departure times
                dynamic[] departArray = o[x.ToString()]["schedule"].ToArray();
                for (int y = 0; y < departArray.Length; y++)
                {
                    departures.Add(new Depart { leave = o[x.ToString()]["schedule"].ElementAtOrDefault(y).First().ToString() });
                }

                //get arrival times
                dynamic[] arriveArray = o[x.ToString()]["stops"].ToArray();
                for (int z = 0; z < arriveArray.Length; z++)
                {
                    string idString = (o[x.ToString()]["stops"].ElementAtOrDefault(z).First()["id"].ToString());
                    int id = Convert.ToInt32(idString);
                    string arrivalName = o[x.ToString()]["stops"].ElementAtOrDefault(z).First()["name"].ToString();
                    string lat = o[x.ToString()]["stops"].ElementAtOrDefault(z).First()["lat"].ToString();
                    string lon = o[x.ToString()]["stops"].ElementAtOrDefault(z).First()["long"].ToString();
                    string arrivalTime = o[x.ToString()]["stops"].ElementAtOrDefault(z).First()["arrival"].ToString();

                    string concat = arrivalName + ":  " + arrivalTime;

                    arrivals.Add(new Arrival { id = id, arrival = arrivalTime, lat = lat, lon = lon, name = arrivalName, concat = concat });
                }
                buses.Add(new Bus { name = routeName, departures = departures, arrivals = arrivals });
            }
            BusRoutes.ItemsSource = buses;
            progGetData.IsVisible = false;
        }

        public Color HexToColor(string hex)
        {
            return Color.FromArgb(
                Convert.ToByte(hex.Substring(1, 2), 16),
                Convert.ToByte(hex.Substring(3, 2), 16),
                Convert.ToByte(hex.Substring(5, 2), 16),
                Convert.ToByte(hex.Substring(7, 2), 16)
                );
        }

        private void setupMap()
        {
            busMap.Center = new GeoCoordinate(40.61, -75.38);
            busMap.ZoomLevel = 15;
            addLocations();
        }

        private void addLocations()
        {
            foreach (Bus bus in buses)
            {
                ObservableCollection<Arrival> arrivals = bus.arrivals;
                string name = bus.name;
                foreach (Arrival arrival in arrivals)
                {
                    double lat = Convert.ToDouble(arrival.lat);
                    double lon = Convert.ToDouble(arrival.lon);
                    plotPoint(lat, lon, name);
                }
            }
        }

        private void plotPoint(double lat, double lon, string name)
        {

            //Create a shape
            Polygon triangle = new Polygon();
            triangle.Points.Add((new Point(0, 0)));
            triangle.Points.Add((new Point(0, 40)));
            triangle.Points.Add((new Point(20, 40)));
            triangle.Points.Add((new Point(20, 20)));
            ScaleTransform flip = new ScaleTransform();
            flip.ScaleY = -1;
            triangle.RenderTransform = flip;

            //Color the shape
            if (name.Contains("Mountaintop"))
            {
                triangle.Fill = new SolidColorBrush(Colors.Blue);
            }
            else if (name.Contains("Saucon"))
            {
                triangle.Fill = new SolidColorBrush(Colors.Red);
            }
            else if (name.Contains("T.R.A.C.S."))
            {
                triangle.Fill = new SolidColorBrush(Colors.Green);
            }
            else if (name.Contains("Athletics"))
            {
                triangle.Fill = new SolidColorBrush(Colors.Black);
            }
            else
            {
                triangle.Fill = new SolidColorBrush(Colors.Gray);
            }

            // Create a MapOverlay to contain the marker
            MapOverlay myLocationOverlay = new MapOverlay();

            myLocationOverlay.Content = triangle;
            myLocationOverlay.PositionOrigin = new Point(0, 0);

            myLocationOverlay.GeoCoordinate = new GeoCoordinate(lat, lon);

            // Create a MapLayer to contain the MapOverlay.
            MapLayer myLocationLayer = new MapLayer();
            myLocationLayer.Add(myLocationOverlay);

            // Add the MapLayer to the Map.
            busMap.Layers.Add(myLocationLayer);
        }

        private void overlayBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            store["buses"] = buses;
            NavigationService.Navigate(new Uri("/FullMap.xaml", UriKind.Relative));
        }
        private void busMap_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            return;
        }
        private void refresh_Click(object sender, EventArgs e)
        {
            getJson();
        }
        
    }
}