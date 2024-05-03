using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Configuration
{
    public class flightsConfig : IEntityTypeConfiguration<flights>
    {
        public void Configure(EntityTypeBuilder<flights> builder)
        {
            builder.HasKey(s => s.flight_id);
            builder.Property(s => s.flight_id).ValueGeneratedNever();
            builder.HasOne(c => c.Routes).WithMany(c => c.Flights).HasForeignKey(c => c.route_id).OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
