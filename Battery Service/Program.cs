using Battery_Service;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();


string ServiceFolder = builder.Configuration.GetValue<string>("ServiceSettings:ServiceFolder");
string SourseEmail = builder.Configuration.GetValue<string>("ServiceSettings:SourseEmail");
string DistinationEmail = builder.Configuration.GetValue<string>("ServiceSettings:DistinationEmail");

Directory.CreateDirectory(ServiceFolder);

Log.Logger = new LoggerConfiguration()
    .WriteTo.File(
        Path.Combine(ServiceFolder, "Battrey States.log"),
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddHostedService<Worker>();
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Battrey Service";
});

builder.Services.AddSerilog();


var host = builder.Build();
host.Run();
