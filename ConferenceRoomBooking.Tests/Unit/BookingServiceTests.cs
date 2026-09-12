using ConferenceRoomBooking.Contracts.Bookings;
using ConferenceRoomBooking.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using BookingService = ConferenceRoomBooking.Services.BookingService;

namespace ConferenceRoomBooking.Tests.Unit;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenEndTimeIsNotAfterStartTime_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new CreateBookingRequest(
            Guid.NewGuid(),
            new DateTime(2026, 9, 6, 12, 0, 0),
            new DateTime(2026, 9, 6, 10, 0, 0),
            []);

        await using var dbContext = CreateDbContext();
        var bookingService = CreateBookingService(dbContext);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => bookingService.CreateAsync(request, CancellationToken.None));

        // Assert
        Assert.Equal("End time must be greater than start time.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenBookingSpansMultipleDays_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new CreateBookingRequest(
            Guid.NewGuid(),
            new DateTime(2026, 9, 6, 22, 0, 0),
            new DateTime(2026, 9, 7, 2, 0, 0),
            []);

        await using var dbContext = CreateDbContext();
        var bookingService = CreateBookingService(dbContext);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => bookingService.CreateAsync(request, CancellationToken.None));

        // Assert
        Assert.Equal("Booking must start and end on the same day.", exception.Message);
    }

    private static BookingService CreateBookingService(ConferenceRoomBookingDbContext dbContext)
    {
        return new BookingService(dbContext, NullLogger<BookingService>.Instance);
    }

    private static ConferenceRoomBookingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ConferenceRoomBookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ConferenceRoomBookingDbContext(options);
    }
}