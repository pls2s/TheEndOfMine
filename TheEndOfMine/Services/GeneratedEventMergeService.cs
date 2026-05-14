using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

public static class GeneratedEventMergeService
{
    public static int AppendEvents(GameState state, IEnumerable<GameEvent> events)
    {
        var remaining = Math.Max(0, state.EventsPerChapter - state.GeneratedEvents.Count);
        if (remaining == 0)
            return 0;

        var existingIds = state.GeneratedEvents
            .Select(gameEvent => gameEvent.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var gameEvent in events)
        {
            if (added >= remaining)
                break;

            var nextNumber = state.GeneratedEvents.Count + 1;
            if (string.IsNullOrWhiteSpace(gameEvent.Id) || existingIds.Contains(gameEvent.Id))
                gameEvent.Id = $"evt_{nextNumber:D2}";

            state.GeneratedEvents.Add(gameEvent);
            existingIds.Add(gameEvent.Id);
            added++;
        }

        return added;
    }
}
