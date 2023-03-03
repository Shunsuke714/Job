using UnityEngine;
using toio;

public class MagnetState : MonoBehaviour
{
    CubeManager cubeManager;
    Cube cube;
    public ConnectType connectType;

    async void Start()
    {
        // create a cube manager
        cubeManager = new CubeManager(connectType);
        // connect to the nearest cube
        cube = await cubeManager.SingleConnect();
        await cube.ConfigMagneticSensor(Cube.MagneticMode.MagnetState);

    }


   

    void Update()
    {
        
        // check connection status and order interval
        if (cubeManager.IsControllable(cube))
        {
            
            Debug.Log(cube.magnetState);

            //         |    |   `--- duration [ms]
            //         |    `------- right motor speed
            //         `------------ left motor speed
        }
    }
}