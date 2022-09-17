using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Security.Cryptography;
using System.Text;

public class Auth : MonoBehaviour
{
    public InputField userName;
    public InputField passWord;
    public Text message;
    public GameObject[] siginObjects;
    public GameObject[] accountObjects;
    public GameObject deviceobject;

    public bool thisAuth = false;

    List<string> deviceList = new List<string>();

    int MaxDevices;

    void Start()
    {
        var b = accountObjects[2].GetComponent<Button>();
        b.onClick.AddListener(Close);
        EnableSignInScreen();

        if(PlayerPrefs.HasKey("accountUserName") && PlayerPrefs.HasKey("accountPassword")){
            userName.text = PlayerPrefs.GetString("accountUserName");
            passWord.text = PlayerPrefs.GetString("accountPassword");
            SignIn();
        }

        string thisid = GetSHA512(SystemInfo.deviceUniqueIdentifier.ToString());
        accountObjects[7].GetComponent<Text>().text = "This Device ID: "+thisid;
    }

    public bool freetrial = false;
    public float freetrialtimer = 0;
    public void start_FreeTrial()
    {
        if(freetrial){Close();return;}
        freetrialtimer = 5f * 60f;
        freetrial = true;
        thisAuth = true;
        if(!PlayerPrefs.HasKey("freetrials")){
            PlayerPrefs.SetInt("freetrials",1);
        }else{
            int f =  PlayerPrefs.GetInt("freetrials");
            f+=1;
            Debug.Log("free trials "+f);
            PlayerPrefs.SetInt("freetrials",f);
            check_free_trials();
        }
        Close();
    }
    #if UNITY_EDITOR
    void Update() {
		if(Input.GetKeyDown(KeyCode.P)){
            PlayerPrefs.DeleteKey("freetrials");
            Debug.Log("Player PRef Deleted freetrials count");}
    }
    #endif
    public void SignOut()
    {
        EnableSignInScreen();
        message.text = "Signed Out";
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
    public void Open()
    {
        gameObject.SetActive(true);
    }
    void OnEnable()
    {
        check_free_trials();
    }
    void check_free_trials()
    {
        Debug.Log("ON AUTH check_free_trials");
        if(PlayerPrefs.HasKey("freetrials")){
            Debug.Log("Has Key Freetrials");
            int f =  PlayerPrefs.GetInt("freetrials");
            Debug.Log(f+" trials used");
            if(f >= 3){
                accountObjects[8].SetActive(false);
            }else{
                accountObjects[8].SetActive(true);}
        }else{
            accountObjects[8].SetActive(true);
        }
    }

    public void SignIn()
    {
        StartCoroutine(Sign_In());
    }
    IEnumerator Sign_In()
    {
        if(userName.text == null || userName.text == ""){
            message.text = "User Name Required!";
            yield break;
        }
        if(passWord.text == null || passWord.text == ""){
            message.text = "Password Required!";
            yield break;
        }
        
        string username = GetSHA512(userName.text.Trim());
        string password = GetSHA512(passWord.text.Trim());
        string deviceID = GetSHA512(SystemInfo.deviceUniqueIdentifier.ToString());
        CoroutineWithData cd = new CoroutineWithData(this, AccountPHP(username,password,"signin",deviceID,0,null) );
        yield return cd.coroutine;
        Debug.Log("Sign In result is: " + cd.result);  //  'success' or 'fail'
        string result = cd.result as string;
        if(result.Contains("ERROR")){
            message.text = "Account Sign In Error: "+result;
            yield break;
        }else{
            //signed in
            EnableAccountScreen(); 
            message.text = "Sign in Successful";
            PlayerPrefs.SetString("accountUserName",userName.text);
            PlayerPrefs.SetString("accountPassword",passWord.text);
            CreateDeviceList(result,"Sign_In");
        }

    }
    void EnableSignInScreen()
    {
        foreach(GameObject g in siginObjects){
            g.SetActive(true);
        }
        foreach(GameObject g in accountObjects){
            g.SetActive(false);
        }
        //remove device objects
        GameObject[] dg = FindObjectsOfType<GameObject>();
        foreach(GameObject ag in dg){
            if(ag.name == "DeviceID(Clone)"){Destroy(ag);}
        }
    }
    void EnableAccountScreen()
    {
        foreach(GameObject g in siginObjects){
            g.SetActive(false);
        }
        foreach(GameObject g in accountObjects){
            g.SetActive(true);
            check_free_trials();
        }
    }
    void CreateDeviceList(string res,string from)
    {
        EnableAccountScreen();
        //remove old device objects
        GameObject[] g = FindObjectsOfType<GameObject>();
        foreach(GameObject ag in g){
            if(ag.name.Contains("DeviceID(Clone")){Destroy(ag);}
        }

        deviceList = new List<string>();
        res = res.Substring(0,res.Length-1);
        string[] devs = res.Split(","[0]);
        Debug.Log("CreateDeviceList: "+res+"\nFROM: "+from);

            string debstring = "";
            foreach(string dd in devs){
                debstring += dd+",";
            }
            Debug.Log("DEVS[]{"+debstring+"}\nFROM: "+from);

        if(devs.Length > 1){
            for(int ii = 1; ii < devs.Length; ii++){

                if(devs[ii]!="" && devs[ii]!= null){
                    deviceList.Add(devs[ii]);
                    Debug.Log("Device Added: "+devs[ii]);
                }
            }
        }
        if(!devs[0].Contains("Cannot resolve destination")){
            Debug.Log(devs[0]);
            MaxDevices = int.Parse(devs[0]);
        }else{
            MaxDevices = 0;
        }
        accountObjects[4].GetComponent<Text>().text = "Max Devices: "+MaxDevices;
        if(MaxDevices ==0){
            message.text = "YOU NEED TO PURCHASE MORE DEVICE AUTHORIZATIONS";
        }
        

        string deviceID = GetSHA512(SystemInfo.deviceUniqueIdentifier.ToString());
        Debug.Log("Populating Device List");
        thisAuth=false;
        accountObjects[1].SetActive(true);
        accountObjects[2].SetActive(false);
        int i = 0;
        foreach(string d in deviceList)
        {
            if(d == deviceID){
                thisAuth=true;
                Debug.Log("This Device is authorized "+i);
                message.text = "This Device is authorized";
                accountObjects[1].SetActive(false);
                accountObjects[2].SetActive(true);
                accountObjects[8].SetActive(false);
            }

            GameObject dev = Instantiate(deviceobject);
            dev.GetComponent<Text>().text = d;
            dev.transform.SetParent(transform);
            RectTransform rt = dev.GetComponent<RectTransform>();
            rt.localPosition = new Vector3(0,260-(30*i),0);
             i++;
            Button b = dev.transform.Find("Remove_Button").GetComponent<Button>();
            b.onClick.AddListener(delegate {RemoveDevice(d); });
        }
    }
    void RemoveDevice(string r)
    {
        StartCoroutine(Remove_Device(r));
    }
    IEnumerator Remove_Device(string r)
    {
        string username = GetSHA512(userName.text.Trim());
        string password = GetSHA512(passWord.text.Trim());
        CoroutineWithData cd = new CoroutineWithData(this, AccountPHP(username,password,"removedevice",r,0,null) );
        yield return cd.coroutine;
        Debug.Log("Remove Device result is: " + cd.result);  //  'success' or 'fail'
        string result = cd.result as string;
 
        if(result.Contains("ERROR")){message.text = "Remove Device Error: "+result;yield break;} 
        CreateDeviceList(result,"Remove_Device");

        message.text = "Device Removed";
    }
    public void AddDevice()
    {
        StartCoroutine(Add_Device());
    }
    IEnumerator Add_Device()
    {
        if(deviceList.Count >= MaxDevices){
            message.text = "You have reached Max Devices, YOU NEED TO PURCHASE MORE DEVICE AUTHORIZATIONS";
            if(MaxDevices == 0){accountObjects[6].GetComponent<RectTransform>().localPosition = new Vector3(0,0,0);}
            yield break;}

        string username = GetSHA512(userName.text.Trim());
        string password = GetSHA512(passWord.text.Trim());
        string deviceID = GetSHA512(SystemInfo.deviceUniqueIdentifier.ToString());
        CoroutineWithData cd = new CoroutineWithData(this, AccountPHP(username,password,"adddevice",deviceID,0,null) );
        yield return cd.coroutine;
        Debug.Log("Add Device result is: " + cd.result);  //  'success' or 'fail'

        string result = cd.result as string;

        if(result.Contains("ERROR")){message.text = "Add Device Error: "+result;yield break;}
        message.text = "Device Added";

        CreateDeviceList(result,"Add_Device");
    }
    public void AddFakeDevice()
    {
        StartCoroutine(Add_Fake_Device());
    }
    IEnumerator Add_Fake_Device()
    {
        if(deviceList.Count >= MaxDevices){
            message.text = "You have reached Max Devices, YOU NEED TO PURCHASE MORE DEVICE AUTHORIZATIONS";
            if(MaxDevices == 0){accountObjects[6].GetComponent<RectTransform>().localPosition = new Vector3(0,0,0);}
            yield break;
        }

        string username = GetSHA512(userName.text.Trim());
        string password = GetSHA512(passWord.text.Trim());

        string YOURdeviceID = GetSHA512(SystemInfo.deviceUniqueIdentifier.ToString());
        string deviceID = "";
        
        for(int i = 0; i < YOURdeviceID.Length; i++){
            string[] stringarr = new string[]{"a","b","c","1","2","3"};
            deviceID+= stringarr[Random.Range(0,stringarr.Length)];
        }
        
        CoroutineWithData cd = new CoroutineWithData(this, AccountPHP(username,password,"adddevice",deviceID,0,null) );
        yield return cd.coroutine;
        Debug.Log("Add Fake Device result is: " + cd.result);  //  'success' or 'fail'

        string result = cd.result as string;

        if(result.Contains("ERROR")){message.text = "Add Device Error: "+result;yield break;}
        message.text = "Fake Device Added";

        CreateDeviceList(result,"Add_Fake_device");
    }
    public void CreateAccount()
    {
        StartCoroutine(Create_Account());
    }
    IEnumerator Create_Account()
    {
        if(userName.text == null || userName.text == ""){
            message.text = "User Name Required!";
            yield break;
        }
        if(passWord.text == null || passWord.text == ""){
            message.text = "Password Required!";
            yield break;
        }
        if(!userName.text.Contains("@") || !userName.text.Contains(".")){
            message.text = "User name must be your email";
            yield break;
        }
        string username = GetSHA512(userName.text.Trim());
        string password = GetSHA512(passWord.text.Trim());
        string deviceID = GetSHA512(SystemInfo.deviceUniqueIdentifier.ToString());

        CoroutineWithData cd = new CoroutineWithData(this, AccountPHP(username,password,"createnew",deviceID,0,null) );
        yield return cd.coroutine;
        Debug.Log("Create Account result is " + cd.result);  //  'success' or 'fail'

        string result = cd.result as string;
        if(result.Contains("ERROR")){message.text = "Account Creation Error: "+result;yield break;}

        #if FREECRED
     
            CoroutineWithData cdfc = new CoroutineWithData(this, Account_PHP_receipt("GooglePlayFreeCredit",1) );
            yield return cdfc.coroutine;
            Purchase_Auth("GooglePlayFreeCredit",1);
        
        #endif

        message.text = "ACCOUNT CREATED! Sign in to your account to continue";
    }
   
    public GameObject store;
    public void Purchase_Autho(){
        store.SetActive(true);
    }
    public void Close_Store(){
        store.SetActive(false);
    }
    public void Purchase_Auth(string id,int qty)
    {
        StartCoroutine(PurchaseAuth(id,qty));
    }
    IEnumerator PurchaseAuth(string id,int qty)
    {
        if(!BoolReceiptResult){message.text = "Receipt Validation Error 0";yield break;}

        string username = GetSHA512(userName.text.Trim());
        string password = GetSHA512(passWord.text.Trim());
        
        CoroutineWithData cdr = new CoroutineWithData(this, AccountPHP(username,password,"echoreceipt",null,0,id) );
        yield return cdr.coroutine;
        Debug.Log("echoreceipt result is " + cdr.result);  //  'success' or 'fail'
        string Rres = cdr.result as string;
        if(!Rres.Contains(id)){
            Debug.Log("RECEIPT NOT VALID");
            message.text = "RECEIPT NOT VALID";
            yield break;
        }
        List<string> receipts = new List<string>();
        string[] rArr = Rres.Split(","[0]);
        foreach(string re in rArr){
            if(receipts.Contains(re)){
                //problem
                BoolReceiptResult = false;
                yield break;
            }else{
                receipts.Add(re);
                BoolReceiptResult = true;
            }
        }
        if(!BoolReceiptResult){message.text = "Receipt Validation Error 1";yield break;}

        string deviceID = GetSHA512(SystemInfo.deviceUniqueIdentifier.ToString());

        CoroutineWithData cd = new CoroutineWithData(this, AccountPHP(username,password,"authoz",deviceID,qty,null) );
        yield return cd.coroutine;
        Debug.Log("Purchase Auth result is " + cd.result);  //  'success' or 'fail'

        string result = cd.result as string;
        if(result.Contains("ERROR")){message.text = "Add Authorizations Error: "+result;yield break;}

        message.text = "Authorization(s) Added";

        CreateDeviceList(result,"PurchaseAuth");
    }

    string GetSHA512 (string String) {
 
        UnicodeEncoding ue = new UnicodeEncoding ();
        byte [] hashValue;
        byte [] message = ue.GetBytes (String);

        SHA256Managed hashString = new SHA256Managed ();
        string hex = "";

        hashValue = hashString.ComputeHash (message);

        foreach (byte x in hashValue) {
            hex += System.String.Format ("{0:x2}", x);
        }

        return hex;
    }
    bool BoolReceiptResult;
    public IEnumerator Account_PHP_receipt(string r,int qty)
    {
        string username = GetSHA512(userName.text.Trim());
        string password = GetSHA512(passWord.text.Trim());
        string type = "receipt";

        CoroutineWithData cd = new CoroutineWithData(this, AccountPHP( username,  password, type, null, qty,r) );
        yield return cd.coroutine;
        string result = cd.result as string;
        Debug.Log("Account_PHP_receipt :"+result);
        List<string> receipts = new List<string>();
        string[] rArr = result.Split(","[0]);
        foreach(string re in rArr){
            if(receipts.Contains(re)){
                //problem
                BoolReceiptResult = false;
                yield break;
            }else{
                receipts.Add(re);
                BoolReceiptResult = true;
            }
        }
        
    }

    IEnumerator AccountPHP(string U, string P,string type,string device,int amount,string receipt)
    {
        WWWForm form = new WWWForm();
        form.AddField("username", U);
        form.AddField("password", P);
        form.AddField("type", type);
       
        if(device!=null){form.AddField("deviceid",device);}
        if(receipt!=null){form.AddField("receipt",receipt);}

        
        if(type == "authoz"){
            form.AddField("PREVauthoz",MaxDevices);
            form.AddField("NEWauthoz",amount);
        }

        string url = "http://www.enigmastudios.tech/Auth/RemoteLink/accountauth.php";

        Debug.Log("Sending "+U+","+P+","+type+","+device+"\nTO: "+url);

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
                yield return www.error.ToString();
            }
            else
            {
                Debug.Log("Form upload complete!");
                string result = www.downloadHandler.text;
                yield return result;
            }
        }

    }


}