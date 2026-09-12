using ConferenceRoomBooking.Contracts.Bookings;

namespace ConferenceRoomBooking.Services.Interfaces
{
    public interface IBookingService
    {
        Task<List<BookingResponse>> GetAllAsync(CancellationToken cancellationToken);
        Task<BookingResponse> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken);
    }
}