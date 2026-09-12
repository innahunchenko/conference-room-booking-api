using ConferenceRoomBooking.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRoomBooking.Database.Configurations
{
    public class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> builder)
        {
            builder.ToTable("Rooms");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);

            builder.Property(x => x.Capacity).IsRequired();

            builder.Property(x => x.HourlyRate).IsRequired().HasPrecision(18, 2);

            builder.HasMany(x => x.Services).WithMany(x => x.Rooms).UsingEntity("RoomServices");

            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
