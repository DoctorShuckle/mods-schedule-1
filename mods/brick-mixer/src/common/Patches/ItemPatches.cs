using System;
using HarmonyLib;

namespace BrickMixer
{
    // The Brick Mixer is a copy of the Mixing Station Mk2 with its own item ID, so these patches add the item
    // and the rules for that ID only. Every other station is left as the game has it.

    /// <summary>Puts the Brick Mixer in the item registry as each game starts, before saved stations are built.</summary>
    [HarmonyPatch(typeof(LoadManager), "StartGame")]
    internal static class LoadManager_StartGame_Patch
    {
        private static void Prefix()
        {
            try
            {
                GameAccess.EnsureRegistered();
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not add the Brick Mixer: " + ex);
            }
        }
    }

    /// <summary>Lists the Brick Mixer in the shop that sells the Mk2, before the shop builds its list.</summary>
    [HarmonyPatch(typeof(ShopInterface), "Awake")]
    internal static class ShopInterface_Awake_Patch
    {
        private static void Prefix(ShopInterface __instance)
        {
            try
            {
                GameAccess.AddToShop(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not list the Brick Mixer in a shop: " + ex);
            }
        }
    }
}
