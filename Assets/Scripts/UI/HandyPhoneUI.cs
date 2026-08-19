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
	[SerializeField] private GameObject UpBarUI;
	private HandyItemObject _handyItemObject;
	[SerializeField]
	readonly private Stack<GameObject> _uiList = new Stack<GameObject>();
	private Canvas _canvas;
	private void Awake()
	{
		_canvas = GetComponentInChildren<Canvas>();
		_handyItemObject = GetComponent<HandyItemObject>();
		_handyItemObject.OnHandyItemObjectSpawnedEvent += OnSpawned;
	}

	private void OnSpawned(Player player)
	{
		_canvas.worldCamera = player.Camera;
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
	}

	public void OnDespawned()
	{
		if (_uiList.Count > 0)
			_uiList.Peek().SetActive(false);
		_uiList.Clear();
	}
}
