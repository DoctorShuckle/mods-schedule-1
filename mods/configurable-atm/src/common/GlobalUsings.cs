// The only place the game's namespaces differ between runtimes. Everything else in this folder is
// compiled unchanged into both the Mono and the IL2CPP build.
#if IL2CPP
global using Il2CppScheduleOne.Money;
global using Il2CppScheduleOne.Quests;
global using Il2CppScheduleOne.UI;
#else
global using ScheduleOne.Money;
global using ScheduleOne.Quests;
global using ScheduleOne.UI;
#endif
global using UnityEngine;
