﻿
using System.Text;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRC.SDKBase;
using StringList = System.Collections.Generic.List<string>;

namespace VrcTracer
{
    public class MainClass : MelonMod
    {
        private enum TracerMode
        {
            Off = 0,
            Follow = 1,
            Stick = 2,
        }

        private TracerMode _tracerMode;

        public override void OnApplicationQuit()
        {
            ConfigWatcher.Unload();
        }

        public override void OnUpdate()
        {
            ConfigWatcher.UpdateIfDirty();

            var trigger = ConfigWatcher.TracerConfig.trigger;
            var hold = ConfigWatcher.TracerConfig.hold;
            if (trigger == KeyCode.None) return;
            if (hold != KeyCode.None && !Input.GetKey(hold)) return;
            if (!Input.GetKeyDown(trigger)) return;

            var deleteCount = TracerToUser.DestroyAllTracers();
            if (deleteCount == 0)
            {
                _tracerMode = TracerMode.Off;
            }

            switch (_tracerMode)
            {
                case TracerMode.Off:
                    CreateTracers();
                    _tracerMode = TracerMode.Follow;
                    break;
                case TracerMode.Follow:
                    CreateTracers();
                    _tracerMode = TracerMode.Stick;
                    break;
                default:
                    _tracerMode = TracerMode.Off;
                    break;
            }
            MelonModLogger.Log($"TracerMode is now {_tracerMode}");
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
            TracerToUser.TracerMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
            var log = new StringList();
            foreach (var avatarDescriptor in AllDescriptors())
            {
                AddTracer(avatarDescriptor.gameObject, log);
            }

            if (log.Count > 0)
            {
                log.Insert(0, "Tracer creation log");
                MelonModLogger.Log(string.Join("\n", log));
            }
        }

        private System.Collections.Generic.IEnumerable<VRC_AvatarDescriptor> AllDescriptors()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                foreach (var rootObject in scene.GetRootGameObjects())
                {
                    var ad = rootObject.GetComponentsInChildren<VRC_AvatarDescriptor>(true);
                    foreach (var avatarDescriptor in ad)
                    {
                        yield return avatarDescriptor;
                    }
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

            var color = Color.red;
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
            var tracerToUser = new TracerToUser(child) { Color = color };
        }

        private Color GetColor(Transform user, StringList logs)
        {
            var profileCanvas = user.FindChild("Canvas - Profile (1)");
            if (profileCanvas == null)
            {
                logs.Add($"W: User had no profile canvas: {ChildNames(user)}");
                return Color.red;
            }

            var framesPanel = profileCanvas.FindChild("Frames");
            if (framesPanel == null)
            {
                logs.Add($"W: User had no frames panel: {ChildNames(profileCanvas)}");
                return Color.red;
            }

            var nameplatePanel = framesPanel.FindChild("Panel - NamePlate");
            if (nameplatePanel == null)
            {
                logs.Add($"W: User had no nameplate panel: {ChildNames(framesPanel)}");
                return Color.red;
            }

            var image = nameplatePanel.GetComponent<Image>();
            if (image == null)
            {
                logs.Add($"W: User frames panel had no image: {ChildNames(nameplatePanel)}");
                return Color.red;
            }

            return image.color;
        }

        private string GetName(Transform user, StringList logs)
        {
            var profileCanvas = user.FindChild("Canvas - Profile (1)");
            if (profileCanvas == null)
            {
                logs.Add($"W: User had no profile canvas: {ChildNames(user)}");
                return "{null}";
            }

            var tetPanel = profileCanvas.FindChild("Text");
            if (tetPanel == null)
            {
                logs.Add($"W: User had no text panel: {ChildNames(profileCanvas)}");
                return "{null}";
            }

            var nameplatePanel = tetPanel.FindChild("Text - NameTag");
            if (nameplatePanel == null)
            {
                logs.Add($"W: User had no nameplate panel: {ChildNames(tetPanel)}");
                return "{null}";
            }

            var text = nameplatePanel.GetComponent<Text>();
            if (text == null)
            {
                logs.Add($"W: User frames panel had no image: {ChildNames(nameplatePanel)}");
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
    }
}