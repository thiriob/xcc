using System;
using System.Collections.Generic;
using System.Linq;
using XCC.Models;

namespace XCC.Services;

public static class SimulationService
{
    private const int    NumPilots    = 20;
    private const double BaseLap      = 90.0;  // seconds
    private const double LapVariation = 8.0;   // ± seconds per lap
    private const double Warmup       = 30.0;  // seconds before lap 1

    public static RaceSession Create(string roundName, int numTurns)
    {
        var rng = new Random();

        // Pilot bib numbers
        var pilots = Enumerable.Range(101, NumPilots).Select(n => n.ToString()).ToList();

        // Shuffle, then assign pace groups: fast / mid / slow
        var shuffled = pilots.OrderBy(_ => rng.Next()).ToList();
        var pace = shuffled.Select((p, i) => (p, mult: i < 4  ? 0.85 + rng.NextDouble() * 0.07   // fast
                                                              : i < 16 ? 0.92 + rng.NextDouble() * 0.13  // mid
                                                                       : 1.05 + rng.NextDouble() * 0.10)) // slow
                           .ToDictionary(x => x.p, x => x.mult);

        // 1–2 falls (only when enough turns to be interesting)
        int numFalls = numTurns >= 3 ? rng.Next(1, 3) : 0;
        var fallPilots = shuffled
            .OrderBy(_ => rng.Next())
            .Take(numFalls)
            .ToDictionary(p => p, _ => rng.Next(2, Math.Max(3, numTurns)));

        // ── Phase 1: compute all cumulative times (no DateTime yet) ──────────
        var cumSec = pilots.ToDictionary(p => p, _ => Warmup);
        var timings = new List<(string Pilot, int Turn, double Seconds)>();

        for (int turn = 1; turn <= numTurns; turn++)
        {
            foreach (var pilot in pilots)
            {
                double lap = BaseLap * pace[pilot]
                           + (rng.NextDouble() * LapVariation * 2 - LapVariation);

                if (fallPilots.TryGetValue(pilot, out int fallTurn) && fallTurn == turn)
                    lap += 70 + rng.NextDouble() * 50;   // fall: +70–120 s

                cumSec[pilot] += lap;
            }

            // Finish order for this turn = order of cumulative time
            foreach (var (pilot, t) in cumSec.OrderBy(kv => kv.Value))
                timings.Add((pilot, turn, t));
        }

        // ── Phase 2: anchor to real time so all entries are in the past ──────
        double maxSec = cumSec.Values.Max();
        var sessionStart = DateTime.Now.AddSeconds(-(maxSec + 30)); // 30 s buffer

        // ── Phase 3: build session ────────────────────────────────────────────
        var session = new RaceSession(roundName, numTurns, sessionStart);

        foreach (var (pilot, turn, sec) in timings)
            session.AddEntry(pilot, turn, sessionStart.AddSeconds(sec));

        session.Finish();
        return session;
    }
}
