using UnityEngine;

/// <summary>
/// 1인칭 다리 모델 인스턴스화 및 3인칭/1인칭 Foot IK 전담 셋업 컴포넌트.
/// - PlayerComponent를 상속받아 부모 Player 엔티티에 직접 접근합니다.
/// </summary>
[DisallowMultipleComponent]
public class FirstPersonLegsSetup : PlayerComponent
{
    [SerializeField]
    private FirstPersonLegsSettings _settings = new FirstPersonLegsSettings();
    public FirstPersonLegsSettings Settings => _settings;

    private void Awake()
    {
        SetupLegs();
        SetupBodyFootIK();
    }

    /// <summary>
    /// 3인칭 몸체를 복제하여 1인칭 전용 다리(FirstPersonLegs)를 생성하고 초기화합니다.
    /// </summary>
    public void SetupLegs()
    {
        if (Player == null || Player.PlayerBodyTransform == null) return;
        if (Player.PlayerLegTransform != null) return;

        // 3인칭 몸체를 복제하여 1인칭 전용 다리 생성
        GameObject legsObj = Instantiate(Player.PlayerBodyTransform.gameObject, transform);
        legsObj.name = "FirstPersonLegs";

        // 1인칭 다리에 불필요한 3인칭 컴포넌트 및 포워더 제거
        var lookAt = legsObj.GetComponentInChildren<LookAtController>();
        if (lookAt != null)
        {
            if (Application.isPlaying) Destroy(lookAt);
            else DestroyImmediate(lookAt);
        }

        var netAnimator = legsObj.GetComponentInChildren<CustomNetworkAnimator>();
        if (netAnimator != null)
        {
            if (Application.isPlaying) Destroy(netAnimator);
            else DestroyImmediate(netAnimator);
        }

        var ikForwarder = legsObj.GetComponentInChildren<AnimatorIKForwarder>();
        if (ikForwarder != null)
        {
            if (Application.isPlaying) Destroy(ikForwarder);
            else DestroyImmediate(ikForwarder);
        }

        // 1인칭 다리 제어기 추가 및 설정 전달
        var legsController = legsObj.GetComponent<FirstPersonLegsController>();
        if (legsController == null)
        {
            legsController = legsObj.AddComponent<FirstPersonLegsController>();
        }
        legsController.settings = _settings;

        // 1인칭 다리 Animator에 직접 FootIKController 추가 (1인칭 독립 IK 연산)
        var legAnimator = legsObj.GetComponentInChildren<Animator>();
        if (legAnimator != null && legAnimator.GetComponent<FootIKController>() == null)
        {
            legAnimator.gameObject.AddComponent<FootIKController>();
        }

        // 다리 렌더러 오클루전 컬링 비활성화 및 화면 밖 애니메이션 유지
        var legRenderers = legsObj.GetComponentsInChildren<Renderer>(true);
        foreach (var r in legRenderers)
        {
            if (r != null)
            {
                r.allowOcclusionWhenDynamic = false;
                if (r is SkinnedMeshRenderer smr)
                {
                    smr.updateWhenOffscreen = true;
                }
            }
        }

        Player.PlayerLegTransform = legsObj.transform;
    }

    /// <summary>
    /// 기존 1인칭 다리를 파괴하고 현재 3인칭 몸체를 기반으로 1인칭 다리를 재생성합니다.
    /// </summary>
    public void RecreateLegs()
    {
        if (Player != null && Player.PlayerLegTransform != null)
        {
            GameObject oldLegs = Player.PlayerLegTransform.gameObject;
            Player.PlayerLegTransform = null;
            if (Application.isPlaying)
            {
                Destroy(oldLegs);
            }
            else
            {
                DestroyImmediate(oldLegs);
            }
        }

        SetupLegs();
    }

    /// <summary>
    /// 3인칭 몸체 Animator에 AnimatorIKForwarder를 보장합니다.
    /// </summary>
    private void SetupBodyFootIK()
    {
        if (Player == null || Player.PlayerBodyTransform == null) return;

        var bodyAnimator = Player.PlayerBodyTransform.GetComponentInChildren<Animator>();
        if (bodyAnimator != null && bodyAnimator.GetComponent<AnimatorIKForwarder>() == null)
        {
            bodyAnimator.gameObject.AddComponent<AnimatorIKForwarder>();
        }
    }
}
