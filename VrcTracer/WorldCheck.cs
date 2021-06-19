using System;
using System.Collections;
using Il2CppSystem.Collections.Generic;

using MelonLoader;
using UnityEngine;

using VRC.Core;
namespace VrcTracer
{
    // Stolen from https://github.com/Psychloor/PlayerRotater/blob/0b30e04cf85fdab769f6e0afc020e6d9bc9900ac/PlayerRotater/Utilities.cs#L76
    class WorldCheck
    {
        private static bool alreadyCheckingWorld;
        private static Dictionary<string, bool> checkedWorlds = new Dictionary<string, bool>();

        internal static IEnumerator CheckWorld()
        {
            if (alreadyCheckingWorld)
            {
                MelonLogger.Error("Attempted to check for world multiple times");
                yield break;
            }

            alreadyCheckingWorld = true;

            string worldId = RoomManager.field_Internal_Static_ApiWorld_0.id;

            if (checkedWorlds.ContainsKey(worldId))
            {
                MainClass.ForceDisable = checkedWorlds[worldId];
                yield break;
            }

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
                        MainClass.ForceDisable = true;
                        checkedWorlds.Add(worldId, true);
                        alreadyCheckingWorld = false;
                        yield break;

                    case "denied":
                        MainClass.ForceDisable = false;
                        checkedWorlds.Add(worldId, false);
                        alreadyCheckingWorld = false;
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
                                    MainClass.ForceDisable = false;
                                    checkedWorlds.Add(worldId, false);
                                    alreadyCheckingWorld = false;
                                    return;
                                }
                            MainClass.ForceDisable = true;
                            checkedWorlds.Add(worldId, true);
                            alreadyCheckingWorld = false;
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
