using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{  
    public class flightsCsv
    {        
        public int flight_id { get; set; }
        public int route_id { get; set; }
        public DateTime departure_time { get; set; }
        public DateTime arrival_time { get; set; }
        public int airline_id { get; set; }

    }
}
