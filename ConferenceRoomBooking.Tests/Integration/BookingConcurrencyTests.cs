using ConferenceRoomBooking.Contracts.Bookings;
using ConferenceRoomBooking.Database;
using ConferenceRoomBooking.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.MsSql;
using BookingService = ConferenceRoomBooking.Services.BookingService;

namespace ConferenceRoomBooking.Tests.Integration;

public class BookingConcurrencyTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private DbContextOptions<ConferenceRoomBookingDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();

        _options = new DbContextOptionsBuilder<ConferenceRoomBookingDbContext>()
            .UseSqlServer(_sqlServer.GetConnectionString())
            .Options;

        await using var dbContext = new ConferenceRoomBookingDbContext(_options);

        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlServer.DisposeAsync();
    }

    /// <summary>
    /// Verifies that when multiple concurrent requests attempt to book the same room
    /// for the same time period, only one booking is created and all other requests are rejected.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenMultipleRequestsBookSameRoom_ShouldCreateOnlyOneBooking()
    {
        // Arrange
        var roomId = await CreateRoomAsync();

        var request = CreateBookingRequest(roomId);

        const int concurrentRequests = 10;

        var startSignal = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => CreateBookingConcurrentlyAsync(request, startSignal))
            .ToArray();

        // Start all requests at approximately the same time.
        startSignal.SetResult();

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulBookings = results.Count(x => x.Success);
        var conflicts = results.Count(x => x.Exception is InvalidOperationException);

        Assert.Equal(1, successfulBookings);
        Assert.Equal(9, conflicts);

        await using var dbContext = new ConferenceRoomBookingDbContext(_options);

        var bookings = await dbContext.Bookings.Where(x => x.RoomId == roomId).ToListAsync();

        Assert.Single(bookings);
    }

    /// <summary>
    /// Verifies that concurrent booking requests for different rooms
    /// can be processed independently, with one booking created for each room.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenMultipleRequestsBookDifferentRooms_ShouldCreateOneBookingPerRoom()
    {
        // Arrange
        var room1Id = await CreateRoomAsync();
        var room2Id = await CreateRoomAsync();

        var request1 = CreateBookingRequest(room1Id);
        var request2 = CreateBookingRequest(room2Id);

        const int concurrentRequestsPerRoom = 10;

        var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = new List<Task<(bool Success, Exception? Exception)>>();

        for (var i = 0; i < concurrentRequestsPerRoom; i++)
        {
            tasks.Add(CreateBookingConcurrentlyAsync(request1, startSignal));
            tasks.Add(CreateBookingConcurrentlyAsync(request2, startSignal));
        }

        // Start all requests at approximately the same time.
        startSignal.SetResult();

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulBookings = results.Count(x => x.Success);
        var conflicts = results.Count(x => x.Exception is InvalidOperationException);

        Assert.Equal(2, successfulBookings);
        Assert.Equal(18, conflicts);

        await using var dbContext = new ConferenceRoomBookingDbContext(_options);

        var room1Bookings = await dbContext.Bookings.CountAsync(x => x.RoomId == room1Id);

        var room2Bookings = await dbContext.Bookings.CountAsync(x => x.RoomId == room2Id);

        Assert.Equal(1, room1Bookings);
        Assert.Equal(1, room2Bookings);
    }

    private async Task<Guid> CreateRoomAsync()
    {
        await using var dbContext = new ConferenceRoomBookingDbContext(_options);

        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = $"Conference Room {Guid.NewGuid()}",
            Capacity = 10,
            HourlyRate = 100
        };

        dbContext.Rooms.Add(room);

        await dbContext.SaveChangesAsync();

        return room.Id;
    }

    private static CreateBookingRequest CreateBookingRequest(Guid roomId)
    {
        return new CreateBookingRequest(
            roomId,
            new DateTime(2026, 9, 6, 10, 0, 0),
            new DateTime(2026, 9, 6, 12, 0, 0),
            new List<Guid>());
    }

    private async Task<(bool Success, Exception? Exception)> CreateBookingConcurrentlyAsync(
        CreateBookingRequest request,
        TaskCompletionSource startSignal)
    {
        try
        {
            // Wait until all requests have been created.
            await startSignal.Task;

            await using var dbContext = new ConferenceRoomBookingDbContext(_options);

            var logger = new Mock<ILogger<BookingService>>();

            var bookingService = new BookingService(dbContext, logger.Object);

            await bookingService.CreateAsync(request, CancellationToken.None);

            return (true, null);
        }
        catch (Exception exception)
        {
            return (false, exception);
        }
    }
}