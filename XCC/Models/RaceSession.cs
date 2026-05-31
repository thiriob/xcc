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

    public PilotEntry GetPreviousEntry(string pilotNumber)
        => _entries.LastOrDefault(e => e.PilotNumber == pilotNumber)
           ?? new PilotEntry("", StartTime, 0);

    public void Finish() => FinishTime = DateTime.Now;
}