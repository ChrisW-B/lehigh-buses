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
using System.Windows.Threading;

namespace LehighBuses
{
    public partial class MainPage : PhoneApplicationPage
    {
        #region variables
        ProgressIndicator progGetData;
        IsolatedStorageSettings store = IsolatedStorageSettings.ApplicationSettings;
        private string currentLat;
        private string currentLon;

        MapOverlay myLocationOverlay;
        MapLayer myLocationLayer;

        private bool isCurrent = true;
        private int locationSearchTimes;

        private bool errorSet;

        public ObservableCollection<Bus> buses { get; set; }
        public ObservableCollection<Depart> departures { get; set; }
        public ObservableCollection<Arrival> arrivals { get; set; }
        #endregion

        public MainPage()
        {
            InitializeComponent();

            initializeCollections();
            initializeProgressBar();
            setTileColor();
        }
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            getJson();
            autoRefresh();
        }
        private void autoRefresh()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += delegate(object s, EventArgs args)
            {
                getJson();
            };
            timer.Interval = new TimeSpan(0, 0, 30);
            timer.Start();
        }

        //General Setup stuff
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

        //Bus Data
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
                locationSearchTimes = 0;
                setData(e.Result);
                findLocation();
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

        //Map Stuff
        private void setupMap()
        {
            busMap.Center = new GeoCoordinate(Convert.ToDouble(currentLat), Convert.ToDouble(currentLon));
            busMap.ZoomLevel = 15;
            busMap.IsEnabled = false;
            addLocations();
        }
        private void addLocations()
        {
            foreach (Bus bus in buses)
            {
                ObservableCollection<Arrival> arrivals = new ObservableCollection<Arrival>();
                   arrivals = bus.arrivals;
                string name = bus.name;
                foreach (Arrival arrival in arrivals)
                {
                    double lat = Convert.ToDouble(arrival.lat);
                    double lon = Convert.ToDouble(arrival.lon);
                    plotPoint(lat, lon, name);
                }
            }
            setCurrentLocation();
        }
        private void setCurrentLocation()
        {
            if (store.Contains("enableLocation"))
            {
                if ((bool)store["enableLocation"])
                {
                    if (myLocationLayer != null)
                    {
                        busMap.Layers.Remove(myLocationLayer);
                    }

                    //create a marker
                    Polygon triangle = new Polygon();
                    triangle.Fill = new SolidColorBrush(Colors.Black);
                    triangle.Points.Add((new Point(0, 0)));
                    triangle.Points.Add((new Point(0, 20)));
                    triangle.Points.Add((new Point(20, 20)));
                    triangle.Points.Add((new Point(20, 0)));

                    //Rotate it
                    RotateTransform rotate = new RotateTransform();
                    rotate.Angle = 45;
                    triangle.RenderTransform = rotate;

                    // Create a MapOverlay to contain the marker
                    myLocationOverlay = new MapOverlay();

                    double lat = Convert.ToDouble(currentLat);
                    double lon = Convert.ToDouble(currentLon);

                    myLocationOverlay.Content = triangle;
                    myLocationOverlay.PositionOrigin = new Point(0, 0);

                    myLocationOverlay.GeoCoordinate = new GeoCoordinate(lat, lon);

                    // Create a MapLayer to contain the MapOverlay.
                    myLocationLayer = new MapLayer();
                    myLocationLayer.Add(myLocationOverlay);

                    // Add the MapLayer to the Map.
                    busMap.Layers.Add(myLocationLayer);
                }
                else
                {
                    return;
                }
            }
            else
            {
                store["enableLocation"] = true;
                setCurrentLocation();
            }
        }
        private void plotPoint(double lat, double lon, string name)
        {

            //Create a shape
            Polygon triangle = new Polygon();
            triangle.Points.Add((new Point(0, 0)));
            triangle.Points.Add((new Point(0, 60)));
            triangle.Points.Add((new Point(20, 60)));
            triangle.Points.Add((new Point(20, 20)));
            ScaleTransform flip = new ScaleTransform();
            flip.ScaleY = -1;
            triangle.RenderTransform = flip;

            //Color the shape
            if (name.Contains("Mountaintop"))
            {
                triangle.Fill = new SolidColorBrush(Colors.Blue);
                triangle.Height = 50;
            }
            else if (name.Contains("Saucon"))
            {
                triangle.Fill = new SolidColorBrush(Colors.Red);
                triangle.Height = 40;
            }
            else if (name.Contains("T.R.A.C.S."))
            {
                triangle.Fill = new SolidColorBrush(Colors.Green);
                triangle.Height = 30;
            }
            else if (name.Contains("Athletics"))
            {
                triangle.Fill = new SolidColorBrush(Colors.Black);
                triangle.Height = 30;
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
        private void findLocation()
        {
            if (store.Contains("enableLocation"))
            {
                if ((bool)store["enableLocation"])
                {
                    if (isCurrent)
                    {
                        //get location
                        var getLocation = new getLocation();
                        if (getLocation.getLat() != null && getLocation.getLat() != "NA")
                        {
                            errorSet = false;
                            //Set long and lat
                            this.currentLat = getLocation.getLat();
                            this.currentLon = getLocation.getLong();

                            //Save
                            String[] loc = { currentLat, currentLon };
                            store["loc"] = loc;
                            store.Save();
                        }
                        else
                        {
                            if (store.Contains("loc"))
                            {
                                String[] loc = (string[])store["loc"];
                                currentLat = loc[0];
                                currentLon = loc[1];

                                //prevent reuse of same location
                                store.Remove("loc");
                            }
                            else
                            {
                                currentLat = "40.61";
                                currentLon = "-75.38";
                            }
                        }
                    }
                }
                else
                {
                    currentLat = "40.61";
                    currentLon = "-75.38";
                }
            }
            else
            {
                store["enableLocation"] = true;
                findLocation();
            }
        }

        //Buttons and clicks and other reactions to user input
        private void overlayBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            store["buses"] = buses;
            NavigationService.Navigate(new Uri("/FullMap.xaml", UriKind.Relative));
        }
        private void busMap_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            store["buses"] = buses;
            NavigationService.Navigate(new Uri("/FullMap.xaml", UriKind.Relative));
        }
        private void refresh_Click(object sender, EventArgs e)
        {
            getJson();
        }
        private void title_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((Panorama)sender).SelectedIndex)
            {
                case 0:
                    ApplicationBar.Mode = ApplicationBarMode.Default;
                    break;
                case 1:
                    //setupMap();
                    ApplicationBar.Mode = ApplicationBarMode.Minimized;
                    break;
            }

        }

        //Convert colors
        public Color HexToColor(string hex)
        {
            return Color.FromArgb(
                Convert.ToByte(hex.Substring(1, 2), 16),
                Convert.ToByte(hex.Substring(3, 2), 16),
                Convert.ToByte(hex.Substring(5, 2), 16),
                Convert.ToByte(hex.Substring(7, 2), 16)
                );
        }

        private void settings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void about_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        } 
    }
}