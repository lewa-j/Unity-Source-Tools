using UnityEngine;
using System.Collections;
using uSrcTools;

public class Player : MonoBehaviour 
{
	public float normalSpeed=5;
	public float runSpeed = 15;
	float speed=5;
	public bool doMove=true;
	public bool crouch=false;
	CapsuleCollider coll;
	float origHeight;
	public Transform cam;
	//public Transform skyCam;

	void Start () 
	{
		coll=GetComponent<CapsuleCollider>();
		origHeight=coll.height;
		speed = normalSpeed;

		//transform.position=Test.Inst.startPos;
	}
	
	void Update()
	{
		if(Test.Inst.skyCamera!=null)
		{
			Test.Inst.skyCamera.transform.localPosition=(Test.Inst.playerCamera.transform.position/16)+Test.Inst.skyCameraOrigin;
			Test.Inst.skyCamera.transform.rotation=Test.Inst.playerCamera.transform.rotation;
		}
		if (Input.GetKey (KeyCode.LeftShift))
			speed = runSpeed;
		else
			speed = normalSpeed;
			
		/*Vector3 move = transform.right * Input.GetAxis ("Horizontal") +
		               transform.forward * Input.GetAxis ("Vertical");
		*/	
		transform.Translate(new Vector3 (Input.GetAxis ("Horizontal"),
		                                Input.GetAxis ("UpDown"),
		                                Input.GetAxis ("Vertical"))*speed*Time.deltaTime);
		
		if(Input.GetKeyDown(KeyCode.LeftControl))
		{
			crouch=!crouch;

			if(crouch)
			{
				coll.height=0.5f;
				//cam.localPosition=new Vector3(0,0.4f,0);
			}
			else
			{
				coll.height=origHeight;
				//cam.localPosition=new Vector3(0,origHeight-0.1f,0);
			}
		}
	}

	/*void FixedUpdate () 
	{
		if (Input.GetKey (KeyCode.LeftShift))
			speed = runSpeed;
		else
			speed = normalSpeed;

		
		Vector3 move = transform.right * Input.GetAxis ("Horizontal") +
		               transform.forward * Input.GetAxis ("Vertical");

		rb.MovePosition (transform.position+(move * speed * Time.fixedDeltaTime));
	
		if (Input.GetButtonDown ("Jump")) 
		{
			rb.AddForce(Vector3.up*10000);
		}

		if(Input.GetKeyDown(KeyCode.R))
		{
			rb.velocity=Vector3.zero;
			transform.position=Test.Inst.startPos;
		}

		if(Input.GetKeyDown(KeyCode.LeftControl))
		{
			crouch=!crouch;

			if(crouch)
			{
				coll.height=0.5f;
				//cam.localPosition=new Vector3(0,0.4f,0);
			}
			else
			{
				coll.height=origHeight;
				//cam.localPosition=new Vector3(0,origHeight-0.1f,0);
			}
		}
	}*/
}
