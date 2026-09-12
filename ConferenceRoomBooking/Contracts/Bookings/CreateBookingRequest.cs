namespace ConferenceRoomBooking.Contracts.Bookings
{
    public record CreateBookingRequest(
        Guid RoomId,
        DateTime StartTime,
        DateTime EndTime,
        List<Guid> ServiceIds);
}
