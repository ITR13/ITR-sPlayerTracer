using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnhollowerBaseLib;
using UnityEngine;
using VRChatUtilityKit.Ui;

namespace VrcTracer
{
    public static class VrChatUtilityKitHelper
    {
        private const string ResPath = "VrcTracer.res.";

        private const string TtiPath = ResPath + "TracerTab.png";
        private const string SbiPath = ResPath + "Settings.png";
        private const string DbiPath = ResPath + "Directory.png";
        private const string WbiPath = ResPath + "Website.png";
        private const string RbiPath = ResPath + "Refresh.png";

        private const string LtiPath = ResPath + "Locked.png";
        private const string UtiPath = ResPath + "Unlocked.png";
        private const string OffImgPath = ResPath + "Off.png";
        private const string OnImgPath = ResPath + "On.png";

        private static readonly Lazy<Assembly> Assembly =
            new Lazy<Assembly>(System.Reflection.Assembly.GetCallingAssembly);

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
            var tabSprite = LoadEmbeddedSprite(TtiPath);
            var settingsSprite = LoadEmbeddedSprite(SbiPath);
            var folderSprite = LoadEmbeddedSprite(DbiPath); 
            var websiteSprite = LoadEmbeddedSprite(WbiPath);

            var lockedSprite = LoadEmbeddedSprite(LtiPath);
            var unlockedSprite = LoadEmbeddedSprite(UtiPath);
            var onSprite = LoadEmbeddedSprite(OnImgPath);
            var offSprite = LoadEmbeddedSprite(OffImgPath);

            var refreshSprite = LoadEmbeddedSprite(RbiPath);

            var tabButton = new TabButton(
                tabSprite,
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
                onSprite,
                offSprite,
                "Toggle Tracers",
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
                lockedSprite,
                unlockedSprite,
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
                    "Tracers",
                    new List<IButtonGroupElement>
                    {
                        enabledToggle,
                        lockedToggle,
                        new SingleButton(
                            UpdateMode,
                            refreshSprite,
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
                    "Settings",
                    new List<IButtonGroupElement>
                    {
                        new SingleButton(
                            ConfigWatcher.OpenConfig,
                            settingsSprite,
                            "Open Config",
                            "OpenConfigButton",
                            "Opens the config in your default program"
                        ),
                        new SingleButton(
                            ConfigWatcher.OpenConfigFolder,
                            folderSprite,
                            "Open UserData",
                            "OpenConfigFolderButton",
                            "Opens the folder containing the config"
                        ),
                        new SingleButton(
                            ConfigWatcher.OpenReadMe,
                            websiteSprite,
                            "Open ReadMe",
                            "OpenConfigButton",
                            "Opens the readme of the tracer config"
                        )
                    }
                )
            );

            tabButton.gameObject.SetActive(!ConfigWatcher.TracerConfig.hideMenuTab);
            MainClass.ConfigUpdated += () => tabButton.gameObject.SetActive(!ConfigWatcher.TracerConfig.hideMenuTab);
        }


        // Stolen from https://github.com/Nirv-git/VRCMods/blob/077165ae07067b13e7bb3a7261030663af44338d/GestureIndicator/LoadAssets.cs#L22
        private static Sprite LoadEmbeddedSprite(string path)
        {
            try
            {
                //Load image into Texture
                using var assetStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
                using var tempStream = new MemoryStream((int)assetStream.Length);
                assetStream.CopyTo(tempStream);
                var Texture2 = new Texture2D(2, 2);
                ImageConversion.LoadImage(Texture2, tempStream.ToArray());
                Texture2.name = path.Replace(".png", "") + "-Tex";
                Texture2.wrapMode = TextureWrapMode.Clamp;
                Texture2.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                //Texture to Sprite
                var rec = new Rect(0.0f, 0.0f, Texture2.width, Texture2.height);
                var piv = new Vector2(.5f, 5f);
                var border = Vector4.zero;
                var s = Sprite.CreateSprite_Injected(Texture2, ref rec, ref piv, 100.0f, 0, SpriteMeshType.Tight, ref border, false);
                s.name = path.Replace(".png", "") + "-Sprite";
                s.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                return s;
            }
            catch (System.Exception ex) { MainClass.Error("Failed to load image: " + path + "\n" + ex.ToString()); return null; }
        }
    }
}