// The only place the game's namespaces differ between runtimes. Everything else in this folder is
// compiled unchanged into both the Mono and the IL2CPP build.
#if IL2CPP
global using Il2CppScheduleOne.Experimental;
global using Il2CppScheduleOne.PlayerScripts;
global using Il2CppScheduleOne.Skating;
#else
global using ScheduleOne.Experimental;
global using ScheduleOne.PlayerScripts;
global using ScheduleOne.Skating;
#endif
global using UnityEngine;
