﻿using System;
using System.IO;
using MelonLoader.TinyJSON;
using Tomlet;
using UnityEngine;

namespace VrcTracer
{
    public static class ConfigWatcher
    {
        private const string ReadMeUrl = "https://github.com/ITR13/ITR-sPlayerTracer/blob/master/README.md";

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

            MainClass.Msg($"Found json config at \"{OldFullPath}\", converting to toml config");

            try
            {
                var json = File.ReadAllText(movedOldFullPath);
                JSON.MakeInto(JSON.Load(json), out TracerConfig);
            }
            catch (Exception e)
            {
                MainClass.Error(e.ToString());
                MainClass.Msg(
                    "Something went wrong when deserializing json. Check the ReadMe in case something has changed"
                );
                return;
            }

            try
            {
                MainClass.Msg(
                    $"Creating toml file based on old json file at \"{FullPath}\""
                );

                var toml = TomletMain.TomlStringFrom(TracerConfig);
                File.WriteAllText(FullPath, toml);
            }
            catch (Exception e)
            {
                MainClass.Error(e.ToString());
                MainClass.Msg(
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
                MainClass.Msg(
                    $"Creating default config file at \"{FullPath}\""
                );

                var toml = TomletMain.TomlStringFrom(new TracerConfig());
                File.WriteAllText(FullPath, toml);
            }

            if (TracerConfig.verbosity > 2) MainClass.Msg("Updating Tracer configs");

            TracerConfig = null;

            try
            {
                var toml = File.ReadAllText(FullPath);
                TracerConfig = TomletMain.To<TracerConfig>(toml);
            }
            catch (Exception e)
            {
                MainClass.Error(e.ToString());
                MainClass.Msg(
                    "Something went wrong when deserializing toml. Check the ReadMe in case something has changed"
                );
            }

            TracerConfig = TracerConfig ?? new TracerConfig();

            return true;
        }

        public static void OpenConfig()
        {
            System.Diagnostics.Process.Start(FullPath);
        }

        public static void OpenConfigFolder()
        {
            System.Diagnostics.Process.Start("explorer.exe", "/select," + FullPath);
        }

        public static void OpenReadMe()
        {
            Application.OpenURL(ReadMeUrl);
        }
    }
}