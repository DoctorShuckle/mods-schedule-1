using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
#if IL2CPP
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#endif

namespace TeleportApp
{
    /// <summary>
    /// The phone and popup calls whose shape differs between Mono and IL2CPP: listening to a button,
    /// and opening the game's own list popup. The rest of the phone code is the same for both builds.
    /// </summary>
    internal static class GamePhoneAccess
    {
        public static void OnClick(Button button, Action action)
        {
#if IL2CPP
            button.onClick.AddListener((UnityAction)action);
#else
            button.onClick.AddListener(new UnityAction(action));
#endif
        }

        /// <summary>
        /// Opens the game's context menu (the list popup its settings dropdowns use) in the middle of
        /// the screen, with the picked entry selected. It already works with a controller (stick and A
        /// to choose, B to close) and with the mouse, so the mod draws no list of its own.
        /// </summary>
        public static void OpenList(IList<string> names, int selected, Action<int> onChosen)
        {
            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            var manager = Singleton<UIScreenManager>.Instance;
#if IL2CPP
            var options = new Il2CppReferenceArray<UIPopupScreen_ContextMenu.ContextMenuOption>(names.Count);
            for (int i = 0; i < names.Count; i++)
            {
                int index = i;
                Action choose = () => onChosen(index);
                options[i] = new UIPopupScreen_ContextMenu.ContextMenuOption(i, names[i], (Il2CppSystem.Action)choose);
            }

            // The game reads these as: options, position, anchor, selected entry, dim the background.
            var args = new Il2CppReferenceArray<Il2CppSystem.Object>(5);
            args[0] = new Il2CppSystem.Object(options.Pointer);
            args[1] = Box(center);
            args[2] = Box(UIPopupScreen_ContextMenu.AnchorType.Center);
            args[3] = Box(selected);
            args[4] = Box(true);
            manager.OpenPopupScreen("ContextMenu", args);
#else
            var options = new UIPopupScreen_ContextMenu.ContextMenuOption[names.Count];
            for (int i = 0; i < names.Count; i++)
            {
                int index = i;
                options[i] = new UIPopupScreen_ContextMenu.ContextMenuOption(i, names[i], () => onChosen(index));
            }

            manager.OpenPopupScreen("ContextMenu",
                new object[] { options, center, UIPopupScreen_ContextMenu.AnchorType.Center, selected, true });
#endif
        }

        /// <summary>
        /// Runs the action after the current frame. A popup runs its choice and only then closes, and the
        /// game will not open a popup that is still closing, so the second list waits one frame. Runs
        /// once and ends: nothing is polled.
        /// </summary>
        public static void NextFrame(Action action) => MelonCoroutines.Start(RunNextFrame(action));

        private static IEnumerator RunNextFrame(Action action)
        {
            yield return null;
            try { action(); }
            catch (Exception ex) { ModLog.Warning("The second list could not open: " + ex.Message); }
        }

        /// <summary>Closes the gameplay menu (the phone and character screens), as the game does when you leave it.</summary>
        public static void CloseMenu()
        {
            if (Singleton<GameplayMenu>.InstanceExists) Singleton<GameplayMenu>.Instance.Close();
        }

#if IL2CPP
        // The popup takes its arguments as boxed objects; this boxes a value the way Il2CppInterop does.
        private static unsafe Il2CppSystem.Object Box<T>(T value) where T : unmanaged
            => new Il2CppSystem.Object(IL2CPP.il2cpp_value_box(Il2CppClassPointerStore<T>.NativeClassPtr, (IntPtr)(&value)));
#endif
    }
}
