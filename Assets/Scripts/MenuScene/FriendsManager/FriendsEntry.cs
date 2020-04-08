﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FriendsEntry : MonoBehaviour {

	public GameObject friendsPanel;
	public Image avatar;
	private GameObject optionsPanel;
	private Action unsub1;
	private Action unsub2;
	private Action unsub3;

	public void Awake () {
		unsub1 = UpdateService.GetInstance ().Subscribe (UpdateType.LoginUser, (sender, message) => {
			if (GetName ().Equals (sender)) {
				UpdateStatus ();
			}
		});

		unsub2 = UpdateService.GetInstance ().Subscribe (UpdateType.LogoutUser, (sender, message) => {
			if (GetName ().Equals (sender)) {
				UpdateStatus ();
			}
		});

		unsub3 = UpdateService.GetInstance ().Subscribe (UpdateType.UserUpdate, (sender, message) => {
			if (GetName ().Equals (sender)) {
				UpdateStatus ();
			}
		});
	}

	public void Start() {
		optionsPanel = GameObject.FindGameObjectWithTag ("PlayerPanel").transform.GetChild (3).gameObject;
		GetOptionPanel ().SetActive (false);
		UpdateStatus ();
	}

	public void OnDestroy () {
		unsub1 ();
		unsub2 ();
		unsub3 ();
	}

	public GameObject GetOptionPanel () {
		return optionsPanel;
	}

	public void ShowOptions() {
		if (GetOptionPanel ().activeSelf && GetOptionPanel ().GetComponent <OptionScript> ().GetPlayerName () != null && 
							GetOptionPanel ().GetComponent <OptionScript> ().GetPlayerName ().Equals (GetName ())) {
			GetOptionPanel ().SetActive (false);
			return;
		}
		GetOptionPanel ().SetActive (true);
		Vector3 friendPos = friendsPanel.transform.position;
		Vector3 OptionPos = GetOptionPanel ().transform.position;
		GetOptionPanel ().transform.position = new Vector3 (OptionPos.x, friendPos.y, OptionPos.z);
		GetOptionPanel ().transform.GetComponent<OptionScript> ().SetPlayerName (GetName ());
	}

	private void ChangeStatus (bool status, string email) {
		ColorBlock cb = gameObject.GetComponent<Button> ().colors;
		cb.normalColor = (status ? (Validator.IsImperial (email) ? Color.blue : Color.green) : Color.red);
		gameObject.GetComponent<Button> ().colors = cb;
	}

	private void UpdateStatus () {
		DBServer.GetInstance ().FindUser (GetName (), (user) => {
			ChangeStatus (user.active, user.email);
			avatar.sprite = user.character.GetImage ();
		}, (error) => {
			Debug.LogError (error);
		});
	}

	public void SetName (string name) {
		gameObject.GetComponentInChildren<Text> ().text = name;
	}

	public string GetName () {
		return gameObject.GetComponentInChildren<Text> ().text;
	}
}
