namespace ConferenceRoomBooking.Contracts.Rooms
{
    public record UpdateRoomRequest(
        string Name,
        int Capacity,
        decimal HourlyRate,
        List<Guid> ServiceIds);
}
