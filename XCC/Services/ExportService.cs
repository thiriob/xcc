using System;
using System.Threading.Tasks;
using XCC.Models;

namespace XCC.Services;

/// <summary>
/// Platform-specific export handler registered at startup by each platform project.
/// Returns a short status string on success, throws on failure.
/// </summary>
public static class ExportProvider
{
    public static Func<RaceSession, Task<string?>>? Handler { get; set; }
}
