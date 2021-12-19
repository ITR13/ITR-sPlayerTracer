using System.Linq;
using System.Runtime.CompilerServices;
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
                if (ConfigWatcher.TracerConfig.verbosity >= 1) MainClass.Msg("Found no Ui Expansion Kit");
                if (ConfigWatcher.TracerConfig.verbosity >= 3)
                {
                    MainClass.Msg(
                        "Found mods:\n" + string.Join("\n", MelonHandler.Mods.Select(mod => mod.Info.Name))
                    );
                }
                return;
            }

            if (ConfigWatcher.TracerConfig.verbosity >= 1) MainClass.Msg("Adding UiExpansion buttons");
            try
            {
                AddButtons();
            }
            catch (Exception e)
            {
                MainClass.Error(e.ToString());
            }
        }

        private static bool HasUiExpansionKit()
        {
            return MelonHandler.Mods.Any(mod => mod.Info.Name == "UI Expansion Kit");
        }

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