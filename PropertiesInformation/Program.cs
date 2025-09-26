using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PropertiesInformation.Api.Class;
using PropertiesInformation.Core.Entities;
using PropertiesInformation.Core.Interface;
using PropertiesInformation.Infrastructure.Repositories;
using System.Text;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add(new ProducesAttribute("application/json"));
    options.OutputFormatters.RemoveType<StringOutputFormatter>();
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kv => kv.Value?.Errors.Count > 0)
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        var resp = ApiResponse<object>.Fail(
            StatusCodes.Status400BadRequest,
            "Validation failed.",
            context.HttpContext,
            details: errors
        );
        return new BadRequestObjectResult(resp);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IOwnerRepository, OwnerRepository>();
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<IPropertyImageRepository, PropertyImageRepository>();
builder.Services.AddScoped<IPropertyTraceRepository, PropertyTraceRepository>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Missing Jwt:Issuer");
var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Missing Jwt:Audience");
var secretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("Missing Jwt:SecretKey");
var keyBytes = Encoding.UTF8.GetBytes(secretKey);
if (keyBytes.Length < 32) throw new InvalidOperationException("Jwt:SecretKey must be at least 32 characters.");
var key = new SymmetricSecurityKey(keyBytes);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    const string groupName = "v1";
    options.SwaggerDoc(groupName, new OpenApiInfo
    {
        Title = $"Real Estate {groupName}",
        Version = groupName,
        Description = "Real Estate API",
        Contact = new OpenApiContact { Name = "Real Estate", Email = "apeinado1996@gmail.com" }
    });

    options.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
    options.MapType<IFormFileCollection>(() => new OpenApiSchema
    {
        Type = "array",
        Items = new OpenApiSchema { Type = "string", Format = "binary" }
    });

    options.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
    options.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Format = "time" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});


var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        
        var (status, title) = ex switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad request"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };
        
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                          .CreateLogger("GlobalException");
        if (ex is not null) logger.LogError(ex, "Unhandled exception");

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = null,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.UseStatusCodePages(async ctx =>
{
    var resp = ctx.HttpContext.Response;
    if (!resp.HasStarted)
    {
        var problem = new ProblemDetails
        {
            Status = resp.StatusCode,
            Title = ReasonPhrases.GetReasonPhrase(resp.StatusCode),
            Instance = ctx.HttpContext.Request.Path
        };
        resp.ContentType = "application/json";
        await resp.WriteAsJsonAsync(problem);
    }
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Real Estate v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
