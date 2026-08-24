using MailKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using MimeKit;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;

namespace Battery_Service
{
    public class Worker : BackgroundService
    {
        // Define the SYSTEM_POWER_STATUS structure with fields as per the Windows API documentation
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte Reserved1;
            public int BatteryLifeTime;
            public int BatteryFullLifeTime;
        }

        // Import the GetSystemPowerStatus API from kernel32.dll
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS sps);



        private string ServiceFolder;
        private string SourseEmail;
        private string DistinationEmail;

        private readonly IConfiguration _configuration;
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            ServiceFolder = _configuration.GetValue<string>("ServiceSettings:LogFolder");
            SourseEmail = _configuration.GetValue<string>("ServiceSettings:SourseEmail");
            DistinationEmail = _configuration.GetValue<string>("ServiceSettings:DistinationEmail");
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var email = new MimeMessage();

                email.From.Add(
                    new MailboxAddress(
                        "Battery Service",
                        SourseEmail));

                email.To.Add(
                    new MailboxAddress(
                        "",
                        to));

                email.Subject = subject;


                email.Body = new TextPart("plain")
                {
                    Text = body
                };


                using var smtp = new MailKit.Net.Smtp.SmtpClient();


                await smtp.ConnectAsync(
                    "smtp.gmail.com",
                    587,
                    MailKit.Security.SecureSocketOptions.StartTls);


                await smtp.AuthenticateAsync(
                    SourseEmail,
                    _configuration.GetValue<string>("ServiceSettings:EmailPassword"));


                await smtp.SendAsync(email);


                await smtp.DisconnectAsync(true);


                _logger.LogInformation(
                    "Email sent successfully to {email}",
                    to);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send email");
            }
        }

        public void EnableDarkMode()
        {
            const string keyPath =
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

            using RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath,true);

            if (key != null)
            {
                key.SetValue(
                    "AppsUseLightTheme",
                    0,
                    RegistryValueKind.DWord);

                key.SetValue(
                    "SystemUsesLightTheme",
                    0,
                    RegistryValueKind.DWord);
            }
        }

        public override async Task StartAsync(CancellationToken CancellationToken)
        {
            _logger.LogInformation("Service Started : {time}", DateTimeOffset.Now);
            await base.StartAsync(CancellationToken);
        }

        public override async Task StopAsync(CancellationToken CancellationToken)
        {
            _logger.LogInformation("Service Stoped : {time}", DateTimeOffset.Now);
            await base.StopAsync(CancellationToken);
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool emailSent = true;
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    if (GetSystemPowerStatus(out SYSTEM_POWER_STATUS status))
                    {
                        if ((status.ACLineStatus == 0 ))
                        {

                            if (status.BatteryLifePercent <= 60 && emailSent)
                            {
                                _logger.LogInformation("The battery is now less than 60 at: {time}", DateTimeOffset.Now);

                                await SendEmailAsync(DistinationEmail,"Battery Warning", "Battery level is below 60%.\n\n" +
                                    "Night mode has been activated.");

                                EnableDarkMode();
                                emailSent =false;
                            }

                        }
                        else
                        {
                            emailSent = true;
                        }
                        
                    }
                    else
                    {
                        Console.WriteLine("Unable to get battery status.");
                    }

                }
                await Task.Delay(10000, stoppingToken);
            }
        }







    }
}
