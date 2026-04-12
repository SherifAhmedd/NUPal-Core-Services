using NUPAL.Core.Application;
using NUPAL.Core.Infrastructure;
using NUPAL.Core.Api.BackgroundServices;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddHealthChecks();
builder.Services.AddHostedService<AIServiceKeepAliveWorker>();
builder.Services.AddHttpClient(); 

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key configuration");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Missing Jwt:Issuer configuration");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Missing Jwt:Audience configuration");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// Configure CORS - read allowed origins from configuration
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
    ?? throw new InvalidOperationException("AllowedOrigins configuration is required");

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
try
{
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "Server crash during startup or shutdown sequence.");
    throw;
}
