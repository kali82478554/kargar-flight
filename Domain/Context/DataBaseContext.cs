using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Configuration;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Domain.Context
{
    public class DataBaseContext : DbContext
    {
        public DataBaseContext(DbContextOptions<DataBaseContext> options) : base(options)
        {

        }

        public virtual DbSet<routes> Routes { get; set; }
        public virtual DbSet<flights> Flights { get; set; }
        public virtual DbSet<subscriptions> SubScriptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new routesConfig());
            modelBuilder.ApplyConfiguration(new flightsConfig());
            modelBuilder.ApplyConfiguration(new subscriptionsConfig());
        }

    }
}
