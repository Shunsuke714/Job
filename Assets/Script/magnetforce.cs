using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class magnetforce : MonoBehaviour
{
    CubeManager cubeManager;
    public ConnectType connectType;
    Cube cube;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(connectType);
        cube = await cubeManager.SingleConnect();
        await cube.ConfigMagneticSensor(Cube.MagneticMode.MagneticForce, 100, Cube.MagneticNotificationType.Always);
    }

    // Update is called once per frame
    void Update()
    {
        if (cubeManager.IsControllable(cube))
        {
            
            Debug.Log(cube.magneticForce);

            //         |    |   `--- duration [ms]
            //         |    `------- right motor speed
            //         `------------ left motor speed
        }
    }
}
