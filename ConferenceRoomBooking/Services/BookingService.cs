using ConferenceRoomBooking.Contracts.Bookings;
using ConferenceRoomBooking.Database;
using ConferenceRoomBooking.Database.Entities;
using ConferenceRoomBooking.Mappings;
using ConferenceRoomBooking.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ConferenceRoomBooking.Services
{
    public class BookingService : IBookingService
    {
        // Tariff multipliers based on the booking start time
        private static readonly (TimeSpan Start, TimeSpan End, decimal Multiplier)[] Tariffs =
        {
            (new TimeSpan(6, 0, 0), new TimeSpan(9, 0, 0), 0.90m),
            (new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0), 1.00m),
            (new TimeSpan(12, 0, 0), new TimeSpan(14, 0, 0), 1.15m),
            (new TimeSpan(14, 0, 0), new TimeSpan(18, 0, 0), 1.00m),
            (new TimeSpan(18, 0, 0), new TimeSpan(23, 0, 0), 0.80m)
        };

        private readonly ConferenceRoomBookingDbContext _dbContext;
        private readonly ILogger<BookingService> _logger;

        public BookingService(ConferenceRoomBookingDbContext dbContext, ILogger<BookingService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<BookingResponse> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken)
        {
            ValidateBookingPeriod(request);

            _logger.LogInformation(
                "Creating booking for room {RoomId} from {StartTime} to {EndTime}",
                request.RoomId, request.StartTime, request.EndTime);

            // Use an explicit transaction because the availability check and booking creation
            // must be performed atomically.
            // Lock the Room row so concurrent bookings for the same room are processed sequentially.
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            // Concurrent requests for the same room must wait for this lock to be released.
            var room = await GetRoomAsync(request.RoomId, cancellationToken);

            // Check availability while the Room lock is still held.
            await EnsureRoomIsAvailableAsync(request.RoomId, request.StartTime, request.EndTime, cancellationToken);

            var selectedServices = GetSelectedServices(room, request.ServiceIds);

            var price = selectedServices.Sum(x => x.Price);

            var totalPrice = CalculateTotalPrice(request.StartTime, request.EndTime, room.HourlyRate, price);

            var booking = request.ToEntity(totalPrice);

            booking.BookingServices = selectedServices.ToBookingServices();

            _dbContext.Bookings.Add(booking);

            // Persist the booking and related BookingService records.
            // Since an explicit transaction is active, the transaction is not committed here.
            await _dbContext.SaveChangesAsync(cancellationToken);

            // SaveChangesAsync does not commit an explicitly started transaction.
            // Commit makes all changes permanent and releases the Room lock.
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Booking {BookingId} created for room {RoomId}",
                booking.Id, request.RoomId);

            return booking.ToResponse();
        }

        public async Task<List<BookingResponse>> GetAllAsync(CancellationToken cancellationToken)
        {
            var bookings = await _dbContext.Bookings
                .AsNoTracking()
                .Include(x => x.BookingServices)
                .ThenInclude(x => x.Service)
                .ToListAsync(cancellationToken);

            return bookings.Select(x => x.ToResponse()).ToList();
        }

        private static void ValidateBookingPeriod(CreateBookingRequest request)
        {
            // A booking must have a positive duration.
            if (request.EndTime <= request.StartTime)
                throw new ArgumentException("End time must be greater than start time.");

            // A booking cannot span multiple calendar days because the tariff calculation
            // is based on tariff periods within the booking day.
            if (request.StartTime.Date != request.EndTime.Date)
                throw new ArgumentException("Booking must start and end on the same day.");
        }

        private async Task<Room> GetRoomAsync(Guid roomId, CancellationToken cancellationToken)
        {
            // UPDLOCK prevents another booking transaction from locking the same Room for update.
            // The lock is held until the current transaction is committed or rolled back.
            var room = await _dbContext.Rooms.FromSqlInterpolated($"""
                    SELECT *
                    FROM Rooms WITH (UPDLOCK)
                    WHERE Id = {roomId}
                    """)
                .Include(x => x.Services)
                .FirstOrDefaultAsync(cancellationToken);

            if (room is null)
                throw new KeyNotFoundException("Room not found.");

            return room;
        }

        private async Task EnsureRoomIsAvailableAsync(Guid roomId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken)
        {
            // A booking conflicts if it starts before the new booking ends and ends after the new booking starts.
            var hasConflict = await _dbContext.Bookings
                .AnyAsync(x => x.RoomId == roomId && x.StartTime < endTime && x.EndTime > startTime, cancellationToken);

            if (hasConflict)
            {
                _logger.LogWarning(
                    "Booking conflict detected for room {RoomId} from {StartTime} to {EndTime}",
                    roomId, startTime, endTime);

                throw new InvalidOperationException("The room is already booked for the selected time.");
            }
        }

        private static List<Service> GetSelectedServices(Room room, IEnumerable<Guid> requestedServiceIds)
        {
            var serviceIds = requestedServiceIds.Distinct().ToList();

            var selectedServices = room.Services.Where(x => serviceIds.Contains(x.Id)).ToList();

            var unavailableServiceIds = serviceIds.Except(selectedServices.Select(x => x.Id)).ToList();

            if (unavailableServiceIds.Count > 0)
                throw new ArgumentException("The following services are not available in this room: " +
                    $"{string.Join(", ", unavailableServiceIds)}.");

            return selectedServices;
        }

        private static decimal CalculateTotalPrice(DateTime startTime, DateTime endTime, decimal rate, decimal price)
        {
            // Start with the fixed price of the selected services.
            var totalPrice = price;

            foreach (var tariff in Tariffs)
            {
                // Convert the tariff time range to the booking date.
                var tariffStart = startTime.Date + tariff.Start;
                var tariffEnd = startTime.Date + tariff.End;

                // Find the part of the booking that overlaps with the current tariff.
                var overlapStart = startTime > tariffStart ? startTime : tariffStart;

                var overlapEnd = endTime < tariffEnd ? endTime : tariffEnd;

                // Skip tariffs that do not overlap with the booking.
                if (overlapStart >= overlapEnd)
                    continue;

                // Calculate only the hours within the current tariff.
                var hours = (decimal)(overlapEnd - overlapStart).TotalHours;

                // Apply the room rate and tariff multiplier.
                totalPrice += rate * hours * tariff.Multiplier;
            }

            return totalPrice;
        }
    }
}