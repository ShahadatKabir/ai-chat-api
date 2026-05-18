using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

// JWT
var jwtKey = configuration["Jwt:Key"] ?? "THIS_IS_SECRET_KEY_123";
var tokenKey = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(tokenKey)
    };
});

// AI Services
var aiProvider = configuration["AiProvider"] ?? "Gemini";
if (aiProvider.Equals("Gemma", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IAiService, GemmaService>();
}
else
{
    builder.Services.AddSingleton<IAiService, GeminiService>();
}

builder.Services.AddSingleton<IDataPersistenceService, DataPersistenceService>();
builder.Services.AddSingleton<IUserService, PersistenceUserService>();
builder.Services.AddSingleton<IChatHistoryService, PersistenceChatHistoryService>();
builder.Services.AddSingleton<IFavoritesService, FavoritesService>();
builder.Services.AddSingleton<IChatFeedbackService, ChatFeedbackService>();
builder.Services.AddSingleton<IPromptTemplateService, PromptTemplateService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}' to authorize requests."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IUserRateLimitService, UserRateLimitService>();
builder.Services.AddHostedService<CleanupBackgroundService>();

var app = builder.Build();

app.UseCors("AllowAll");

// Add logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

// Add rate limiting middleware
app.UseMiddleware<RateLimitingMiddleware>();

// Disable HTTPS for simplicity
// app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
