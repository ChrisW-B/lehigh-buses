﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Device.Location;
using System.IO.IsolatedStorage;
using System.Collections.ObjectModel;
using Microsoft.Phone.Maps.Controls;


namespace LehighBuses
{
    public partial class FullMap : PhoneApplicationPage
    {
        #region variables
        private bool errorSet;
        private bool isCurrent;
        private int locationSearchTimes;
        

        private string currentLat;
        private string currentLon;

        dynamic store = IsolatedStorageSettings.ApplicationSettings;

        public ObservableCollection<Bus> buses { get; set; }
        public ObservableCollection<Depart> departures { get; set; }
        public ObservableCollection<Arrival> arrivals { get; set; }
        #endregion

        public FullMap()
        {
            InitializeComponent();
            if (store.Contains("buses"))
            {
                buses = store["buses"];
            }
            isCurrent = true;
            setupMap();
        }

        //Map stuff
        private void setupMap()
        {
            locationSearchTimes = 0;
            errorSet = false;

            findLocation();
            busMap.Center = new GeoCoordinate(Convert.ToDouble(currentLat), Convert.ToDouble(currentLon));
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
            addCurrentLocation();
        }
        private void addCurrentLocation()
        {

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
            MapOverlay myLocationOverlay = new MapOverlay();

            double lat = Convert.ToDouble(currentLat);
            double lon = Convert.ToDouble(currentLon);

            myLocationOverlay.Content = triangle;
            myLocationOverlay.PositionOrigin = new Point(0, 0);

            myLocationOverlay.GeoCoordinate = new GeoCoordinate(lat, lon);

            // Create a MapLayer to contain the MapOverlay.
            MapLayer myLocationLayer = new MapLayer();
            myLocationLayer.Add(myLocationOverlay);

            // Add the MapLayer to the Map.
            busMap.Layers.Add(myLocationLayer);

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

                    if (isCurrent && locationSearchTimes <= 5)
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
                            locationSearchTimes++;
                            findLocation();
                        }
                    }
                    if (locationSearchTimes > 5)
                    {
                        if (store.Contains("loc"))
                        {
                            String[] loc = (string[])store["loc"];
                            currentLat = loc[0];
                            currentLon = loc[1];

                            //prevent reuse of same location
                            store.Remove("loc");
                        }
                        else if (!errorSet)
                        {
                            errorSet = true;
                            //errorText.Text = "Cannot get current location. Check to make sure location services are enabled";
                        }
                        else
                        {
                            currentLat = "0";
                            currentLon = "0";
                        }
                    }
                }
                else
                {
                    errorSet = true;
                    //errorText.Text = "Cannot get current location. Check to make sure location services are enabled";
                }
            }
            else
            {
                store["enableLocation"] = true;
                findLocation();
            }
        }

        void busMap_Hold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (busMap.CartographicMode == MapCartographicMode.Hybrid)
            {
                busMap.CartographicMode = MapCartographicMode.Road;
            }
            else if (busMap.CartographicMode == MapCartographicMode.Road)
            {
                busMap.CartographicMode = MapCartographicMode.Hybrid;
            }
        }

    }
}