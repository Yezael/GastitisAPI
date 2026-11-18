using Gastitis.API.ExceptionHandling;
using Gastitis.Application.Interfaces.Services;
using Gastitis.Infrastructure.Persistence;
using Gastitis.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Gastitis.API.Services;
using FluentValidation;
using System.Text.Json;
using Gastitis.API.Validation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("GastitisDatabase");
if(string.IsNullOrWhiteSpace(connectionString))
{
	throw new InvalidOperationException("Connection string 'GastitisDatabase' not found.");
}

builder.Services.AddDbContextFactory<GastitisDBContext>(options =>
{
	options.UseNpgsql(connectionString);
	options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
}
);

builder.Services.AddControllers(options =>
{
	options.Filters.Add<FluentValidationActionFilter>();
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

ValidatorOptions.Global.PropertyNameResolver = (_, member, _) 
	=> member is null ? null : JsonNamingPolicy.CamelCase.ConvertName(member.Name);

builder.Services.AddProblemDetails(options =>
	options.CustomizeProblemDetails = ctx =>
		ctx.ProblemDetails.Instance ??= $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}");
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IUserIdentityService, UserIdentityService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);

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

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();


public partial class Program
{

}

