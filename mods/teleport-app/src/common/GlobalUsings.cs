// The only place the game's namespaces differ between runtimes. Everything else in this folder is
// compiled unchanged into both the Mono and the IL2CPP build.
#if IL2CPP
global using Il2CppScheduleOne.PlayerScripts;
global using Il2CppScheduleOne.UI;
global using Il2CppScheduleOne;
global using Il2CppScheduleOne.DevUtilities;
global using GameProperty = Il2CppScheduleOne.Property.Property;
global using GameBusiness = Il2CppScheduleOne.Property.Business;
global using GameDealer = Il2CppScheduleOne.Economy.Dealer;
global using GameVehicle = Il2CppScheduleOne.Vehicles.LandVehicle;
global using GameVehicleColors = Il2CppScheduleOne.Vehicles.Modification.VehicleColors;
global using GameVehicleManager = Il2CppScheduleOne.Vehicles.VehicleManager;
global using GameHomeScreen = Il2CppScheduleOne.UI.Phone.HomeScreen;
global using GamePhone = Il2CppScheduleOne.UI.Phone.Phone;
#else
global using ScheduleOne.PlayerScripts;
global using ScheduleOne.UI;
global using ScheduleOne;
global using ScheduleOne.DevUtilities;
global using GameProperty = ScheduleOne.Property.Property;
global using GameBusiness = ScheduleOne.Property.Business;
global using GameDealer = ScheduleOne.Economy.Dealer;
global using GameVehicle = ScheduleOne.Vehicles.LandVehicle;
global using GameVehicleColors = ScheduleOne.Vehicles.Modification.VehicleColors;
global using GameVehicleManager = ScheduleOne.Vehicles.VehicleManager;
global using GameHomeScreen = ScheduleOne.UI.Phone.HomeScreen;
global using GamePhone = ScheduleOne.UI.Phone.Phone;
#endif
global using UnityEngine;
global using UnityEngine.Events;
global using UnityEngine.UI;

// Shared code compiled into every mod (see shared/).
global using ModShared;
global using static ModShared.Interop;
