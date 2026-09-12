using ConferenceRoomBooking.Contracts.Bookings;
using ConferenceRoomBooking.Contracts.Rooms;
using ConferenceRoomBooking.Services.Interfaces;

namespace ConferenceRoomBooking.Endpoints;

public static class RoomEndpoints
{
    public static void MapRoomEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/rooms").WithTags("Rooms");

        // GET /api/rooms
        group.MapGet("/", async (IRoomService roomService, CancellationToken cancellationToken) =>
        {
            var rooms = await roomService.GetAllAsync(cancellationToken);

            return Results.Ok(rooms);
        })
            .WithName("GetAllRooms").WithSummary("Get all conference rooms");


        // POST /api/rooms
        group.MapPost("/", async (CreateRoomRequest request, IRoomService roomService, CancellationToken cancellationToken) =>
        {
            var room = await roomService.CreateAsync(request, cancellationToken);

            return Results.Created($"/api/rooms/{room.Id}", room);
        })
            .WithName("CreateRoom").WithSummary("Create a conference room");

        // PUT /api/rooms/{id}
        group.MapPut("/{id:guid}", async (Guid id, UpdateRoomRequest request, IRoomService roomService, CancellationToken cancellationToken) =>
        {
            await roomService.UpdateAsync(id, request, cancellationToken);

            return Results.Ok(new { message = "Room updated successfully." });
        })
            .WithName("UpdateRoom").WithSummary("Update a conference room");

        // DELETE /api/rooms/{id}
        group.MapDelete("/{id:guid}", async (Guid id, IRoomService roomService, CancellationToken cancellationToken) =>
        {
            await roomService.DeleteAsync(id, cancellationToken);

            return Results.Ok(new { message = "Room deleted successfully." });
        })
            .WithName("DeleteRoom").WithSummary("Delete a conference room");

        // GET /api/rooms/available
        group.MapGet("/available", async ([AsParameters] SearchRoomsRequest request, IRoomService roomService, CancellationToken cancellationToken) =>
        {
            var rooms = await roomService.GetAvailableAsync(request.StartTime, request.EndTime, request.Capacity, cancellationToken);

            return Results.Ok(rooms);
        })
            .WithName("GetAvailableRooms").WithSummary("Find available conference rooms");
    }
}