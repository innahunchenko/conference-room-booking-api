using ConferenceRoomBooking.Database.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Database.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("Bookings");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.StartTime).IsRequired();

            builder.Property(x => x.EndTime).IsRequired();

            builder.Property(x => x.TotalPrice).IsRequired().HasPrecision(18, 2);

            builder.HasOne(x => x.Room)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.RoomId, x.StartTime, x.EndTime });
        }
    }
}
