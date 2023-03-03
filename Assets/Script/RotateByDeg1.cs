using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class RotateByDeg1 : MonoBehaviour
{
    CubeManager cubeManager;
    public ConnectType connectType;
    float elapsedTime;
    

    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(connectType);
        await cubeManager.SingleConnect();
    }

    // Update is called once per frame
    void Update()
    {
        elapsedTime += Time.deltaTime;
        if(elapsedTime < 2.0f)
        {
            return;
        }
        elapsedTime = 0.0f;

        
        foreach(var handle in cubeManager.syncHandles)
        {
            handle.RotateByDeg(-90, 40).Exec();          
            Debug.Log(handle.deg);
        }
        

        

        
    }
}
