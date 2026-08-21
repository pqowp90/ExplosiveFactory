using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 최상위 네트워크 엔티티 및 컴포넌트 허브(Context Container).
/// - 이동, 회전, 애니메이션, 입력, 아이템 등 하위 서브시스템 컴포넌트들을 참조하고 조율합니다.
/// - 1인칭 다리 및 Foot IK 설정은 FirstPersonLegsSetup / FirstPersonLegsController에 위임되어 있습니다.
/// </summary>
public class Player : NetworkBehaviour, IPoolable
{
    [Header("Player Components")]
    public ItemHolder ItemHolder { get; private set; }
    public PlayerMove PlayerMove { get; private set; }
    public PlayerRotate PlayerRotate { get; private set; }
    public PlayerAnimation PlayerAnimation { get; private set; }
    public PlayerInput PlayerInput { get; private set; }
    public InputController InputController { get; private set; }
    public FirstPersonLegsSetup LegsSetup { get; private set; }

    [Header("Model Transforms")]
    public Transform PlayerBodyTransform;
    public Transform PlayerHandTransform;
    [HideInInspector]
    public Transform PlayerLegTransform;

    [Header("Cameras")]
    public Camera Camera;
    public Camera HandCamera;

    private void Awake()
    {
        ItemHolder = GetComponent<ItemHolder>();
        PlayerMove = GetComponent<PlayerMove>();
        PlayerRotate = GetComponent<PlayerRotate>();
        PlayerAnimation = GetComponent<PlayerAnimation>();
        PlayerInput = GetComponent<PlayerInput>();
        InputController = GetComponent<InputController>();
        LegsSetup = GetComponent<FirstPersonLegsSetup>() ?? gameObject.AddComponent<FirstPersonLegsSetup>();
    }

    public void OnSpawned()
    {
    }

    public void OnDespawned()
    {
    }
}
