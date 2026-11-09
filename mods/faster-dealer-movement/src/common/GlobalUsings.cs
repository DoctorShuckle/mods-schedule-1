// The only place the game's namespaces differ between runtimes. Everything else in this folder is
// compiled unchanged into both the Mono and the IL2CPP build.
#if IL2CPP
global using Il2CppScheduleOne.Cartel;
global using Il2CppScheduleOne.Economy;
global using Il2CppScheduleOne.NPCs;
global using Il2CppScheduleOne.NPCs.Behaviour;
#else
global using ScheduleOne.Cartel;
global using ScheduleOne.Economy;
global using ScheduleOne.NPCs;
global using ScheduleOne.NPCs.Behaviour;
#endif
global using UnityEngine;

// Shared code compiled into every mod (see shared/).
global using ModShared;
global using static ModShared.Interop;
