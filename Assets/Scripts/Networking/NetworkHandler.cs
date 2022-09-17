using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class NetworkHandler : MonoBehaviour {
    // Start is called before the first frame update
    public GameObject server;
    public Server serverscript;
    public GameObject client;
    public Client clientscript;

    public InputField ip;
    public InputField port;

    public Text networktext;

    public GameObject ServerButton;
    TelloController tellocontroller;// = GameObject.Find("TelloController").GetComponent<TelloController>();

    public bool isClient = false;
	public bool isServer = false;

    void Start()
    {
        if(PlayerPrefs.HasKey("savedip")){
            ip.text = PlayerPrefs.GetString("savedip");
        }
        if(PlayerPrefs.HasKey("savedPort")){
            port.text = PlayerPrefs.GetString("savedPort");
        }else{
            port.text = "9999";
        }
        tellocontroller = GameObject.Find("TelloController").GetComponent<TelloController>();
    }

    public void saveip()
    {
        PlayerPrefs.SetString("savedip", ip.text );
    }
    public void savePort()
    {
        PlayerPrefs.SetString("savedPort", port.text );
    }

    bool TelloConnected()
    {
        if(tellocontroller == null){
            tellocontroller = GameObject.Find("TelloController").GetComponent<TelloController>();
        }

        if (tellocontroller.wifiStrength > 0)
        {
            return (true);
        }
        else
        {
            return (false);
        }
    }
    public IEnumerator CheckIP()
    {
        WWW myExtIPWWW = new WWW("http://checkip.dyndns.org");
        if(myExtIPWWW==null){yield return null;}
        yield return myExtIPWWW;
        string myExtIP = myExtIPWWW.text;
        myExtIP=myExtIP.Substring(myExtIP.IndexOf(":")+1);
        myExtIP=myExtIP.Substring(0,myExtIP.IndexOf("<"));
        yield return myExtIP;
    }
    IEnumerator InternetConnected()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get("http://checkip.dyndns.org"))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError)
            {
                Debug.Log(": Error: " + webRequest.error);
                yield return (false);
            }
            else
            {
                Debug.Log(":\nReceived: " + webRequest.downloadHandler.text);
                yield return (true);
            }
        }
    }

    public void createClient()
    {
        StartCoroutine(create_Client());
    }

    IEnumerator create_Client()
    {
        networktext.text = "Starting Client...";

        if(tellocontroller.accountmenus[tellocontroller.language].GetComponent<Auth>().freetrial){
            networktext.text = "NOT AVAILABLE IN FREE TRIAL";
            yield break;
        }

        bool drone_connected = TelloConnected();
        if(!drone_connected){
            networktext.text = "YOU NEED TO CONNTECT TELLO DRONE";
            #if !UNITY_EDITOR
            yield break;
            #endif
        }
        
        //check internetconnection
        CoroutineWithData cd = new CoroutineWithData(this, InternetConnected( ) );
        yield return cd.coroutine;
        Debug.Log("result is " + cd.result);  //  'success' or 'fail'
        var internet_connected = (bool)cd.result;
        if(!internet_connected)
        {
            networktext.text = "YOU HAVE NO DATA/INTERNET CONNECTION";
            yield return new WaitForSeconds(3f);
        }

        if(GameObject.Find("Server")){
            Destroy(GameObject.Find("Server"));
            networktext.text = "Deleted Server Object, safe to start client";
            if(!GameObject.Find("Server")){isServer = false;}
        }
        if(!isClient && !isServer){
            networktext.text = "Creating Client Object";
            GameObject c = Instantiate(client,Vector3.zero,Quaternion.identity);
            clientscript = c.GetComponent<Client>();
        }else if(isClient || GameObject.Find("Client")){
            if(GameObject.Find("Client")){
                networktext.text = "Connecting to existing Client Object";
                GameObject.Find("Client").GetComponent<Client>().connect();
            }else{
                //isClient == true but not client object
                networktext.text = "Creating Client Object";
                GameObject c = Instantiate(client,Vector3.zero,Quaternion.identity);
                clientscript = c.GetComponent<Client>();
            }
        }
    }
    public void createServer()
    {
        StartCoroutine(create_Server());
    }
    public IEnumerator create_Server()
    {
        bool drone_connected = TelloConnected();
        if(drone_connected){
            networktext.text = "YOU CANNOT START SERVER WITH DRONE CONNECTED";
            yield break;
        }

        //check internetconnection
        CoroutineWithData cd = new CoroutineWithData(this, InternetConnected( ) );
        yield return cd.coroutine;
        Debug.Log("result is " + cd.result);  //  'success' or 'fail'
        var internet_connected = (bool)cd.result;
        if(!internet_connected)
        {
            networktext.text = "YOU HAVE NO DATA/INTERNET CONNECTION";
            #if !UNITY_EDITOR
            yield break;
            #endif
        }else{
            networktext.text = "Creating Server...";
        }

        if(tellocontroller.accountmenus[tellocontroller.language].GetComponent<Auth>().freetrial){
            networktext.text = "NOT AVAILABLE IN FREE TRIAL";
            yield break;
        }

        if(GameObject.Find("Client"))
        { 
            Destroy(GameObject.Find("Client"));
            networktext.text = "Deleted Client Object, safe to start server";
            if(!GameObject.Find("Client")){isClient = false;}
        }
        if(!isServer && !isClient){
            //create server
            networktext.text = "Creating Server Object";
            GameObject s = Instantiate(server,Vector3.zero,Quaternion.identity);
            serverscript = s.GetComponent<Server>();
        }else if(isServer || GameObject.Find("Server"))
        {
            //turn off server
            if(serverscript){
                networktext.text = "Deleteing Server Object";
                serverscript.StopServer();
            }else{
                if(GameObject.Find("Server")){
                    networktext.text = "Searching Server Object";
                    GameObject.Find("Server").GetComponent<Server>().StopServer();
                }else{
                    networktext.text = "No Server Object, safe to start new server or client";
                    isServer = false;
                }
            }
        }
    }
}
//custom message classes

public class boolMessage : MessageBase
{
    public bool tf;
}
public class controllerMessage : MessageBase
{
    public bool t;
    public bool l;
    public float lx;
    public float ly;
    public float rx;
    public float ry;
}
public class videoMessage : MessageBase
{
    public byte[] data;
}
public class statusMessage : MessageBase
{
    public string stats;
    public int bat;
}

public class stateMessage : MessageBase
{
    public Vector3 pos;
    public Vector3 rotation;
}
public class nameMessage : MessageBase
{   
    public string name;
}