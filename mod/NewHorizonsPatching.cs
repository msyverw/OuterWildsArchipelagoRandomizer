using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace ArchipelagoRandomizer;

internal class NewHorizonsPatching {
    protected static bool CheckIfLoaded() => AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "NewHorizons");
    protected static MethodBase GetMethod(string typeName, string methodName) => Type.GetType($"{typeName}, NewHorizons").GetMethod(methodName);
    protected static MethodBase GetMethod(string typeName, string methodName, BindingFlags flags) => Type.GetType($"{typeName}, NewHorizons").GetMethod(methodName, flags);
}

[HarmonyPatch]
internal class FinishWarpInPatching : NewHorizonsPatching {
    [HarmonyPrepare]
    private static bool Prepare() => CheckIfLoaded();

    [HarmonyTargetMethod]
    private static MethodBase Target() => GetMethod("NewHorizons.Components.Ship.ShipWarpController", "FinishWarpIn");

    [HarmonyPostfix]
    private static void ShipWarpController_FinishWarpIn() {
        APRandomizer.OWMLModConsole.WriteLine("ShipWarpController_FinishWarpIn called");
    }
}

[HarmonyPatch]
internal class MakeBlackHolePatching : NewHorizonsPatching {
    [HarmonyPrepare]
    private static bool Prepare() => CheckIfLoaded();

    [HarmonyTargetMethod]
    private static MethodBase Target() => GetMethod("NewHorizons.Components.Ship.ShipWarpController", "MakeBlackHole", BindingFlags.NonPublic | BindingFlags.Instance);

    [HarmonyPostfix]
    private static void ShipWarpController_MakeBlackHole() {
        APRandomizer.OWMLModConsole.WriteLine("ShipWarpController_MakeBlackHole called");
    }
}
