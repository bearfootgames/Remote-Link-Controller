using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TelloLib;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System;
using UnityEngine.EventSystems;
using UnityEditor;

public class TelloController : SingletonMonoBehaviour<TelloController> {

	private static bool isLoaded = false;
	public TelloVideoTexture telloVideoTexture;
	public NetworkHandler networkhandler;
	public Text networktext;
	public Text message;
	public Text ControlInputMessage;
	public GameObject drone;
	public GameObject ghostdrone;
	public GameObject pointprefab;
	public GameObject[] canvas;
	public GameObject[] settingsmenu;
	public GameObject[] accountmenus;
	Auth auth;
	public static bool showdronestatus = false;

	#if UNITY_ANDROID || UNITY_IOS

	public GameObject DualStickCanvas;
	public GameObject DualStickTouchController;
	public LeftJoystick leftJoystick; // the game object containing the LeftJoystick script
    public RightJoystick rightJoystick; // the game object containing the RightJoystick script
	private Vector3 leftJoystickInput; // holds the input of the Left Joystick
    private Vector3 rightJoystickInput; // hold the input of the Right Joystick
	
	#endif

	public string videopath;
	public string videoname;
	//------tracking - autopilot-------

	//live positions
	public Vector3 pos, lastPos;
	Vector4 quat;
	//list of positions and corilating transforms
	List<Transform> tran_list = new List<Transform>();//list of point transforms
	List<Vector3> pos_list = new List<Vector3>();//list of point Vector3
	public enum AutoPilot {off,home,orbit,rec,trace};
	public AutoPilot autoPilot = AutoPilot.off;
	float rec_interval = 1.0f;
	float rec_timer = 0f;
	Vector3 target_pos;
	Transform target_transform;
	public Vector3 originPoint = new Vector3(0,0,0);
	public Vector3 toEuler;
	Vector3 prevDeltaPos;
 	public float yaw, pitch, roll;
	bool localStateUpdated = false;
	bool ghostrotFound = false;
	bool brake = false;
	float Bpitch;
	float Byaw;
	float Broll;
	float ghostrot;
	float elevationOffset = 0f;
	float minDist = .9f;

	//-------Tello api--------
	public float posUncertainty;
	public bool batteryLow;
	public int batteryPercent;
	public int cameraState;
	public bool downVisualState;
	public int telloBatteryLeft;
	public int telloFlyTimeLeft;
	public int flymode;
	public int flyspeed;
	public int flyTime;
	public bool flying;
	public bool gravityState;
	public int height;
	public int imuCalibrationState;
	public bool imuState;
	public int lightStrength;
	public bool onGround = true;
	public bool powerState;
	public bool pressureState;
	public int temperatureHeight;
	public int wifiDisturb;
	public int wifiStrength;
	public bool windState;

	 bool testFlight = false;

	// FlipType is used for the various flips supported by the Tello.
	public enum FlipType
	{
		// FlipFront flips forward.
		FlipFront = 0,

		// FlipLeft flips left.
		FlipLeft = 1,

		// FlipBack flips backwards.
		FlipBack = 2,

		// FlipRight flips to the right.
		FlipRight = 3,

		// FlipForwardLeft flips forwards and to the left.
		FlipForwardLeft = 4,

		// FlipBackLeft flips backwards and to the left.
		FlipBackLeft = 5,

		// FlipBackRight flips backwards and to the right.
		FlipBackRight = 6,

		// FlipForwardRight flips forewards and to the right.
		FlipForwardRight = 7,
	};

	// VideoBitRate is used to set the bit rate for the streaming video returned by the Tello.
	public enum VideoBitRate
	{
		// VideoBitRateAuto sets the bitrate for streaming video to auto-adjust.
		VideoBitRateAuto = 0,

		// VideoBitRate1M sets the bitrate for streaming video to 1 Mb/s.
		VideoBitRate1M = 1,

		// VideoBitRate15M sets the bitrate for streaming video to 1.5 Mb/s
		VideoBitRate15M = 2,

		// VideoBitRate2M sets the bitrate for streaming video to 2 Mb/s.
		VideoBitRate2M = 3,

		// VideoBitRate3M sets the bitrate for streaming video to 3 Mb/s.
		VideoBitRate3M = 4,

		// VideoBitRate4M sets the bitrate for streaming video to 4 Mb/s.
		VideoBitRate4M = 5,
	};

	override protected void Awake()
	{
		if(PlayerPrefs.HasKey("language")){
			language = PlayerPrefs.GetInt("language")-1;
			LanguageSelect();
		}
		if (!isLoaded) {
			DontDestroyOnLoad(this.gameObject);
			isLoaded = true;
		}
		base.Awake();

		Tello.onConnection += Tello_onConnection;
		Tello.onUpdate += Tello_onUpdate;
		Tello.onVideoData += Tello_onVideoData;

		if (telloVideoTexture == null){
			telloVideoTexture = FindObjectOfType<TelloVideoTexture>();
		}

		auth = accountmenus[language].GetComponent<Auth>();
		auth.Open();
	}

	private void OnEnable()
	{
		if (telloVideoTexture == null){
			telloVideoTexture = FindObjectOfType<TelloVideoTexture>();
		}
	}

	public int language;
	public void LanguageSelect()
	{
		language+=1;
		if(language == canvas.Length){language=0;}
		
		for(int i =0; i < canvas.Length; i++){
			if(i == language){
				canvas[i].SetActive(true);
			}else{
				canvas[i].SetActive(false);
			}
		}

		auth = accountmenus[language].GetComponent<Auth>();

		PlayerPrefs.SetInt("language",language);

	}

	//video recording vars
	#if INCLUDE_VIDEO
	FFmpegOut.CameraCapture camperacapturescript;
	#endif
	private void Start()
	{
		settingsmenu[language].SetActive(true);

		#if UNITY_STANDALONE || UNITY_WEBGL || UNITY_WSA
		//Destroy(GameObject.Find("Dual-Joystick Canvas"));
		//Destroy(GameObject.Find("Dual-Joystick Touch Controller"));
		#endif

		#if UNITY_ANDROID || UNITY_IOS
		DualStickCanvas = GameObject.Find("Dual-Joystick Canvas");
		DualStickTouchController = GameObject.Find("Dual-Joystick Touch Controller");

		leftJoystick = DualStickCanvas.transform.Find("Left Joystick").GetComponent<LeftJoystick>(); // the game object containing the LeftJoystick script
   		rightJoystick = DualStickCanvas.transform.Find("Right Joystick").GetComponent<RightJoystick>(); // the game object containing the RightJoystick script
		Destroy(GameObject.Find("TelloHowToControl"));
		#else
		GameObject.Find("Dual-Joystick Canvas").SetActive(false);
		GameObject.Find("Dual-Joystick Touch Controller").SetActive(false);
		#endif

		if (telloVideoTexture == null){
			telloVideoTexture = FindObjectOfType<TelloVideoTexture>();
		}

		//video recording setup
		#if INCLUDE_VIDEO
		camperacapturescript =	GameObject.Find("Main Camera").AddComponent<FFmpegOut.CameraCapture>();
		#endif
		#if !INCLUDE_VIDEO
		GameObject.Find("Button_RecordVideo").SetActive(false);
		#endif

		Tello.startConnecting();
		
	}

	public void handle_video()
	{
		#if INCLUDE_VIDEO
		camperacapturescript.video_handler();
		#endif
	}

	void OnApplicationQuit()
	{
		Tello.stopConnecting();
	}

	//from server to client
	public void Network_Update(bool t,bool l,float lx ,float ly,float rx , float ry)
	{
		if (t) {
			TakeOff();
		} 
		if (l) {
			Land();
		}
		ControlInputMessage.text = "Received Input: "+t+","+ l+","+lx+","+ly+","+rx+","+ry;
		Tello.controllerState.setAxis(lx, ly, rx, ry);
	}

	public void TakeOff()
	{
		transform.position = new Vector3(0,0,0);
		message.text = "Taking Off - Auto Pilot Mode: "+autoPilot;
		if(networkhandler.isServer){
			networkhandler.serverscript.sendcontrollermessage(true,false,0,0,0,0);
			return;
		}
		Tello.takeOff();
		StartCoroutine( CheckForLaunchComplete() );
	}
	public void Land()
	{
		Debug.Log("Landing");
		autoPilot = AutoPilot.off;
		message.text = "Landing - Auto Pilot Mode: "+autoPilot;
		if(networkhandler.isServer){
			networkhandler.serverscript.sendcontrollermessage(false,true,0,0,0,0);
			return;
		}
		Tello.land();
	}
	float lx = 0f;
	float ly = 0f;
	float rx = 0f;
	float ry = 0f;
	float w,x,y,z;
	bool matchGhost = false;
	void Update ()
	{
		if(!auth.thisAuth){
			message.text = "THIS DEVICE IS NOT AUTHORIZED";
			return;
		}else {
			if(message.text == "THIS DEVICE IS NOT AUTHORIZED"){
				message.text = "THIS DEVICE IS AUTHORIZED";
			}
		}

		if(auth.freetrial){
            auth.freetrialtimer -= 1f *Time.deltaTime;
            if(auth.freetrialtimer <=0){
                auth.thisAuth = false;
                auth.freetrial = false;
                auth.Open();
				return;
            }
        }
		
		CheckForUpdate();
		
		lx = 0f;
		ly = 0f;
		rx = 0f;
		ry = 0f;

		#if UNITY_ANDROID
		// get input from both joysticks
        rightJoystickInput = leftJoystick.GetInputDirection();
        leftJoystickInput = rightJoystick.GetInputDirection();
	
		lx = Mathf.Round(leftJoystickInput.x); // The horizontal movement from joystick 01
		ly = Mathf.Round(leftJoystickInput.y); // The vertical movement from joystick 01	
		rx = Mathf.Round(rightJoystickInput.x); // The horizontal movement from joystick 02
		ry= Mathf.Round(rightJoystickInput.y); // The vertical movement from joystick 02
		#endif

		#if UNTIY_EDITOR || UNITY_WEBGL || UNITY_STANDALONE || UNITY_WSA
		//keyboard input
		if (Input.GetKeyDown(KeyCode.T)) {
			TakeOff();
		}
		if (Input.GetKeyDown(KeyCode.L)) {
			Land();
		}
		if (Input.GetKey(KeyCode.UpArrow)) {
			ly = 1;//Up
			if(testFlight){transform.Translate(Vector3.up * Time.deltaTime * .5f);}
		}
		if (Input.GetKey(KeyCode.DownArrow)) {
			ly = -1;//Down
			if(testFlight){transform.Translate(-Vector3.up * Time.deltaTime * .5f);}
		}
		if (Input.GetKey(KeyCode.RightArrow)) {
			lx = 1;//TURN RIGHT
			if(testFlight){transform.Rotate(0,50 * Time.deltaTime,0, Space.Self);}
		}
		if (Input.GetKey(KeyCode.LeftArrow)) {
			lx = -1;//TURN LEFT
			if(testFlight){transform.Rotate(0,-50 * Time.deltaTime,0, Space.Self);}
		}

		if (Input.GetKey(KeyCode.W)) {
			ry = 1;//Forward
			if(testFlight){transform.Translate(Vector3.forward * Time.deltaTime * 2f);}
		}
		if (Input.GetKey(KeyCode.S)) {
			ry = -1;//Backward
			if(testFlight){transform.Translate(-Vector3.forward * Time.deltaTime * 2f);}
		}
		if (Input.GetKey(KeyCode.D)) {
			rx = 1;//Sideways Right
			if(testFlight){transform.Translate(Vector3.right * Time.deltaTime * 2f);}
		}
		if (Input.GetKey(KeyCode.A)) {
			rx = -1;//Sideways Left
			if(testFlight){transform.Translate(-Vector3.right * Time.deltaTime * 2f);}
		}
		
		/*if (Input.GetKeyDown(KeyCode.P)) {
			if(!testFlight){ testFlight = true;}else{testFlight = false;}
		}
		if (Input.GetKeyDown(KeyCode.Z)) {
			for(int i = 0; i < pos_list.Count; i++)
			{
				Debug.Log(i+" "+pos_list[i]);
			}
		}*/
		#endif

		//for testing autopilot (no drone connected)
		if(testFlight)
		{
			w = transform.rotation.w;
			x = transform.rotation.x;
			y = transform.rotation.y;
			z = transform.rotation.z;
			roll  = Mathf.Atan2(2*y*w - 2*x*z, 1 - 2*y*y - 2*z*z);
			pitch = Mathf.Atan2(2*x*w - 2*y*z, 1 - 2*x*x - 2*z*z);
			yaw  = Mathf.Asin(2*x*y + 2*z*w);
			
			toEuler = new Vector3(pitch, yaw, roll);

			if(autoPilot != AutoPilot.home)
			{
				w = ghostdrone.transform.rotation.w;
				x = ghostdrone.transform.rotation.x;
				y = ghostdrone.transform.rotation.y;
				z = ghostdrone.transform.rotation.z;
				Byaw  = Mathf.Atan2(2*y*w - 2*x*z, 1 - 2*y*y - 2*z*z);
				Bpitch = Mathf.Atan2(2*x*w - 2*y*z, 1 - 2*x*x - 2*z*z);
				Broll  =  Mathf.Asin(2*x*y + 2*z*w);
				ControlInputMessage.text = "drone:"+pitch+","+yaw+","+roll+"\nghost:"+Bpitch+","+Byaw+","+Broll;
			}
			localStateUpdated = true;
		}

		//autopilot														server device					unconnected device
		if(autoPilot != AutoPilot.off && autoPilot != AutoPilot.rec && (networkhandler.isServer || (!networkhandler.isServer && !networkhandler.isClient)))
		{
			if(localStateUpdated)
			{
				if(target_pos == null)
				{
					Debug.Log("Target Point NULL");
					target_pos = FindClosestPoint();
					//return;
				}
				
				//Home in on Current Target
				if(!matchGhost){
					//match ghost to player rotation
					ghostdrone.transform.eulerAngles = transform.eulerAngles;
					matchGhost = true;
				}
				
				ghostdrone.transform.position = transform.position;
				
				Vector3 targetDir = target_transform.position - ghostdrone.transform.position;
				// The step size is equal to speed times frame time.
				float step = 10f * Time.deltaTime;
				Vector3 newDir = Vector3.RotateTowards(ghostdrone.transform.forward, targetDir, step, 0.0f);
				Debug.DrawRay(ghostdrone.transform.position, newDir, Color.red);
				// Move our position a step closer to the target.
				ghostdrone.transform.rotation = Quaternion.LookRotation(newDir);
				//level ghost
				ghostdrone.transform.eulerAngles = new Vector3(-90f,ghostdrone.transform.eulerAngles.y,0);

				float w = ghostdrone.transform.rotation.w;
				float x = ghostdrone.transform.rotation.x;
				float y = ghostdrone.transform.rotation.y;
				float z = ghostdrone.transform.rotation.z;

				Byaw  = Mathf.Atan2(2*y*w - 2*x*z, 1 - 2*y*y - 2*z*z);
				Bpitch = Mathf.Atan2(2*x*w - 2*y*z, 1 - 2*x*x - 2*z*z);
				Broll  =  Mathf.Asin(2*x*y + 2*z*w);

				ghostrot = Byaw;
				ghostrot *= 10f;
				ghostrot = Mathf.Round(ghostrot);
				ghostrot /= 10f;

				float dronerot;
				
				dronerot = toEuler.z;
				dronerot *= 10f;
				dronerot= Mathf.Round(dronerot);
				dronerot /= 10f;
				
				//rotation distance
				float rotation_dist = .15f;
				float rDist = Vector2.Distance(new Vector2(ghostrot,0f),new Vector2(dronerot,0f));
				//compare ghost to drone rotation
				if(rDist >= rotation_dist)
				{
					ghostrotFound = false;
					
					if(rDist < 3){
						if(dronerot < ghostrot){
							lx = 1f;//Turn
							if(testFlight){transform.Rotate(0,50f * Time.deltaTime,0, Space.Self);}
						}
						if(dronerot > ghostrot){
							lx = -1f;//Turn 
							if(testFlight){transform.Rotate(0,-50f * Time.deltaTime,0, Space.Self);}
						}
					}else{
						if(dronerot > ghostrot){
							lx = 1f;//Turn
							if(testFlight){transform.Rotate(0,50f * Time.deltaTime,0, Space.Self);}
						}
						if(dronerot < ghostrot){
							lx = -1f;//Turn 
							if(testFlight){transform.Rotate(0,-50f * Time.deltaTime,0, Space.Self);}
						}
					}
					
				}else{
					ghostrotFound = true;
				}
				
				//height
				float minDist_Height = .2f;
				float hDist = Vector2.Distance(new Vector2(transform.position.y,0f),new Vector2(target_pos.y,0f));
				bool heightFound = false;
				if(transform.position.y < target_pos.y && hDist > minDist_Height){
					ly = 1;
					if(testFlight){transform.Translate(Vector3.up * Time.deltaTime * 1f);}
				}
				if(transform.position.y > target_pos.y && hDist > minDist_Height){
					ly = -1;
					if(testFlight){transform.Translate(-Vector3.up * Time.deltaTime * 1f);}
				}
				if( hDist <= minDist_Height){heightFound=true;}

				//forward go - only if height and rotation are correct
				float minDist_Forward = minDist/2f;
				// -> find x,z distance
				float pDist = Vector2.Distance(new Vector2(transform.position.x,transform.position.z), new Vector2(target_pos.x,target_pos.z));
				if(pDist > minDist_Forward && ghostrotFound && heightFound){
					ry = 1;//Forward
					if(testFlight){transform.Translate(Vector3.forward * Time.deltaTime * 1f);}
				}else{
					if(!brake && pDist <= minDist_Forward){
						ry = -1;//Stop
						brake = true;
					}
				}

				//flew off course
				/*if(pDist > minDist*5f)
				{
					Debug.Log("Off Course");
					target_pos = FindClosestPoint();
				}*/

				//reached point
				if(pDist <= minDist_Forward)
				{
					if(autoPilot == AutoPilot.home){
						Debug.Log("Reached Point");
						PrevPoint();
					}
					if(autoPilot == AutoPilot.orbit){
						Debug.Log("Reached Point");
						NextPoint();
					}
					if(autoPilot == AutoPilot.trace){
						Debug.Log("Reached Point");
						if(TraceBack){
							PrevPoint();
						}else{
							NextPoint();
						}
					}
				}
				
				//networktext.text = "rDist: "+rDist+" pDsit: "+pDist+" hDist: "+hDist+"\ndronerot: "+dronerot+" - "+pitch+","+yaw+","+roll+" | ghostrot: "+ghostrot+" - "+Bpitch+","+Byaw+","+Broll;
			
			}else{
				//local state was not updated - do something
			}
		}

		localStateUpdated = false;

		//send controller inputs
		if(networkhandler.isServer){
			//linked
			networkhandler.serverscript.sendcontrollermessage(Input.GetKey(KeyCode.T),Input.GetKey(KeyCode.L),lx,ly,rx,ry);
			ControlInputMessage.text = "Input: "+lx+" , "+ly+" , "+rx+" , "+ry;
		}
		if(!networkhandler.isServer && !networkhandler.isClient){
			//not linked
			Tello.controllerState.setAxis(lx, ly, rx, ry);
			ControlInputMessage.text = "Input: "+lx+" , "+ly+" , "+rx+" , "+ry;
		}
		
	}

	private void Tello_onConnection(Tello.ConnectionState newState)
	{
		//throw new System.NotImplementedException();
		//Debug.Log("Tello_onConnection : " + newState);
		if (newState == Tello.ConnectionState.Connected) 
		{
            Tello.queryAttAngle();
            Tello.setMaxHeight(500);
			Tello.setPicVidMode(1); // 0: picture, 1: video
			Tello.setVideoBitRate((int)VideoBitRate.VideoBitRateAuto);
			
			//Tello.setEV(0);
			Tello.requestIframe();
		}
	}

	private void Tello_onVideoData(byte[] data)
	{
		if (telloVideoTexture != null)
		{
			if(networkhandler.isClient || (!networkhandler.isClient && !networkhandler.isServer)){
				telloVideoTexture.SortData(data);
			}
		}
	}

	public IEnumerator CheckForLaunchComplete()
	{
		if(autoPilot != AutoPilot.rec){autoPilot = AutoPilot.off;}
		while(flymode != 6){
			yield return new WaitForSeconds(.05f);
		}
		// is launched
		originPoint = transform.position;
		elevationOffset = height * .1f;
		if(autoPilot == AutoPilot.rec){ClearPoints();}
	}

	bool updateReceived = false;
	private void Tello_onUpdate(int cmdId)
	{
		//throw new System.NotImplementedException();
		//Debug.Log("Tello_onUpdate :\n" + Tello.state);
		updateReceived = true;
		
	}
	public void CheckForUpdate()
	{
		if(networkhandler.isServer){return;}
		if (updateReceived)
		{
			//-----update state-----
			if(networkhandler.isClient)
			{
				//linked
				UpdateLocalState();
			}
			if(!networkhandler.isServer && !networkhandler.isClient){
				//not linked
				UpdateLocalState();
			}
			
			updateReceived = false;
		}
	}
	Vector3 GetCurrentPos()
	{
		float telloPosY = pos.y - originPoint.y;
		float telloPosX = pos.x - originPoint.x;
		float telloPosZ = pos.z - originPoint.z;
		return new Vector3(telloPosX, telloPosY, telloPosZ); 
	}
	public void SetTelloPosition()
	{
		Vector3 currentPos = GetCurrentPos();
		
		transform.position = currentPos;
		transform.position += new Vector3(0, elevationOffset, 0);

		yaw = yaw * (180 / Mathf.PI);
		transform.eulerAngles = new Vector3(0, yaw, 0);
		pitch = pitch * (180 / Mathf.PI);
		roll = roll * (180 / Mathf.PI);
		drone.transform.localEulerAngles = new Vector3(pitch - 90f, 0, roll);

		localStateUpdated = true;
		
		//only record if device is server or not connected
		if (networkhandler.isServer || (!networkhandler.isServer && !networkhandler.isClient)){
			if(autoPilot == AutoPilot.rec){RecordPoint();}
		}
		
		/* Vector3 dif = currentPos - transform.position;
		var xDif = dif.x;
		var yDif = dif.y;
		var zDif = dif.z;

		valid tello frame
		if (Mathf.Abs(xDif) < 2 & Mathf.Abs(yDif) < 2 & Mathf.Abs(zDif) < 2)
		{
			
		}
		else
		{
			Debug.Log("Tracking lost ");
			PlaceGameObject("Pre Offset " + telloFrameCount);
			transform.position += prevDeltaPos;
			PlaceGameObject("Post Offset " + telloFrameCount);
		}  */
		
	}

	// tracking and autopilot
	public void UpdateLocalState()
	{
		if(Tello.state == null){return;}

		var state = Tello.state;

		if(Tello.state.posX == null || Tello.state.posY == null || Tello.state.posZ == null){return;}
		pos.x = Tello.state.posY;
        pos.y = -Tello.state.posZ;
        pos.z = Tello.state.posX;
		
		if(state.toEuler() != null)
		{
			double[] eulerInfo = state.toEuler();
			pitch = float.Parse(eulerInfo[0].ToString());
			roll = float.Parse(eulerInfo[1].ToString());
			yaw = float.Parse(eulerInfo[2].ToString());
			toEuler = new Vector3(pitch, roll, yaw);
		
			SetTelloPosition();
			
		}else{
			return;
		}

		if(networkhandler.isClient){
			//for 3d tracking
			networkhandler.clientscript.ClientSendState(pos,new Vector3(pitch,roll,yaw));
		}

		posUncertainty = state.posUncertainty;
		batteryLow = state.batteryLow;
		batteryPercent = state.batteryPercentage;
		cameraState = state.cameraState;
		downVisualState = state.downVisualState;
		telloBatteryLeft = state.droneBatteryLeft;
		telloFlyTimeLeft = state.droneFlyTimeLeft;
		flymode = state.flyMode;
		flyspeed = state.flySpeed;
		flyTime = state.flyTime;
		flying = state.flying;
		gravityState = state.gravityState;
		height = state.height;
		imuCalibrationState = state.imuCalibrationState;
		imuState = state.imuState;
		lightStrength = state.lightStrength;
		onGround = state.onGround;
		powerState = state.powerState;
		pressureState = state.pressureState;
		temperatureHeight = state.temperatureHeight;
		wifiDisturb = state.wifiDisturb;
		wifiStrength = state.wifiStrength;
		windState = state.windState;

		if(networkhandler.isClient)
		{
			networkhandler.clientscript.ClientSendBatteryStatus(TelloLib.Tello.state.batteryPercentage);
			if(showdronestatus){
				networkhandler.clientscript.ClientSendDroneStatus(TelloLib.Tello.state.ToString());
			}
		}

	}
	void resetbools()
	{
		ghostrotFound = false;
		matchGhost = false;
		brake = false;
	}
	public void AutoPilotStartRecord()
	{
		autoPilot = AutoPilot.rec;
		ClearPoints();
		resetbools();
		message.text = "Auto Pilot Mode: "+autoPilot;
	}
	public void ReturnHome()
	{
		if(pos_list.Count <=1){return;}
		autoPilot = AutoPilot.home;
		target_pos = FindClosestPoint();
		Debug.Log("Returning Home , curr_point:"+curr_point);
		message.text = "Auto Pilot Mode: "+autoPilot;
	}
	public void OrbitPath()
	{
		if(pos_list.Count <=1){return;}
		autoPilot = AutoPilot.orbit;
		target_pos = FindClosestPoint();
		Debug.Log("Orbiting , curr_point:"+curr_point);
		message.text = "Auto Pilot Mode: "+autoPilot;
	}
	bool TraceBack = true;
	public void TracePath()
	{
		if(pos_list.Count <=1){return;}
		autoPilot = AutoPilot.trace;
		target_pos = FindClosestPoint();
		TraceBack = true;
		message.text = "Auto Pilot Mode: "+autoPilot;
	}
	void RecordPoint()
	{
		Debug.Log("Recording Point");

		Vector3 p = transform.position;

		if(pos_list.Count >= 1)
		{	
			float dist = Vector3.Distance(p, lastPos);
			if(dist < minDist){
				//rec_timer = 0;
				return;
			}
		}

		//record position
		rec_timer = 0f;
		
		lastPos = p;
		pos_list.Add(p);
		
		GameObject poi = Instantiate(pointprefab, p, Quaternion.identity);
		poi.transform.rotation = transform.rotation;
		poi.name = "Point "+pos_list.Count;
		tran_list.Add(poi.transform);

		Debug.Log("Recorded Added Point: "+p+" Recorded Transform: "+poi.transform);
	}	

	int curr_point = 0;
	void NextPoint()
	{
		//go up points
		curr_point ++;
		if(curr_point >= pos_list.Count){
			if(autoPilot == AutoPilot.orbit){
				curr_point = 0;
			}
			if(autoPilot == AutoPilot.trace){
				curr_point = pos_list.Count-1;
				TraceBack = true;
			}
		}
		target_pos = pos_list[curr_point];
		target_transform = tran_list[curr_point];
		resetbools();
		Debug.Log("Next Point: "+target_pos+" #"+curr_point+" Next transform: "+tran_list[curr_point].position);
	}
	void PrevPoint()
	{
		//go down points
		curr_point --;
		if(curr_point < 0){
			if(autoPilot == AutoPilot.home){
				Debug.Log("Auto Landing");
				Land();
				return;
			}
			if(autoPilot == AutoPilot.trace){
				curr_point = pos_list.Count-1;
				TraceBack = false;
			}
		}
		target_pos = pos_list[curr_point];
		target_transform = tran_list[curr_point];
		resetbools();
		Debug.Log("Prev Point: "+target_pos+" #"+curr_point+" Prev Transform: "+tran_list[curr_point].position);
	}
	Vector3 FindClosestPoint()
	{
		float closest = 99999999999999f;
		Vector3 cp = new Vector3();
		foreach(Vector3 v3 in pos_list){
			float dist = Vector3.Distance(transform.position,v3);
			if(dist < closest){
				cp = v3;
				closest = dist;
			}
		}
		curr_point = pos_list.IndexOf(cp);
		target_transform = tran_list[curr_point];
		Debug.Log("Find Closest Point: "+cp+" #"+curr_point+" Closest Transform: "+target_transform.position);
		resetbools();
		return cp;
	}
	void RemovePoint(Vector3 p)
	{
		pos_list.Remove(p);
		if(pos_list.Count < 1){
			Debug.Log("Auto Landing");
			Land();
			tran_list = new List<Transform>();
			pos_list = new List<Vector3>();
		}

		GameObject[] pnt;
		pnt = GameObject.FindGameObjectsWithTag("point");
		
		foreach(GameObject g in pnt)
		{
			if(g.transform.position == p){
				int i = pos_list.IndexOf(p);
				pos_list.Remove(p);
				Transform t = tran_list[i];
				tran_list.Remove(t);
				Destroy(g);
			}
		}
		Debug.Log("Point "+p+" Removed");
	}

	void ClearPoints()
	{
		GameObject[] p;
		p = GameObject.FindGameObjectsWithTag("point");
		foreach(GameObject g in p){
			Destroy(g);
		}
		tran_list = new List<Transform>();
		pos_list = new List<Vector3>();
		Debug.Log("Points Cleared");
	}

	public AudioClip cameraSFX;
	public AudioSource audsrc;
	public bool keepUI = false;
	public void captureimage()
	{
		#if UNITY_WEBGL || UNITY_STANDALONE || UNITY_WSA
		StartCoroutine(SaveScreenshot_Standalone());
		#endif
		#if UNITY_ANDROID
		StartCoroutine(SaveScreenshot_Android());
		#endif
	}

	#if UNITY_STANDALONE || UNITY_WEBGL || UNITY_WSA
	IEnumerator SaveScreenshot_Standalone()
	{
		if(!keepUI){foreach(GameObject g in canvas){g.SetActive(false);}}
		
		audsrc.PlayOneShot(cameraSFX);

		string name = "Capture" + System.DateTime.Now.Hour + System.DateTime.Now.Minute + System.DateTime.Now.Second + ".png";
		
		ScreenCapture.CaptureScreenshot(name);
		yield return new WaitForSeconds(1f);
		
		if(!keepUI){foreach(GameObject g in canvas){g.SetActive(true);}}
		message.text = "Saved as: "+name;

	}
	#endif

	#if UNITY_ANDROID
	IEnumerator SaveScreenshot_Android() 
	{ 
		yield return new WaitForEndOfFrame();
		// string TwoStepScreenshotPath = MobileNativeShare.SaveScreenshot("Screenshot" + System.DateTime.Now.Hour + System.DateTime.Now.Minute + System.DateTime.Now.Second);
		// Debug.Log("A new screenshot was saved at " + TwoStepScreenshotPath);

		if(!keepUI){canvas[language].SetActive(false);}
	
		string myFileName = "Capture" + System.DateTime.Now.Hour + System.DateTime.Now.Minute + System.DateTime.Now.Second + ".png";
		
		ScreenCapture.CaptureScreenshot(myFileName);
		audsrc.PlayOneShot(cameraSFX);

		yield return new WaitForSeconds(1f);

		if(!keepUI){canvas[language].SetActive(true);}
		message.text = "Saved as: "+Application.persistentDataPath+"/"+myFileName;

		//move image to gallery
		/*string mySavedLocation = Application.persistentDataPath+"/"+myFileName;
		string myFolderLocation = "Phone/DCIM/Camera/";  //DIRECTLY ACCESSING A FOLDER OF THE GALLERY
		string myNewScreenshotLocation = myFolderLocation + myFileName;

		message.text = "Moving from "+mySavedLocation+" to "+myNewScreenshotLocation;

		if (System.IO.File.Exists(mySavedLocation) )
        {  

			//MOVE THE SCREENSHOT WHERE WE WANT IT TO BE STORED
			System.IO.File.Move(mySavedLocation, myNewScreenshotLocation);

			Debug.Log("MOVED FROM: "+mySavedLocation+" TO "+myNewScreenshotLocation);
			message.text = "IMAGE MOVED TO: "+myNewScreenshotLocation;
			
			//REFRESHING THE ANDROID PHONE PHOTO GALLERY IS BEGUN
			AndroidJavaClass classPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject objActivity = classPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			AndroidJavaClass classUri = new AndroidJavaClass("android.net.Uri");
			AndroidJavaObject objIntent = new AndroidJavaObject("android.content.Intent", new object[2] { "android.intent.action.MEDIA_MOUNTED", classUri.CallStatic<AndroidJavaObject>("parse", "file://" + myNewScreenshotLocation) });
			objActivity.Call("sendBroadcast", objIntent);
			//REFRESHING THE ANDROID PHONE PHOTO GALLERY IS COMPLETE
		}else{
			GameObject.Find("Message").GetComponent<Text>().text = "File not found: "+mySavedLocation;
		}*/
	}
	#endif
}
