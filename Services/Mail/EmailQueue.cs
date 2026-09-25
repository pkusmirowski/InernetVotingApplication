using InternetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace InternetVotingApplication.Services.Mail
{
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

        public void MarkSent(WiadomoscEmail row)
        {
            ArgumentNullException.ThrowIfNull(row);
            row.Wyslano = Now();
            row.Proby++;
            row.OstatniBlad = null;
        }

        public void MarkFailed(WiadomoscEmail row, string error)
        {
            ArgumentNullException.ThrowIfNull(row);
            row.Proby++;
            row.OstatniBlad = error.Length > 1000 ? error[..1000] : error;
            row.NastepnaProba = Now().AddSeconds(Math.Pow(2, row.Proby) * 15);
        }

        public Task<int> SaveAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);

        private DateTime Now() => timeProvider.GetLocalNow().DateTime;
    }
}
