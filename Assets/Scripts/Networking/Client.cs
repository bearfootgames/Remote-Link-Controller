using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Client : NetworkBehaviour
{
    string ip = "";

    // The network client
    NetworkClient client;
    NetworkHandler networkhandler;
    Text networktext;
    TelloController tellocontroller;

    void Awake ()
    {
        networkhandler = GameObject.Find("NetworkHandler").GetComponent<NetworkHandler>();
        networktext = GameObject.Find("NetworkMessages").GetComponent<Text>();
        tellocontroller = GameObject.Find("TelloController").GetComponent<TelloController>();
        gameObject.name = "Client";
        networktext.text = "Configuring Client...";
        CreateClient();
    }
    void CreateClient()
    {
        var config = new ConnectionConfig ();

        // Config the Channels we will use
        config.AddChannel (QosType.ReliableFragmented);
        config.AddChannel (QosType.UnreliableFragmented);

        // Create the client ant attach the configuration
        client = new NetworkClient ();
        client.Configure (config,1);

        // Register the handlers for the different network messages
        RegisterHandlers();

        connect();
    }
    public void connect()
    {
        ip = networkhandler.ip.text;
        client.Connect (ip, int.Parse(networkhandler.port.text));
    }
    void OnApplicationPause(bool paused)
    {
        if(!paused)
        {
            // App coming into foreground
            //NetworkManager.singleton.StartClient();
            connect();
        }
        else
        {
            // App getting suspended
            //NetworkManager.singleton.client.Disconnect();
            client.Disconnect();
        }
    }

    // Register the handlers for the different message types
    void RegisterHandlers () {
    
        // Unity have different Messages types defined in MsgType
        client.RegisterHandler(MsgType.Connect, OnConnected);
        client.RegisterHandler(MsgType.Disconnect, OnDisconnected);
        client.RegisterHandler (1000, OnControllerMessageReceived);
        client.RegisterHandler (1001, OnDroneStatusBoolReceived);
        client.RegisterHandler (1002, OnFalseAccountReceived);
    }

    void OnDroneStatusBoolReceived(NetworkMessage netMessage) {  
         var objectMessage = netMessage.ReadMessage<boolMessage>();
         TelloController.showdronestatus = objectMessage.tf;
    }

    void OnConnected(NetworkMessage message) {        
        // Do stuff when connected to the server
        networktext.text = "Connected to Server";
        networkhandler.isClient = true;
        //NetworkIdentity.Instantiate(netobj);
        string name = "";
        name = tellocontroller.accountmenus[tellocontroller.language].GetComponent<Auth>().userName.text; 
        Debug.Log("sending username: "+name+" to server");
        ClientSenduserName(name);
    }

    void OnDisconnected(NetworkMessage message) {
        // Do stuff when disconnected to the server
        networktext.text = "Disconnected from Server";
        networkhandler.isClient = false;
        //Destroy(this.gameObject);
        //networkhandler.isClient = false;
    }

    // Message received from the server
    void OnControllerMessageReceived(NetworkMessage netMessage)
    {
        var objectMessage = netMessage.ReadMessage<controllerMessage>();
        tellocontroller.Network_Update(objectMessage.t,objectMessage.l,objectMessage.lx,objectMessage.ly,objectMessage.rx,objectMessage.ry);
    }
    void OnFalseAccountReceived(NetworkMessage n)
    {
        networktext.text = "Client and server accounts do not match! Disconnecting";
        tellocontroller.accountmenus[tellocontroller.language].GetComponent<Auth>().thisAuth = false;
        tellocontroller.accountmenus[tellocontroller.language].GetComponent<Auth>().Open();
        networkhandler.createClient();
        Debug.Log("False Account");
    }

    public void ClientSendVideo(byte[] data)
    {
        videoMessage messageContainer = new videoMessage();
        
        messageContainer.data = data;

        //  to the server when connected
        client.Send(1001,messageContainer);
    }
    public void ClientSendBatteryStatus(int bat)
    {
        statusMessage messageContainer = new statusMessage();
        //messageContainer.stats = stats;
        messageContainer.bat = bat;
        client.Send(1002,messageContainer);
    }
    public void ClientSendDroneStatus(string stats)
    {
        statusMessage messageContainer = new statusMessage();
        messageContainer.stats = stats;
        client.Send(1002,messageContainer);
    }

    public void ClientSendState(Vector3 p, Vector3 e)
    {
        stateMessage messageContainer = new stateMessage();

        messageContainer.pos = p;
        messageContainer.rotation = e;

        client.Send(1003,messageContainer);
    }
    public void ClientSenduserName(string name)
    {
        nameMessage messageContainer = new nameMessage();
        messageContainer.name = name;
        Debug.Log("Client Send Username: "+name);
        client.Send(1004,messageContainer);
    }
    
}