using ConferenceRoomBooking.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Database
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(ConferenceRoomBookingDbContext dbContext)
        {
            if (await dbContext.Rooms.AnyAsync())
                return;

            var projector = new Service { Name = "Projector", Price = 500m };

            var wifi = new Service { Name = "Wi-Fi", Price = 300m };
 
            var sound = new Service { Name = "Sound", Price = 700m };

            var roomA = new Room { 
                Name = "Room A", 
                Capacity = 50, 
                HourlyRate = 2000m, 
                Services = { projector, wifi, sound } 
            };
            
            var roomB = new Room { 
                Name = "Room B", 
                Capacity = 100, 
                HourlyRate = 3500m,
                Services = { projector, wifi } 
            };
            
            var roomC = new Room { 
                Name = "Room C", 
                Capacity = 30, 
                HourlyRate = 1500m,
                Services = { wifi, sound } 
            };

            var tomorrow = DateTime.UtcNow.Date.AddDays(1);

            var bookingA = new Booking { 
                RoomId = roomA.Id, 
                StartTime = tomorrow.AddHours(10),
                EndTime = tomorrow.AddHours(12), 
                TotalPrice = 4000m,
                BookingServices =
                {
                    new BookingService
                    {
                        Service = projector,
                        Price = projector.Price
                    },
                    new BookingService
                    {
                        Service = wifi,
                        Price = wifi.Price
                    }
                }
            };

            var bookingB = new Booking { 
                RoomId = roomB.Id, 
                StartTime = tomorrow.AddHours(14),
                EndTime = tomorrow.AddHours(17), 
                TotalPrice = 10500m,
                BookingServices =
                {
                    new BookingService
                    {
                        Service = projector,
                        Price = projector.Price
                    },
                    new BookingService
                    {
                        Service = wifi,
                        Price = wifi.Price
                    }
                }
            };

            var bookingC = new Booking { 
                RoomId = roomC.Id, 
                StartTime = tomorrow.AddHours(9),
                EndTime = tomorrow.AddHours(11), 
                TotalPrice = 3000m ,
                BookingServices =
                {
                    new BookingService
                    {
                        Service = wifi,
                        Price = wifi.Price
                    }
                }
            };

            dbContext.Services.AddRange(projector, wifi, sound);

            dbContext.Rooms.AddRange(roomA, roomB, roomC);
            
            dbContext.Bookings.AddRange(bookingA, bookingB, bookingC);

            await dbContext.SaveChangesAsync();
        }
    }
}