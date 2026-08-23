using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandyPhoneUI : MonoBehaviour, IPoolable
{
	[SerializeField] private GameObject MainUI;
	[SerializeField] private GameObject HomeUI;
	[SerializeField] private GameObject InviteUI;
	[SerializeField] private GameObject BlackMarketUI;
	[SerializeField] private GameObject WardrobeUI;
	[SerializeField] private GameObject UpBarUI;
	private HandyItemObject _handyItemObject;
	[SerializeField]
	readonly private Stack<GameObject> _uiList = new Stack<GameObject>();
	private Canvas _canvas;
	private Canvas[] _canvases = Array.Empty<Canvas>();
	private GraphicRaycaster[] _raycasters = Array.Empty<GraphicRaycaster>();

	private void Awake()
	{
		_canvases = GetComponentsInChildren<Canvas>(true);
		_raycasters = GetComponentsInChildren<GraphicRaycaster>(true);
		_canvas = _canvases.Length > 0 ? _canvases[0] : GetComponentInChildren<Canvas>();
		_handyItemObject = GetComponent<HandyItemObject>();
		if (_handyItemObject != null)
		{
			_handyItemObject.OnHandyItemObjectSpawnedEvent += OnSpawned;
		}
	}

	private void OnSpawned(Player player)
	{
		bool isFirstPerson = _handyItemObject != null && _handyItemObject.CurrentAttachMode == HandyAttachMode.FirstPerson;
		bool isShadowOnly = _handyItemObject != null && _handyItemObject.CurrentAttachMode == HandyAttachMode.ShadowOnly;
		bool showCanvas = !isShadowOnly;

		// 그림자 모드일 때만 캔버스를 끄고, 1인칭 및 일반 3인칭에서는 캔버스 활성화 유지
		foreach (var c in _canvases)
		{
			if (c != null)
			{
				c.enabled = showCanvas;
				c.gameObject.SetActive(showCanvas);
				if (isFirstPerson && player != null)
				{
					c.worldCamera = player.Camera;
				}
			}
		}

		foreach (var r in _raycasters)
		{
			if (r != null)
			{
				r.enabled = isFirstPerson;
			}
		}

		if (isFirstPerson)
		{
			if (WardrobeUI != null && player != null)
			{
				var modelSelect = WardrobeUI.GetComponent<ModelSelectUI>();
				if (modelSelect != null) modelSelect.SetLocalPlayer(player);
			}
		}
	}

	public void OpenHomeUI()
	{
		Open(HomeUI);
	}
	public void OpenInviteUI()
	{
		Open(InviteUI);
	}
	public void OpenBlackMarketUI()
	{
		Open(BlackMarketUI);
	}
	public void OpenWardrobeUI()
	{
		Open(WardrobeUI);
	}
	private void Open(GameObject gameObject)
	{
		if (gameObject != MainUI)
		{
			UpBarUI.SetActive(true);
		}
		else
		{
			UpBarUI.SetActive(false);
		}
		gameObject.SetActive(true);
		if (_uiList.Count > 0)
			_uiList.Peek().SetActive(false);
		_uiList.Push(gameObject);
	}
	public bool CloseUI()
	{
		Debug.Log("UI CLOSED");
		if (_uiList.Count > 0)
			_uiList.Pop().SetActive(false);
		if (_uiList.Count > 0)
		{
			_uiList.Peek().SetActive(true);
			if (_uiList.Peek() == MainUI) return false;
			return true;
		}
		return false;
	}

	public void OnSpawned()
	{
		_uiList.Clear();
		_uiList.Push(MainUI);
		if (MainUI != null) MainUI.SetActive(true);
		if (HomeUI != null) HomeUI.SetActive(false);
		if (InviteUI != null) InviteUI.SetActive(false);
		if (BlackMarketUI != null) BlackMarketUI.SetActive(false);
		if (WardrobeUI != null) WardrobeUI.SetActive(false);
	}

	public void OnDespawned()
	{
		if (_uiList.Count > 0)
			_uiList.Peek().SetActive(false);
		_uiList.Clear();
	}
}
