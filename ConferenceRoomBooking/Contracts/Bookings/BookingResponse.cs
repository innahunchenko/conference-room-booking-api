using ConferenceRoomBooking.Contracts.Services;

namespace ConferenceRoomBooking.Contracts.Bookings
{
    public record BookingResponse(
        Guid Id,
        Guid RoomId,
        DateTime StartTime,
        DateTime EndTime,
        decimal TotalPrice,
        List<ServiceResponse> Services);
}
