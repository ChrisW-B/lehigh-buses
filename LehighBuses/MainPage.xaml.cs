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

namespace LehighBuses
{
    public partial class MainPage : PhoneApplicationPage
    {


        public MainPage()
        {
            InitializeComponent();


            buses = new ObservableCollection<Bus>();
            departures = new ObservableCollection<Depart>();
            arrivals = new ObservableCollection<Arrival>();

            getJson();
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
           
            WebClient webClient = new WebClient();
            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(webClient_DownloadStringCompleted);
            webClient.DownloadStringAsync(new Uri("http://bus.lehigh.edu/scripts/routestoptimes.php?format=json"));
        }

        private void webClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null && e.Result != null)
            {
                //Clear out lists from last time
                buses = new ObservableCollection<Bus>();
                departures = new ObservableCollection<Depart>();
                arrivals = new ObservableCollection<Arrival>();
                

                buses.Clear();
                departures.Clear();
                arrivals.Clear();

                //Convert to a JObject
                JObject o = JObject.Parse(e.Result);

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

                        arrivals.Add(new Arrival { id = id, arrival = arrivalTime, lat = lat, lon = lon, name = arrivalName, concat= concat });
                    }
                    buses.Add(new Bus { name = routeName, departures = departures, arrivals = arrivals });
                }
                BusRoutes.ItemsSource = buses;
            }
        }

        
    }
}