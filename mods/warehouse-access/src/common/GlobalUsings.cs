// The only place the game's namespaces differ between runtimes. Everything else in this folder is
// compiled unchanged into both the Mono and the IL2CPP build.
#if IL2CPP
global using Il2CppScheduleOne.DevUtilities;
global using Il2CppScheduleOne.Levelling;
global using Il2CppScheduleOne.Map;
global using Il2CppScheduleOne.NPCs.CharacterClasses;
global using Il2CppScheduleOne.Quests;
#else
global using ScheduleOne.DevUtilities;
global using ScheduleOne.Levelling;
global using ScheduleOne.Map;
global using ScheduleOne.NPCs.CharacterClasses;
global using ScheduleOne.Quests;
#endif
global using UnityEngine;
