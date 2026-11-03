// The only place the game's namespaces differ between runtimes. Everything else in this folder is
// compiled unchanged into both the Mono and the IL2CPP build.
#if IL2CPP
global using Il2CppScheduleOne.UI.MainMenu;
#else
global using ScheduleOne.UI.MainMenu;
#endif
global using UnityEngine;
