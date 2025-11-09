using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace ArchipelagoRandomizer;

internal class NewHorizonsPatches {
    public static bool IsWarping { get; protected set; } = false;

    protected static bool CheckIfLoaded() => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "NewHorizons");
    protected static MethodBase GetMethod(string typeName, string methodName) => Type.GetType($"{typeName}, NewHorizons").GetMethod(methodName);
    protected static MethodBase GetMethod(string typeName, string methodName, BindingFlags flags) => Type.GetType($"{typeName}, NewHorizons").GetMethod(methodName, flags);
}

[HarmonyPatch]
internal class WarpOutPatch : NewHorizonsPatches {
    [HarmonyPrepare]
    private static bool Prepare() => CheckIfLoaded();

    [HarmonyTargetMethod]
    private static MethodBase Target() => GetMethod("NewHorizons.Components.Ship.ShipWarpController", "WarpOut");

    [HarmonyPostfix]
    private static void ShipWarpController_WarpOut() {
        APRandomizer.OWMLModConsole.WriteLine("ShipWarpController_WarpOut called", OWML.Common.MessageType.Success);
        IsWarping = true;
    }
}

[HarmonyPatch]
internal class FinishWarpInPatch : NewHorizonsPatches {
    [HarmonyPrepare]
    private static bool Prepare() => CheckIfLoaded();

    [HarmonyTargetMethod]
    private static MethodBase Target() => GetMethod("NewHorizons.Components.Ship.ShipWarpController", "FinishWarpIn");

    [HarmonyPostfix]
    private static void ShipWarpController_FinishWarpIn() {
        APRandomizer.OWMLModConsole.WriteLine("ShipWarpController_FinishWarpIn called", OWML.Common.MessageType.Success);
        IsWarping = false;
    }
}
