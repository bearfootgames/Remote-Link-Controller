using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;

using System.Net; 
using System.Net.Sockets;
public class Server : NetworkBehaviour {
    int maxConnections = 999;

    // The id we use to identify our messages and register the handler
    Text networktext;
    NetworkHandler networkhandler;

    TelloController tellocontroller;
    TelloVideoTexture tellovideotexture;
   
    // Use this for initialization
    void Awake()
    {
        GameObject g = GameObject.Find("NetworkHandler");
        networkhandler = g.GetComponent<NetworkHandler>();

        GameObject gt = GameObject.Find("NetworkMessages");
        networktext = gt.GetComponent<Text>();

        GameObject gc = GameObject.Find("TelloController");
        tellocontroller = gc.GetComponent<TelloController>();
        GameObject gv = GameObject.Find("TelloVideo");
        tellovideotexture = gv.GetComponent<TelloVideoTexture>();

        gameObject.name = "Server";

        networktext.text = "Configuring Server...";

        CreateServer();
    }    

    void CreateServer() {

        // Register handlers for the types of messages we can receive
        RegisterHandlers ();
      
        var config = new ConnectionConfig ();
        // There are different types of channels you can use, check the official documentation
        config.AddChannel (QosType.ReliableFragmented);
        config.AddChannel (QosType.UnreliableFragmented);

        var ht = new HostTopology (config, maxConnections);

        if (!NetworkServer.Configure (ht)) {
            Debug.Log ("No server created, error on the configuration definition");
            return;
        } else {
            // Start listening on the defined port
            if(NetworkServer.Listen ( int.Parse(networkhandler.port.text) ))
            {
                Debug.Log ("Server created, listening on port: " + networkhandler.port.text);   
                networktext.text = "Server created, listening on port: " + networkhandler.port.text;
           
                StartCoroutine(getpublicIP());

                networkhandler.isServer = true;

                networkhandler.ServerButton.transform.Find("Text").GetComponent<Text>().text = "Stop Server";
            }

            else{
                Debug.Log ("No server created, could not listen to the port: " + networkhandler.port.text);    
                networktext.text = "No server created, could not listen to the port: " + networkhandler.port.text;
            }
        }
    }
    IEnumerator getpublicIP()
    {
        CoroutineWithData cd = new CoroutineWithData(this, networkhandler.CheckIP());
        yield return cd.coroutine;
        string ip = cd.result as string;  //  'success' or 'fail'
        ip = ip.Replace(" ","");
        networktext.text = "Server Started "+ip;
        networkhandler.ip.text = ip;
    }
    
    public void StopServer()
    {
        if(networkhandler.isServer){
            NetworkServer.Shutdown ();
            networkhandler.isServer=false;
            networktext.text = "Server Disconnected";
            networkhandler.ServerButton.transform.Find("Text").GetComponent<Text>().text = "Start Server";
            Destroy(this.gameObject);
        }
    }
    void OnApplicationQuit()
    {
        StopServer();
    }
    private void RegisterHandlers () {
        // Unity have different Messages types defined in MsgType
        NetworkServer.RegisterHandler (MsgType.Connect, OnClientConnected);
        NetworkServer.RegisterHandler (MsgType.Disconnect, OnClientDisconnected);

        // Our message use his own message type.
        NetworkServer.RegisterHandler (1000, OnControllerMessageReceived);
        NetworkServer.RegisterHandler (1001, OnVideoMessageReceived);
        NetworkServer.RegisterHandler (1002, OnStatusMessageReceived);
        NetworkServer.RegisterHandler (1003, OnStateMessageReceived);
        NetworkServer.RegisterHandler (1004, OnNameMessageReceived);
    }

   

    private void RegisterHandler(short t, NetworkMessageDelegate handler) {
        NetworkServer.RegisterHandler (t, handler);
    }

    void OnClientConnected(NetworkMessage netMessage)
    {
        // Do stuff when a client connects to this server
        networktext.text = "Client is Connected";
    }

    void OnClientDisconnected(NetworkMessage netMessage)
    {
        // Do stuff when a client dissconnects
        networktext.text = "Client has Disconnected";
    }

    void OnControllerMessageReceived(NetworkMessage netMessage)
    {
        var objectMessage = netMessage.ReadMessage<controllerMessage>();
        
    }
    void OnVideoMessageReceived(NetworkMessage netMessage)
    {
        var objectMessage = netMessage.ReadMessage<videoMessage>();
        tellovideotexture.SortData(objectMessage.data);
        networktext.text = "Received Data: "+objectMessage.data.Length;
    }
    void OnStatusMessageReceived(NetworkMessage netMessage)
    {
        var objectMessage = netMessage.ReadMessage<statusMessage>();
        if(objectMessage.bat != null){
            int bat = objectMessage.bat;
            GameObject.Find("TelloBatteryStatus").GetComponent<TextUpdater>().ReceiveBattery(bat);
        }
        if(objectMessage.stats != null){
            string stat = objectMessage.stats;
            GameObject.Find("TelloSystemStatus").GetComponent<TextUpdater>().ReceiveStats(stat);
        }
    }
    void OnStateMessageReceived(NetworkMessage netMessage)
    {
        var objectMessage = netMessage.ReadMessage<stateMessage>();
        
        //drone state
         
        tellocontroller.pitch = objectMessage.rotation.x;
		tellocontroller.roll = objectMessage.rotation.y;
		tellocontroller.yaw = objectMessage.rotation.z;

        tellocontroller.pos.x = objectMessage.pos.x;
        tellocontroller.pos.y = objectMessage.pos.y;
        tellocontroller.pos.z = objectMessage.pos.z;

        tellocontroller.SetTelloPosition();
    }
    void OnNameMessageReceived(NetworkMessage netMessage)
    {
        var objectMessage = netMessage.ReadMessage<nameMessage>();
        
        string clientname = objectMessage.name;
        string servername = tellocontroller.accountmenus[tellocontroller.language].GetComponent<Auth>().userName.text;
        if(clientname != servername){
            networktext.text = "Client and Server Accounts do not match! Disconnecting";
            tellocontroller.accountmenus[tellocontroller.language].GetComponent<Auth>().thisAuth = false;
            tellocontroller.accountmenus[tellocontroller.language].GetComponent<Auth>().Open();
            NetworkServer.SendToAll(1002,null);
            networkhandler.createServer();
        }
    }

    public void sendcontrollermessage(bool t,bool l,float lx,float ly,float rx,float ry)
    {
        // Send a message to all the clients connected
        controllerMessage messageContainer = new controllerMessage();

        messageContainer.t = t;
        messageContainer.l = l;
        messageContainer.lx = lx;
        messageContainer.ly = ly;
        messageContainer.rx = rx;
        messageContainer.ry = ry;

        // Broadcast a message a to everyone connected
        NetworkServer.SendToAll(1000,messageContainer);
    } 
    public void sendDroneStatusBool(bool tf)
    {
        boolMessage messageContainer = new boolMessage();
        messageContainer.tf = tf;
        NetworkServer.SendToAll(1001,messageContainer);
    }

}
public class CoroutineWithData {
    public Coroutine coroutine { get; private set; }
    public object result;
    private IEnumerator target;
    public CoroutineWithData(MonoBehaviour owner, IEnumerator target) {
        this.target = target;
        this.coroutine = owner.StartCoroutine(Run());
    }

    private IEnumerator Run() {
        while(target.MoveNext()) {
            result = target.Current;
            yield return result;
        }
    }
}
