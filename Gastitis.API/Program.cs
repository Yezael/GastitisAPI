using Gastitis.API.ExceptionHandling;
using Gastitis.Application.Interfaces;
using Gastitis.Infrastructure.Persistence;
using Gastitis.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<GastitisDBContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("GastitisDatabase")).
        LogTo(Console.WriteLine)
       .EnableSensitiveDataLogging();
}
);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();


var app = builder.Build();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();


