﻿using System;
using System.Collections.Generic;
using System.Text;
using MelonLoader;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase;
using VRChatUtilityKit.Utilities;
using StringList = System.Collections.Generic.List<string>;

namespace VrcTracer
{
    public class MainClass : MelonMod
    {
        public static Action<string> Msg { get; private set; }
        public static Action<string> Warning { get; private set; }
        public static Action<string> Error { get; private set; }

        private static bool _forceUpdate;
        public static bool NotForceDisable;

        private TracerMode _tracerMode;

        public static Action DisableTracers { get; private set; }
        public static Action EnableTracers { get; private set; }
        public static Action LockTracers { get; private set; }

        public static event Action<TracerMode> TracerModeChanged;
        public static event Action ConfigUpdated;

        public override void OnApplicationStart()
        {
            Msg = LoggerInstance.Msg;
            Warning = LoggerInstance.Warning;
            Error = LoggerInstance.Error;

            DisableTracers = () =>
            {
                if(ConfigWatcher.TracerConfig.verbosity >= 2) MainClass.Msg("Turned off tracers");
                ForceSetMode(TracerMode.Off);
            };
            EnableTracers = () => ForceSetMode(TracerMode.Follow);
            LockTracers = () => ForceSetMode(TracerMode.Stick);


            VRCUtils.OnUiManagerInit += VrChatUtilityKitHelper.Init;
            WorldCheck.Init();
        }

        public override void OnApplicationQuit()
        {
            ConfigWatcher.Unload();
        }

        public override void OnUpdate()
        {
            if (!NotForceDisable) return;

            var updated = ConfigWatcher.UpdateIfDirty() || _forceUpdate;
            var shouldChangeMode = ShouldChangeMode();
            _forceUpdate = false;

            if (updated || shouldChangeMode) TracerToUser.DestroyAllTracers();

            if (shouldChangeMode) ChangeMode();

            if ((updated || shouldChangeMode) && _tracerMode != TracerMode.Off) CreateTracers();

            if (updated) ConfigUpdated?.Invoke();
        }

        private bool ShouldChangeMode()
        {
            var trigger = ConfigWatcher.TracerConfig.trigger;
            var hold = ConfigWatcher.TracerConfig.hold;
            if (trigger == KeyCode.None) return false;
            if (hold != KeyCode.None && !Input.GetKey(hold)) return false;

            return Input.GetKeyDown(trigger);
        }

        private void ChangeMode()
        {
            switch (_tracerMode)
            {
                case TracerMode.Off:
                    _tracerMode = TracerMode.Follow;
                    break;
                case TracerMode.Follow:
                    _tracerMode = TracerMode.Stick;
                    break;
                case TracerMode.Stick:
                default:
                    _tracerMode = TracerMode.Off;
                    break;
            }

            TracerModeChanged?.Invoke(_tracerMode);
        }

        private void ForceSetMode(TracerMode tracerMode)
        {
            if (!NotForceDisable) return;

            TracerToUser.DestroyAllTracers();
            _tracerMode = tracerMode;
            TracerModeChanged?.Invoke(_tracerMode);

            if (_tracerMode != TracerMode.Off) CreateTracers();
        }

        public override void OnLateUpdate()
        {
            switch (_tracerMode)
            {
                case TracerMode.Follow:
                    if (!PlayerMarker.UpdatePosition(true)) return;
                    break;
                case TracerMode.Stick:
                    if (!PlayerMarker.UpdatePosition(false)) return;
                    break;
                default:
                    return;
            }

            TracerToUser.LateUpdate();
        }

        private void CreateTracers()
        {
            if (ConfigWatcher.TracerConfig.verbosity >= 2) MainClass.Msg("Creating tracers");
            TracerToUser.TracerMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            var log = new StringList();
            foreach (var avatarDescriptor in AllDescriptors()) AddTracer(avatarDescriptor.gameObject, log);

            if (log.Count > 0 && ConfigWatcher.TracerConfig.verbosity >= 3)
            {
                log.Insert(0, "Tracer creation log");
                MainClass.Msg(string.Join("\n", log.ToArray()));
            }
        }

        private IEnumerable<VRC_AvatarDescriptor> AllDescriptors()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                foreach (var rootObject in scene.GetRootGameObjects())
                {
                    var ad = rootObject.GetComponentsInChildren<VRC_AvatarDescriptor>(true);
                    foreach (var avatarDescriptor in ad) yield return avatarDescriptor;
                }
            }
        }

        private void AddTracer(GameObject gameObject, StringList logs)
        {
            var parent = gameObject.transform.parent;
            if (parent == null)
            {
                logs.Add($"E: Parent of {gameObject.name} is null!");
                return;
            }

            var grandParent = parent.parent;
            if (grandParent == null)
            {
                logs.Add($"E: Grandparent of {gameObject.name} and parent of {grandParent.gameObject.name} is null!");
                return;
            }

            var playerObjectName = grandParent.gameObject.name;
            var username = GetName(grandParent, logs);
            if (playerObjectName.StartsWith("VRCPlayer[Local]"))
            {
                logs.Add($"Found local user: {username}");
                PlayerMarker.Player = gameObject;
                return;
            }

            var color = (Color) ConfigWatcher.TracerConfig.blockedColor;
            if (gameObject.name != "avatar_invisible(Clone)")
            {
                logs.Add($"Found remote user: {username}");
                color = GetColor(grandParent, logs);
            }
            else
            {
                logs.Add($"Found blocked user: {username}");
            }

            var child = new GameObject($"Tracer #{TracerToUser.Count}");
            child.transform.parent = gameObject.transform;
            child.transform.localPosition = Vector3.zero;
            var tracerToUser = new TracerToUser(child) {Color = color};
        }

        private Color GetColor(Transform user, StringList logs)
        {
            var holder = GetChild(
                user,
                logs,
                "Player Nameplate",
                "Canvas",
                "Nameplate",
                "Contents",
                "Quick Stats",
                "Trust Text"
            );
            if (holder == null) return ConfigWatcher.TracerConfig.errorColor;

            var text = holder.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                logs.Add("W: Found Trust Text, but found not TextMeshProUGUI component");
                return ConfigWatcher.TracerConfig.errorColor;
            }

            return text.color;
        }

        private Transform GetChild(Transform root, StringList logs, params string[] childNames)
        {
            var current = root;
            var path = root.name;
            foreach (var childName in childNames)
            {
                path += $"/{childName}";
                var prev = current;
                current = current.FindChild(childName);

                if (current != null) continue;
                logs.Add($"W: User had no '{path}': {ChildNames(prev)}");
                return null;
            }

            return current;
        }

        private string GetName(Transform user, StringList logs)
        {
            var holder = GetChild(
                user,
                logs,
                "Player Nameplate",
                "Canvas",
                "Nameplate",
                "Contents",
                "Main",
                "Text Container",
                "Name"
            );
            if (holder == null) return "{null}";

            var text = holder.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                logs.Add("W: Found object with text, but found not TextMeshProUGUI component");
                return "{null}";
            }

            return text.text;
        }

        private string ChildNames(Transform transform)
        {
            if (transform.childCount == 0) return "";

            var sb = new StringBuilder();
            sb.Append(transform.GetChild(0).gameObject.name);
            for (var i = 1; i < transform.childCount; i++)
            {
                sb.Append(", ");
                sb.Append(transform.GetChild(i).gameObject.name);
            }

            return sb.ToString();
        }

        public enum TracerMode
        {
            Off = 0,
            Follow = 1,
            Stick = 2
        }
    }
}