using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Configure Gateway Port 5000
builder.WebHost.UseUrls("http://localhost:5000");

// 1. Centralized JWT Authentication at the Gateway
var jwtKey = builder.Configuration["Jwt:Key"] ?? "HealthcareSuperSecretKeyForJwtAuthentication2026!Raj";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "HealthcareAuthService";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "HealthcareApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"[API Gateway JWT] Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var user = context.Principal?.Identity?.Name ?? "User";
            Console.WriteLine($"[API Gateway JWT] Valid token presented for: {user}");
            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"statusCode\": 401, \"message\": \"API Gateway: Access Denied. A valid Bearer token is required to access this healthcare resource.\"}");
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"statusCode\": 403, \"message\": \"API Gateway: Forbidden. You do not have sufficient permissions to access this healthcare resource.\"}");
        }
    };
});

// 2. Authorization Policies for YARP Routes
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireJwtToken", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

// 3. CORS configuration for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 4. YARP Reverse Proxy configuration
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// 5. Gateway Claim Forwarding Middleware
// When a request is authenticated at Gateway, forward identity headers downstream
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? context.User.FindFirst("userId")?.Value
                     ?? context.User.FindFirst("nameid")?.Value;
        var role = context.User.FindFirst(ClaimTypes.Role)?.Value
                   ?? context.User.FindFirst("role")?.Value;
        var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                    ?? context.User.FindFirst("email")?.Value;
        var name = context.User.FindFirst(ClaimTypes.Name)?.Value;

        if (!string.IsNullOrEmpty(userId))
            context.Request.Headers["X-User-Id"] = userId;
        if (!string.IsNullOrEmpty(role))
            context.Request.Headers["X-User-Role"] = role;
        if (!string.IsNullOrEmpty(email))
            context.Request.Headers["X-User-Email"] = email;
        if (!string.IsNullOrEmpty(name))
            context.Request.Headers["X-User-Name"] = name;

        Console.WriteLine($"[API Gateway] {context.Request.Method} {context.Request.Path} -> Authenticated User: {email} (Role: {role})");
    }
    else
    {
        Console.WriteLine($"[API Gateway] {context.Request.Method} {context.Request.Path} -> Public/Anonymous");
    }

    await next();
});

// Map YARP reverse proxy endpoints
app.MapReverseProxy();

// Health check endpoint for Gateway
app.MapGet("/", () => Results.Ok(new
{
    Service = "Healthcare API Gateway",
    Status = "Healthy",
    CentralizedJwtAuth = "Active",
    Timestamp = DateTime.UtcNow
}));

app.Run();
