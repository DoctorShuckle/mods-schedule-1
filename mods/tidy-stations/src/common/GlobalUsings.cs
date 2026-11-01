// The only place the game's namespaces differ between runtimes. Everything else in this folder is
// compiled unchanged into both the Mono and the IL2CPP build.
#if IL2CPP
global using Il2CppScheduleOne.Growing;
global using Il2CppScheduleOne.NPCs.Behaviour;
global using Il2CppScheduleOne.ObjectScripts;
global using Il2CppScheduleOne.PlayerTasks.Tasks;
global using Il2CppScheduleOne.Trash;
#else
global using ScheduleOne.Growing;
global using ScheduleOne.NPCs.Behaviour;
global using ScheduleOne.ObjectScripts;
global using ScheduleOne.PlayerTasks.Tasks;
global using ScheduleOne.Trash;
#endif
