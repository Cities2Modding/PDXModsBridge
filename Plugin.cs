using BepInEx;
using HarmonyLib;
using System.Reflection;
using System.Linq;

#if BEPINEX_V6
    using BepInEx.Unity.Mono;
#endif

namespace PDXModsBridge
{
    [BepInPlugin( MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION )]
    public class Plugin : BaseUnityPlugin
    {
        private static Harmony _harmony;

        private void Awake( )
        {
            _harmony = Harmony.CreateAndPatchAll( Assembly.GetExecutingAssembly( ), MyPluginInfo.PLUGIN_GUID + "_Cities2Harmony" );

            var patchedMethods = _harmony.GetPatchedMethods( ).ToArray( );

            Logger.LogInfo( "=================================================================" );
            Logger.LogInfo( MyPluginInfo.PLUGIN_NAME + " by Cities2Modding community." );
            Logger.LogInfo( "=================================================================" );
            Logger.LogInfo( "Reddit link: https://www.reddit.com/r/cities2modding/" );
            Logger.LogInfo( "Discord link: https://discord.gg/KGRNBbm5Fh" );
            Logger.LogInfo( "Our mods are officially distributed via Thunderstore.io and https://github.com/Cities2Modding" );
            Logger.LogInfo( "Example mod repository and modding info: https://github.com/optimus-code/Cities2Modding" );
            Logger.LogInfo( "Thanks to 89pleasure, Rebecca, optimus-code and the Cites2Modding community!" );
            Logger.LogInfo( "=================================================================" );

            // Plugin startup logic
            Logger.LogInfo( $"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded! Patched methods: " + patchedMethods.Length );

            foreach ( var patchedMethod in patchedMethods )
            {
                Logger.LogInfo( $"Patched method: {patchedMethod.Module.Name}:{patchedMethod.Name}" );
            }

            ModLoader.ScanDirectory( );
        }

        // Doesn't work as BepInEx destroys object
        private void OnDestroy( )
        {
            //ModLoader.Unload( );
            //_harmony?.UnpatchSelf( );
        }
    }
}
