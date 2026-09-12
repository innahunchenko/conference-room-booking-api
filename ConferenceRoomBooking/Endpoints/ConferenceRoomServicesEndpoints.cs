using ConferenceRoomBooking.Services.Interfaces;

namespace ConferenceRoomBooking.Endpoints
{
    public static class ConferenceRoomServicesEndpoints
    {
        public static void MapServiceEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/services").WithTags("Services");

            // GET /api/services
            group.MapGet("/", async (IConferenceRoomServicesService serviceManager, CancellationToken cancellationToken) =>
            {
                var services = await serviceManager.GetAllAsync(cancellationToken);

                return Results.Ok(services);
            })
                .WithName("GetAllServices").WithSummary("Get all available services");
        }
    }
}