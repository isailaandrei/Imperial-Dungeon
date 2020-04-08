﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryController : MonoBehaviour {

	public GameObject playerPrefab;
	public GameObject partyPrefab;
	public Transform spawnPoint;

	// Use this for initialization
	void Start () {
		ChatController.GetChat ().InitDefaultChat ();
		GameObject player = NetworkService.GetInstance ().Spawn (playerPrefab.name, spawnPoint.position, Quaternion.identity, 0,
			new object[1] {CurrentUser.GetInstance ().GetUserInfo ()});
		player.GetComponent<Player> ().SetAttack (false);
		NetworkService.GetInstance ().SpawnScene (partyPrefab.name, Vector3.zero, Quaternion.identity, 0);
	}

	void OnApplicationQuit () {
		CurrentUser.GetInstance ().UnsubscribeCH (CurrentUser.GetInstance ().GetUserInfo ().party.owner);
		DBServer.GetInstance ().LeaveParty (CurrentUser.GetInstance ().GetUserInfo ().username, () => {
			DBServer.GetInstance ().Logout (false, () => {}, (err) => {});
		}, (error) => {
			Debug.LogError (error);	
		});
	}

	// Update is called once per frame
	void Update () {
		
	}
}
