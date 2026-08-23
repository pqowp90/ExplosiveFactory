using UnityEngine;

/// <summary>
/// 플레이어 3인칭 캐릭터 모델링 스킨 ScriptableObject 데이터.
/// </summary>
[CreateAssetMenu(fileName = "SkinData_", menuName = "ExplosiveFactory/PlayerSkinData", order = 1)]
public class PlayerSkinData : ScriptableObject
{
    [Header("Skin Metadata")]
    [Tooltip("스킨 고유 식별자 (예: Skin_Default, Skin_Worker)")]
    public string skinId = "Skin_Default";

    [Tooltip("UI 표시 이름")]
    public string skinName = "기본 모델";

    [Tooltip("핸드폰 UI 표시 아이콘")]
    public Sprite skinIcon;

    [Tooltip("설명")]
    [TextArea(2, 4)]
    public string description = "기본 작업자 외형입니다.";

    [Header("Model Prefab")]
    [Tooltip("교체될 3D 캐릭터 모델 프리팹 (Humanoid Animator 포함)")]
    public GameObject modelPrefab;
}
