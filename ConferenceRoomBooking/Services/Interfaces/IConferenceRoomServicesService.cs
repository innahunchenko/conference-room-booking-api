using ConferenceRoomBooking.Contracts.Services;

namespace ConferenceRoomBooking.Services.Interfaces
{
    public interface IConferenceRoomServicesService
    {
        Task<List<ServiceResponse>> GetAllAsync(CancellationToken cancellationToken);
    }
}