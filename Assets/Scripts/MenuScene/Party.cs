﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Party : MonoBehaviour {

	public const int maxSize = 4;

	public GameObject playerPrefab;
	public GameObject addPlayer;
	public GameObject leaveParty;
	public Text gameModeLabel;
	public MenuController menuController;
	public GameObject playButton;
	public GameObject withAnimationToggle;

	private Action unsub2;
	private Action unsub3;
	private Action unsub4;
	private string chatName;

	public void Awake () {
		unsub2 = UpdateService.GetInstance ().Subscribe (UpdateType.PartyRequestAccept, (sender, message) => {
			UpdateParty ();
		});

		unsub3 = UpdateService.GetInstance ().Subscribe (UpdateType.LogoutUser, (sender, message) => {
			UpdateParty ();
		});

		unsub4 = UpdateService.GetInstance ().Subscribe (UpdateType.PartyLeft, (sender, message) => {
			UpdateParty ();
		});
	}

	public void Start () {
		SetWithAnimation (withAnimationToggle.GetComponent<Toggle> ().enabled);
	}

	public void OnDestroy () {
		unsub2 ();
		unsub3 ();
		unsub4 ();
	}

	public void RequestAddPlayer () {
		var owner = CurrentUser.GetInstance ().GetUserInfo ().party.owner;
		var partyMembers = CurrentUser.GetInstance ().GetUserInfo ().party;

		if (owner == CurrentUser.GetInstance ().GetUserInfo ().username && partyMembers.GetSize () < maxSize) {
			RequestAlertController.Create("Who would you want to add to the party?", (controller, input) => {
				DBServer.GetInstance ().FindUser (input, (user) => {
					if (!partyMembers.ContainsPlayer (user.username) && user.active) {
						UpdateService.GetInstance ().SendUpdate (new string[]{user.username}, UpdateService.CreateMessage (UpdateType.PartyRequest, 
							UpdateService.CreateKV ("party_type", CurrentUser.GetInstance ().GetUserInfo ().party.state.ToString ())));
					}
					controller.Close ();
				}, (error) => {
					Debug.LogError (error);
				});
			});	
		}
	}

	private void AddPlayer (string username) {
		GameObject newPlayer = (GameObject) Instantiate (playerPrefab);
		newPlayer.transform.SetParent (transform);
		newPlayer.GetComponent<PartyEntry> ().ChangeName (username);
	}

	public void RequestLeaveParty () {
		DBServer.GetInstance ().LeaveParty (CurrentUser.GetInstance ().GetUserInfo ().username, () => {
			UpdateParty ();
		}, (error) => {
			Debug.LogError (error);
		});
	}

	private void UpdateParty () {
		if (CurrentUser.GetInstance ().GetUserInfo ().party.GetSize () == 0) {
			ChatController.GetChat ().DestroyChat (GetPartyChatName ());
			menuController.SwtichToMenuView ();
			NetworkService.GetInstance ().LeaveRoom ();
			SetPartyChatName (null);
		} else {
			ClearParty ();
			foreach (var member in CurrentUser.GetInstance ().GetUserInfo ().party.partyMembers) {
				AddPlayer (member);
			}
				
			ChatController.GetChat ().CreateNewChat (GetPartyChatName (), "Party", false);
		}
	}

	public void ClearParty () {
		foreach (var obj in GameObject.FindGameObjectsWithTag ("PlayerPartyEntity")) {
			Destroy (obj);
		}
	}

	public void Join () {
		menuController.SwitchToPartyView ();
		SetPartyChatName (CurrentUser.GetInstance ().GetUserInfo ().party.owner);
		UpdateParty ();
	}

	public void JoinParty (string ownerParty, int mode) {
		if(!CurrentUser.GetInstance ().GetUserInfo ().party.ContainsPlayer(ownerParty)) {
			DBServer.GetInstance ().JoinParty (ownerParty, CurrentUser.GetInstance ().GetUserInfo ().username, mode, () => {
				Join ();
			}, (error) => {
				Debug.LogError(error);
			});
		} else {
			Debug.Log("Duplicate invite");
		}
	}

	public void OnReceivedInvite (string from, int mode) {
		ConfirmAlertController.Create ("You have received a " + (mode == PartyMembers.ADVENTURE ? "adventure" : "endless")
											+ " party invite from " + from, (alert) => {
			JoinParty (from, mode);
			alert.Close ();
		}, (alert) => {
			alert.Close ();
		});
	}

	public void Update () {
		var owner = CurrentUser.GetInstance ().GetUserInfo ().party.owner;
		addPlayer.SetActive (owner == CurrentUser.GetInstance ().GetUserInfo ().username);
		addPlayer.SetActive (GetPartyMode () != PartyMembers.STORY);
		playButton.SetActive (owner == CurrentUser.GetInstance ().GetUserInfo ().username);
		playButton.GetComponent<Button> ().interactable = NetworkService.GetInstance ().IsInRoom ();

		string text = "";

		switch (GetPartyMode ()) {
		case PartyMembers.ADVENTURE: text = "Game Mode\n--Adventure--" ; break;
		case PartyMembers.ENDLESS: text = "Game Mode\n--Endless--" ; break;
		case PartyMembers.STORY: text = "Game Mode\n--Story--" ; break;
		}

		gameModeLabel.text = text;

		withAnimationToggle.SetActive (GetPartyMode () == PartyMembers.ENDLESS);
	}

	public void SetWithAnimation (bool value) {
		PlayerPrefs.SetInt ("endless_animation", value ? 1 : 0);
	}

	public int GetPartyMode () {
		return CurrentUser.GetInstance ().GetUserInfo ().party.state;
	}

	public void Play () {
		NetworkService.GetInstance ().LoadScene (GetPartyMode ());
	}

	public string GetPartyChatName () {
		return chatName;
	}

	public void SetPartyChatName (string name) {
		chatName = name;
	}
}
