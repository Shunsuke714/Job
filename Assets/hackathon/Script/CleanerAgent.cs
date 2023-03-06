using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class CleanerAgent : Agent
{
    Rigidbody rBody;

    //�����������Ă��邩��\���ϐ�
    int hand;
    
    //���Еt�������̕ϐ�
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


    //�G�s�\�[�h�J�n��
    public override void OnEpisodeBegin()
    {
        hand = -1;
        cnt = 0;

        //�^�[�Q�b�g�̔z�u
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

    //�ϑ���^����
    public override void CollectObservations(VectorSensor sensor)
    {
        //Agent�̍��W
        sensor.AddObservation(this.transform.localPosition.x / 10f);
        sensor.AddObservation(this.transform.localPosition.z / 10f);
        
        //Target�Ɋւ�����
        for(int i = 0; i < 4; i++)
        {
            sensor.AddObservation(Check(Targets[i], i));
        }
        
        //Agent�̉�]��
        sensor.AddObservation(this.transform.localEulerAngles.y / 360f);
        
        //Agent�̑��x
        sensor.AddObservation(rBody.velocity.x / 10f);
        sensor.AddObservation(rBody.velocity.z / 10f);
    }

    //�s�����Ƃ�
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
       
        Vector3 dirToGo = Vector3.zero;
        Vector3 rotateDir = Vector3.zero;
        int action = actionBuffers.DiscreteActions[0];

        if (action == 1) dirToGo = transform.forward;
        if (action == 2) dirToGo = transform.forward * -1.0f;
        if (action == 3) rotateDir = transform.up;
        if (action == 4) rotateDir = transform.up * -1.0f;

        //��]
        transform.Rotate(rotateDir, Time.deltaTime * 200f);
        
        //���i
        this.rBody.AddForce(dirToGo * 0.4f, ForceMode.VelocityChange);

        //�^�[�Q�b�g�ւ̋���
        float[] distancetoTargets = new float [4];

        //���ւ̋���
        float[] distancetoBoxes = new float [4];

        for(int i = 0; i < 4; i++)
        {
            distancetoTargets[i] = Vector3.Distance(this.transform.localPosition, Targets[i].transform.localPosition);
            distancetoBoxes[i] = Vector3.Distance(this.transform.localPosition, Boxes[i].localPosition);
        }
        
        for(int i = 0; i < 4; i++)
        {
            //���������Ă��Ȃ��āA�^�[�Q�b�g����������Ƃ�
            if(hand==-1 && Targets[i].activeSelf && distancetoTargets[i] < 1.42f)
            {
                AddReward(0.1f);
                Targets[i].SetActive(false);
                hand = i;
                dis = distancetoBoxes[i];
                var objColor = Targets[i].GetComponent<Renderer>().material.color;
                GetComponent<Renderer>().material.color = objColor;
            }

            //�^�[�Q�b�g�������Ă��āA���������ɋ߂Â��Ƃ�
            if (hand == i && distancetoBoxes[i] < dis - 1f)
            {
                dis = distancetoBoxes[i];
                AddReward(0.01f);
            }
            
            //�^�[�Q�b�g�������Ă��āA���ɕԂ����Ƃ�
            if (hand == i && distancetoBoxes[i] < 1.42f)
            {
                hand = -1;
                cnt++;
                GetComponent<Renderer>().material.color = Color.black;
                AddReward(0.15f);

                //���ׂĕԂ����Ȃ�A�G�s�\�[�h�I��
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
   
    //�^�[�Q�b�g�̏���one-hot�x�N�g���ŕԂ��֐�
    //(1,0,0) -> �^�[�Q�b�g���܂�������Ă��Ȃ� 
    //(0,1,0) -> �^�[�Q�b�g����Ɏ����Ă���
    //(0,0,1) -> �^�[�Q�b�g��Еt���I�����
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
