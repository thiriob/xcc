using System.Diagnostics;
using XCC.Services;

// Usage: dotnet run --project XCC.Cli -- <RoundName> <Turns> <id1> <id2> ...

if (args.Length < 3 || !int.TryParse(args[1], out var turns))
{
    Console.Error.WriteLine("Usage: XCC.Cli <RoundName> <Turns> <id1> <id2> ...");
    return 1;
}

var name   = args[0];
var pilots = args[2..];

var session = SimulationService.Create(name, turns, pilots);

var standings = session.Entries
    .GroupBy(e => e.PilotNumber)
    .Select(g =>
    {
        var mt   = g.Max(e => e.Turn);
        var last = g.Where(e => e.Turn == mt).Min(e => e.Timestamp);
        return (PilotNumber: g.Key, MaxTurn: mt, Last: last);
    })
    .OrderByDescending(s => s.MaxTurn)
    .ThenBy(s => s.Last)
    .Select((s, i) =>
    {
        var chrono = s.Last - session.StartTime;
        return (Pos: i + 1, s.PilotNumber, s.MaxTurn, Chrono: $"{(int)chrono.TotalMinutes:D2}:{chrono.Seconds:D2}", Finished: s.MaxTurn >= turns);
    })
    .ToList();

var dnfCount = standings.Count(s => !s.Finished);

// Print all results to console
Console.WriteLine($"── {name}  ({turns} tours · {pilots.Length} pilotes · {dnfCount} DNF) ──");
Console.WriteLine($"   Départ : {session.StartTime:HH:mm:ss}   Fin : {session.FinishTime:HH:mm:ss}");
Console.WriteLine();
foreach (var s in standings)
{
    var dnf = s.Finished ? "" : $"  [DNF T{s.MaxTurn}]";
    Console.WriteLine($"  {s.Pos,2}.  {s.PilotNumber}\t{s.MaxTurn}\t{s.Chrono}{dnf}");
}
Console.WriteLine();

// Copy all to clipboard
var clipboardText = string.Join("\n", standings
    .Select(s => $"{s.PilotNumber}\t{s.MaxTurn}\t{s.Chrono}"));

try
{
    var proc = new Process
    {
        StartInfo = new ProcessStartInfo("clip")
        {
            RedirectStandardInput = true,
            UseShellExecute       = false,
        }
    };
    proc.Start();
    proc.StandardInput.Write(clipboardText);
    proc.StandardInput.Close();
    proc.WaitForExit();
    Console.WriteLine("  ✓ Classement copié dans le presse-papiers");
}
catch
{
    Console.WriteLine("  (presse-papiers non disponible sur cette plateforme)");
}

return 0;
