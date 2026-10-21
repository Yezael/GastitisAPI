using Gastitis.API.ExceptionHandling;
using Gastitis.Application.Interfaces.Services;
using Gastitis.Infrastructure.Persistence;
using Gastitis.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Gastitis.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContextFactory<GastitisDBContext>(options =>
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
builder.Services.AddScoped<IUserIdentityService, UserIdentityService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddHttpContextAccessor();

//Authentication
var authority = builder.Configuration["Authentication:Authority"];
var audience = builder.Configuration["Authentication:Audience"];

if (string.IsNullOrWhiteSpace(authority))
{
	throw new InvalidOperationException("Authentication:Authority is required.");
}

if(string.IsNullOrWhiteSpace(audience))
{
	throw new InvalidOperationException("Authentication:Audience is required.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
	{
		options.Authority = authority;
		options.Audience = audience;
		options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
		options.MapInboundClaims = false;
		options.IncludeErrorDetails = false;
		options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
	});

builder.Services.AddAuthorization(options =>
{
	var apiAccessPolicy = new AuthorizationPolicyBuilder()
	.RequireAuthenticatedUser()
	.RequireAssertion(context => 
	{
		var claims = context.User.FindAll("sub").ToList();
		if (claims.Count != 1) return false;
		var claim = claims[0];
		return !string.IsNullOrWhiteSpace(claim.Value);
	})
	.RequireAssertion(context =>
	{
		var claims = context.User.FindAll("iss").ToList();
		if (claims.Count != 1) return false;
		var claim = claims[0];
		return !string.IsNullOrWhiteSpace(claim.Value);
	})
	.RequireAssertion(context =>
	{
		return context.User.FindAll("scope")
		.SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
		.Contains("gastitis.access", StringComparer.Ordinal);
	})
	.Build();

	options.DefaultPolicy = apiAccessPolicy;
	options.FallbackPolicy = apiAccessPolicy;
});


var app = builder.Build();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();


public partial class Program
{

}

