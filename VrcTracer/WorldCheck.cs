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

namespace VrcTracer
{
    // Mostly stolen from https://github.com/Psychloor/PlayerRotater/blob/0b30e04cf85fdab769f6e0afc020e6d9bc9900ac/PlayerRotater/Utilities.cs#L76
    class WorldCheck
    {

        #region ModPatch
        // Also stolen from player rotator

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FadeTo(IntPtr instancePtr, IntPtr fadeNamePtr, float fade, IntPtr actionPtr, IntPtr stackPtr);

        private static FadeTo origFadeTo;
        private static void FadeToPatch(IntPtr instancePtr, IntPtr fadeNamePtr, float fade, IntPtr actionPtr, IntPtr stackPtr)
        {
            if (instancePtr == IntPtr.Zero) return;
            origFadeTo(instancePtr, fadeNamePtr, fade, actionPtr, stackPtr);

            if (!IL2CPP.Il2CppStringToManaged(fadeNamePtr).Equals("BlackFade", StringComparison.Ordinal)
                || !fade.Equals(0f)
                || RoomManager.field_Internal_Static_ApiWorldInstance_0 == null) return;

            MainClass.OnWorldJoined();
        }

        internal static bool PatchMethods()
        {
            try
            {
                // Faded to and joined and initialized room
                MethodInfo fadeMethod = typeof(VRCUiManager).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).First(
                    m => m.Name.StartsWith("Method_Public_Void_String_Single_Action_")
                         && m.Name.IndexOf("PDM", StringComparison.OrdinalIgnoreCase) == -1
                         && m.GetParameters().Length == 3);
                origFadeTo = Patch<FadeTo>(fadeMethod, GetDetour(nameof(FadeToPatch)));
            }
            catch (Exception e)
            {
                MelonLogger.Error("Failed to patch FadeTo\n" + e.Message);
                return false;
            }

            return true;
        }

        private static unsafe TDelegate Patch<TDelegate>(MethodBase originalMethod, IntPtr patchDetour)
        {
            IntPtr original = *(IntPtr*)UnhollowerSupport.MethodBaseToIl2CppMethodInfoPointer(originalMethod);
            MelonUtils.NativeHookAttach((IntPtr)(&original), patchDetour);
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(original);
        }

        private static IntPtr GetDetour(string name)
        {
            return typeof(WorldCheck).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).MethodHandle.GetFunctionPointer();
        }

        #endregion

        private static bool alreadyCheckingWorld;
        private static Dictionary<string, bool> checkedWorlds = new Dictionary<string, bool>();

        internal static IEnumerator CheckWorld()
        {
            if (alreadyCheckingWorld)
            {
                MelonLogger.Error("Attempted to check for world multiple times");
                yield break;
            }

            var worldId = RoomManager.field_Internal_Static_ApiWorld_0.id;

            if (checkedWorlds.ContainsKey(worldId))
            {
                MainClass.NotForceDisable = checkedWorlds[worldId];
                MelonLogger.Msg($"Using cached check {checkedWorlds[worldId]} for world '{worldId}'");
                yield break;
            }

            alreadyCheckingWorld = true;

            // Check if black/whitelisted from EmmVRC - thanks Emilia and the rest of EmmVRC Staff
            var www = new WWW($"https://dl.emmvrc.com/riskyfuncs.php?worldid={worldId}", null, new Dictionary<string, string>());

            while (!www.isDone)
                yield return new WaitForEndOfFrame();

            var result = www.text?.Trim().ToLower();
            www.Dispose();
            if (!string.IsNullOrWhiteSpace(result))
                switch (result)
                {
                    case "allowed":
                        MainClass.NotForceDisable = true;
                        checkedWorlds.Add(worldId, true);
                        alreadyCheckingWorld = false;
                        MelonLogger.Msg($"EmmVRC allows world '{worldId}'");
                        yield break;

                    case "denied":
                        MainClass.NotForceDisable = false;
                        checkedWorlds.Add(worldId, false);
                        alreadyCheckingWorld = false;
                        MelonLogger.Msg($"EmmVRC denies world '{worldId}'");
                        yield break;
                }

            // no result from server or they're currently down
            // Check tags then. should also be in cache as it just got downloaded
            API.Fetch<ApiWorld>(
                worldId,
                new Action<ApiContainer>(
                    container =>
                    {
                        ApiWorld apiWorld;
                        if ((apiWorld = container.Model.TryCast<ApiWorld>()) != null)
                        {
                            foreach (var worldTag in apiWorld.tags)
                                if (worldTag.IndexOf("game", StringComparison.OrdinalIgnoreCase) != -1
                                    || worldTag.IndexOf("club", StringComparison.OrdinalIgnoreCase) != -1)
                                {
                                    MainClass.NotForceDisable = false;
                                    checkedWorlds.Add(worldId, false);
                                    alreadyCheckingWorld = false;
                                    MelonLogger.Msg($"Found game or club tag in world world '{worldId}'");
                                    return;
                                }
                            MainClass.NotForceDisable = true;
                            checkedWorlds.Add(worldId, true);
                            alreadyCheckingWorld = false;
                            MelonLogger.Msg($"Found no game or club tag in world world '{worldId}'");
                        }
                        else
                        {
                            MelonLogger.Error("Failed to cast ApiModel to ApiWorld");
                        }
                    }),
                disableCache: false);
        }
    }
}
