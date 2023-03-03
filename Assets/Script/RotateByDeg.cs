using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class RotateByDeg : MonoBehaviour
{
    public ConnectType connectType;

    CubeManager cm;
   
    Cube cube;
    // Start is called before the first frame update
    async void Start()
    {
        cm = new CubeManager(connectType);
        cube = await cm.SingleConnect();
        await cube.ConfigAttitudeSensor(Cube.AttitudeFormat.Eulers, 100, Cube.AttitudeNotificationType.Always);
        
        

    }
    
    void Update()
    {
        if (cm.IsControllable(cube))
        {
           if(Time.time < 2.0)
            {
                cube.Move(20, -20, 100);
                
            }
            Debug.Log(cube.eulers.z);
        }
    }
    
}
