using System;
using System.Threading.Tasks;

namespace XCC.Services;

public static class ClipboardProvider
{
    public static Func<string, Task<bool>>? Handler { get; set; }
}
