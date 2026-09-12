using ConferenceRoomBooking.Contracts.Bookings;
using ConferenceRoomBooking.Contracts.Services;
using ConferenceRoomBooking.Database.Entities;

namespace ConferenceRoomBooking.Mappings;

public static class BookingMapping
{
    public static Booking ToEntity(this CreateBookingRequest request, decimal totalPrice)
    {
        return new Booking
        {
            RoomId = request.RoomId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            TotalPrice = totalPrice
        };
    }

    public static List<BookingService> ToBookingServices(this IEnumerable<Service> services)
    {
        return services.Select(service => new BookingService {
            ServiceId = service.Id,
            Service = service,
            Price = service.Price
        }).ToList();
    }

    public static BookingResponse ToResponse(this Booking booking)
    {
        return new BookingResponse(
            booking.Id,
            booking.RoomId,
            booking.StartTime,
            booking.EndTime,
            booking.TotalPrice,
            booking.BookingServices.Select(bookingService => new ServiceResponse(
                bookingService.Service.Id,
                bookingService.Service.Name,
                bookingService.Price)).ToList());
    }
}