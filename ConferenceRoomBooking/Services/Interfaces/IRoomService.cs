using ConferenceRoomBooking.Contracts.Rooms;

namespace ConferenceRoomBooking.Services.Interfaces
{
    public interface IRoomService
    {
        Task<RoomResponse> CreateAsync(CreateRoomRequest request, CancellationToken cancellationToken);
        Task UpdateAsync(Guid id, UpdateRoomRequest request, CancellationToken cancellationToken);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken);
        Task<List<RoomResponse>> GetAllAsync(CancellationToken cancellationToken);
        Task<List<RoomResponse>> GetAvailableAsync(DateTime startTime, DateTime endTime, int capacity, CancellationToken cancellationToken);
    }
}