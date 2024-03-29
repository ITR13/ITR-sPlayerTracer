﻿using UnityEngine;

namespace VrcTracer
{
    internal static class PlayerMarker
    {
        private static GameObject _player;
        private static bool _isSet;
        public static Vector3 Position { get; private set; }

        public static GameObject Player
        {
            get => _player;
            set
            {
                _player = value;
                _isSet = true;
                Position = Player.transform.position +
                           ConfigWatcher.TracerConfig.originOffset;
            }
        }

        public static bool UpdatePosition(bool follow)
        {
            if (!_isSet || Player == null)
            {
                _isSet = false;
                return false;
            }

            if (follow)
                Position = Player.transform.position +
                           ConfigWatcher.TracerConfig.originOffset;

            return true;
        }
    }
}