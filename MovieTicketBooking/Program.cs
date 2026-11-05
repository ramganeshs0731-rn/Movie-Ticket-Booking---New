using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MovieTicketBooking.Authorization;
using MovieTicketBooking.Data;
using MovieTicketBooking.Middlewares; // <-- Namespace for your diagnostic middlewares
using MovieTicketBooking.Repositories;
using MovieTicketBooking.Repositories.Interfaces;
using MovieTicketBooking.Services;
using MovieTicketBooking.Services.Interfaces;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register OpenAPI services required for Scalar
builder.Services.AddOpenApi();

// Configure CORS for Angular Frontend (Port 4200)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

// Configure Entity Framework Core with SQL Server using the DefaultConnection string
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ==========================================
// DEPENDENCY INJECTION REGISTRATIONS
// ==========================================
// 1. User & Auth Module
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

// 2. Movies Module
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IMovieService, MovieService>();

// 3. Theatres Module
builder.Services.AddScoped<ITheatreRepository, TheatreRepository>();
builder.Services.AddScoped<ITheatreService, TheatreService>();

// 4. Screens Module
builder.Services.AddScoped<IScreenRepository, ScreenRepository>();
builder.Services.AddScoped<IScreenService, ScreenService>();

// 5. Seats Module
builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<ISeatService, SeatService>();

// 6. Show Module
builder.Services.AddScoped<IShowRepository, ShowRepository>();
builder.Services.AddScoped<IShowService, ShowService>();

// 7. ShowSeat Module
builder.Services.AddScoped<IShowSeatRepository, ShowSeatRepository>();

// 8. Booking Module
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();

// ==========================================
// AUTHORIZATION HANDLER & ACCESSOR
// ==========================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthorizationHandler, SameUserBookingHandler>();

// ==========================================
// JWT AUTHENTICATION & AUTHORIZATION CONFIGURATION
// ==========================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
        )
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SameUserBookingPolicy", policy =>
        policy.Requirements.Add(new SameUserBookingRequirement()));
});

var app = builder.Build();

// ==========================================
// HTTP REQUEST PIPELINE
// ==========================================

//app.UseHttpsRedirection();

// CORS MUST be placed before Authentication and Authorization
app.UseCors("AllowAngular");

// Endpoint Metadata Diagnostics (Development only)
if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<EndpointMetadataMiddleware>();
}

// Authentication middleware populates HttpContext.User from JWT
app.UseAuthentication();

// Authentication and Authorization Diagnostics (Development only)
if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<AuthenticationDiagnosticsMiddleware>();
    app.UseMiddleware<AuthorizationDiagnosticsMiddleware>();
}

// Authorization checks permissions and custom policies (like SameUserBookingPolicy)
app.UseAuthorization();

app.MapControllers();

// Map the OpenAPI JSON endpoint so Scalar can read it
app.MapOpenApi();

// Configure Scalar API reference endpoint at /scalar
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Movie Ticket Booking API")
        .WithTheme(ScalarTheme.Mars);
});

app.Run();
