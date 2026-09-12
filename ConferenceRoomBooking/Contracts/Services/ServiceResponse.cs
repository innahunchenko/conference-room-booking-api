namespace ConferenceRoomBooking.Contracts.Services
{
    public record ServiceResponse(
        Guid Id,
        string Name,
        decimal Price);
}
