using System.Collections.Generic;
using UnityEngine;

namespace ConfigurableEmployees
{
    /// <summary>
    /// Changes the time of a worker's actions. The game divides each action's base time by the worker's
    /// work speed, so holding a work speed multiplier of (game seconds / configured seconds) while the
    /// action runs makes it take the configured time, without touching the game's numbers (they are
    /// constants inside the game's coroutines, with nothing to patch). The actions and their settings
    /// are listed in Rules/TaskTimes.cs; the patches in Patches/TaskTimePatches.cs call in here.
    /// </summary>
    internal static class TaskHold
    {
        private const string TaskSpeedLabel = "ConfigurableEmployees.TaskTime";

        // Every timed action works the same way. The game divides the action's base time by the
        // worker's work speed, so holding a work speed multiplier of (game seconds / configured seconds)
        // while the action runs makes it take the configured time, without touching the game's numbers
        // (they are constants inside the game's coroutines, with nothing to patch). Only the waits the
        // game divides by work speed change: walking and its fixed pauses do not.

        /// <summary>
        /// The entry for a botanist's grow task (soil, water, additive, sow, harvest, spawn), or null.
        /// Mist inherits Water, so it is covered by it.
        /// </summary>
        private static TaskTime TimeOfGrowTask(Behaviours.GrowContainerBehaviour task)
        {
            if (GameAccess.As<Behaviours.AddSoilToGrowContainerBehaviour>(task) != null) return TaskTimes.Soil;
            if (GameAccess.As<Behaviours.WaterPotBehaviour>(task) != null) return TaskTimes.Water;
            if (GameAccess.As<Behaviours.ApplyAdditiveToGrowContainerBehaviour>(task) != null) return TaskTimes.Additive;
            if (GameAccess.As<Behaviours.SowSeedInPotBehaviour>(task) != null) return TaskTimes.Sow;
            if (GameAccess.As<Behaviours.HarvestPotBehaviour>(task) != null || GameAccess.As<Behaviours.HarvestMushroomBedBehaviour>(task) != null) return TaskTimes.Harvest;
            if (GameAccess.As<Behaviours.ApplySpawnToMushroomBedBehaviour>(task) != null) return TaskTimes.Spawn;
            return null;
        }

        /// <summary>The entry for any other timed behaviour (chemist, packager, and the botanist's racks and stations), or null.</summary>
        private static TaskTime TimeOfBehaviour(Behaviours.Behaviour behaviour, Employee employee)
        {
            if (GameAccess.As<Behaviours.StartChemistryStationBehaviour>(behaviour) != null) return TaskTimes.ChemistryStation;
            if (GameAccess.As<Behaviours.StartLabOvenBehaviour>(behaviour) != null) return TaskTimes.LabOvenLoad;
            if (GameAccess.As<Behaviours.FinishLabOvenBehaviour>(behaviour) != null) return TaskTimes.LabOvenUnload;
            if (GameAccess.As<Behaviours.StartMixingStationBehaviour>(behaviour) != null) return TaskTimes.MixingStation;
            if (GameAccess.As<Behaviours.StartCauldronBehaviour>(behaviour) != null) return TaskTimes.Cauldron;
            if (GameAccess.As<Behaviours.PackagingStationBehaviour>(behaviour) != null) return TaskTimes.PackagingStation;
            if (GameAccess.As<Behaviours.BrickPressBehaviour>(behaviour) != null) return TaskTimes.BrickPress;
            if (GameAccess.As<Behaviours.StartDryingRackBehaviour>(behaviour) != null) return TaskTimes.DryingRack;
            if (GameAccess.As<Behaviours.UseSpawnStationBehaviour>(behaviour) != null) return TaskTimes.SpawnStation;
            // Several kinds of worker run this one behaviour; each has a setting of its own for it.
            if (GameAccess.As<Behaviours.MoveItemBehaviour>(behaviour) != null) return TaskTimes.MoveItemFor(GameAccess.KindOf(employee));
            return null;
        }

        /// <summary>
        /// A botanist is about to work on a grow task. The game's timer is `GetActionDuration() / work
        /// speed`, read in the coroutine step right after OnStartPerformAction, so the multiplier is
        /// held from this hook to OnStopPerformAction: the one window in which the timer is read. The
        /// duration method itself is a constant-returning stub that cannot be detoured safely on IL2CPP
        /// (the compiler folds the stubs of several tasks into one function).
        /// </summary>
        public static void BeginTask(Behaviours.GrowContainerBehaviour task)
        {
            Employee employee = GameAccess.AsEmployee(task.Npc);
            if (employee != null) Begin(task, employee, TimeOfGrowTask(task));
        }

        /// <summary>The task is over (finished, or interrupted: the game also calls its stop hook when it cancels), so the multiplier is dropped.</summary>
        public static void EndTask(Behaviours.GrowContainerBehaviour task)
        {
            Employee employee = GameAccess.AsEmployee(task.Npc);
            if (employee != null) End(task, employee, TimeOfGrowTask(task), false);
        }

        /// <summary>
        /// Any other behaviour became the one the worker is doing (or resumed). These have no start and
        /// stop hook of their own to wait for the timer, so the multiplier is held for as long as the
        /// behaviour is active, the walk to the station included. Called for every behaviour of every
        /// NPC, so it leaves quickly for anything that is not a worker or not a timed task.
        /// </summary>
        public static void BeginBehaviour(Behaviours.Behaviour behaviour)
        {
            // The botanist's grow tasks have their own, more exact, hooks above.
            if (GameAccess.As<Behaviours.GrowContainerBehaviour>(behaviour) != null) return;
            Employee employee = GameAccess.AsEmployee(behaviour.Npc);
            if (employee != null) Begin(behaviour, employee, TimeOfBehaviour(behaviour, employee));
        }

        /// <summary>The behaviour ended or was paused (a resume starts the hold again), so the multiplier is dropped.</summary>
        public static void EndBehaviour(Behaviours.Behaviour behaviour)
        {
            if (GameAccess.As<Behaviours.GrowContainerBehaviour>(behaviour) != null) return;
            Employee employee = GameAccess.AsEmployee(behaviour.Npc);
            if (employee != null) End(behaviour, employee, TimeOfBehaviour(behaviour, employee), true);
        }

        private static void Begin(Behaviours.Behaviour behaviour, Employee employee, TaskTime time)
        {
            if (time == null || employee.WorkSpeedController == null) return;

            // Drop any entry a task that never reached its stop call left behind.
            ClearTaskSpeed(employee);
            if (ModLog.Level >= 2) _taskStarts[GameAccess.IdOf(behaviour)] = Time.time;

            if (!time.IsSet)
            {
                ModLog.Trace("Task start " + Stamp(employee) + ": " + time.Name + ", not timed (no time configured).");
                return;
            }

            float factor = time.Factor;
            if (factor == 1f)
            {
                ModLog.Trace("Task start " + Stamp(employee) + ": " + time.Name + ", configured time equals the game's (" + time.GameSeconds + " s): no change.");
                return;
            }

            employee.WorkSpeedController.Add(new FloatStack.StackEntry(TaskSpeedLabel, factor, FloatStack.EStackMode.Multiplicative, 100));
            ModLog.Verbose(GameAccess.KindOf(employee) + " task (" + time.Name + "): work speed x" + factor + ".");
            ModLog.Trace("Task start " + Stamp(employee) + ": " + time.Name + ", " + time.GameSeconds + " s -> " + time.Configured + " s, work speed x" + factor + " held.");
        }

        private static void End(Behaviours.Behaviour behaviour, Employee employee, TaskTime time, bool wholeBehaviour)
        {
            if (time == null || employee.WorkSpeedController == null) return;

            bool released = ClearTaskSpeed(employee);
            if (ModLog.Level < 2) return;

            // Game time between the start and stop calls. For a botanist's grow task that is the wait
            // itself plus up to a frame, so it should land near the configured seconds (times the
            // quantity for harvests); a shorter figure means it was interrupted. For the other tasks it
            // spans the whole behaviour, walking and fixed pauses included: compare runs with the
            // setting on and off.
            string span = "";
            float started;
            long id = GameAccess.IdOf(behaviour);
            if (_taskStarts.TryGetValue(id, out started))
            {
                string seconds = (Time.time - started).ToString("0.00");
                span = wholeBehaviour ? ", active " + seconds + " s of game time (includes walking)" : ", ran " + seconds + " s of game time";
                _taskStarts.Remove(id);
            }
            ModLog.Trace("Task stop " + Stamp(employee) + ": " + time.Name + span + (released ? ", work speed restored." : ", nothing to restore."));
        }

        /// <summary>Removes the mod's work speed entry; true when there was one.</summary>
        private static bool ClearTaskSpeed(Employee employee)
        {
            FloatStack.StackEntry held;
            if (!employee.WorkSpeedController.TryGetEntry(TaskSpeedLabel, out held)) return false;
            employee.WorkSpeedController.Remove(TaskSpeedLabel);
            return true;
        }

        // Game time (Time.time) at which each task began, keyed by behaviour. Filled at verbosity 2 only.
        private static readonly Dictionary<long, float> _taskStarts = new Dictionary<long, float>();

        /// <summary>
        /// Who and when, for the trace lines: wall clock, game seconds since launch, and the worker's
        /// name with the game's id for them (the id is what distinguishes two workers with the same name,
        /// and pairs a start line with its stop line). Empty below verbosity 2, as nobody reads it then.
        /// </summary>
        private static string Stamp(NPC npc)
        {
            if (ModLog.Level < 2) return "";
            return "[" + System.DateTime.Now.ToString("HH:mm:ss.fff") + ", game t=" + Time.time.ToString("0.00") + "] "
                + npc.FirstName + " " + npc.LastName + " (" + npc.ID + ")";
        }
    }
}
