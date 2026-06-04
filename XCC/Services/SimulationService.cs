using System;
using System.Collections.Generic;
using System.Linq;
using XCC.Models;

namespace XCC.Services;

public static class SimulationService
{
    private const double BaseLap      = 90.0;  // seconds — targets ~6 min for winner on 4 turns
    private const double LapVariation = 8.0;   // ± seconds per lap
    private const double Warmup       = 30.0;  // seconds before lap 1

    public static RaceSession Create(string roundName, int numTurns, IList<string>? pilotIds = null)
    {
        var rng = new Random();

        var pilots = pilotIds?.ToList()
            ?? Enumerable.Range(101, 20).Select(n => n.ToString()).ToList();

        // Shuffle, then assign pace multipliers by rank bucket
        var shuffled = pilots.OrderBy(_ => rng.Next()).ToList();
        var pace = shuffled.Select((p, i) =>
        {
            double ratio = (double)i / shuffled.Count;
            double mult = ratio < 0.20 ? 0.82 + rng.NextDouble() * 0.06   // fast (~6 min)
                        : ratio < 0.75 ? 0.93 + rng.NextDouble() * 0.12   // mid
                                       : 1.08 + rng.NextDouble() * 0.12;  // slow (may get lapped)
            return (p, mult);
        }).ToDictionary(x => x.p, x => x.mult);

        // DNFs: ~12% of the field, never the top 25%
        int numDnf = numTurns >= 3 ? Math.Max(1, (int)Math.Round(pilots.Count * 0.12)) : 0;
        var dnfPilots = shuffled
            .Skip((int)(shuffled.Count * 0.25))
            .OrderBy(_ => rng.Next())
            .Take(numDnf)
            .ToDictionary(p => p, _ => rng.Next(1, numTurns)); // last completed turn

        // Falls: 1 per ~8 pilots, never on turn 1
        int numFalls = numTurns >= 3 ? rng.Next(1, Math.Max(2, pilots.Count / 8 + 1)) : 0;
        var fallPilots = shuffled
            .OrderBy(_ => rng.Next())
            .Take(numFalls)
            .ToDictionary(p => p, _ => rng.Next(2, Math.Max(3, numTurns)));

        // ── Phase 1: compute cumulative times ────────────────────────────────
        var cumSec = pilots.ToDictionary(p => p, _ => Warmup);
        var timings = new List<(string Pilot, int Turn, double Seconds)>();

        for (int turn = 1; turn <= numTurns; turn++)
        {
            foreach (var pilot in pilots)
            {
                if (dnfPilots.TryGetValue(pilot, out int dnfTurn) && turn > dnfTurn)
                    continue;

                double lap = BaseLap * pace[pilot]
                           + (rng.NextDouble() * LapVariation * 2 - LapVariation);

                if (fallPilots.TryGetValue(pilot, out int fallTurn) && fallTurn == turn)
                    lap += 70 + rng.NextDouble() * 50;

                cumSec[pilot] += lap;
                timings.Add((pilot, turn, cumSec[pilot]));
            }
        }

        // ── Phase 2: anchor to real time so all entries are in the past ──────
        double maxSec = cumSec.Values.Max();
        var sessionStart = DateTime.Now.AddSeconds(-(maxSec + 30));

        // ── Phase 3: build session ────────────────────────────────────────────
        var session = new RaceSession(roundName, numTurns, sessionStart);

        foreach (var (pilot, turn, sec) in timings.OrderBy(t => t.Turn).ThenBy(t => t.Seconds))
            session.AddEntry(pilot, turn, sessionStart.AddSeconds(sec));

        session.Finish();
        return session;
    }
}
