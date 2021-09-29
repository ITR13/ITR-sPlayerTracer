using System;
using System.IO;
using MelonLoader;
using MelonLoader.TinyJSON;

namespace VrcTracer
{
    public static class ConfigWatcher
    {
        private const string FileName = "TracerConfig.json";

        private static readonly string FileDirectory = Path.Combine(
            Environment.CurrentDirectory,
            "UserData"
        );

        private static readonly string FullPath = Path.Combine(
            FileDirectory,
            FileName
        );

        public static TracerConfig TracerConfig = new TracerConfig();

        private static readonly FileSystemWatcher FileSystemWatcher;
        private static bool _dirty;

        static ConfigWatcher()
        {
            FileSystemWatcher = new FileSystemWatcher(FileDirectory, FileName)
            {
                NotifyFilter = (NotifyFilters) ((1 << 9) - 1),
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

        public static bool UpdateIfDirty()
        {
            if (!_dirty) return false;
            _dirty = false;

            if (!File.Exists(FullPath))
            {
                MelonLogger.Msg(
                    $"Creating default config file at \"{FullPath}\""
                );

                var json = JSON.Dump(
                    new TracerConfig(),
                    EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints
                );
                File.WriteAllText(FullPath, json);
            }

            if (TracerConfig.verbosity > 2) MelonLogger.Msg("Updating Tracer configs");

            TracerConfig = null;

            try
            {
                var json = File.ReadAllText(FullPath);
                JSON.MakeInto(JSON.Load(json), out TracerConfig);
            }
            catch (Exception e)
            {
                MelonLogger.Error(e.ToString());
                MelonLogger.Msg(
                    "Something went wrong when deserializing json. Check the ReadMe in case something has changed"
                );
            }

            TracerConfig = TracerConfig ?? new TracerConfig();

            return true;
        }
    }
}