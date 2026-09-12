namespace ConferenceRoomBooking.Database.Entities
{
    public class Room
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = null!;

        public int Capacity { get; set; }

        public decimal HourlyRate { get; set; }

        public bool IsDeleted { get; set; }

        public ICollection<Service> Services { get; set; }
            = new List<Service>();

        public ICollection<Booking> Bookings { get; set; }
            = new List<Booking>();
    }
}
