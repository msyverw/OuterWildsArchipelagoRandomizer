using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace ArchipelagoRandomizer;

internal class NewHorizonsPatches {
    public static bool IsWarping { get; protected set; } = false;

    private static bool diedInOtherSystem = false;
    public static bool DiedInOtherSystem {
        get {
            bool temp = diedInOtherSystem;
            diedInOtherSystem = false; // Reset back to false when checked
            return temp;
        }
        protected set => diedInOtherSystem |= value; // Don't change true to false here in case multiple deaths happen back-to-back
    }

    public static bool DelaySpawn { get; set; } = false;

    protected static bool CheckIfLoaded() => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "NewHorizons");
    protected static MethodBase GetMethod(string prefix, string typeName, string methodName) => GetMethod($"{prefix}.{typeName}", methodName);
    protected static MethodBase GetMethod(string typeName, string methodName) => Type.GetType($"{typeName}, NewHorizons").GetMethod(methodName);
    protected static MethodBase GetMethod(string typeName, string methodName, BindingFlags flags) => Type.GetType($"{typeName}, NewHorizons").GetMethod(methodName, flags);
}

[HarmonyPatch]
internal class WarpOutPatch : NewHorizonsPatches {
    private const string Namespace = "NewHorizons.Components.Ship";
    private const string Classname = "ShipWarpController";
    private const string Method = "WarpOut";

    [HarmonyPrepare]
    private static bool Prepare() => CheckIfLoaded();

    [HarmonyTargetMethod]
    private static MethodBase Target() => GetMethod(Namespace, Classname, Method);

    [HarmonyPostfix]
    private static void ShipWarpController_WarpOut() {
        APRandomizer.OWMLModConsole.WriteLine($"{Classname}_{Method} called", OWML.Common.MessageType.Success);
        IsWarping = true;
    }
}

[HarmonyPatch]
internal class FinishWarpInPatch : NewHorizonsPatches {
    private const string Namespace = "NewHorizons.Components.Ship";
    private const string Classname = "ShipWarpController";
    private const string Method = "FinishWarpIn";

    [HarmonyPrepare]
    private static bool Prepare() => CheckIfLoaded();

    [HarmonyTargetMethod]
    private static MethodBase Target() => GetMethod(Namespace, Classname, Method);

    [HarmonyPostfix]
    private static void ShipWarpController_FinishWarpIn() {
        APRandomizer.OWMLModConsole.WriteLine($"{Classname}_{Method} called", OWML.Common.MessageType.Success);
        IsWarping = false;
    }
}

[HarmonyPatch(typeof(DeathManager), nameof(DeathManager.KillPlayer))]
internal class KillPlayerPatch : NewHorizonsPatches {
    [HarmonyPrepare]
    private static bool Prepare() => CheckIfLoaded();

    [HarmonyPrefix, HarmonyPriority(Priority.High)]
    private static void DeathManager_KillPlayer() {
        APRandomizer.OWMLModConsole.WriteLine($"DeathManager_KillPlayer called; in other system: {!APRandomizer.IsVanillaSystemLoaded()} (current system: {APRandomizer.NewHorizonsAPI.GetCurrentStarSystem()})", OWML.Common.MessageType.Success);
        DiedInOtherSystem = !APRandomizer.IsVanillaSystemLoaded();
    }
}

[HarmonyPatch]
internal class OnSystemReadyPatch : NewHorizonsPatches {
    private const string Namespace = "NewHorizons";
    private const string Classname = "Main";
    private const string Method = "OnSystemReady";

    [HarmonyPrepare]
    private static bool Prepare() => CheckIfLoaded();

    [HarmonyTargetMethod]
    private static MethodBase Target() => GetMethod($"{Namespace}.{Classname}", Method, BindingFlags.NonPublic | BindingFlags.Instance);

    [HarmonyPostfix]
    private static void Main_OnSystemReady() {
        APRandomizer.OWMLModConsole.WriteLine($"{Classname}_{Method} called", OWML.Common.MessageType.Success);
        if (DelaySpawn) {
            DelaySpawn = false;
            APRandomizer.OWMLModConsole.WriteLine($"{Classname}_{Method} spawning the player because they died elsewhere", OWML.Common.MessageType.Success);
            Spawn.SpawnPlayer();
        }
    }
}
