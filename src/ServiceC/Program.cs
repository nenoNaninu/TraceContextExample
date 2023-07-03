using System.Text.Json;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var formatterName = ConsoleFormatterNames.Simple;

if (formatterName == ConsoleFormatterNames.Simple)
{
    builder.Logging.AddSimpleConsole(options =>
    {
        options.IncludeScopes = true;
    });
}
else if (formatterName == ConsoleFormatterNames.Json)
{
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
        options.UseUtcTimestamp = true;
        options.JsonWriterOptions = new JsonWriterOptions
        {
            Indented = false
        };
    });
}

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestProperties | HttpLoggingFields.ResponseStatusCode;
});

builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpLogging();
app.UseExceptionHandler();

app.UseAuthorization();

app.MapControllers();

app.Run();
