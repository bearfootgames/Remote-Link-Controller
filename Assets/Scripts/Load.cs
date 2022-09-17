using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Load : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
       StartCoroutine(loadnew(1f));
    }
    void OnEnable()
    {
         StartCoroutine(loadnew(1f));
    }
    IEnumerator loadnew(float time)
    {
        yield return new WaitForSeconds(time);
        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }

}
