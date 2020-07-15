
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
        private bool _forceUpdate;

        public override void OnApplicationQuit()
        {
            ConfigWatcher.Unload();
        }

        public override void OnLevelWasInitialized(int level)
        {
            _forceUpdate = true;
        }

        public override void OnUpdate()
        {
            var updated = ConfigWatcher.UpdateIfDirty() || _forceUpdate;
            var shouldChangeMode = ShouldChangeMode();
            _forceUpdate = false;

            if (updated || shouldChangeMode)
            {
                TracerToUser.DestroyAllTracers();
            }

            if (shouldChangeMode)
            {
                ChangeMode();
            }

            if ((updated || shouldChangeMode) && _tracerMode != TracerMode.Off)
            {
                CreateTracers();
            }
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
            MelonModLogger.Log("Creating tracers");
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

            var color = (Color)ConfigWatcher.TracerConfig.blockedColor;
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
                return ConfigWatcher.TracerConfig.errorColor;
            }

            var framesPanel = profileCanvas.FindChild("Frames");
            if (framesPanel == null)
            {
                logs.Add($"W: User had no frames panel: {ChildNames(profileCanvas)}");
                return ConfigWatcher.TracerConfig.errorColor;
            }

            var nameplatePanel = framesPanel.FindChild("Panel - NamePlate");
            if (nameplatePanel == null)
            {
                logs.Add($"W: User had no nameplate panel: {ChildNames(framesPanel)}");
                return ConfigWatcher.TracerConfig.errorColor;
            }

            var image = nameplatePanel.GetComponent<Image>();
            if (image == null)
            {
                logs.Add($"W: User frames panel had no image: {ChildNames(nameplatePanel)}");
                return ConfigWatcher.TracerConfig.errorColor;
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
