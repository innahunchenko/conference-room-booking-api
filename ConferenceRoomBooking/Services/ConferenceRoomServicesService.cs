using ConferenceRoomBooking.Contracts.Services;
using ConferenceRoomBooking.Database;
using ConferenceRoomBooking.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Services
{
    public class ConferenceRoomServicesService : IConferenceRoomServicesService
    {
        private readonly ConferenceRoomBookingDbContext dbContext;

        public ConferenceRoomServicesService(ConferenceRoomBookingDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<List<ServiceResponse>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await dbContext.Services.AsNoTracking()
                .Select(x => new ServiceResponse(x.Id, x.Name, x.Price)).ToListAsync(cancellationToken);
        }
    }
}