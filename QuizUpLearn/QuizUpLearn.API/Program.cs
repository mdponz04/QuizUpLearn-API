using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using QuizUpLearn.API.DI;
using QuizUpLearn.API.Hubs;
using System.Text;
using System.Text.Json.Serialization;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = builder.Configuration["Redis:InstanceName"];
});

builder.Services.AddSignalR()
    .AddStackExchangeRedis(
        builder.Configuration["Redis:ConnectionString"]!, 
        options =>
        {
            options.Configuration.ChannelPrefix = "QuizUpLearn_SignalR";
            options.Configuration.AbortOnConnectFail = false;
        }
    );

builder.Services.AddEndpointsApiExplorer();

var jwtSection = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.Events = new JwtBearerEvents
    {

        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && 
                (path.StartsWithSegments("/game-hub") || path.StartsWithSegments("/one-vs-one-hub")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true
    };
});

// Initialize Firebase Admin SDK
if (FirebaseApp.DefaultInstance == null)
{
    var fb = builder.Configuration.GetSection("Firebase");

    var json = $@"
    {{
      ""type"": ""service_account"",
      ""project_id"": ""{fb["ProjectId"]}"",
      ""client_email"": ""{fb["ClientEmail"]}"",
      ""private_key"": ""{fb["PrivateKey"]?.Replace("\\n", "\n")}""
    }}";

    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromJson(json)
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseMiddleware<QuizUpLearn.API.Middlewares.ExceptionHandlingMiddleware>();
app.UseMiddleware<QuizUpLearn.API.Middlewares.ApiResponseWrappingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<GameHub>("/game-hub").RequireCors("AllowFrontend");
app.MapHub<OneVsOneHub>("/one-vs-one-hub").RequireCors("AllowFrontend");
app.MapHub<BackgroundJobHub>("/background-jobs").RequireCors("AllowFrontend");

app.Run();
