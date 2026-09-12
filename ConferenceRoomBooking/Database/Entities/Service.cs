namespace ConferenceRoomBooking.Database.Entities
{
    public class Service
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = null!;

        // Current price of the service
        public decimal Price { get; set; }

        public ICollection<Room> Rooms { get; set; }
            = new List<Room>();

        public ICollection<BookingService> BookingServices { get; set; }
            = new List<BookingService>();
    }
}
