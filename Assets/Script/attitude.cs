using UnityEngine;
using toio;

public class attitude : MonoBehaviour
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
        await cube.ConfigAttitudeSensor(Cube.AttitudeFormat.Eulers, 100, Cube.AttitudeNotificationType.Always);

    }




    void Update()
    {

        // check connection status and order interval
        if (cubeManager.IsControllable(cube))
        {

            Debug.Log(cube.eulers.z);
            

           
        }
    }
}