using ConferenceRoomBooking.Contracts.Rooms;
using ConferenceRoomBooking.Database;
using ConferenceRoomBooking.Database.Entities;
using ConferenceRoomBooking.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConferenceRoomBooking.Tests.Unit;

public class RoomServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenValidRequest_ShouldCreateRoom()
    {
        await using var dbContext = CreateDbContext();
        var roomService = CreateRoomService(dbContext);

        var request = new CreateRoomRequest("Meeting Room", 10, 100, []);

        var result = await roomService.CreateAsync(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Meeting Room", result.Name);
        Assert.Equal(10, result.Capacity);
        Assert.Equal(100, result.HourlyRate);

        Assert.Single(await dbContext.Rooms.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenServiceDoesNotExist_ShouldThrow()
    {
        await using var dbContext = CreateDbContext();
        var roomService = CreateRoomService(dbContext);

        var request = new CreateRoomRequest("Meeting Room", 10, 100, [Guid.NewGuid()]);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => roomService.CreateAsync(request, CancellationToken.None));

        Assert.Equal("One or more services do not exist.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_WhenValidRequest_ShouldUpdateRoom()
    {
        await using var dbContext = CreateDbContext();

        var room = new Room { Name = "Old name", Capacity = 5, HourlyRate = 50 };

        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();

        var roomService = CreateRoomService(dbContext);

        var request = new UpdateRoomRequest("New name", 20, 150, []);

        await roomService.UpdateAsync(room.Id, request, CancellationToken.None);

        var updatedRoom = await dbContext.Rooms.SingleAsync();

        Assert.Equal("New name", updatedRoom.Name);
        Assert.Equal(20, updatedRoom.Capacity);
        Assert.Equal(150, updatedRoom.HourlyRate);
    }

    [Fact]
    public async Task UpdateAsync_WhenRoomDoesNotExist_ShouldThrow()
    {
        await using var dbContext = CreateDbContext();
        var roomService = CreateRoomService(dbContext);

        var request = new UpdateRoomRequest("New name", 20, 150, []);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => roomService.UpdateAsync(Guid.NewGuid(), request, CancellationToken.None));

        Assert.Equal("Room not found.", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteRoom()
    {
        await using var dbContext = CreateDbContext();

        var room = new Room { Name = "Meeting Room", Capacity = 10, HourlyRate = 100 };

        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();

        var roomService = CreateRoomService(dbContext);

        await roomService.DeleteAsync(room.Id, CancellationToken.None);

        var deletedRoom = await dbContext.Rooms.IgnoreQueryFilters().SingleAsync();

        Assert.True(deletedRoom.IsDeleted);
    }

    [Fact]
    public async Task GetAvailableAsync_WhenBookingOverlaps_ShouldNotReturnRoom()
    {
        await using var dbContext = CreateDbContext();

        var room = new Room { Name = "Booked Room", Capacity = 10, HourlyRate = 100 };

        dbContext.Rooms.Add(room);

        dbContext.Bookings.Add(new Booking
        {
            RoomId = room.Id,
            StartTime = new DateTime(2026, 9, 6, 10, 0, 0),
            EndTime = new DateTime(2026, 9, 6, 12, 0, 0),
            TotalPrice = 200
        });

        await dbContext.SaveChangesAsync();

        var roomService = CreateRoomService(dbContext);

        var result = await roomService.GetAvailableAsync(
            new DateTime(2026, 9, 6, 11, 0, 0),
            new DateTime(2026, 9, 6, 13, 0, 0),
            5,
            CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableAsync_WhenRequestIsInvalid_ShouldThrow()
    {
        await using var dbContext = CreateDbContext();
        var roomService = CreateRoomService(dbContext);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => roomService.GetAvailableAsync(
                new DateTime(2026, 9, 6, 12, 0, 0),
                new DateTime(2026, 9, 6, 11, 0, 0),
                5,
                CancellationToken.None));

        Assert.Equal("End time must be greater than start time.", exception.Message);
    }

    private static RoomService CreateRoomService(ConferenceRoomBookingDbContext dbContext)
    {
        return new RoomService(dbContext, NullLogger<RoomService>.Instance);
    }

    private static ConferenceRoomBookingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ConferenceRoomBookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ConferenceRoomBookingDbContext(options);
    }
}