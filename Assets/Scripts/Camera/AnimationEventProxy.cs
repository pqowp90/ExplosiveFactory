using UnityEngine;

namespace ExplosiveFactory.Network
{
    public class AnimationEventProxy : MonoBehaviour
    {
        [SerializeField] private PlayerMove? playerMove;
        [SerializeField] private CameraShocShak? cameraShocShak;

        private void Awake()
        {
            if (playerMove == null) playerMove = GetComponentInParent<PlayerMove>();
            if (cameraShocShak == null) cameraShocShak = GetComponentInParent<CameraShocShak>() ?? FindFirstObjectByType<CameraShocShak>();
        }

        public void AddForce(Vector3 velocity)
        {
            if (playerMove != null) playerMove.AddForce(velocity);
        }

        public void AddForce(ForceModeEnum forceMode)
        {
            if (cameraShocShak != null) cameraShocShak.AddForce(forceMode);
        }

        public void AddForce(int forceModeInt)
        {
            AddForce((ForceModeEnum)forceModeInt);
        }

        public void AddForce(string forceModeStr)
        {
            if (System.Enum.TryParse<ForceModeEnum>(forceModeStr, out var mode))
            {
                AddForce(mode);
            }
        }
    }
}
