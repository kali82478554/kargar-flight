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
    public class routesConfig : IEntityTypeConfiguration<routes>
    {
        public void Configure(EntityTypeBuilder<routes> builder)
        {
            builder.HasKey(s => s.route_id);
            builder.Property(s => s.route_id).ValueGeneratedNever();
        }
    }
}
