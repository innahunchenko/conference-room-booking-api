using ConferenceRoomBooking.Contracts.Rooms;
using ConferenceRoomBooking.Database;
using ConferenceRoomBooking.Database.Entities;
using ConferenceRoomBooking.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.MsSql;

namespace ConferenceRoomBooking.Tests.Integration;

public class RoomServiceIntegrationTests : IAsyncLifetime
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

        await using var dbContext = CreateDbContext();

        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlServer.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_WhenValidRequest_ShouldCreateRoom()
    {
        // Arrange
        var serviceId = await CreateServiceAsync("Projector", 50);

        var request = new CreateRoomRequest("Conference Room A", 10, 100, [serviceId]);

        await using var dbContext = CreateDbContext();

        var logger = new Mock<ILogger<RoomService>>();
        var roomService = new RoomService(dbContext, logger.Object);

        // Act
        var result = await roomService.CreateAsync(request, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Conference Room A", result.Name);
        Assert.Equal(10, result.Capacity);
        Assert.Equal(100, result.HourlyRate);

        var room = await dbContext.Rooms.Include(x => x.Services).SingleAsync(x => x.Id == result.Id);

        Assert.Equal("Conference Room A", room.Name);

        var linkedService = Assert.Single(room.Services);
        Assert.Equal(serviceId, linkedService.Id);
    }

    [Fact]
    public async Task CreateAsync_WhenServiceDoesNotExist_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new CreateRoomRequest("Conference Room A", 10, 100, [Guid.NewGuid()]);

        await using var dbContext = CreateDbContext();

        var logger = new Mock<ILogger<RoomService>>();
        var roomService = new RoomService(dbContext, logger.Object);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => roomService.CreateAsync(request, CancellationToken.None));

        // Assert
        Assert.Equal("One or more services do not exist.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenValidRequest_ShouldUpdateRoom()
    {
        // Arrange
        var oldServiceId = await CreateServiceAsync("Projector", 50);

        var newServiceId = await CreateServiceAsync("Whiteboard", 20);

        var roomId = await CreateRoomAsync("Conference Room A", 10, 100, oldServiceId);

        var request = new UpdateRoomRequest("Conference Room B", 20, 150, [newServiceId]);

        await using var dbContext = CreateDbContext();

        var logger = new Mock<ILogger<RoomService>>();
        var roomService = new RoomService(dbContext, logger.Object);

        // Act
        await roomService.UpdateAsync(roomId, request, CancellationToken.None);

        // Assert
        var room = await dbContext.Rooms.Include(x => x.Services).SingleAsync(x => x.Id == roomId);

        Assert.Equal("Conference Room B", room.Name);
        Assert.Equal(20, room.Capacity);
        Assert.Equal(150, room.HourlyRate);

        var linkedService = Assert.Single(room.Services);
        Assert.Equal(newServiceId, linkedService.Id);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteRoom()
    {
        // Arrange
        var roomId = await CreateRoomAsync("Conference Room A", 10, 100);

        await using var dbContext = CreateDbContext();

        var logger = new Mock<ILogger<RoomService>>();
        var roomService = new RoomService(dbContext, logger.Object);

        // Act
        await roomService.DeleteAsync(roomId, CancellationToken.None);

        // Assert
        var deletedRoom = await dbContext.Rooms.IgnoreQueryFilters().SingleAsync(x => x.Id == roomId);

        Assert.True(deletedRoom.IsDeleted);

        var visibleRoom = await dbContext.Rooms.SingleOrDefaultAsync(x => x.Id == roomId);

        Assert.Null(visibleRoom);
    }

    [Fact]
    public async Task GetAvailableAsync_ShouldReturnOnlyAvailableRooms()
    {
        // Arrange
        var availableRoomId = await CreateRoomAsync("Available Room", 10, 100);

        var occupiedRoomId = await CreateRoomAsync("Occupied Room", 10, 100);

        await CreateBookingAsync(
            occupiedRoomId,
            new DateTime(2026, 9, 6, 11, 0, 0),
            new DateTime(2026, 9, 6, 13, 0, 0));

        await using var dbContext = CreateDbContext();

        var logger = new Mock<ILogger<RoomService>>();
        var roomService = new RoomService(dbContext, logger.Object);

        // Act
        var result = await roomService.GetAvailableAsync(
            new DateTime(2026, 9, 6, 10, 0, 0),
            new DateTime(2026, 9, 6, 12, 0, 0),
            5,
            CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(availableRoomId, result[0].Id);
    }

    [Fact]
    public async Task GetAvailableAsync_WhenExistingBookingEndsAtRequestedStart_ShouldReturnRoom()
    {
        // Arrange
        var roomId = await CreateRoomAsync("Conference Room A", 10, 100);

        await CreateBookingAsync(
            roomId,
            new DateTime(2026, 9, 6, 8, 0, 0),
            new DateTime(2026, 9, 6, 10, 0, 0));

        await using var dbContext = CreateDbContext();

        var logger = new Mock<ILogger<RoomService>>();
        var roomService = new RoomService(dbContext, logger.Object);

        // Act
        var result = await roomService.GetAvailableAsync(
            new DateTime(2026, 9, 6, 10, 0, 0),
            new DateTime(2026, 9, 6, 12, 0, 0),
            5,
            CancellationToken.None);

        // Assert
        Assert.Contains(result, x => x.Id == roomId);
    }

    private ConferenceRoomBookingDbContext CreateDbContext()
    {
        return new ConferenceRoomBookingDbContext(_options);
    }

    private async Task<Guid> CreateServiceAsync(string name, decimal price)
    {
        await using var dbContext = CreateDbContext();

        var service = new Service
        {
            Name = name,
            Price = price
        };

        dbContext.Services.Add(service);

        await dbContext.SaveChangesAsync();

        return service.Id;
    }

    private async Task<Guid> CreateRoomAsync(string name, int capacity, decimal hourlyRate, Guid? serviceId = null)
    {
        await using var dbContext = CreateDbContext();

        var room = new Room { Name = name, Capacity = capacity, HourlyRate = hourlyRate };

        if (serviceId.HasValue)
        {
            var service = await dbContext.Services.SingleAsync(x => x.Id == serviceId.Value);

            room.Services.Add(service);
        }

        dbContext.Rooms.Add(room);

        await dbContext.SaveChangesAsync();

        return room.Id;
    }

    private async Task CreateBookingAsync(Guid roomId, DateTime startTime, DateTime endTime)
    {
        await using var dbContext = CreateDbContext();

        var booking = new Booking
        {
            RoomId = roomId,
            StartTime = startTime,
            EndTime = endTime,
            TotalPrice = 100
        };

        dbContext.Bookings.Add(booking);

        await dbContext.SaveChangesAsync();
    }
}