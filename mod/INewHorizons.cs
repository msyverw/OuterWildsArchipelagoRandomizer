﻿using OWML.Common;
using UnityEngine.Events;

namespace ArchipelagoRandomizer;

public interface INewHorizons
{
    string GetCurrentStarSystem();
    bool SetDefaultSystem(string name);
    UnityEvent<string> GetStarSystemLoadedEvent();
    void DefineStarSystem(string name, string config, IModBehaviour mod);
}
