using System;
using System.Collections.Generic;
using System.Linq;

namespace XCC.Models;

public class RaceSession(string roundName, int turnsMax, DateTime? startTime = null)
{
    public string RoundName { get; } = roundName;
    public int TurnsMax { get; } = turnsMax;
    public DateTime StartTime { get; } = startTime ?? DateTime.Now;
    public DateTime? FinishTime { get; private set; }
    public TimeSpan Elapsed => (FinishTime ?? DateTime.Now) - StartTime;

    private readonly List<PilotEntry> _entries = [];
    public IReadOnlyList<PilotEntry> Entries => _entries.AsReadOnly();

    public PilotEntry AddEntry(string pilotNumber, int turn)
    {
        var entry = new PilotEntry(pilotNumber, DateTime.Now, turn);
        _entries.Add(entry);
        return entry;
    }

    public PilotEntry AddEntry(string pilotNumber, int turn, DateTime timestamp)
    {
        var entry = new PilotEntry(pilotNumber, timestamp, turn);
        _entries.Add(entry);
        return entry;
    }

    public PilotEntry GetPreviousEntry(string pilotNumber)
        => _entries.LastOrDefault(e => e.PilotNumber == pilotNumber)
           ?? new PilotEntry("", StartTime, 0);

    public void Finish(bool addFinalizationEntries = true)
    {
        FinishTime = DateTime.Now;
        if (!addFinalizationEntries) return;

        var finishTime = FinishTime.Value;
        var pilots = _entries.Select(e => e.PilotNumber).Distinct().ToList();
        foreach (var pilot in pilots)
        {
            var lastTurn = _entries.Where(e => e.PilotNumber == pilot).Max(e => e.Turn);
            if (lastTurn < TurnsMax)
                _entries.Add(new PilotEntry(pilot, finishTime, lastTurn));
        }
    }
}