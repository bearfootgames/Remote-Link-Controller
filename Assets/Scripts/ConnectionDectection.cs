using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TelloLib;

public class ConnectionDectection : MonoBehaviour
{
    // Start is called before the first frame update
    MeshRenderer MR;
    void Start()
    {
        MR = GetComponent<MeshRenderer>();
    }

    bool isOn = true;

    // Update is called once per frame
    void Update()
    {
        if(Tello.state.wifiStrength > 0 && isOn){
            MR.enabled = false;
            isOn = false; 
        }
        if(Tello.state.wifiStrength <= 0 && !isOn){
            MR.enabled = true;
            isOn = true; 
        }
    }
}
