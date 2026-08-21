using UnityEngine;

/// <summary>
/// 플레이어 하위 모든 서브시스템 컴포넌트의 베이스 클래스.
/// - 부모 Player 엔티티를 자동으로 지연 캐싱(Lazy Caching)하여 중복 GetComponent를 제거합니다.
/// - 이동, 애니메이션, 카메라, 네트워크 소유권(IsOwned) 등에 즉시 접근할 수 있는 편의 프로퍼티를 제공합니다.
/// </summary>
public abstract class PlayerComponent : MonoBehaviour
{
    private Player _player;
    public Player Player
    {
        get
        {
            if (_player == null)
            {
                _player = GetComponent<Player>() ?? GetComponentInParent<Player>();
            }
            return _player;
        }
    }

    // 자주 쓰는 플레이어 서브시스템 바로 접근
    public PlayerMove PlayerMove => Player != null ? Player.PlayerMove : null;
    public PlayerRotate PlayerRotate => Player != null ? Player.PlayerRotate : null;
    public PlayerAnimation PlayerAnimation => Player != null ? Player.PlayerAnimation : null;
    public ItemHolder ItemHolder => Player != null ? Player.ItemHolder : null;
    public InputController InputController => Player != null ? Player.InputController : null;
    public FirstPersonLegsSetup LegsSetup => Player != null ? Player.LegsSetup : null;
    public Camera Camera => Player != null ? Player.Camera : null;
    public Camera HandCamera => Player != null ? Player.HandCamera : null;

    // 모델 트랜스폼
    public Transform PlayerBodyTransform => Player != null ? Player.PlayerBodyTransform : null;
    public Transform PlayerHandTransform => Player != null ? Player.PlayerHandTransform : null;
    public Transform PlayerLegTransform => Player != null ? Player.PlayerLegTransform : null;

    // 네트워크 소유권 및 서버 여부 헬퍼
    public bool IsOwned => Player != null && Player.isOwned;
    public bool IsServer => Player != null && Player.isServer;
}
