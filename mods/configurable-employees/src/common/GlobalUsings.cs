// The only place the game's namespaces differ between runtimes. Everything else in this folder is
// compiled unchanged into both the Mono and the IL2CPP build.
#if IL2CPP
global using Il2CppScheduleOne;
global using Il2CppScheduleOne.DevUtilities;
global using Il2CppScheduleOne.Employees;
global using Il2CppScheduleOne.Management;
global using Il2CppScheduleOne.NPCs;
global using Il2CppScheduleOne.NPCs.CharacterClasses;
global using Il2CppScheduleOne.Tools;
global using Il2CppScheduleOne.UI.Management;
global using Il2CppScheduleOne.Variables;
// "Property" is both a namespace and a class in the game, hence the alias.
global using GameProperty = Il2CppScheduleOne.Property.Property;
global using Behaviours = Il2CppScheduleOne.NPCs.Behaviour;
#else
global using ScheduleOne;
global using ScheduleOne.DevUtilities;
global using ScheduleOne.Employees;
global using ScheduleOne.Management;
global using ScheduleOne.NPCs;
global using ScheduleOne.NPCs.CharacterClasses;
global using ScheduleOne.Tools;
global using ScheduleOne.UI.Management;
global using ScheduleOne.Variables;
global using GameProperty = ScheduleOne.Property.Property;
global using Behaviours = ScheduleOne.NPCs.Behaviour;
#endif
