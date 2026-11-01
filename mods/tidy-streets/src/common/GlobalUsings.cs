// The only place the game's namespaces differ between runtimes. Everything else in this folder is
// compiled unchanged into both the Mono and the IL2CPP build.
#if IL2CPP
global using Il2CppScheduleOne.DevUtilities;
global using Il2CppScheduleOne.Persistence;
global using Il2CppScheduleOne.Trash;
#else
global using ScheduleOne.DevUtilities;
global using ScheduleOne.Persistence;
global using ScheduleOne.Trash;
#endif
