using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public GameObject android_button;
    public GameObject[] pc_buttons;
    public GameObject dronestatus;
    public GameObject buttoncolors;
    List<Image> buttons_image = new List<Image>();
    public Slider red;
    public Slider green;
    public Slider blue;
    public GameObject AccountInfo;
    bool firstload = true;
    
    void Start()
    {
        open();
        open_buttoncolors_menu();

        #if UNITY_ANDROID
        Destroy(android_button);
        #endif
        #if UNITY_STANDALONE || UNITY_WSA
        foreach(GameObject g in pc_buttons){Destroy(g);}
        #endif

        //Find Buttons + joystick circle
        Button[] tempbuttons = FindObjectsOfType<Button>();
        
        foreach(Button g in tempbuttons){
          
            Image i = g.transform.GetComponent<Image>();
            Debug.Log(i.sprite.ToString());
            if(i.sprite.ToString().Contains("Button")){
                Debug.Log("Found Button "+i.sprite.ToString());
                buttons_image.Add(i);
            }
            #if UNITY_ANDROID
            buttons_image.Add(GameObject.Find("Left Joystick").GetComponent<Image>());
            buttons_image.Add(GameObject.Find("Right Joystick").GetComponent<Image>());
            #endif
        }
        if(PlayerPrefs.HasKey("buttoncolors"))
        {
            string colorstring = PlayerPrefs.GetString("buttoncolors");
            string[] arr = colorstring.Split(","[0]);
            red.value = float.Parse(arr[0]);
            green.value = float.Parse(arr[1]);
            blue.value = float.Parse(arr[2]);
        }else{
            red.value = 0.0f;
            green.value = .6f;
            blue.value = 1.0f;
        }
        ChangeButtonColors();
    }
    public void OpenAccountInfo()
    {
        AccountInfo.SetActive(true);
        close();
    }
    
    public void open_buttoncolors_menu()
    {
        buttoncolors.SetActive(true);
    }
    public void close_buttoncolors_menu()
    {
        buttoncolors.SetActive(false);
    }
    public void close()
    {
        this.gameObject.SetActive(false);
    }
     public void open()
    {
        this.gameObject.SetActive(true);
    }

    public void Download_App()
    {
        Application.OpenURL("http://www.enigmastudios.tech/Apps/RemoteLink/");
    }
    
    public void droneStaus_handle()
    {
        if(dronestatus.active){
            dronestatus.SetActive(false);
            TelloController.showdronestatus = false;
        }else{
            dronestatus.SetActive(true);
            TelloController.showdronestatus = true;
            if(GameObject.Find("Server")){GameObject.Find("Server").GetComponent<Server>().sendDroneStatusBool(TelloController.showdronestatus);}
        }
    }
    
    public void ChangeButtonColors()
    {
        foreach (Image b in buttons_image){
            b.transform.GetComponent<Image>().color = new Color(red.value,green.value,blue.value,1);
        }
        PlayerPrefs.SetString("buttoncolors",red.value+","+green.value+","+blue.value);
        if(firstload){
            firstload = false; 
            close_buttoncolors_menu();
            close();
        }
    }
}
