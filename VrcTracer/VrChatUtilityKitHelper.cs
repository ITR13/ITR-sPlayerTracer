using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MelonLoader;
using VRChatUtilityKit.Ui;

namespace VrcTracer
{
    public static class VrChatUtilityKitHelper
    {
        public static void Init()
        {
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddButtons()
        {
            var tabButton = new TabButton(
                null,
                "Tracers",
                "TracersTabButton",
                "ITR's Vrc Tracer",
                "Various buttons for when you don't have a keyboard easily accessible"
            );


            var locked = false;
            var enabled = false;

            void UpdateMode()
            {
                if (!enabled) MainClass.DisableTracers();
                else if (locked) MainClass.LockTracers();
                else MainClass.EnableTracers();
            }

            var enabledToggle = new ToggleButton(
                isOn =>
                {
                    enabled = isOn;
                    UpdateMode();
                },
                null,
                null,
                "Toggle tracers",
                "ToggleTracerButton",
                "Enables tracers",
                "Disables all tracers"
            );

            var lockedToggle = new ToggleButton(
                isOn =>
                {
                    locked = isOn;
                    UpdateMode();
                },
                null,
                null,
                "Lock Tracers",
                "ToggleTracerButton",
                "Locks the origin point of the tracer to your current position",
                "Makes tracers follow your position"
            );

            MainClass.TracerModeChanged += mode =>
            {
                switch (mode)
                {
                    case MainClass.TracerMode.Follow:
                        enabled = true;
                        locked = false;
                        break;
                    case MainClass.TracerMode.Stick:
                        enabled = true;
                        locked = true;
                        break;
                    case MainClass.TracerMode.Off:
                        enabled = false;
                        break;
                }
                enabledToggle.ToggleComponent.SetIsOnWithoutNotify(enabled);
                lockedToggle.ToggleComponent.SetIsOnWithoutNotify(locked);
            };

            tabButton.SubMenu.AddButtonGroup(
                new ButtonGroup(
                    "Tracers",
                    "Modify the state of tracer",
                    new List<IButtonGroupElement>
                    {
                        enabledToggle,
                        lockedToggle,
                        new SingleButton(
                            UpdateMode,
                            null,
                            "Force Update",
                            "UpdateLabel",
                            "Recreates missing tracers for new people that have joined"
                        )
                    }
                )
            );

            tabButton.SubMenu.AddButtonGroup(
                new ButtonGroup(
                    "Settings",
                    "Easy access to tracer settings",
                    new List<IButtonGroupElement>
                    {
                        new SingleButton(
                            ConfigWatcher.OpenConfig,
                            null,
                            "Open config",
                            "OpenConfigButton",
                            "Opens the config in your default program"
                        ),
                        new SingleButton(
                            ConfigWatcher.OpenConfigFolder,
                            null,
                            "Open UserData",
                            "OpenConfigFolderButton",
                            "Opens the folder containing the config"
                        ),
                        new SingleButton(
                            ConfigWatcher.OpenReadMe,
                            null,
                            "Open ReadMe",
                            "OpenConfigButton",
                            "Opens the readme of the tracer config"
                        )
                    }
                )
            );
        }
    }
}