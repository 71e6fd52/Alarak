using Barotrauma;

namespace Alarak;

public partial class Plugin : IAssemblyPlugin
{
    public void Initialize()
    {
#if SERVER
        InitServer();
#endif
    }

    public void OnLoadCompleted()
    {
        // After all plugins have loaded
        // Put code that interacts with other plugins here.
    }

    public void PreInitPatching()
    {
        // Not yet supported: Called during the Barotrauma startup phase before vanilla content is loaded.
    }

    public void Dispose()
    {
#if SERVER
        DisposeServer();
#endif
    }
}
