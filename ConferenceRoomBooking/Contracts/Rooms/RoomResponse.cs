using ConferenceRoomBooking.Contracts.Services;

namespace ConferenceRoomBooking.Contracts.Rooms
{
    public record RoomResponse(
        Guid Id,
        string Name,
        int Capacity,
        decimal HourlyRate,
        List<ServiceResponse> Services);
}
