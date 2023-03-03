using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using toio;
using toio.Simulator;


public class Voice : Agent
{
    //設定
    public ConnectType connectType;
    CubeManager cm;
    GameObject cube;
    Stage stage;
    Transform targetPole;

    float radius = 0.1f;
    
    async public override void Initialize()
    {
        this.stage = GameObject.FindObjectOfType<Stage>();
        this.cube = GameObject.Find("Cube");
        this.targetPole = this.stage.transform.Find("TargetPole");

        this.cm = new CubeManager(connectType);
        await this.cm.SingleConnect();
    }

    public override void OnEpisodeBegin()
    {
        //キューブの回転と位置を初期位置に戻す
        this.cube.transform.rotation = Quaternion.identity;
        this.cube.transform.position = Vector3.zero;
        //ランダムな角度を取得
        int value = Random.Range(0, 361);
        Debug.Log(value);

        float z = radius * Mathf.Cos(value * Mathf.Deg2Rad);
        float x = radius * Mathf.Sin(value * Mathf.Deg2Rad);
        this.targetPole.position = new Vector3(x, 0, z);
        this.targetPole.rotation = Quaternion.Euler(0, value, 0);
        this.targetPole.gameObject.SetActive(true);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(this.cube.transform.rotation.eulerAngles.y);
        sensor.AddObservation(this.targetPole.rotation.eulerAngles.y);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-0.005f);
        int action = actionBuffers.DiscreteActions[0];
        foreach (var handle in cm.syncHandles)
        {
            if (action == 1) handle.Move(0, -40, 100, false);
            if (action == 2) handle.Move(0, 40, 100, false);
        }
        var agentAngle = this.cube.transform.rotation.eulerAngles.y;
        var targetAngle = this.targetPole.rotation.eulerAngles.y;
        

        if (agentAngle > targetAngle - 5 && agentAngle < targetAngle + 5)
        {
            AddReward(1.0f);
            EndEpisode();
        }
    }

        public override void Heuristic(in ActionBuffers actionsOut)
    {
        float h = Input.GetAxis("Horizontal");
        var discreseActionsOut = actionsOut.DiscreteActions;
        discreseActionsOut[0] = 0;
        if (h < -0.5f) discreseActionsOut[0] = 1;
        if (h > 0.5f) discreseActionsOut[0] = 2;
    }


}



