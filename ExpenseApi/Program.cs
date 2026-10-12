using Microsoft.EntityFrameworkCore;
using ExpenseApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Configuration;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("JWT signing key is not configured.");

string? connString = builder.Configuration.GetConnectionString("ExpenseDatabase");

builder.Services.AddControllers();
builder.Services.AddOpenApi(
    options => 
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
}
);
builder.Services.AddDbContext<ExpenseContext>(opt => opt.UseNpgsql(connString));

//Token Validation Parameters 
var tokenValidationParameters = new TokenValidationParameters()
        {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey= new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),

        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
        };    

builder.Services.AddSingleton(tokenValidationParameters);

//Add identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ExpenseContext>()
                .AddDefaultTokenProviders();

// Add authentication
builder.Services.AddAuthentication( o =>
    // Enable default auth schemas
    {
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    // Configure Jwt bearer token use for authentication
    .AddJwtBearer( o =>
    {
    o.SaveToken = true;
    o.RequireHttpsMetadata = false;
    o.TokenValidationParameters = tokenValidationParameters;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUi(options =>
    {
        options.DocumentPath = "/openapi/v1.json";
    }
    );
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
AppDbInitializer.SeedRoles(app).Wait();

app.Run();