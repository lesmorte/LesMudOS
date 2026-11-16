using LesMudOS.Driver;
using LesMudOS.Driver.Net;

// Usage: LesMudOS.Driver [--mudlib <dir>] [--save <dir>] [--port <n>]
var mudlib = "mudlib";
var save = "save";
var port = 4000;

for (var i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--mudlib": mudlib = args[++i]; break;
        case "--save": save = args[++i]; break;
        case "--port": port = int.Parse(args[++i]); break;
    }
}

if (!Directory.Exists(mudlib))
{
    Console.Error.WriteLine($"Mudlib directory '{Path.GetFullPath(mudlib)}' not found. Use --mudlib <dir>.");
    return 1;
}

Log.Info($"LesMudOS driver starting; mudlib={Path.GetFullPath(mudlib)} save={Path.GetFullPath(save)}");
var kernel = new Kernel(mudlib, save);
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
