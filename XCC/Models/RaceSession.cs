using System;
using System.Collections.Generic;

namespace XCC.Models;

public class RaceSession
{
    public string RoundName { get; }
    public int TurnsMax { get; }
    public DateTime StartTime { get; }
    public DateTime? FinishTime { get; private set; }
    public TimeSpan Elapsed => (FinishTime ?? DateTime.Now) - StartTime;

    private readonly List<PilotEntry> _entries = [];
    public IReadOnlyList<PilotEntry> Entries => _entries.AsReadOnly();

    public RaceSession(string roundName, int turnsMax, DateTime? startTime = null)
    {
        RoundName = roundName;
        TurnsMax = turnsMax;
        StartTime = startTime ?? DateTime.Now;
    }

    public void AddEntry(string pilotNumber, int turn)
        => _entries.Add(new PilotEntry(pilotNumber, DateTime.Now, turn));

    public void Finish() => FinishTime = DateTime.Now;
}
