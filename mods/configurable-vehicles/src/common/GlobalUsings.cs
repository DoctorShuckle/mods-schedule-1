// The only place the game's namespaces differ between runtimes. Everything else in this folder is
// compiled unchanged into both the Mono and the IL2CPP build.
#if IL2CPP
global using Il2CppScheduleOne.DevUtilities;
global using Il2CppScheduleOne.Storage;
global using Il2CppScheduleOne.Vehicles;
#else
global using ScheduleOne.DevUtilities;
global using ScheduleOne.Storage;
global using ScheduleOne.Vehicles;
#endif
global using UnityEngine;

// Shared code compiled into every mod (see shared/).
global using ModShared;
