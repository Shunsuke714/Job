using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using toio;
using toio.Simulator;
using System.Linq;

public class WallAgent : Agent
{
    //ê›íË
    public ConnectType connectType;
    CubeManager cm;
    GameObject cube;
    Stage stage;
    Mat mat;
    Transform targetPole;
    GameObject obstacle;
    GameObject obstacle2;
    GameObject obstacle3;
    GameObject obstacle4;

    float matW = 0.54f;
    float matH = 0.54f;
    float radius = 0.2f;
    int targetAngle;
    float previousDistance;
    //Vector3 prevPosition;

    
    async public override void Initialize()
    {
        this.stage = GameObject.FindObjectOfType<Stage>();
        this.mat = GameObject.FindObjectOfType<Mat>();
        this.targetPole = this.stage.transform.Find("TargetPole");
        this.cube = GameObject.Find("Cube");
        this.obstacle = GameObject.Find("Obstacle");
        this.obstacle2 = GameObject.Find("Obstacle2");
        this.obstacle3 = GameObject.Find("Obstacle3");
        this.obstacle4 = GameObject.Find("Obstacle4");

        this.cm = new CubeManager(connectType);
        await this.cm.SingleConnect();

    }

    public override void OnEpisodeBegin()
    {
        this.cube.transform.rotation = new Quaternion();
        this.cube.transform.position = new Vector3();

        previousDistance = float.MaxValue;
        targetAngle = Random.Range(0, 361);

        float z = radius * Mathf.Cos(targetAngle * Mathf.Deg2Rad);
        float x = radius * Mathf.Sin(targetAngle * Mathf.Deg2Rad);
        this.targetPole.position = new Vector3(x, 0, z);
        this.targetPole.gameObject.SetActive(true);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(this.cube.transform.position.x);
        sensor.AddObservation(this.cube.transform.position.z);
        sensor.AddObservation(this.cube.transform.rotation.eulerAngles.y /360.0f);
        sensor.AddObservation(this.targetPole.position.x);
        sensor.AddObservation(this.targetPole.position.z);
        sensor.AddObservation(this.obstacle.transform.position.x);
        sensor.AddObservation(this.obstacle2.transform.position.x);
        sensor.AddObservation(this.obstacle3.transform.position.x);
        sensor.AddObservation(this.obstacle4.transform.position.x);
        sensor.AddObservation(this.obstacle.transform.position.z);
        sensor.AddObservation(this.obstacle2.transform.position.z);
        sensor.AddObservation(this.obstacle3.transform.position.z);
        sensor.AddObservation(this.obstacle4.transform.position.z);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {

        //éûä‘åoâﬂÇ≈ïÒèVå∏
        AddReward(-0.002f);Å@
        

        //çsìÆ
        int action = actionBuffers.DiscreteActions[0];
        foreach (var handle in cm.syncHandles)
        {
            if (action == 1) handle.Move(0, -40, 100, false); //ç∂
            if (action == 2) handle.Move(0, 40, 100, false); //âE
            if (action == 3) handle.Move(40, 0, 100, false); //ëO
            if (action == 4) handle.Move(-40, 0, 100, false); //å„ÇÎ
        }

        float distance = Vector3.Distance(this.cube.transform.position, this.targetPole.position);
        

        //ìûíB
        if (distance < 0.03f)
        {
            AddReward(1.0f);
            distance = 0.2f;
            EndEpisode();
            
        }

        

        //óéâ∫ÇµÇΩÇÁèIóπÇ©Ç¬ÅAïÒèVå∏
        Vector3 CubePos = this.cube.transform.position;
        if (CubePos.x < -matW * 0.9f / 2f || matW * 0.9f / 2f < CubePos.x
            || CubePos.z < -matH * 0.9f / 2f || matH * 0.9f / 2f < CubePos.z)
        {
            distance = 0.2f;
            EndEpisode();
            

        }

        //ãﬂÇ√Ç¢ÇΩÇÁïÒèV
        if (distance < previousDistance-0.01f)
        {
            previousDistance = distance;
            AddReward(0.03f);
        }

        float obstacleDistance = Vector3.Distance(this.cube.transform.position, this.obstacle.transform.position);
        float obstacleDistance2 = Vector3.Distance(this.cube.transform.position, this.obstacle2.transform.position);
        float obstacleDistance3 = Vector3.Distance(this.cube.transform.position, this.obstacle3.transform.position);
        float obstacleDistance4 = Vector3.Distance(this.cube.transform.position, this.obstacle4.transform.position);
        float[] list = { obstacleDistance, obstacleDistance2, obstacleDistance3, obstacleDistance4 };
        float mini = list.Min();

        //è·äQï®Ç…ìñÇΩÇÈÇ∆ïÒèVå∏
        if (mini < 0.055f)
        {
            AddReward(-0.03f);
        }

        /*var currentPostion = this.cube.transform.position;
        Vector3 deltaPosition = currentPostion - prevPosition;


        //à⁄ìÆï˚å¸ÇÃäpìx
        float deltaAngle = Mathf.Atan2(deltaPosition.x, deltaPosition.z) * Mathf.Rad2Deg;
        if (deltaAngle < 0)
        {
            deltaAngle += 360f;
        }

        Vector3 goal = targetPole.position - this.cube.transform.position;

        //ÉSÅ[ÉãÇ÷ÇÃäpìx
        float goalAngle = Mathf.Atan2(goal.x, goal.z) * Mathf.Rad2Deg;
        if (goalAngle < 0)
        {
            goalAngle += 360f;
        }


        if (deltaPosition.magnitude > 0.001f)
        {

            //à⁄ìÆï˚å¸Ç™çáÇ¡ÇƒÇ¢ÇÈ
            if (deltaAngle > goalAngle - 5 && deltaAngle < goalAngle + 5)
            {

                AddReward(0.01f);
            }

            



        }

        float agentAngle = this.cube.transform.rotation.eulerAngles.y;

        //å¸Ç´Ç™çáÇ¡ÇƒÇ¢ÇÈ  
        if (agentAngle > goalAngle - 5 && agentAngle < goalAngle + 5)
        {

            AddReward(0.03f);
        }

        prevPosition = currentPostion;
        */

    }


    



    public override void Heuristic(in ActionBuffers actionsOut)
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        var discreseActionsOut = actionsOut.DiscreteActions;
        discreseActionsOut[0] = 0;
        if (h < -0.5f) discreseActionsOut[0] = 1;
        if (h > 0.5f) discreseActionsOut[0] = 2;
        if (v > 0.5f) discreseActionsOut[0] = 3;
        if (v < -0.5f) discreseActionsOut[0] = 4;
    }

    

}
