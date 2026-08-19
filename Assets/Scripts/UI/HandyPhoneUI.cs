using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   //Canvas而댄룷?뚰듃瑜??ъ슜?댁빞 ?섎?濡?異붽?.
using UnityEngine.EventSystems;    //PointerEventData瑜??ъ슜?댁빞 ?섎?濡?異붽?.

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
		_uiList.Peek().SetActive(false);
		_uiList.Push(gameObject);
	}
	public bool CloseUI()
	{
		Debug.Log("UI CLOSED");
		_uiList.Pop().SetActive(false);
		_uiList.Peek().SetActive(true);
		if (_uiList.Peek() == MainUI) return false;
		return true;
	}

	public void OnSpawned()
	{
		_uiList.Push(MainUI);
		MainUI.SetActive(true);
		MainUI.SetActive(true);
		HomeUI.SetActive(false);
		InviteUI.SetActive(false);
		BlackMarketUI.SetActive(false);
	}

	public void OnDespawned()
	{
		_uiList.Peek().SetActive(false);
		_uiList.Clear();
	}
}
