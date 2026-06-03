using System;
using System.Threading.Tasks;

namespace XCC.Services;

public static class ClipboardProvider
{
    public static Func<string, Task>? Handler { get; set; }
}
