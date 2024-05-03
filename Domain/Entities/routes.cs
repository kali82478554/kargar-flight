using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{    
    public class routes
    {        
        public int route_id { get; set; }
        public int origin_city_id { get; set; }
        public int destination_city_id { get; set; }
        public DateTime departure_date { get; set; }

        public virtual ICollection<flights> Flights { get; set; } = new List<flights>();
    }
}
