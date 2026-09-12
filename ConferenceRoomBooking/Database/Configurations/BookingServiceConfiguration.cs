using ConferenceRoomBooking.Database.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Database.Configurations
{
    public class BookingServiceConfiguration : IEntityTypeConfiguration<BookingService>
    {
        public void Configure(EntityTypeBuilder<BookingService> builder)
        {
            builder.ToTable("BookingServices");

            builder.HasKey(x => new { x.BookingId, x.ServiceId });

            builder.Property(x => x.Price).IsRequired().HasPrecision(18, 2);

            builder.HasOne(x => x.Booking)
                .WithMany(x => x.BookingServices)
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Service)
                .WithMany(x => x.BookingServices)
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
