using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AudioSetting : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
       
    }

    private void Update()
    {
        Debug.Log(AudioSettings.driverCapabilities);
    }
}
