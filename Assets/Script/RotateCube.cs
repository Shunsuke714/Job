using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class RotateCube : MonoBehaviour
{
    public ConnectType connectType;

    CubeManager cm;
    Cube cube;

    // Start is called before the first frame update
    async void Start()
    {
        cm = new CubeManager(connectType);
        await cm.MultiConnect(1);
        await cube.ConfigAttitudeSensor(Cube.AttitudeFormat.Eulers, 100, Cube.AttitudeNotificationType.Always);
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time < 1)
        {
            foreach (var cube in cm.syncCubes)
            {
                cube.Move(50, -50, 100);
                Debug.Log(cube.eulers.z);
            }
            
        }
        
    }
}
