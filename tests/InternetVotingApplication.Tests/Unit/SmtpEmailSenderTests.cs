using System.Net;
using System.Net.Sockets;
using System.Text;
using InternetVotingApplication.Configuration;
using InternetVotingApplication.Services.Mail;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InternetVotingApplication.Tests.Unit;

/// <summary>The MailKit sender against a minimal SMTP server running inside the test.</summary>
public sealed class SmtpEmailSenderTests
{
    [Fact]
    public async Task Sends_the_message_over_smtp_and_authenticates_when_configured()
    {
        using var server = new FakeSmtpServer();
        var sender = new SmtpEmailSender(Options.Create(new SmtpOptions
        {
            Enabled = true,
            Host = "127.0.0.1",
            Port = server.Port,
            SecureSocket = "None",
            UserName = "user",
            Password = "secret",
            FromAddress = "no-reply@example.com",
            FromName = "Głosowanie",
        }), NullLogger<SmtpEmailSender>.Instance);

        await sender.SendAsync(new EmailMessage("jan@example.com", "Temat", "<p>Treść</p>"));
        var session = await server.Session;

        Assert.Contains("AUTH PLAIN", session, StringComparison.Ordinal);
        Assert.Contains("MAIL FROM:<no-reply@example.com>", session, StringComparison.Ordinal);
        Assert.Contains("RCPT TO:<jan@example.com>", session, StringComparison.Ordinal);
        Assert.Contains("Subject: Temat", session, StringComparison.Ordinal);
        Assert.Contains("QUIT", session, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Does_nothing_when_smtp_is_disabled()
    {
        var sender = new SmtpEmailSender(Options.Create(new SmtpOptions { Enabled = false, Host = "unreachable.invalid" }), NullLogger<SmtpEmailSender>.Instance);

        await sender.SendAsync(new EmailMessage("jan@example.com", "Temat", "<p>Treść</p>"));
    }

    /// <summary>Accepts one SMTP session (EHLO, AUTH, MAIL, RCPT, DATA, QUIT) and records what the client sent.</summary>
    private sealed class FakeSmtpServer : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);

        public FakeSmtpServer()
        {
            _listener.Start();
            Session = RunAsync();
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public Task<string> Session { get; }

        private async Task<string> RunAsync()
        {
            using var client = await _listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII);
            using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
            var log = new StringBuilder();

            await writer.WriteLineAsync("220 localhost ESMTP test");
            while (await reader.ReadLineAsync() is { } line)
            {
                log.AppendLine(line);
                var command = line.ToUpperInvariant();
                if (command.StartsWith("EHLO", StringComparison.Ordinal))
                {
                    await writer.WriteLineAsync("250-localhost");
                    await writer.WriteLineAsync("250 AUTH PLAIN");
                }
                else if (command.StartsWith("AUTH", StringComparison.Ordinal))
                {
                    await writer.WriteLineAsync("235 2.7.0 Authentication successful");
                }
                else if (command.StartsWith("DATA", StringComparison.Ordinal))
                {
                    await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");
                    while (await reader.ReadLineAsync() is { } data && data != ".")
                    {
                        log.AppendLine(data);
                    }

                    await writer.WriteLineAsync("250 2.0.0 OK");
                }
                else if (command.StartsWith("QUIT", StringComparison.Ordinal))
                {
                    await writer.WriteLineAsync("221 2.0.0 Bye");
                    break;
                }
                else
                {
                    await writer.WriteLineAsync("250 2.0.0 OK");
                }
            }

            return log.ToString();
        }

        public void Dispose()
        {
            _listener.Stop();
        }
    }
}
