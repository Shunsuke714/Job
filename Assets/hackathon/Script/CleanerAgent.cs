using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class CleanerAgent : Agent
{
    Rigidbody rBody;

    //今何を持っているかを表す変数
    int hand;
    
    //何個片付けたかの変数
    int cnt;
    
    float dis;
    //float distance;
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
    }

    public static GameObject Target1;
    public static GameObject Target2;
    public static GameObject Target3;
    public static GameObject Target4;
    
    public static Transform Box1;
    public static Transform Box2;
    public static Transform Box3;
    public static Transform Box4;
    public GameObject [] Targets= { Target1, Target2, Target3, Target4};
    public Transform[] Boxes = { Box1, Box2, Box3, Box4 };


    //エピソード開始時
    public override void OnEpisodeBegin()
    {
        hand = -1;
        cnt = 0;

        //ターゲットの配置
        while (true)
        {
            for(int i = 0; i < 4; i++)
            {
                Targets[i].transform.localPosition = new Vector3(Random.Range(-7f, 7f), 0.5f, Random.Range(-7f, 7f));
            }
            bool space = true;
            for(int i = 0; i < 4; i++)
            {
                for(int j = i + 1; j < 4; j++)
                {
                    float distance = Vector3.Distance(Targets[i].transform.localPosition, Targets[j].transform.localPosition);
                    if (distance < 1f)
                    {
                        space = false;
                    }
                }
            }
            if (!space) continue;
            for(int i = 0; i < 4; i++)
            {
                Targets[i].SetActive(true);
            }
            break;
        }

    }

    //観測を与える
    public override void CollectObservations(VectorSensor sensor)
    {
        //Agentの座標
        sensor.AddObservation(this.transform.localPosition.x / 10f);
        sensor.AddObservation(this.transform.localPosition.z / 10f);
        
        //Targetに関する情報
        for(int i = 0; i < 4; i++)
        {
            sensor.AddObservation(Check(Targets[i], i));
        }
        
        //Agentの回転量
        sensor.AddObservation(this.transform.localEulerAngles.y / 360f);
        
        //Agentの速度
        sensor.AddObservation(rBody.velocity.x / 10f);
        sensor.AddObservation(rBody.velocity.z / 10f);
    }

    //行動をとる
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
       
        Vector3 dirToGo = Vector3.zero;
        Vector3 rotateDir = Vector3.zero;
        int action = actionBuffers.DiscreteActions[0];

        if (action == 1) dirToGo = transform.forward;
        if (action == 2) dirToGo = transform.forward * -1.0f;
        if (action == 3) rotateDir = transform.up;
        if (action == 4) rotateDir = transform.up * -1.0f;

        //回転
        transform.Rotate(rotateDir, Time.deltaTime * 200f);
        
        //並進
        this.rBody.AddForce(dirToGo * 0.4f, ForceMode.VelocityChange);

        //ターゲットへの距離
        float[] distancetoTargets = new float [4];

        //箱への距離
        float[] distancetoBoxes = new float [4];

        for(int i = 0; i < 4; i++)
        {
            distancetoTargets[i] = Vector3.Distance(this.transform.localPosition, Targets[i].transform.localPosition);
            distancetoBoxes[i] = Vector3.Distance(this.transform.localPosition, Boxes[i].localPosition);
        }
        
        for(int i = 0; i < 4; i++)
        {
            //何も持っていなくて、ターゲットを回収したとき
            if(hand==-1 && Targets[i].activeSelf && distancetoTargets[i] < 1.42f)
            {
                AddReward(0.1f);
                Targets[i].SetActive(false);
                hand = i;
                dis = distancetoBoxes[i];
                var objColor = Targets[i].GetComponent<Renderer>().material.color;
                GetComponent<Renderer>().material.color = objColor;
            }

            //ターゲットを持っていて、正しい箱に近づくとき
            if (hand == i && distancetoBoxes[i] < dis - 1f)
            {
                dis = distancetoBoxes[i];
                AddReward(0.01f);
            }
            
            //ターゲットを持っていて、箱に返したとき
            if (hand == i && distancetoBoxes[i] < 1.42f)
            {
                hand = -1;
                cnt++;
                GetComponent<Renderer>().material.color = Color.black;
                AddReward(0.15f);

                //すべて返したなら、エピソード終了
                if (cnt==4)
                {
                    Debug.Log(GetCumulativeReward());
                    EndEpisode();
                }
            }
        }

        
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.DiscreteActions;
        actions[0] = 0;
        if (Input.GetKey(KeyCode.UpArrow)) actions[0] = 1;
        if (Input.GetKey(KeyCode.DownArrow)) actions[0] = 2;
        if (Input.GetKey(KeyCode.LeftArrow)) actions[0] = 3;
        if (Input.GetKey(KeyCode.RightArrow)) actions[0] = 4;
    }
   
    //ターゲットの情報をone-hotベクトルで返す関数
    //(1,0,0) -> ターゲットをまだ回収していない 
    //(0,1,0) -> ターゲットを手に持っている
    //(0,0,1) -> ターゲットを片付け終わった
    public Vector3 Check(GameObject target, int n)
    {
        Vector3  v = new Vector3(0, 0, 0);
        if (target.activeSelf)
        {
            v[0] = 1;
        }
        else if(!target.activeSelf && hand == n)
        {
            v[1] = 1;
        }
        else if(!target.activeSelf && hand != n)
        {
            v[2] = 1;
        }
        return v;
    }
}
