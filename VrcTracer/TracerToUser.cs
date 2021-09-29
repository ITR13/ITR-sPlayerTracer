using System.Collections.Generic;
using UnityEngine;

namespace VrcTracer
{
    internal class TracerToUser
    {
        private static readonly List<TracerToUser> Tracers
            = new List<TracerToUser>();

        private readonly GameObject _gameObject;

        private readonly LineRenderer _lineRenderer;
        private readonly Transform _transform;

        public TracerToUser(GameObject gameObject)
        {
            Tracers.Add(this);
            _gameObject = gameObject;
            _transform = gameObject.transform;

            _lineRenderer = _gameObject.AddComponent<LineRenderer>();
            _lineRenderer.sharedMaterial = TracerMaterial;

            _lineRenderer.startWidth = 0.02f;
            _lineRenderer.endWidth = 0.02f;
            _lineRenderer.positionCount = 2;

            var position = _transform.position;
            _lineRenderer.SetPosition(0, position);
            _lineRenderer.SetPosition(1, position + Vector3.up * 2);
        }

        public static int Count => Tracers.Count;

        public Color Color
        {
            get => _lineRenderer.startColor;
            set
            {
                _lineRenderer.startColor = value;
                _lineRenderer.endColor = value;
            }
        }

        public static Material TracerMaterial { get; set; }

        public static int DestroyAllTracers()
        {
            var count = Tracers.Count;
            foreach (var tracer in Tracers) Object.Destroy(tracer._gameObject);
            Tracers.Clear();

            return count;
        }

        public static void LateUpdate()
        {
            for (var i = Tracers.Count - 1; i >= 0; i--)
            {
                var tracer = Tracers[i];
                if (tracer._gameObject == null)
                {
                    Tracers.RemoveAt(i);
                    continue;
                }

                tracer._lineRenderer.SetPosition(
                    0,
                    tracer._transform.position +
                    ConfigWatcher.TracerConfig.destinationOffset
                );
                tracer._lineRenderer.SetPosition(1, PlayerMarker.Position);
            }
        }
    }
}