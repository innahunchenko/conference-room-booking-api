namespace ConferenceRoomBooking.Contracts.Bookings
{
    public record SearchRoomsRequest(
        DateTime StartTime,
        DateTime EndTime,
        int Capacity);
}
