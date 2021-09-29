using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UIExpansionKit.API;

namespace VrcTracer
{
    public static class UIExpansionKitHelper
    {
        public static void Init()
        {
            if (!HasUiExpansionKit())
            {
                MelonLogger.Msg($"Found no Ui Expansion Kit");
                MelonLogger.Msg("Found mods:\n" + string.Join("\n", MelonHandler.Mods.Select(mod => mod.Info.Name)));
                return;
            }

            MelonLogger.Msg("Adding UiExpansion buttons");
            AddButtons();
        }

        private static bool HasUiExpansionKit() => 
            MelonHandler.Mods.Any(mod => mod.Info.Name == "UI Expansion Kit");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddButtons()
        {
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu)
                .AddSimpleButton("Disable Tracers", MainClass.DisableTracers);

            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu)
                .AddSimpleButton("Enable Tracers", MainClass.EnableTracers);

            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu)
                .AddSimpleButton("Lock Tracers", MainClass.LockTracers);
        }
    }
}
