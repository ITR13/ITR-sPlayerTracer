using System;
using System.IO;
using MelonLoader;
using MelonLoader.TinyJSON;
using Tomlet;

namespace VrcTracer
{
    public static class ConfigWatcher
    {
        private const string FileName = "TracerConfig.toml";
        private const string OldFileName = "TracerConfig.json";

        private static readonly string FileDirectory = Path.Combine(
            Environment.CurrentDirectory,
            "UserData"
        );

        private static readonly string FullPath = Path.Combine(
            FileDirectory,
            FileName
        );

        private static readonly string OldFullPath = Path.Combine(
            FileDirectory,
            OldFileName
        );

        public static TracerConfig TracerConfig = new TracerConfig();

        private static readonly FileSystemWatcher FileSystemWatcher;
        private static bool _dirty;

        static ConfigWatcher()
        {
            TransferOldConfig();

            FileSystemWatcher = new FileSystemWatcher(FileDirectory, FileName)
            {
                NotifyFilter = (NotifyFilters)((1 << 9) - 1),
                EnableRaisingEvents = true
            };
            FileSystemWatcher.Changed += (_, __) => _dirty = true;
            FileSystemWatcher.Created += (_, __) => _dirty = true;
            FileSystemWatcher.Renamed += (_, __) => _dirty = true;
            FileSystemWatcher.Deleted += (_, __) => _dirty = true;
            _dirty = true;
        }

        public static void Unload()
        {
            FileSystemWatcher.EnableRaisingEvents = false;
            _dirty = false;
        }

        private static void TransferOldConfig()
        {
            if (!File.Exists(OldFullPath)) return;

            var movedOldFullPath = OldFullPath + ".old";

            if (File.Exists(movedOldFullPath))
            {
                File.Delete(movedOldFullPath);
            }


            File.Move(OldFullPath, movedOldFullPath);

            MelonLogger.Msg($"Found json config at \"{OldFullPath}\", converting to toml config");

            try
            {
                var json = File.ReadAllText(movedOldFullPath);
                JSON.MakeInto(JSON.Load(json), out TracerConfig);
            }
            catch (Exception e)
            {
                MelonLogger.Error(e.ToString());
                MelonLogger.Msg(
                    "Something went wrong when deserializing json. Check the ReadMe in case something has changed"
                );
                return;
            }

            try
            {
                MelonLogger.Msg(
                    $"Creating toml file based on old json file at \"{FullPath}\""
                );

                var toml = TomletMain.TomlStringFrom(TracerConfig);
                File.WriteAllText(FullPath, toml);
            }
            catch (Exception e)
            {
                MelonLogger.Error(e.ToString());
                MelonLogger.Msg(
                    "Something went wrong when serializing toml"
                );
            }
        }


        public static bool UpdateIfDirty()
        {
            if (!_dirty) return false;
            _dirty = false;

            if (!File.Exists(FullPath))
            {
                MelonLogger.Msg(
                    $"Creating default config file at \"{FullPath}\""
                );

                var toml = TomletMain.TomlStringFrom(new TracerConfig());
                File.WriteAllText(FullPath, toml);
            }

            if (TracerConfig.verbosity > 2) MelonLogger.Msg("Updating Tracer configs");

            TracerConfig = null;

            try
            {
                var toml = File.ReadAllText(FullPath);
                TracerConfig = TomletMain.To<TracerConfig>(toml);
            }
            catch (Exception e)
            {
                MelonLogger.Error(e.ToString());
                MelonLogger.Msg(
                    "Something went wrong when deserializing toml. Check the ReadMe in case something has changed"
                );
            }

            TracerConfig = TracerConfig ?? new TracerConfig();

            return true;
        }
    }
}