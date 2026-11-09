// The only place the game's namespaces differ between runtimes. Everything else in this folder is
// compiled unchanged into both the Mono and the IL2CPP build.
#if IL2CPP
global using Il2CppScheduleOne;
global using Il2CppScheduleOne.DevUtilities;
global using Il2CppScheduleOne.ItemFramework;
global using Il2CppScheduleOne.ObjectScripts;
global using Il2CppScheduleOne.Persistence;
global using Il2CppScheduleOne.Storage;
#else
global using ScheduleOne;
global using ScheduleOne.DevUtilities;
global using ScheduleOne.ItemFramework;
global using ScheduleOne.ObjectScripts;
global using ScheduleOne.Persistence;
global using ScheduleOne.Storage;
#endif
global using UnityEngine;

// Shared code compiled into every mod (see shared/).
global using ModShared;
global using static ModShared.Interop;
