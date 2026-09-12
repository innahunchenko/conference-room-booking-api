using ConferenceRoomBooking.Contracts.Bookings;
using ConferenceRoomBooking.Services.Interfaces;

namespace ConferenceRoomBooking.Endpoints;

public static class BookingEndpoints
{
    public static void MapBookingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/bookings").WithTags("Bookings");

        group.MapGet("/", async (IBookingService bookingService, CancellationToken cancellationToken) =>
        {
            var bookings = await bookingService.GetAllAsync(cancellationToken);

            return Results.Ok(bookings);
        })
            .WithName("GetAllBookings").WithSummary("Get all bookings");

        // POST /api/bookings
        group.MapPost("/", async (CreateBookingRequest request, IBookingService bookingService, CancellationToken cancellationToken) =>
        {
            var booking = await bookingService.CreateAsync(request, cancellationToken);

            return Results.Created($"/api/bookings/{booking.Id}", booking);
        })
            .WithName("CreateBooking").WithSummary("Book a conference room");
    }
}