﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : Entity<EnemyStats> {

	public Transform target;
	public bool attackingPlayer;
	public float actionTime = 1f;
	public float nextAction = 0;
	public Grid grid;
	public Point lastTargetPos;
	public BreadCrumb currentBr;
	public bool targetInRange = false;

	// Use this for initialization
	new void Start () {
		base.Start ();
		grid = GetComponentInParent<Grid> ();
	}

	void OnTriggerEnter2D(Collider2D coll) {
		if (coll.gameObject.tag == "Player") {
			targetInRange = true;
		}
	}

	void OnTriggerExit2D(Collider2D coll) {
		if (coll.gameObject.tag == "Player") {
			targetInRange = false;
		}
	}

	private bool FindNewTarget () {
		var targets = GameObject.FindGameObjectsWithTag ("Player");
		if (targets.Length == 0) {
			return false;
		}
		target = targets [UnityEngine.Random.Range (0, targets.Length - 1)].transform;
		return true;
	}

	private bool HasTarget () {
		return target != null;
	}

	protected override void Move () {
		if (!HasTarget ()) {
			if (!FindNewTarget ()) {
				return;
			}
		}

		if (targetInRange) {
			// pathfinding
			Point curTargetPos = CurrentTargetPoint();
			// RECALCULATE
			if (!curTargetPos.Equals (lastTargetPos)) {
				lastTargetPos = curTargetPos;

				if (grid.endless) {
					Vector3 movement = target.position - transform.position;
					GetComponent<Rigidbody2D> ().velocity = movement.normalized * 0.8f;
				} else {

				if (curTargetPos != null) {
					Point enemyPos = CurrentEnemyPoint ();

					currentBr = PathFinder.FindPath (grid, enemyPos, curTargetPos);
					// grid.DrawPath (currentBr);
					if (currentBr != null) {
						currentBr = currentBr.next;
					} else {
						Vector3 movement = target.position - transform.position;
						GetComponent<Rigidbody2D> ().velocity = movement.normalized * 0.8f;
					}
				}
				}
			}
			// MOVEMENT
			MoveEnemy ();
		}

		PlayAnimation ("Rotate");
	}

	public Point CurrentTargetPoint() {
		return new Point (Mathf.RoundToInt((target.localPosition.x * 2)), Mathf.RoundToInt(target.localPosition.y * 2));
	}

	public Point CurrentEnemyPoint() {
		return new Point (Mathf.RoundToInt((transform.localPosition.x * 2)), Mathf.RoundToInt(transform.localPosition.y * 2));
	}

	void OnCollisionExit2D(Collision2D coll) {
		if (coll.gameObject.tag == "Player") {
			PlayNormalAnimation ();
			attackingPlayer = false;
		}
	}

	public virtual void MoveEnemy () {
		if (grid.endless) {
			Vector3 movement = target.position - transform.position;
			GetComponent<Rigidbody2D> ().velocity = movement.normalized * 0.8f;
			return;
		} else {
			if (currentBr != null) {
				Vector2 bcRealPos = currentBr.toRealCoordinates (grid);
				if (Vector2.Distance (transform.position, 
					   new Vector2 (bcRealPos.x, bcRealPos.y)) > 0.1f) {
					Vector3 movement = new Vector2 (bcRealPos.x - transform.position.x,
						                  bcRealPos.y - transform.position.y);
					GetComponent<Rigidbody2D> ().velocity = movement.normalized * 0.8f;
				} else {
					currentBr = currentBr.next;
				}
			} else {
				if (Vector2.Distance (target.position, transform.position) < 0.2f) {
					GetComponent<Rigidbody2D> ().velocity = new Vector2 (0, 0);
				} else {
					Vector3 movement = target.position - transform.position;
					GetComponent<Rigidbody2D> ().velocity = movement.normalized * 0.8f;
					return;
				}
			}
		}
	}

	public override void GetHit<E> (Entity<E> entity) {
		if (isDead ()) {
			if (entity is Player) {
				(entity as Player).Killed (this);	
			}
		}

		base.GetHit<E> (entity);
	}

	// ----------------------------------------------------------------------------------------------------------
	// -------------------------------------------------SYNCH----------------------------------------------------
	// ----------------------------------------------------------------------------------------------------------

	protected override void OnSendNext (PhotonStream stream, PhotonMessageInfo info) {

	}

	protected override void OnReceiveNext (PhotonStream stream, PhotonMessageInfo info) {

	}

	// ----------------------------------------------------------------------------------------------------------
	// ----------------------------------------------ANIMATIONS--------------------------------------------------
	// ----------------------------------------------------------------------------------------------------------

	protected override IEnumerator PlayDeadAnimation () {
		yield return GetEmptyIE ();
		gameObject.SetActive (false);
	}

	protected virtual IEnumerator PlayAttackAnimation() {
		yield return GetEmptyIE ();
	}
		
	protected virtual IEnumerator PlayNormalAnimation () {
		yield return GetEmptyIE ();
	}

	protected virtual IEnumerator Rotate() {
		yield return GetEmptyIE ();
	}

	// ----------------------------------------------------------------------------------------------------------
	// ---------------------------------------------------A*-----------------------------------------------------
	// ----------------------------------------------------------------------------------------------------------

	void OnCollisionEnter2D(Collision2D coll) {
		if (coll.gameObject.tag.Equals("Player")) {
			PlayAnimation ("PlayAttackAnimation");
			attackingPlayer = true;
		} else {
			if (currentBr != null) {
				BreadCrumb next = currentBr.next;
				if (next == null) {
					Vector3 movement = target.position - transform.position;
					GetComponent<Rigidbody2D> ().velocity = movement.normalized * 0.8f;
					return;
				}
				/*// Debug.Log ("current: " + currentBr.position.X + ", " + currentBr.position.Y);
				// Debug.Log ("next: " + next.position.X + ", " + next.position.Y);
				int xdiff = currentBr.position.X - next.position.X;
				int ydiff = currentBr.position.Y - next.position.Y;
				// LEFT
				if (xdiff == 1) {
					// LEFT - DOWN
					if (ydiff == 1) {
						// Collider on the left
						if (GetComponent<Rigidbody2D> ().velocity.x > GetComponent<Rigidbody2D> ().velocity.y) {
							// Debug.Log ("LEFT-DOWN collider left");
							currentBr.position = new Point (currentBr.position.X, currentBr.position.Y - 1);
							return;
							// Collider down
						} else {
							// Debug.Log ("LEFT-DOWN collider down");
							currentBr.position = new Point (currentBr.position.X - 1, currentBr.position.Y);
							return;
						}

						// LEFT - UP
					} else if (ydiff == -1) {
						// Collider on the left
						if (-GetComponent<Rigidbody2D> ().velocity.x > GetComponent<Rigidbody2D> ().velocity.y) {
							// Debug.Log ("LEFT-UP collider left");
							currentBr.position = new Point (currentBr.position.X, currentBr.position.Y + 1);
							return;
							// Collider up
						} else {
							// Debug.Log ("LEFT-UP collider up");
							currentBr.position = new Point (currentBr.position.X - 1, currentBr.position.Y);
							return;
						}
						// LEFT
					} else {
						// Debug.Log ("LEFT");
						// collider up
						if (coll.transform.position.y > transform.position.y) {
							// Debug.Log ("LEFT collider up");
							currentBr.position = new Point (currentBr.position.X - 1, currentBr.position.Y - 1);
							return;
							// Collider down
						} else {
							// Debug.Log ("LEFT collider down");
							currentBr.position = new Point (currentBr.position.X - 1, currentBr.position.Y + 1);
							return;
						}
					}
					// RIGHT
				} else if (xdiff == -1) {
					// RIGHT - DOWN
					if (ydiff == 1) {
						// Collider on the right
						if (GetComponent<Rigidbody2D> ().velocity.x > -GetComponent<Rigidbody2D> ().velocity.y) {
							// Debug.Log ("RIGHT-DOWN collider right");
							currentBr.position = new Point (currentBr.position.X, currentBr.position.Y - 1);
							return;
							// Collider down
						} else {
							// Debug.Log ("RIGHT-DOWN collider down");
							currentBr.position = new Point (currentBr.position.X + 1, currentBr.position.Y);
							return;
						}
						// RiGHT - UP
					} else if (ydiff == -1) {
						// Collider on the right
						if (GetComponent<Rigidbody2D> ().velocity.x > GetComponent<Rigidbody2D> ().velocity.y) {
							// Debug.Log ("RIGHT-UP collider right");
							currentBr.position = new Point (currentBr.position.X, currentBr.position.Y + 1);
							return;
							// Collider up
						} else {
							// Debug.Log ("RIGHT-UP collider up");
							currentBr.position = new Point (currentBr.position.X + 1, currentBr.position.Y);
							return;
						}
					} else {
						// Debug.Log ("RIGHT");
						// collider up
						if (coll.transform.position.y > transform.position.y) {
							// Debug.Log ("RIGHT collider up");
							currentBr.position = new Point (currentBr.position.X + 1, currentBr.position.Y - 1);
							return;
							// Collider down
						} else {
							// Debug.Log ("RIGHT collider down");
							currentBr.position = new Point (currentBr.position.X + 1, currentBr.position.Y + 1);
							return;
						}
					}
					// UP OR DOWN
				} else {
					// DOWN
					if (ydiff == 1) {
						// collider right
						if (coll.transform.position.x > transform.position.x) {
							// Debug.Log ("DOWN collider right");
							currentBr.position = new Point (currentBr.position.X - 1, currentBr.position.Y + ydiff);
							return;
							// Collider left
						} else {
							// Debug.Log ("DOWN collider left");
							currentBr.position = new Point (currentBr.position.X + 1, currentBr.position.Y + ydiff);
							return;
						}

						// UP
					} else {
						// collider right
						if (coll.transform.position.x > transform.position.x) {
							// Debug.Log ("UP collider right");
							currentBr.position = new Point (currentBr.position.X - 1, currentBr.position.Y + ydiff);
							return;
							// Collider left
						} else {
							// Debug.Log ("UP collider left");
							currentBr.position = new Point (currentBr.position.X + 1, currentBr.position.Y + ydiff);
							return;
						}
					}

				}*/
			}
		}
	}
}