namespace ConferenceRoomBooking.Contracts.Rooms
{
    public record CreateRoomRequest(
        string Name, 
        int Capacity, 
        decimal HourlyRate, 
        List<Guid> ServiceIds);
}
