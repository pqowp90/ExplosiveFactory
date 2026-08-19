using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;

public class LocalPlayerSetter : NetworkBehaviour
{
    private PlayerInput _playerInput;
    [SerializeField]
    private Camera _myCamera;
    [SerializeField]
    private Camera _handCamera;
    public Camera Camera => _myCamera;
    private Player _player;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _player = GetComponent<Player>();
        FindCameras();
    }

    private void FindCameras()
    {
        if (_myCamera == null)
        {
            var cams = GetComponentsInChildren<Camera>(true);
            foreach (var c in cams)
            {
                if (c.gameObject.name.ToLower().Contains("hand"))
                {
                    _handCamera = c;
                }
                else
                {
                    _myCamera = c;
                }
            }
        }

        if (_player != null)
        {
            if (_myCamera != null) _player.Camera = _myCamera;
            if (_handCamera != null) _player.HandCamera = _handCamera;
        }
    }

    private void SetRenderersShadowOnly(Transform target, bool shadowOnly)
    {
        if (target == null) return;
        var renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (shadowOnly)
            {
                r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
            else
            {
                r.shadowCastingMode = ShadowCastingMode.On;
                r.enabled = true;
            }
        }
    }

    private void SetRenderersActive(Transform target, bool active)
    {
        if (target == null) return;
        var renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.enabled = active;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (_player == null)
        {
            _player = GetComponent<Player>();
        }
        FindCameras();

        if (!isOwned)
        {
            // 타 플레이어 시점:
            // 1. 카메라 비활성화
            if (_myCamera != null)
            {
                var listener = _myCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
                _myCamera.enabled = false;
                _myCamera.gameObject.SetActive(false);
            }
            if (_handCamera != null)
            {
                _handCamera.enabled = false;
                _handCamera.gameObject.SetActive(false);
            }
            if (_playerInput != null) _playerInput.enabled = false;

            // 2. 1인칭 손 렌더러 숨김
            if (_player != null && _player.PlayerHandTransform != null)
            {
                SetRenderersActive(_player.PlayerHandTransform, false);
            }

            // 3. 3인칭 전신 모델 보이기
            if (_player != null && _player.PlayerBodyTransform != null)
            {
                SetRenderersShadowOnly(_player.PlayerBodyTransform, false);
            }

            // 4. InteractHintUI 비활성화 (타 플레이어 화면에 표시되지 않도록)
            var hintUI = transform.Find("InteractHintUI");
            if (hintUI != null) hintUI.gameObject.SetActive(false);
        }
        else
        {
            // 로컬 플레이어 시점:
            // 1. 씬 내 중복 AudioListener 정리
            var allListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var l in allListeners)
            {
                l.enabled = false;
            }

            // 2. 1인칭 메인 카메라 활성화
            if (_myCamera != null)
            {
                _myCamera.targetTexture = null;
                _myCamera.gameObject.SetActive(true);
                _myCamera.enabled = true;
                _myCamera.tag = "MainCamera";
                var listener = _myCamera.GetComponent<AudioListener>() ?? _myCamera.gameObject.AddComponent<AudioListener>();
                listener.enabled = true;
            }

            // 3. 1인칭 손 카메라 활성화
            if (_handCamera != null)
            {
                _handCamera.targetTexture = null;
                _handCamera.gameObject.SetActive(true);
                _handCamera.enabled = true;
            }

            // 4. 씬 내 기본 독립 Main Camera 비활성화
            var sceneCams = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var c in sceneCams)
            {
                if (c != _myCamera && c != _handCamera && !c.transform.IsChildOf(transform) && c.gameObject.name == "Main Camera")
                {
                    c.gameObject.SetActive(false);
                }
            }

            // 5. 1인칭 손 렌더러 켜기
            if (_player != null && _player.PlayerHandTransform != null)
            {
                SetRenderersActive(_player.PlayerHandTransform, true);
            }

            // 6. 3인칭 몸체(얼굴/선글라스/몸통)는 화면에서 가리고 바닥 그림자만 남김 (ShadowsOnly)
            if (_player != null && _player.PlayerBodyTransform != null)
            {
                SetRenderersShadowOnly(_player.PlayerBodyTransform, true);
            }
        }
    }
}
