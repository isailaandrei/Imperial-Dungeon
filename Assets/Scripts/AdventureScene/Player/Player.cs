﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Entity<PlayerStats> {
	
	public Direction move;
	public Camera mainCamera;

	public GameObject attackRadius;
	public RuntimeAnimatorController[] playerControllers;
	public GameObject explosionPrefab;

	private PlayerAbilities abilities;
	public User user;
	private bool canAttack = true;
	private bool canMove = true;

	private Vector2 mouseOther;

	void Awake () {
		this.user = (User) photonView.instantiationData [0];
	}

	new void Start () {
		base.Start ();
		GetComponent<SpriteRenderer> ().sprite = user.character.GetImage ();
		GetComponent<Animator> ().runtimeAnimatorController = playerControllers [user.character.type];
		mainCamera.enabled = photonView.isMine;
		mainCamera.GetComponent<AudioListener> ().enabled = photonView.isMine;

		if (IsStory ()) {
			InvokeRepeating ("GetHitOvertime", 10, 20);	
		}

		if (photonView.isMine) {
			abilities = GameObject.FindGameObjectWithTag ("PlayerAbilities").GetComponent<PlayerAbilities> ();
			abilities.Init (this);
		}
	}

	public void SetAttack (bool value) {
		this.canAttack = value;
	}

	protected override void SetStats () {
		stats = user.character.GetStats ();
	}

	private bool IsStory () {
		return user.party.state == PartyMembers.STORY;
	}

	private Vector2 GetMouseInput (string[] layer) {
		RaycastHit2D hit = Physics2D.Raycast (mainCamera.ScreenToWorldPoint (Input.mousePosition), 
			Vector2.zero, 1f, LayerMask.GetMask (layer[0]));
		return (new Vector3 (hit.point.x, hit.point.y, transform.position.z) - transform.position).normalized;
	}

	private void SelectAbility () {
		if (Input.GetKeyUp (KeyCode.Alpha1)) {
			abilities.SelectAbility (1);
		} else if (Input.GetKeyUp (KeyCode.Alpha2)) {
			abilities.SelectAbility (2);
		} else if (Input.GetKeyUp (KeyCode.Alpha3)) {
			abilities.SelectAbility (3);
		} else if (Input.GetKeyUp (KeyCode.Alpha4)) {
			abilities.SelectAbility (4);
		}

		curSpeed = stats.speed;
		if (Input.GetKey (KeyCode.LeftShift)) {
			if (IsStory ()) {
				curSpeed = stats.runSpeed;
				return;
			}
			if (abilities.Sprint ()) {
				curSpeed = stats.runSpeed;
			} else {
				curSpeed = stats.speed;
			}		
		}
	}

	protected override void Attack () {
		if (abilities == null) {
			return;
		}

		SelectAbility ();

		Ability selectedAbility = abilities.GetSelectedAbility ();

		if (Input.GetKeyUp (KeyCode.Space) && canAttack) {
			if (selectedAbility.type == Ability.Mele) {
				if (!abilities.UseAbility ()) {
					return;
				}
				PlayAnimation ("PlayMeleAttackAnimation");
			} else if (selectedAbility.type == Ability.ForkBomb) {
				RaycastHit2D hit = Physics2D.Raycast (mainCamera.ScreenToWorldPoint (Input.mousePosition), 
					Vector2.zero, 1f, LayerMask.GetMask ("ForkBomb"));
				if (hit.transform != null) {
					if (!abilities.UseAbility ()) {
						return;
					}
					hit.transform.GetComponent<Computer> ().Explode (this);
					PlayAnimation ("PlayForkBombAttackAnimation");
				}
			} else if (selectedAbility.type == Ability.DebugGun) {
				if (!abilities.UseAbility ()) {
					return;
				}

				PlayAnimation ("PlayBulletAttackAnimation");
			} else if (selectedAbility.type == Ability.ElectricShock) {
				if (!abilities.UseAbility ()) {
					return;
				}
				CircleCollider2D electricRange = gameObject.AddComponent<CircleCollider2D> () as CircleCollider2D;
				electricRange.isTrigger = true;
				electricRange.radius = 1;
				Collider2D[] colls = new Collider2D[50];
				ContactFilter2D filter = new ContactFilter2D();
				filter.NoFilter ();
				electricRange.OverlapCollider(filter, colls);
				foreach (Collider2D coll in colls) {
					if (coll == null) {
						break;
					}
					if (coll.gameObject.GetComponent<Enemy> () != null) {
						// should do another class or something where we divide attack by 2
						coll.gameObject.GetComponent<Enemy> ().GetHit (this);
					}
				}
				PlayAnimation ("PlayElectricShockAttackAnimation");
			}
		}
	}

	protected override void Move () {
		if (!canMove) {
			return;
		}

		if (photonView.isMine) {
			// GET MOVEMENT INPUT
			float h = Input.GetAxisRaw ("Horizontal");
			float v = Input.GetAxisRaw ("Vertical");

			// MOVEMENT
			Vector2 movement = new Vector2 (h, v).normalized;
			GetComponent<Rigidbody2D> ().velocity = movement * curSpeed;
			// RIGHT
			if (h > 0.1) {
				if (v > 0.1) {
					move = Direction.UpRight;
				} else if (v < -0.1) {
					move = Direction.DownRight;
				} else {
					move = Direction.Right;
				}
				// LEFT
			} else if (h < -0.1) {
				if (v > 0.1) {
					move = Direction.UpLeft;
				} else if (v < -0.1) {
					move = Direction.DownLeft;
				} else {
					move = Direction.Left;
				}
				// STILL
			} else {
				if (v > 0.1) {
					move = Direction.Up;
				} else if (v < -0.1) {
					move = Direction.Down;
				} else {
					move = Direction.Still;
				}
			}
		} else {
			transform.position = Vector3.Lerp(transform.position, networkPosition, 0.5f);
		}
		PlayAnimation ("Animate");
	}

	public void Killed (Enemy other) {
		stats.GainXp (other.stats.xpReward);
		if (photonView.isMine) {
			stats.UpdateDBXP ();
		}
	}
		
	public void GetHitOvertime () {
		ChangeHealth (curHP - 1);
	}

	public void GetBuff (Buff buff) {
		if (buff == Buff.Coffee) {
			IncreaseHealth(10f);
		}
	}

	public void IncreaseHealth(float points) {
		ChangeHealth (curHP + points);
	}

	public void DecreaseHealth (float points) {
		ChangeHealth (curHP - points);
	}

	public override void GetHit<E> (Entity<E> entity) {
		ChangeHealth (curHP - entity.stats.damage);
		base.GetHit (entity);
	}

	public string GetName () {
		return user.username;
	}

	// ----------------------------------------------------------------------------------------------------------
	// -------------------------------------------------SYNCH----------------------------------------------------
	// ----------------------------------------------------------------------------------------------------------

	protected override void OnSendNext (PhotonStream stream, PhotonMessageInfo info) {
		stream.SendNext (move);
		stream.SendNext (GetMouseInput (new string[] {"MouseInput"}));
	}

	protected override void OnReceiveNext (PhotonStream stream, PhotonMessageInfo info) {
		move = (Direction) stream.ReceiveNext ();
		mouseOther = (Vector2) stream.ReceiveNext ();
	}


	// ----------------------------------------------------------------------------------------------------------
	// ----------------------------------------------ANIMATIONS--------------------------------------------------
	// ----------------------------------------------------------------------------------------------------------

	private bool isAttacking = false;

	protected IEnumerator PlayMeleAttackAnimation () {
		Vector3 mouseDirection = GetMouseInput (new string[] {"MouseInput"});

		if (!photonView.isMine) {
			mouseDirection = mouseOther;
		}

		bool inverted = Vector3.Cross (attackRadius.transform.localPosition, mouseDirection).z < 0;
		float angle = Vector3.Angle (attackRadius.transform.localPosition, mouseDirection);
		angle = inverted ? -angle : angle;
		attackRadius.transform.RotateAround (transform.position, Vector3.forward, angle);

		isAttacking = true;
		attackRadius.GetComponent<Animator> ().Play ("Slash");
		attackRadius.GetComponent<PlayerAttack> ().StartAttack ();
		GetComponent<Animator> ().Play ("P"+(user.character.type+1)+"_LaptopAttack");
		yield return new WaitForSeconds (0.1f);
		attackRadius.GetComponent<Animator> ().Play ("Default");
		yield return new WaitForSeconds (0.2f);
		attackRadius.GetComponent<PlayerAttack> ().StopAttack ();
		isAttacking = false;
	}

	protected IEnumerator PlayForkBombAttackAnimation () {
		isAttacking = true;
		attackRadius.GetComponent<PlayerAttack> ().StartAttack ();

		GetComponent<Animator> ().Play ("P"+(user.character.type+1)+"_LaptopAttack");
		yield return new WaitForSeconds (0.9f);
		attackRadius.GetComponent<Animator> ().Play ("Default");
		attackRadius.GetComponent<PlayerAttack> ().StopAttack ();
		isAttacking = false;
	}

	protected IEnumerator PlayElectricShockAttackAnimation () {
		isAttacking = true;
		attackRadius.GetComponent<PlayerAttack> ().StartAttack ();
		Object ectricity = Resources.Load ("Electricity");
		GameObject ob = NetworkService.GetInstance ().SpawnScene (ectricity.name, transform.position, Quaternion.identity, 0);
		GetComponent<Animator> ().Play ("P"+(user.character.type+1)+"_LaptopAttack");
		yield return new WaitForSeconds (0.9f);
		NetworkService.GetInstance().Destroy (ob);
		attackRadius.GetComponent<Animator> ().Play ("Default");
		attackRadius.GetComponent<PlayerAttack> ().StopAttack ();
		isAttacking = false;
	}

	protected IEnumerator PlayBulletAttackAnimation () {
		isAttacking = true;

		Object bullet = Resources.Load ("vimBullet");
		((GameObject)bullet).GetComponent<Bullet> ().SetPlayer(this);
		Vector3 mouseDirection = GetMouseInput (new string[] {"MouseInput"});

		if (!photonView.isMine) {
			mouseDirection = mouseOther;
		}

		GameObject ob = NetworkService.GetInstance ().SpawnScene (bullet.name, transform.position, Quaternion.identity, 0);
		((GameObject)ob).GetComponent<Rigidbody2D> ().velocity = mouseDirection * 2f;

		attackRadius.GetComponent<PlayerAttack> ().StartAttack ();
		GetComponent<Animator> ().Play ("P"+(user.character.type+1)+"_LaptopAttack");
		yield return new WaitForSeconds (0.3f);
		attackRadius.GetComponent<Animator> ().Play ("Default");
		attackRadius.GetComponent<PlayerAttack> ().StopAttack ();
		isAttacking = false;
	}

	protected override IEnumerator PlayDeadAnimation () {
		move = Direction.Dead;
		GetComponent<Rigidbody2D> ().velocity = Vector2.zero;
		PlayAnimation ("Animate");
		yield return GetEmptyIE ();
	}

	protected override IEnumerator PlayGetHitAnimation () {
		GetComponent<SpriteRenderer> ().color = UnityEngine.Color.red;
		yield return new WaitForSeconds (0.1f);
		GetComponent<SpriteRenderer> ().color = UnityEngine.Color.white;
	}

	protected IEnumerator Animate () {
		if (!isAttacking) {
			Animator animator = GetComponent<Animator> ();
			animator.speed = curSpeed;

			int index = user.character.type + 1;

			switch (move) {
			case Direction.Still:
				animator.Play ("P"+index+"_Still");
				break;
			case Direction.Down:
				animator.Play ("P"+index+"_Down");
				break;
			case Direction.Up:
				animator.Play ("P"+index+"_Up");
				break;
			case Direction.Left:
				animator.Play ("P"+index+"_Left");
				break;
			case Direction.Right:
				animator.Play ("P"+index+"_Right");
				break;
			case Direction.UpRight:
				animator.Play ("P"+index+"_UpRight");
				break;
			case Direction.UpLeft:
				animator.Play ("P"+index+"_UpLeft");
				break;
			case Direction.DownRight:
				animator.Play ("P"+index+"_DownRight");
				break;
			case Direction.DownLeft:
				animator.Play ("P"+index+"_DownLeft");
				break;
			case Direction.Dead:
				animator.Play ("P"+index+"_Dead");
				break;
			}	
		}

		yield return GetEmptyIE ();
	}

	public void SetMovement (bool value) {
		this.canMove = value;
	}
}

