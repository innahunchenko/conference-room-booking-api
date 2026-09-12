using ConferenceRoomBooking.Contracts.Rooms;
using ConferenceRoomBooking.Database;
using ConferenceRoomBooking.Database.Entities;
using ConferenceRoomBooking.Mapping;
using ConferenceRoomBooking.Mappings;
using ConferenceRoomBooking.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Services
{
    public class RoomService : IRoomService
    {
        private readonly ConferenceRoomBookingDbContext _dbContext;
        private readonly ILogger<RoomService> _logger;

        public RoomService(ConferenceRoomBookingDbContext dbContext, ILogger<RoomService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<RoomResponse> CreateAsync(CreateRoomRequest request, CancellationToken cancellationToken)
        {
            var services = await GetServicesAsync(request.ServiceIds, cancellationToken);

            var room = request.ToEntity();

            room.Services = services;

            _dbContext.Rooms.Add(room);

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Room {RoomId} created", room.Id);

            return room.ToResponse();
        }

        public async Task UpdateAsync(Guid id, UpdateRoomRequest request, CancellationToken cancellationToken)
        {
            var room = await GetRoomAsync(id, cancellationToken);

            var services = await GetServicesAsync(request.ServiceIds, cancellationToken);

            request.UpdateEntity(room);

            room.Services = services;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Room {RoomId} updated", id);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var room = await GetRoomAsync(id, cancellationToken);

            room.IsDeleted = true;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Room {RoomId} deleted", id);
        }

        public async Task<List<RoomResponse>> GetAvailableAsync(DateTime startTime, DateTime endTime, int capacity, CancellationToken cancellationToken)
        {
            ValidateAvailabilityRequest(startTime, endTime, capacity);

            var rooms = await _dbContext.Rooms
                .AsNoTracking()
                .Where(x => x.Capacity >= capacity)
                .Where(x => !x.Bookings.Any(booking => booking.StartTime < endTime && booking.EndTime > startTime))
                .Include(x => x.Services)
                .ToListAsync(cancellationToken);

            return rooms.Select(x => x.ToResponse()).ToList();
        }

        public async Task<List<RoomResponse>> GetAllAsync(CancellationToken cancellationToken)
        {
            var rooms = await _dbContext.Rooms.AsNoTracking().Include(x => x.Services).ToListAsync(cancellationToken);

            return rooms.Select(x => x.ToResponse()).ToList();
        }

        private async Task<Room> GetRoomAsync(Guid roomId, CancellationToken cancellationToken)
        {
            var room = await _dbContext.Rooms.Include(x => x.Services).FirstOrDefaultAsync(x => x.Id == roomId, cancellationToken);

            if (room is null)
                throw new KeyNotFoundException("Room not found.");

            return room;
        }

        private async Task<List<Service>> GetServicesAsync(IEnumerable<Guid> requestedServiceIds, CancellationToken cancellationToken)
        {
            var serviceIds = requestedServiceIds.Distinct().ToList();

            if (serviceIds.Count == 0)
                return new List<Service>();

            var services = await _dbContext.Services.Where(x => serviceIds.Contains(x.Id)).ToListAsync(cancellationToken);

            if (services.Count != serviceIds.Count)
                throw new ArgumentException("One or more services do not exist.");

            return services;
        }

        private static void ValidateAvailabilityRequest(DateTime startTime, DateTime endTime, int capacity)
        {
            if (endTime <= startTime)
                throw new ArgumentException("End time must be greater than start time.");

            if (capacity <= 0)
                throw new ArgumentException("Capacity must be greater than zero.");
        }
    }
}