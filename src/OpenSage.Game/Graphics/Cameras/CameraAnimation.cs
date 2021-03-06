﻿using System;
using System.Collections.Generic;
using System.Numerics;
using OpenSage.Mathematics;

namespace OpenSage.Graphics.Cameras
{
    public sealed class CameraAnimation
    {
        private readonly IReadOnlyList<Vector3> _points;

        private readonly Vector3 _startDirection;
        private Vector3? _endDirection;
        private Vector3? _lookToward;

        private readonly TimeSpan _startTime;
        private readonly TimeSpan _duration;
        private readonly TimeSpan _endTime;

        private readonly float _startPitch;
        private float? _endPitch;

        private readonly float _startZoom;
        private float? _endZoom;

        public bool Finished { get; internal set; }

        public CameraAnimation(
            IReadOnlyList<Vector3> points,
            Vector3 startDirection,
            TimeSpan startTime,
            TimeSpan duration,
            float startPitch,
            float startZoom)
        {
            _points = points;
            _startDirection = startDirection;

            _startTime = startTime;
            _duration = duration;
            _endTime = startTime + duration;

            _startPitch = startPitch;
            _startZoom = startZoom;
        }

        public void SetFinalLookToward(Vector3 lookToward)
        {
            var endPosition = _points[_points.Count - 1];
            _endDirection = Vector3.Normalize(lookToward - endPosition);
        }

        public void SetFinalPitch(float endPitch)
        {
            _endPitch = endPitch;
        }

        public void SetFinalZoom(float endZoom)
        {
            _endZoom = endZoom;
        }

        public void SetLookToward(Vector3 lookToward)
        {
            _lookToward = lookToward;
        }

        internal void Update(RtsCameraController camera, in TimeInterval gameTime)
        {
            var currentTimeFraction = (float) ((gameTime.TotalTime - _startTime).TotalSeconds / _duration.TotalSeconds);
            currentTimeFraction = Math.Min(currentTimeFraction, 1);

            var pos = currentTimeFraction * (_points.Count - 1);
            var integralPart = (int) MathF.Truncate(pos);
            var decimalPart = pos - integralPart;

            // TODO: Not sure how right this is
            // Near the end of the path 2-3 points are the same, so the movement becomes more linear?
            var point1 = integralPart;
            var point2 = Math.Min(point1 + 1, _points.Count - 1);
            var point0 = Math.Max(point1 - 1, 0);
            var point3 = Math.Min(point2 + 1, _points.Count - 1);

            camera.TerrainPosition = Interpolation.CatmullRom(
                _points[point0],
                _points[point1],
                _points[point2],
                _points[point3],
                decimalPart);

            if (_lookToward != null)
            {
                var lookDirection = Vector3.Normalize(_lookToward.Value - camera.TerrainPosition);
                camera.SetLookDirection(lookDirection);
            }
            else if (_endDirection != null)
            {
                var lookDirection = Vector3.Normalize(Vector3Utility.Slerp(_startDirection, _endDirection.Value, currentTimeFraction));

                camera.SetLookDirection(lookDirection);
            }

            if (_endPitch != null)
            {
                var pitch = MathUtility.Lerp(_startPitch, _endPitch.Value, currentTimeFraction);

                camera.Pitch = pitch;
            }

            if (_endZoom != null)
            {
                var zoom = MathUtility.Lerp(_startZoom, _endZoom.Value, currentTimeFraction);

                camera.Zoom = zoom;
            }

            if (gameTime.TotalTime > _endTime)
            {
                Finished = true;
            }
        }
    }
}
