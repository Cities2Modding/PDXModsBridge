using Game;
using Game.Modding;
using HarmonyLib;

namespace PDXModsBridge.Patches
{
    [HarmonyPatch( typeof( ModManager ), "InitializeMods" )]
    static class InitializeMods_Patch
    {
        static void Postfix( UpdateSystem updateSystem )
        {
            ModLoader.Load( updateSystem );
        }
    }
}
