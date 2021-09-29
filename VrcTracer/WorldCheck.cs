using System;
using System.Collections;
using Il2CppSystem.Collections.Generic;

using MelonLoader;
using UnityEngine;

using VRC.Core;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnhollowerBaseLib;
using VRChatUtilityKit.Utilities;

namespace VrcTracer
{
    // Mostly stolen from https://github.com/loukylor/VRC-Mods/blob/main/TriggerESP/TriggerESPMod.cs#L82-L89
    static class WorldCheck
    {
        public static void Init()
        {
            VRCUtils.OnEmmWorldCheckCompleted += areRiskyFuncsAllowed =>
            {
                MainClass.NotForceDisable = areRiskyFuncsAllowed;
            };
        }
    }
}
