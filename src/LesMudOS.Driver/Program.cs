using LesMudOS.Driver;
using LesMudOS.Driver.Net;

// Usage: LesMudOS.Driver [--mudlib <dir>] [--save <dir>] [--port <n>] [--idle-minutes <n>] [--timeout-minutes <n>]
// (the environment overrides the idle settings: LESMUDOS_IDLE_MINUTES, LESMUDOS_TIMEOUT_MINUTES; see docs/DEPLOY.md)
var mudlib = "mudlib";
var save = "save";
var port = 4000;
var settings = new DriverSettings();

for (var i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--mudlib": mudlib = args[++i]; break;
        case "--save": save = args[++i]; break;
        case "--port": port = int.Parse(args[++i]); break;
        case "--idle-minutes": settings.SetIdle(args[++i], "--idle-minutes"); break;
        case "--timeout-minutes": settings.SetTimeout(args[++i], "--timeout-minutes"); break;
        case "--sandbox-stall": settings.SetStall(args[++i], "--sandbox-stall"); break;
    }
}

if (!Directory.Exists(mudlib))
{
    Console.Error.WriteLine($"Mudlib directory '{Path.GetFullPath(mudlib)}' not found. Use --mudlib <dir>.");
    return 1;
}

Log.Info($"LesMudOS driver starting; mudlib={Path.GetFullPath(mudlib)} save={Path.GetFullPath(save)}");
// The bootstrap secret comes from the environment, never from the mudlib or the
// command line (which other users of the machine can read from the process list).
var adminSecret = Environment.GetEnvironmentVariable("LESMUDOS_ADMIN_SECRET");
Log.Info(string.IsNullOrEmpty(adminSecret)
    ? "no LESMUDOS_ADMIN_SECRET set: the first level-10 wizard cannot be created"
    : "admin secret configured");
settings.SetIdle(Environment.GetEnvironmentVariable("LESMUDOS_IDLE_MINUTES"), "LESMUDOS_IDLE_MINUTES");
settings.SetTimeout(Environment.GetEnvironmentVariable("LESMUDOS_TIMEOUT_MINUTES"), "LESMUDOS_TIMEOUT_MINUTES");
settings.SetStall(Environment.GetEnvironmentVariable("LESMUDOS_SANDBOX_STALL"), "LESMUDOS_SANDBOX_STALL");
settings.Settle();
Log.Info($"idle after {settings.IdleSeconds:0.##}s without input, disconnect after {settings.TimeoutSeconds:0.##}s");
var kernel = new Kernel(mudlib, save, adminSecret, settings);
kernel.StartWatchdog();
var server = new TelnetServer(kernel, port);

// The game loop gets a dedicated thread with a generous stack, so the
// recursion guard has room to fire before the stack is exhausted.
Exception? fatal = null;
var gameThread = new Thread(() =>
{
    try
    {
        kernel.Post(server.Start); // start accepting only after boot succeeds
        kernel.Run();
    }
    catch (Exception error)
    {
        fatal = error;
    }
}, maxStackSize: 16 * 1024 * 1024) { Name = "game" };

gameThread.Start();
gameThread.Join();

if (fatal is not null)
{
    Console.Error.WriteLine($"FATAL: {fatal.Message}");
    return 1;
}
return 0;
