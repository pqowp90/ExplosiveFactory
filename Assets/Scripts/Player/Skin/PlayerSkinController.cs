using Mirror;
using UnityEngine;

/// <summary>
/// 플레이어 3D 캐릭터 모델링 스킨 교체 및 네트워크 동기화 전담 컴포넌트.
/// - GamePlayer 루트에 부착되어 모든 플레이어에게 모델링 변경을 실시간 동기화합니다.
/// </summary>
public class PlayerSkinController : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnSkinIndexChanged))]
    private int _currentSkinIndex = 0;
    public int CurrentSkinIndex => _currentSkinIndex;

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

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplySkin(_currentSkinIndex);
    }

    private void OnSkinIndexChanged(int oldIndex, int newIndex)
    {
        ApplySkin(newIndex);
    }

    [Command]
    public void CmdChangeSkin(int skinIndex)
    {
        var database = PlayerSkinDatabase.Instance;
        if (database == null || skinIndex < 0 || skinIndex >= database.SkinCount) return;

        _currentSkinIndex = skinIndex;
        RpcChangeSkin(skinIndex);
    }

    [ClientRpc]
    private void RpcChangeSkin(int skinIndex)
    {
        ApplySkin(skinIndex);
    }

    /// <summary>
    /// 대상 스킨 인덱스의 모델 프리팹을 Body 오브젝트 하위에 인스턴스화하고 서브시스템을 리바인딩합니다.
    /// </summary>
    public void ApplySkin(int skinIndex)
    {
        var database = PlayerSkinDatabase.Instance;
        if (database == null) return;

        var skinData = database.GetSkin(skinIndex);
        if (skinData == null || skinData.modelPrefab == null) return;

        Transform bodyTransform = Player != null ? Player.PlayerBodyTransform : null;
        if (bodyTransform == null) return;

        // 1. Body 오브젝트 하위의 기존 모델 인스턴스 즉시 비활성화 및 분리 후 제거
        for (int i = bodyTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = bodyTransform.GetChild(i);
#if UNITY_EDITOR
            if (UnityEditor.Selection.activeTransform != null && (UnityEditor.Selection.activeTransform == child || UnityEditor.Selection.activeTransform.IsChildOf(child)))
            {
                UnityEditor.Selection.activeObject = null;
            }
#endif
            child.gameObject.SetActive(false);
            child.SetParent(null);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // 2. 새 3D 캐릭터 모델 인스턴스화 및 Body 자식 배치
        if (skinData.modelPrefab == null)
        {
            Debug.LogError($"[PlayerSkinController] Skin modelPrefab is null for {skinData.skinName}");
            return;
        }

        GameObject newModel = Instantiate(skinData.modelPrefab, bodyTransform) as GameObject;
        if (newModel == null)
        {
            Debug.LogError($"[PlayerSkinController] Failed to instantiate modelPrefab for {skinData.skinName}");
            return;
        }

        newModel.name = skinData.modelPrefab.name;
        newModel.transform.localPosition = Vector3.zero;
        newModel.transform.localRotation = Quaternion.identity;
        newModel.transform.localScale = Vector3.one;

        // 3. Animator 및 AnimatorIKForwarder 보장
        var animator = newModel.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (animator.GetComponent<AnimatorIKForwarder>() == null)
            {
                animator.gameObject.AddComponent<AnimatorIKForwarder>();
            }
        }

        // 4. Player 서브시스템에 모델 및 애니메이터 리바인딩 전파
        if (Player != null)
        {
            Player.SetPlayerBody(bodyTransform);

            // 5. 로컬 플레이어 시야에서 3인칭 몸체 숨김(그림자 전용) 및 1인칭 다리 렌더러 갱신
            var setter = Player.GetComponent<LocalPlayerSetter>();
            if (setter != null)
            {
                setter.RefreshBodyRenderers();
                setter.RefreshLegRenderers();
            }
        }

        Debug.Log($"[PlayerSkinController] Skin applied: {skinData.skinName} (Index: {skinIndex})");
    }
}
