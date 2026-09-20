using Microsoft.EntityFrameworkCore;
using FileFlow.API.Data;
using FileFlow.API.Services;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "FileFlow.API", Version = "v1" });
});

builder.Services.AddDbContext<FileFlowDbContext>(options =>
    options.UseSqlite("Data Source=fileflow.db"));

builder.Services.AddControllers();
builder.Services.AddScoped<FolderPathResolver>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDev", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FileFlow.API v1");
    });
}

app.UseHttpsRedirection();

// Map incoming network requests to controller functions
app.UseCors("AllowReactDev");
app.MapControllers();

app.Run();
