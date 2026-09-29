using InternetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace InternetVotingApplication.Services.Mail;

/// <summary>
/// Outbox operations over the <c>WiadomoscEmail</c> table. Rows are written in the caller's transaction and
/// delivered later by <see cref="EmailDispatcher"/>, so a message is never lost with a restart and never
/// sent for a business change that was rolled back.
/// </summary>
public sealed class EmailQueue(InternetVotingContext context, TimeProvider timeProvider)
{
    public void Enqueue(EmailMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        context.WiadomosciEmail.Add(new WiadomoscEmail
        {
            Odbiorca = message.To,
            Temat = message.Subject,
            Tresc = message.HtmlBody,
            Utworzono = Now(),
            NastepnaProba = Now(),
        });
    }

    public Task<List<WiadomoscEmail>> TakeDueAsync(int batchSize, int maxAttempts, CancellationToken cancellationToken)
    {
        var now = Now();
        return context.WiadomosciEmail
            .Where(m => m.Wyslano == null && m.Proby < maxAttempts && (m.NastepnaProba == null || m.NastepnaProba <= now))
            .OrderBy(m => m.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// A delivered message is removed rather than kept: the body of a vote receipt links a voter's e-mail address
    /// to a vote hash, and that link must not live in the database longer than delivery requires.
    /// </summary>
    public void MarkSent(WiadomoscEmail row)
    {
        ArgumentNullException.ThrowIfNull(row);
        context.WiadomosciEmail.Remove(row);
    }

    /// <summary>
    /// Records a failed attempt. After the last allowed attempt the message is removed, for the same reason as a
    /// delivered one; returns <c>true</c> when it was given up.
    /// </summary>
    public bool MarkFailed(WiadomoscEmail row, string error, int maxAttempts)
    {
        ArgumentNullException.ThrowIfNull(row);
        row.Proby++;
        if (row.Proby >= maxAttempts)
        {
            context.WiadomosciEmail.Remove(row);
            return true;
        }

        row.OstatniBlad = error.Length > 1000 ? error[..1000] : error;
        row.NastepnaProba = Now().AddSeconds(Math.Pow(2, row.Proby) * 15);
        return false;
    }

    public Task<int> SaveAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);

    private DateTime Now() => timeProvider.GetLocalNow().DateTime;
}
