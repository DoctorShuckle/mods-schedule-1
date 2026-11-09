using System.Collections.Generic;

namespace ConfigurableStorage
{
    /// <summary>
    /// Every read of or call into the game goes through here. This file holds the runtime
    /// differences between Mono and IL2CPP, so the patches stay identical for both builds.
    /// </summary>
    internal static class GameAccess
    {
#if IL2CPP
        // IL2CPP proxies are wrappers: a reference typed as a base class must be asked with TryCast.
        private static T As<T>(object value) where T : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
            => (value as Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase)?.TryCast<T>();
#else
        private static T As<T>(object value) where T : class => value as T;
#endif

        /// <summary>The game's own size of each container, read the first time it is seen, before the mod changes it.</summary>
        private static readonly Dictionary<string, (int Slots, int Rows)> GameSizes = new Dictionary<string, (int, int)>();

        /// <summary>
        /// Sets the size on the prefab of every configured container. The game builds each placed container
        /// by instantiating that prefab, and StorageEntity creates its slots from SlotCount in Awake, so
        /// containers built or loaded afterwards come out the new size. Containers already standing keep
        /// theirs until the save is loaded again. Does nothing while the item registry does not exist yet.
        /// </summary>
        public static void ApplyAll()
        {
            Registry registry = Singleton<Registry>.Instance;
            if (registry == null) return;

            var items = registry.GetAllItems();
            for (int i = 0; i < items.Count; i++)
            {
                BuildableItemDefinition definition = As<BuildableItemDefinition>(items[i]);
                if (definition == null) continue;

                PlaceableStorageEntity placeable = As<PlaceableStorageEntity>(definition.BuiltItem);
                if (placeable == null || placeable.StorageEntity == null) continue;

                string id = definition.ID;
                StorageEntity storage = placeable.StorageEntity;
                if (!ModConfig.TryGet(id, out int configuredSlots, out int configuredRows))
                {
                    ModLog.Verbose("Not handled: " + id + " (" + definition.Name + "), " + storage.SlotCount + " slots in " + storage.DisplayRowCount + " rows.");
                    continue;
                }

                if (!GameSizes.TryGetValue(id, out var game))
                {
                    game = (storage.SlotCount, storage.DisplayRowCount);
                    GameSizes[id] = game;
                }

                int slots = StorageSizing.Slots(configuredSlots, game.Slots);
                int rows = StorageSizing.Rows(configuredRows, slots, game.Slots, game.Rows);
                if (storage.SlotCount != slots) storage.SlotCount = slots;
                if (storage.DisplayRowCount != rows) storage.DisplayRowCount = rows;
                ModLog.Verbose(id + " (" + definition.Name + "): game " + game.Slots + " slots in " + game.Rows + " rows, now " + slots + " in " + rows + ".");
            }

            foreach ((string id, string name) in Containers.All)
            {
                if (!GameSizes.ContainsKey(id)) ModLog.WarnOnce("Container " + id + " (" + name + ") was not found as a placeable storage item; its settings do nothing.");
            }
        }
    }
}
