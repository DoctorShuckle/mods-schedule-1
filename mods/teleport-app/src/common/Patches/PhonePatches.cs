using HarmonyLib;

namespace TeleportApp
{
    /// <summary>
    /// The phone's open call is the moment to add the icon: every other app has put its icon on the
    /// home screen by then, and nothing has to be searched for or waited on.
    /// </summary>
    [HarmonyPatch(typeof(GamePhone), "SetIsOpen")]
    internal static class Phone_SetIsOpen_Patch
    {
        private static void Postfix(bool o)
        {
            if (o) PhoneIcon.EnsureIcon();
        }
    }
}
