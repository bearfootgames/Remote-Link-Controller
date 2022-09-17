using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextUpdater : MonoBehaviour {

	// Use this for initialization
	Text t;
	public NetworkHandler networkhandler;
	void Start () {
		t = GetComponent<Text>();
		if(networkhandler == null){
			networkhandler = GameObject.Find("NetworkHandler").GetComponent<NetworkHandler>();
		}
	}

	
	// Update is called once per frame
	void Update () {
		if(t.gameObject.name == "TelloBatteryStatus" && (networkhandler.isClient || (!networkhandler.isClient && !networkhandler.isServer)) ){
			t.text = string.Format("Battery {0} %", ((TelloLib.Tello.state != null) ? ("" + TelloLib.Tello.state.batteryPercentage) : " - "));
			if(TelloLib.Tello.state.batteryPercentage <= 10 && TelloLib.Tello.state.flying && t.color != Color.red){
				t.color = Color.red;
			}
		}
		if(t.gameObject.name == "Tello System Status" && (networkhandler.isClient || (!networkhandler.isClient && !networkhandler.isServer)) ){
			t.text = "System Status: " + TelloLib.Tello.state;
		}
	}
	public void ReceiveStats(string stats){
		t.text = "System Status: " + stats;
	}
	public void ReceiveBattery(int bat){
		t.text = bat.ToString();
	}
}
