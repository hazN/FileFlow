using Microsoft.EntityFrameworkCore;
using FileFlow.API.Data;
using FileFlow.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<FileFlowDbContext>(options =>
    options.UseSqlite("Data Source=fileflow.db"));

builder.Services.AddControllers();
builder.Services.AddScoped<FolderPathResolver>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map incoming network requests to controller functions
app.MapControllers();

app.Run();
