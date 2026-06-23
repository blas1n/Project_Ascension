using UnityEngine;

namespace ProjectAscension.Player
{
    /// <summary>
    /// FPS look. Yaw rotates the player body; pitch rotates the camera pivot.
    /// A Cinemachine camera parented to the pivot inherits this transform, and the
    /// CinemachineBrain on the Main Camera renders from it.
    /// </summary>
    public sealed class PlayerCamera
    {
        private readonly PlayerData _data;

        private Transform _body;
        private Transform _pivot;
        private float _yaw;
        private float _pitch;

        public PlayerCamera(PlayerData data)
        {
            _data = data;
        }

        public void Initialize(Transform body, Transform pivot)
        {
            _body = body;
            _pivot = pivot;
            _yaw = body.eulerAngles.y;
            _pitch = 0f;
        }

        public void Tick(Vector2 lookInput)
        {
            if (_body == null) return;

            _yaw += lookInput.x * _data.LookSensitivity;
            _pitch -= lookInput.y * _data.LookSensitivity;
            _pitch = Mathf.Clamp(_pitch, _data.MinPitch, _data.MaxPitch);

            _body.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (_pivot != null)
                _pivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
