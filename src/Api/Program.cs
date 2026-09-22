using Microsoft.EntityFrameworkCore;
using SupportDesk.Infrastructure.Persistence;
using SupportDesk.Application.Auth;
using SupportDesk.Infrastructure.Auth;
using SupportDesk.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ITenantContext, TenantContext>();

builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "SupportDesk API is running");

app.MapAuthEndpoints();

app.Run();