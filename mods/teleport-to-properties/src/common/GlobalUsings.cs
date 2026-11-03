// The only place the game's namespaces differ between runtimes. Everything else in this folder is
// compiled unchanged into both the Mono and the IL2CPP build.
#if IL2CPP
global using Il2CppScheduleOne.PlayerScripts;
global using Il2CppScheduleOne.UI;
global using Il2CppScheduleOne;
global using Il2CppScheduleOne.DevUtilities;
global using GameProperty = Il2CppScheduleOne.Property.Property;
global using GameBusiness = Il2CppScheduleOne.Property.Business;
#else
global using ScheduleOne.PlayerScripts;
global using ScheduleOne.UI;
global using ScheduleOne;
global using ScheduleOne.DevUtilities;
global using GameProperty = ScheduleOne.Property.Property;
global using GameBusiness = ScheduleOne.Property.Business;
#endif
global using UnityEngine;
