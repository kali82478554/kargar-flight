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
    public class subscriptionsConfig : IEntityTypeConfiguration<subscriptions>
    {
        public void Configure(EntityTypeBuilder<subscriptions> builder)
        {
            builder.HasKey(s => new { s.agency_id, s.origin_city_id, s.destination_city_id });
        }
    }
}
