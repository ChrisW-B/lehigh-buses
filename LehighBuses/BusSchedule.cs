using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LehighBuses
{
    public class scheduleobjs
    {
        public class objItem
        {
            public string id { get; set; }
            public string name { get; set; }
            public string time { get; set; }
        }
    }

    public class schedule
    {
        public string first { get; set; }
        public string second { get; set; }
        public string third { get; set; }
        public string fourth { get; set; }
    }

    public class stops
    {
        public class stopItem
        {
            public int id { get; set; }
            public string name { get; set; }
            public string lat { get; set; }
            public string lon { get; set; }
            public string arrival { get; set; }
        }
    }


    public class BusSchedule
    {
        public string name { get; set; }
        public string status { get; set; }
        public scheduleobjs scheduleobs { get; set; }
        public schedule schedule { get; set; }
        public stops stops { get; set; }
    }
}
