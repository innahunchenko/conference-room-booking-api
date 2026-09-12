using ConferenceRoomBooking.Database;
using ConferenceRoomBooking.Endpoints;
using ConferenceRoomBooking.Exceptions;
using ConferenceRoomBooking.Services;
using ConferenceRoomBooking.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ConferenceRoomBookingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConferenceRoomBookingDatabase")));

// Services
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IConferenceRoomServicesService, ConferenceRoomServicesService>();
builder.Services.AddScoped<IBookingService, BookingService>();

// Exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Exception handling
app.UseExceptionHandler();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply migrations and seed initial data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ConferenceRoomBookingDbContext>();
    await dbContext.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(dbContext);
}

app.UseHttpsRedirection();

// Endpoints
app.MapRoomEndpoints();
app.MapBookingEndpoints();
app.MapServiceEndpoints();

app.Run();