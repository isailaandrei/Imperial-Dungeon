﻿using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class CurrentUser : MonoBehaviour {
	
	private User userInfo = null;
	public const String userCache = "Assets/cache";
	private bool withCaching = true;
	private static CurrentUser instance = null;
	private HashSet<string> subscribedCH = new HashSet<string> () {ChatService.GLOBAL_CH};

	public void Awake () {
		if (instance == null) {
			instance = this;
			InvokeRepeating ("UpdateOnlineStatus", 0, 60);
			LoadFromCache ();
		}
	}

	public void UpdateOnlineStatus () {
		if (IsLoggedIn ()) {
			DBServer.GetInstance ().SetActiveStatus (true, () => {
				RequestUpdate ((user) => {});
			}, (error) => {
				Logout (true);
				Debug.LogError (error);
			});
		}
	}

	public void OnApplicationQuit() {
		DBServer.GetInstance ().Logout (false, () => { }, (error) => {Debug.LogError (error);});
	}
		
	public static CurrentUser GetInstance () {
		return instance;
	}

	private void SaveToCache () {
		if (!withCaching) {
			return;
		}

		String userJSON = JsonUtility.ToJson (userInfo);
		PlayerPrefs.SetString ("player_cache", userJSON);
	}

	private void LoadFromCache () {
		if (!withCaching) {
			return;
		}

		String userInfoJSON = PlayerPrefs.GetString ("player_cache");

		if (userInfoJSON.Equals ("")) {
			return;
		}

		User loadedUser = JsonUtility.FromJson<User> (userInfoJSON);

		DBServer.GetInstance ().Login (loadedUser.username, loadedUser.password, false, (user) => {
		}, (error) => {
			Logout (true);
		});
	}

	public void ClearCahce () {
		PlayerPrefs.DeleteKey ("player_cache");
	}

	public void Login (User userInfo) {
		SetUserInfo (userInfo);
	}

	public void Logout (bool overwriteCache) {
		userInfo = null;
		subscribedCH = new HashSet<string> () {ChatService.GLOBAL_CH};
		CancelInvoke ();
		if (overwriteCache) {
			ClearCahce ();
		}
	}

	public User GetUserInfo () {
		return userInfo;
	}

	private void SetUserInfo (User userInfo) {
		this.userInfo = userInfo;
		this.userInfo.active = true;
		SaveToCache ();
	}

	public bool IsLoggedIn () {
		return userInfo != null;
	}

	public override String ToString () {
		if (userInfo != null) {
			return userInfo.ToString ();
		} else {
			return "Not Logged";
		}
	}

	public void RequestUpdate (Action<User> onFinish) {
		if (IsLoggedIn ()) {
			DBServer.GetInstance ().FindUser (userInfo.username, (user) => {
				SetUserInfo (user);
				onFinish (user);
			}, (error) => {
				Debug.LogError ("Something happened to the user: " + error);
				Logout (true);
			});
		}
	}

	public void SetWithCache (bool value) {
		this.withCaching = value;
	}

	public bool IsInParty () {
		return userInfo.party.ContainsPlayer (userInfo.username);
	}

	public void SubscribeToCH (string name) {
		subscribedCH.Add (name);
	}

	public string[] GetSubscribedCH () {
		string[] result = new string[subscribedCH.Count];
		subscribedCH.CopyTo (result);
		return result;
	}

	public void UnsubscribeCH (string name) {
		subscribedCH.Remove (name);
	}

	public int GetPositionInParty () {
		if (!IsInParty ()) {
			return -1;
		}

		int i = 0;
		foreach (var player in userInfo.party.partyMembers) {
			if (player.Equals (userInfo.username)) {
				return i;
			}
			i++;
		}

		return -1;
	}
}

