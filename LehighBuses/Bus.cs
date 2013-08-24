using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LehighBuses
{
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
}
