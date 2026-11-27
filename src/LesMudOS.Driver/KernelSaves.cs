using LesMudOS.Api;
using LesMudOS.Driver.Compilation;

namespace LesMudOS.Driver;

// The one function that lists the save directory (for /secure/roster, the index behind `all-players`). It returns names only: no content, no
// size, no date. The master allows the roster blueprint and nothing else, never restricted code, and only for the directory "/players".
internal sealed partial class Kernel
{
    public IReadOnlyList<string> SaveNames(ObjectRef caller, string dir)
    {
        var clean = ProgramCompiler.NormalizePath(dir ?? "");
        if (!AskMaster(m => m.ValidOperation("save-names", caller, clean)))
            throw new MudRuntimeException($"{caller.Name} may not list {clean}");
        return _saves.Names(clean);
    }
}
