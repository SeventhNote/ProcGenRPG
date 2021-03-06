﻿using UnityEngine;
using System.Collections;

public class PlayerControl : MonoBehaviour {
	
	public float speed;

	private static Animator playerAnim;

	private bool comboTime = false;

	private bool attack1, attack2, attack3;

	private static Transform camTransform;

	private static Player playerref;
	
	private static LineRenderer rangedIndicator;
	
	public static bool PLAYINGWITHOCULUS;
	
	// Use this for initialization
	void Start () {
		playerAnim = this.GetComponent<Animator>();
		playerref = this.GetComponent<Player>();
		rangedIndicator = GameObject.Find("RangedAimIndicator").GetComponent<LineRenderer>();
		camTransform = transform.parent.GetChild(1).transform;
		PLAYINGWITHOCULUS = transform.parent.gameObject.name.Equals("OculusPlayer");
		rangedIndicator.enabled = false;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		/****** Resetting animation booleans to false ****/
		playerAnim.SetBool("Slash1", false);
		playerAnim.SetBool("Slash2", false);
		playerAnim.SetBool("Slash3", false);
		playerAnim.SetBool("ShootBow", false);

		/***** Updates booleans to check what attack player is in *****/
		attack1 = playerAnim.GetCurrentAnimatorStateInfo(0).IsName("Base.Slash1") || playerAnim.GetCurrentAnimatorStateInfo(1).IsName("SlashWalking.SlashWalking");
		attack2 = playerAnim.GetCurrentAnimatorStateInfo(0).IsName("Base.Slash2");
		attack3 = playerAnim.GetCurrentAnimatorStateInfo(0).IsName("Base.Slash3");

		/****** Set movement variables *****/
		playerAnim.SetFloat("Speed",Input.GetAxis("Vertical"));
//		playerAnim.SetFloat("Direction",Input.GetAxis("Horizontal"));
		if(playerAnim.GetFloat("Speed") == 0) {
			playerAnim.transform.Rotate(new Vector3(0f, playerAnim.GetFloat("Direction"), 0f));
		}

		/***** Rolling functionality ****/
		if(Input.GetKey(KeyCode.Space)) {
			playerAnim.SetBool("Roll", true);
		} else {
			playerAnim.SetBool("Roll", false);
		}

		/***** Running functionality ****/
		if(playerAnim.GetFloat("Speed") > 0.5f && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
			if(playerAnim.speed < 1.5f) {
				playerAnim.speed += Time.deltaTime/2f;
			}
		} else {
			playerAnim.speed = 1;
		}
		GetComponent<CapsuleCollider>().height = 0.1787377f - 0.1f*playerAnim.GetFloat("ColliderHeight");
		GetComponent<CapsuleCollider>().center = new Vector3(0,0.09f - 0.09f*playerAnim.GetFloat("ColliderY"), 0f);


		/**** Simple front collision handler *****/
		if(playerAnim.GetFloat("Speed") > 0.5f) {
			RaycastHit info = new RaycastHit();
			if(Physics.Raycast(new Ray(this.transform.position + new Vector3(0f, 1f, 0f), this.transform.forward), out info, playerAnim.GetFloat("Speed")*1.5f)) {
				if(!info.collider.gameObject.name.Equals("Byte")) {
					playerAnim.SetFloat("Speed",Mathf.Min(Input.GetAxis("Vertical"),0));
				}
			}
		}

		/**** Messy way of handling LightStick's green trail ****/
		if(attack3) {
			GetComponent<Player>().StartAttack();
		} else {
			GetComponent<Player>().StopAttack();
		}

		/***** Kinda messy way of binding numbers to quick access items ****/
		if(Input.GetKeyDown(KeyCode.Alpha1)) {
			playerref.SetActiveItem(0);
		} else if (Input.GetKeyDown(KeyCode.Alpha2)) {
			playerref.SetActiveItem(1);
		} else if(Input.GetKeyDown(KeyCode.Alpha3)) {
			playerref.SetActiveItem(2);
		} else if (Input.GetKeyDown(KeyCode.Alpha4)) {
			playerref.SetActiveItem(3);
		} else if (Input.GetKeyDown(KeyCode.Alpha5)) {
			playerref.SetActiveItem(4);
		} else if(Input.GetKeyDown(KeyCode.Alpha6)) {
			playerref.SetActiveItem(5);
		} else if (Input.GetKeyDown(KeyCode.Alpha7)) {
			playerref.SetActiveItem(6);
		} else if (Input.GetKeyDown(KeyCode.Alpha8)) {
			playerref.SetActiveItem(7);
		} else if(Input.GetKeyDown(KeyCode.Alpha9)) {
			playerref.SetActiveItem(8);
		} else if (Input.GetKeyDown(KeyCode.Alpha0)) {
			playerref.SetActiveItem(9);
		}

		/**** Handling attacking ****/
		if(!PlayerCanvas.inConsole) {
			if (playerref.GetWeapon() != null && playerref.GetWeapon().Type().Equals(WeaponType.Melee)) {
				if (Input.GetMouseButtonDown(0) && comboTime && !attack1 && attack2) {
					playerAnim.SetBool("Slash3", true);
					comboTime = false;
				}

				if (Input.GetMouseButtonDown(0) && comboTime && attack1 && !attack2) {
					playerAnim.SetBool("Slash2", true);
					comboTime = false;
				}

				if (Input.GetMouseButtonDown(0) && !attack2 && !attack3) {
					playerAnim.SetBool("Slash1", true);
					comboTime = false;
				}
			} else if (playerref.GetWeapon() != null && playerref.GetWeapon().Type().Equals(WeaponType.Bow)) {
				if (Input.GetMouseButtonDown(0)) {
					if(playerAnim.GetFloat("Speed") < 0.2f) {
						rangedIndicator.enabled = true;
					}
					playerAnim.SetBool("DrawArrow", true);
				} else if (Input.GetMouseButtonUp(0)) {
					rangedIndicator.enabled = false;
					playerAnim.SetBool("DrawArrow", false);
					playerAnim.SetBool("ShootBow", true);
				}

				if(!Input.GetMouseButton(0)) {
					rangedIndicator.enabled = false;
					playerAnim.SetBool("DrawArrow", false);
				}
			}

			if (playerref.GetHack() != null && Input.GetMouseButton(1)) {
				playerref.Hack();
			}
		}

		/****** Making the player look at the mouse *******/
		float mousePosX = Input.mousePosition.x;
		float mousePosY = Input.mousePosition.y + Screen.height/10f;
		float screenX = Screen.width;
		float screenY = Screen.height;
		float angle;
		if(playerAnim.GetFloat("Speed") < 0.1f) {
			if (mousePosY < screenY/2) {
				angle = Mathf.Rad2Deg * Mathf.Atan(((mousePosX/screenX*2) - 1)/((mousePosY/screenY*2) - 1)) + 180;
			} else {
				angle = Mathf.Rad2Deg * Mathf.Atan(((mousePosX/screenX*2) - 1)/((mousePosY/screenY*2) - 1));
			}
			transform.eulerAngles = new Vector3(0f, angle + camTransform.eulerAngles.y, 0f);
		} else {
			transform.eulerAngles = new Vector3(0f, camTransform.eulerAngles.y, 0f);
		}

//		if(!PLAYINGWITHOCULUS) {
//			transform.eulerAngles = new Vector3(0f, angle + camTransform.eulerAngles.y, 0f);
//		} else {
//			transform.eulerAngles = new Vector3(0f, camTransform.eulerAngles.y, 0f);
//		}

		/** Other ways of handling looking **/
		//transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(new Vector3(0f, transform.eulerAngles.y + Input.GetAxis("Mouse X")*(2-playerAnim.GetFloat("Speed")), 0f)), 2000*Time.deltaTime);
		//playerAnim.SetFloat("Direction",(mousePosX - screenX/2f)/(screenX/2f));
	}

	/**
	 * Called by Mecanim to set when combo attacks can and can't occur
	 */
	void SetComboTime() {
		comboTime = !comboTime;
	}

}
