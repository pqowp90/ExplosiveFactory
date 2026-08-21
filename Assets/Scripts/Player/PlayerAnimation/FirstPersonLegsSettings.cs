using UnityEngine;

/// <summary>
/// 1인칭 전용 다리(FirstPersonLegs) 및 상체 오프셋 세팅 데이터 클래스.
/// </summary>
[System.Serializable]
public class FirstPersonLegsSettings
{
    [Header("First Person Body Offset (1인칭 시 몸체/다리 후방 이동)")]
    [Tooltip("1인칭 로컬 플레이어일 때 3인칭 몸체(그림자) 및 1인칭 다리를 뒤로 빼는 거리 (단위: m, 기본 0.12m)")]
    public float firstPersonBodyBackwardOffset = 0.12f;

    [Header("Standing Torso Offsets (서 있을 때)")]
    [Tooltip("서 있을 때 허리 하단(Spine) 후방 거리 (단위: m, 기본 0.15m)")]
    public float spineBackwardOffset = 0.15f;

    [Tooltip("서 있을 때 허리(Spine) 위쪽 높이 (단위: m, 기본 0.27m)")]
    public float spineUpwardOffset = 0.27f;

    [Tooltip("서 있을 때 가슴 중간(Chest) 추가 후방 거리 (단위: m, 기본 0.15m)")]
    public float chestBackwardOffset = 0.15f;

    [Tooltip("서 있을 때 가슴(Chest) 위쪽 높이 (단위: m, 기본 0.15m)")]
    public float chestUpwardOffset = 0.15f;

    [Tooltip("서 있을 때 가슴 상단(UpperChest) 추가 후방 거리 (단위: m, 기본 -0.16m)")]
    public float upperChestBackwardOffset = -0.16f;

    [Header("Crouching Torso Offsets (앉았을 때)")]
    [Tooltip("앉았을 때 허리 하단(Spine) 후방 거리 (단위: m, 기본 0.15m)")]
    public float crouchSpineBackwardOffset = 0.15f;

    [Tooltip("앉았을 때 허리(Spine) 위쪽 높이 (단위: m, 기본 0.27m)")]
    public float crouchSpineUpwardOffset = 0.27f;

    [Tooltip("앉았을 때 가슴 중간(Chest) 추가 후방 거리 (단위: m, 기본 0.15m)")]
    public float crouchChestBackwardOffset = 0.15f;

    [Tooltip("앉았을 때 가슴(Chest) 위쪽 높이 (단위: m, 기본 0.15m)")]
    public float crouchChestUpwardOffset = 0.15f;

    [Tooltip("앉았을 때 가슴 상단(UpperChest) 추가 후방 거리 (단위: m, 기본 -0.16m)")]
    public float crouchUpperChestBackwardOffset = -0.16f;
}
