using ConferenceRoomBooking.Contracts.Rooms;
using ConferenceRoomBooking.Contracts.Services;
using ConferenceRoomBooking.Database.Entities;

namespace ConferenceRoomBooking.Mapping
{
    public static class RoomMapping
    {
        public static Room ToEntity(this CreateRoomRequest request)
        {
            return new Room
            {
                Name = request.Name,
                Capacity = request.Capacity,
                HourlyRate = request.HourlyRate
            };
        }

        public static void UpdateEntity(this UpdateRoomRequest request, Room room)
        {
            room.Name = request.Name;
            room.Capacity = request.Capacity;
            room.HourlyRate = request.HourlyRate;
        }

        public static RoomResponse ToResponse(this Room room)
        {
            return new RoomResponse(
                room.Id,
                room.Name,
                room.Capacity,
                room.HourlyRate,
                room.Services.Select(service => 
                    new ServiceResponse(service.Id, service.Name, service.Price)).ToList());
        }
    }
}
