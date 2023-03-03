using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
public class version : MonoBehaviour
{
    // Start is called before the first frame update
    CubeManager cubeManager;
    Cube cube;
    public ConnectType connectType;

    async void Start()
    {
        // create a cube manager
        cubeManager = new CubeManager(connectType);
        // connect to the nearest cube
        cube = await cubeManager.SingleConnect();


    }




    void Update()
    {

        // check connection status and order interval
        if (cubeManager.IsControllable(cube))
        {

            Debug.Log(cube.version);

            //         |    |   `--- duration [ms]
            //         |    `------- right motor speed
            //         `------------ left motor speed
        }
    }
}
