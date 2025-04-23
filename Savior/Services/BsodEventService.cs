using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using Savior.Models;

namespace Savior.Services
{
    public class BsodEventService
    {
        public List<BsodEvent> GetRecentBsodEvents(int maxEvents = 20)
        {
            var events = new List<BsodEvent>();
            string query = "*[System[(Level=1 or Level=2) and (EventID=41 or EventID=1001)]]";
            var logQuery = new EventLogQuery("System", PathType.LogName, query);

            try
            {
                using var reader = new EventLogReader(logQuery);
                EventRecord entry;
                int count = 0;

                while ((entry = reader.ReadEvent()) != null && count < maxEvents)
                {
                    events.Add(new BsodEvent
                    {
                        Date = entry.TimeCreated?.ToString("g") ?? "N/A",
                        Source = entry.ProviderName ?? "Inconnu",
                        EventId = entry.Id.ToString(),
                        ShortMessage = entry.FormatDescription()?.Substring(0, 80) + "..."
                    });

                    count++;
                }
            }
            catch { }

            return events;
        }
    }
}