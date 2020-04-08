﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainPanelController : MonoBehaviour {
	
	public GameObject loginPanel;
	public GameObject registerPanel;
	public InputField userNameField;
	public InputField userNameFieldRegister;

	public void Update () {
		if (CurrentUser.GetInstance ().IsLoggedIn ()) {
			SceneManager.LoadScene ("Menu");
		}
	}

	/* Used by login button to change the pane to login pane */
	public void Login () {
		this.gameObject.SetActive (false);
		loginPanel.GetComponent<LoginPanelController> ().Activate (true);
		userNameField.Select ();
	}

	/* Used by registeer button to change the pane to register pane */
	public void Register () {
		this.gameObject.SetActive (false);
		registerPanel.GetComponent<RegisterPanelController> ().Activate (true);
		userNameFieldRegister.Select ();
	}
}
