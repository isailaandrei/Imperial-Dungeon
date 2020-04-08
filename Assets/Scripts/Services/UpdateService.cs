﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ExitGames.Client.Photon.Chat;

public class UpdateService : MonoBehaviour {
	
	private static UpdateService instance = null;
	private Dictionary<UpdateType, List<Action<String, Dictionary<String, String>>>> subscribers = new Dictionary<UpdateType, List<Action<String, Dictionary<String, String>>>> ();
	private Queue<KeyValuePair<String[], Dictionary<String, String>>> messagesQueue = new Queue<KeyValuePair<String[], Dictionary<String, String>>> ();
	private bool started = false;

	public void Awake () {
		if (instance == null) {
			instance = this;
			InstantiateSubs ();
		} else {
			Destroy (gameObject);
		}
	}

	public void StartService () {
		started = true;
	}

	public void StopService () {
		started = false;
		subscribers = new Dictionary<UpdateType, List<Action<String, Dictionary<String, String>>>> ();
		messagesQueue = new Queue<KeyValuePair<String[], Dictionary<String, String>>> ();
		InstantiateSubs ();
	}

	private void InstantiateSubs () {
		foreach (UpdateType en in Enum.GetValues (typeof (UpdateType))) {
			subscribers.Add (en, new List<Action<String, Dictionary<String, String>>> ());	
		}
	}

	public static UpdateService GetInstance () {
		return instance;
	}

	public static Dictionary<String, String> CreateMessage (UpdateType type, params KeyValuePair<String, String>[] els) {
		Dictionary<String, String> message = new Dictionary<String, String> ();

		message.Add ("type", JsonUtility.ToJson (type));
		foreach (var pair in els) {
			message.Add (pair.Key, pair.Value);
		}

		return message;
	}

	public void SendUpdate (string[] targets, Dictionary<String, String> message) {
		messagesQueue.Enqueue (new KeyValuePair<string[], Dictionary<string, string>> (targets, message));
		SendQueueItems ();
	}

	private void SendQueueItems () {
		if (ChatService.GetInstance () == null || !ChatService.GetInstance ().connected || ! started) {
			return;
		}

		while (messagesQueue.Count != 0) {
			KeyValuePair<String[], Dictionary<String, String>> messageEntry = messagesQueue.Dequeue ();
			foreach (var target in messageEntry.Key) {
				
				Debug.LogWarning ("Send update of type " + GetTypeOfData (messageEntry.Value).ToString () + " from " + CurrentUser.GetInstance ().GetUserInfo ().username + " to " + target);
				ChatService.GetInstance ().SendPrivateMessage (target, messageEntry.Value);
			}
		}
		messagesQueue = new Queue<KeyValuePair<String[], Dictionary<String, String>>> ();
	}

	public IEnumerator Wait (float seconds) {
		yield return new WaitForSeconds (seconds);
	}

	public void Recieve (string sender, Dictionary<String, String> message) {
		if (!CurrentUser.GetInstance ().IsLoggedIn ()) {
			return;
		}

		if (sender.Equals (CurrentUser.GetInstance ().GetUserInfo ().username)) {
			return;
		}
			
		CurrentUser.GetInstance ().RequestUpdate ((userInfo) => {
			List<Action<String, Dictionary<String, String>>> functions = 
							subscribers[GetTypeOfData (message)];
			Debug.LogWarning ("Received update of type " + GetTypeOfData (message).ToString () + " from " + sender);
			for (int i = 0; i < functions.Count; i++) {
				Action<String, Dictionary<String, String>> func = functions [i];
				// doesn't work
				if (func != null) {
					func (sender, message);
				}
			}
		});
	}

	// returns unsubscribe function
	public Action Subscribe (UpdateType ev, Action<String, Dictionary<String, String>> func) {
		List<Action<String, Dictionary<String, String>>> functions;
		functions = subscribers [ev];
		functions.Add (func);
		
		return () => {
			functions.Remove (func);
		};
	}

	public static string GetData (Dictionary<String, String> message, string key) {
		return message[key];
	}

	public static UpdateType GetTypeOfData (Dictionary<String, String> message) {
		return JsonUtility.FromJson<UpdateType> (GetData (message, "type"));
	}

	public static KeyValuePair<string, string> CreateKV (string key, string value) {
		return new KeyValuePair<string, string> (key, value);
	}

	public void Update () {
		SendQueueItems ();
	}
}
