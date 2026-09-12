using ConferenceRoomBooking.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Database
{
    public class ConferenceRoomBookingDbContext : DbContext
    {
        public ConferenceRoomBookingDbContext(DbContextOptions<ConferenceRoomBookingDbContext> options)
            : base(options) { }

        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<BookingService> BookingServices => Set<BookingService>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConferenceRoomBookingDbContext).Assembly);
        }
    }
}
