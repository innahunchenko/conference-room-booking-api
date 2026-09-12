namespace ConferenceRoomBooking.Database.Entities
{
    public class BookingService
    {
        public Guid BookingId { get; set; }

        public Booking Booking { get; set; } = null!;

        public Guid ServiceId { get; set; }

        public Service Service { get; set; } = null!;

        // Price of the service at the time of booking
        public decimal Price { get; set; }
    }
}
