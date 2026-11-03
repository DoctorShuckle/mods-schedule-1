// The only place the game's namespaces differ between runtimes. Everything else in this folder is
// compiled unchanged into both the Mono and the IL2CPP build.
#if IL2CPP
global using Il2CppFishNet;
global using Il2CppScheduleOne;
global using Il2CppScheduleOne.DevUtilities;
global using Il2CppScheduleOne.Persistence;
global using Il2CppScheduleOne.ItemFramework;
global using Il2CppScheduleOne.ObjectScripts;
global using Il2CppScheduleOne.Product;
global using Il2CppScheduleOne.Product.Packaging;
global using Il2CppScheduleOne.UI;
global using Il2CppScheduleOne.UI.Items;
#else
global using FishNet;
global using ScheduleOne;
global using ScheduleOne.DevUtilities;
global using ScheduleOne.Persistence;
global using ScheduleOne.ItemFramework;
global using ScheduleOne.ObjectScripts;
global using ScheduleOne.Product;
global using ScheduleOne.Product.Packaging;
global using ScheduleOne.UI;
global using ScheduleOne.UI.Items;
#endif
