﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using System.Text;

public class NetworkService : Photon.PunBehaviour {

	public const String GAME_VERSION = "v0.01";
	public const String partyPrefabName = "Party";
	public Text infoLabel;
	private static NetworkService instance = null;
	private static TypedLobby adventureLobby = new TypedLobby ("Adventure", LobbyType.Default);
	private static TypedLobby endlessLobby = new TypedLobby ("Endless", LobbyType.Default);
	private static TypedLobby storyLobby = new TypedLobby ("Story", LobbyType.Default);

	private Action onFinish;

	public void Awake () {
		if (instance == null) {
			instance = this;
		} else {
			Destroy (gameObject);
		}
	}

	public void StartService (Action onFinish) {
		PhotonNetwork.ConnectUsingSettings (GAME_VERSION);
		PhotonNetwork.automaticallySyncScene = true;
		PhotonNetwork.InstantiateInRoomOnly = true;
		PhotonNetwork.sendRate = 20;
		PhotonNetwork.sendRateOnSerialize = 20;
		ExitGames.Client.Photon.PhotonPeer.RegisterType	(typeof (User), 2, 
			new SerializeMethod ((object userObj) => {
				User cast = (User) userObj;
				return Encoding.UTF8.GetBytes (JsonUtility.ToJson (cast));
			}), new DeserializeMethod ((byte[] obj) => {
				return JsonUtility.FromJson<User> (Encoding.UTF8.GetString(obj));
			}));
		this.onFinish = onFinish;
	}

	public void StopService () {
		DestroyConnection ();
	}

	public static NetworkService GetInstance () {
		return instance;
	}

	private void DestroyConnection () {
		PhotonNetwork.Disconnect ();
	}

	public RoomInfo[] GetRoomList () {
		return PhotonNetwork.GetRoomList ();
	}

	public void JoinLobby (int mode) {
		if (mode == PartyMembers.ADVENTURE) {
			PhotonNetwork.JoinLobby (adventureLobby);
		} else if (mode == PartyMembers.ENDLESS) {
			PhotonNetwork.JoinLobby (endlessLobby);
		} else {
			PhotonNetwork.JoinLobby (storyLobby);
		}
	}

	public void JoinRoom (string roomName) {
		PhotonNetwork.JoinRoom (roomName);
	}

	public void CreateRoom (string roomName) {
		PhotonNetwork.CreateRoom (roomName, new RoomOptions () {MaxPlayers = 4}, PhotonNetwork.lobby);
	}

	public void LeaveRoom () {
		PhotonNetwork.LeaveRoom ();
	}

	public void LoadScene (int mode) {
		if (mode == 1) {
			PhotonNetwork.LoadLevel ("Adventure");
		} else if (mode == 2) {
			PhotonNetwork.LoadLevel ("Endless");
		} else {
			PhotonNetwork.LoadLevel ("Story");
		}
	}

	public GameObject Spawn (string prefabName, Vector3 position, Quaternion rotation, int groupID) {
		return PhotonNetwork.Instantiate (prefabName, position, rotation, groupID);
	}

	public GameObject Spawn (string prefabName, Vector3 position, Quaternion rotation, int groupID, object[] data) {
		return PhotonNetwork.Instantiate (prefabName, position, rotation, groupID, data);
	}

	public void Destroy (GameObject ob) {
		PhotonNetwork.Destroy (ob);
	}

	public GameObject SpawnScene (string prefabName, Vector3 position, Quaternion rotation, int groupID) {
		return PhotonNetwork.InstantiateSceneObject (prefabName, position, rotation, groupID, new object[0]);
	}

	public bool IsMasterClient () {
		return PhotonNetwork.isMasterClient;
	}

	public override void OnConnectedToPhoton () {
		base.OnConnectedToPhoton ();
		onFinish ();
	}

	public bool IsInRoom () {
		return PhotonNetwork.inRoom;
	}
}
