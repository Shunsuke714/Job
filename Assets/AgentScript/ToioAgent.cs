using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using toio;
using toio.Simulator;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using System.Linq; 

public class ToioAgent : Agent
{
    //�ݒ�
    public ConnectType connectType;
    public bool ROS_use;
    CubeManager cm;
    GameObject cube;
    Stage stage;
    Mat mat;
    Transform targetPole;
    Cube c;
    ROSConnection ros;

    //bool canSubscribe;
    float matW = 0.54f;
    float matH = 0.54f;
    public float radius = 0.2f;
    int targetAngle;
    float distance;
    float previousDistance;
    float raydistance;
    float rightraydistance;
    float leftraydistance;
    private float[] waveData_ = new float[1024];
    

    async public override void Initialize()
    {
        this.stage = GameObject.FindObjectOfType<Stage>();
        this.mat = GameObject.FindObjectOfType<Mat>();
        this.targetPole = this.stage.transform.Find("TargetPole");
        this.cube = GameObject.Find("Cube");
        this.cm = new CubeManager(connectType);
        //ros = ROSConnection.GetOrCreateInstance();
        //�T�u�X�N���C�o�[�̓o�^
        //ros.Subscribe<Int16Msg>("voice", OnSubscribe);
        c = await this.cm.SingleConnect();   
    }

    //�T�u�X�N���C�u�����Ƃ��ɌĂ΂��֐�
    /*public void OnSubscribe(Int16Msg msg)
    {
        if (canSubscribe)
        {
            targetAngle = -msg.data;
            float z = radius * Mathf.Cos(targetAngle * Mathf.Deg2Rad);
            float x = radius * Mathf.Sin(targetAngle * Mathf.Deg2Rad);
            this.targetPole.position = new Vector3(x, 0.001f, z);
            this.targetPole.gameObject.SetActive(true);
            Time.timeScale = 1;
            canSubscribe = false;
        }
        
    }*/

    //RayCast�ŋ������o���֐�
    public float RayDistance(Ray ray, RaycastHit hit)
    {
        if (Physics.Raycast(ray, out hit))
        {
            return Vector3.Distance(hit.transform.position, this.cube.transform.position);
        }
        else
        {
            return 0;
        }
        
    }
    public override void OnEpisodeBegin()
    {
        if(this.connectType != ConnectType.Real)
        {
            this.cube.transform.rotation = new Quaternion();
            this.cube.transform.position = new Vector3();
        }
        
        previousDistance = float.MaxValue;
        //ROS�ƒʐM����Ƃ�
        if (ROS_use)
        {
            //this.targetPole.gameObject.SetActive(false);
            //Time.timeScale = 0;
            //canSubscribe = true;
        }
        else
        {
            targetAngle = 90;
            //targetAngle = Random.Range(0, 361);
            float z = radius * Mathf.Cos(targetAngle * Mathf.Deg2Rad);
            float x = radius * Mathf.Sin(targetAngle * Mathf.Deg2Rad);
            this.targetPole.position = new Vector3(x, 0.001f, z);
            this.targetPole.gameObject.SetActive(true);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 goal = targetPole.position - this.cube.transform.position;
        //�^�[�Q�b�g�|�[���ƃL���[�u�̂Ȃ��p
        float goalangle = Mathf.Atan2(goal.x, goal.z) * Mathf.Rad2Deg;
        //�L���[�u�̉�]�p
        float cubeangle = this.cube.transform.rotation.eulerAngles.y;

        if (cubeangle > 180f)
        {
            cubeangle -= 360f;
        }

        var direction = this.cube.transform.forward;
        var rightangle = new Vector3(0f, 45f, 0f);
        var leftangle = new Vector3(0f, -45f, 0f);
        var rightdirection = Quaternion.Euler(rightangle) * direction;
        var leftdirection = Quaternion.Euler(leftangle) * direction;

        Vector3 rayPosition = this.cube.transform.position + new Vector3(0.0f, 0.01f, 0.0f);
        Ray ray = new Ray(rayPosition, direction);
        Ray rightray = new Ray(rayPosition, rightdirection);
        Ray leftray = new Ray(rayPosition, leftdirection);
        RaycastHit hit = new RaycastHit();

        raydistance = RayDistance(ray, hit);
        rightraydistance = RayDistance(rightray, hit);
        leftraydistance = RayDistance(leftray, hit);
        /*
        float[] volume_list = new float[4];
        //���ʂ�4ch�ł���
        for(int i = 0; i < 4; i++)
        {
            AudioListener.GetOutputData(waveData_, i);
            volume_list[i] = waveData_.Select(x => x * x).Sum() / waveData_.Length;
        }

        //���O�̉���
        sensor.AddObservation(volume_list[0]);
        //�E�O�̉���
        sensor.AddObservation(volume_list[1]);
        //�����̉���
        sensor.AddObservation(volume_list[2]);
        //�E���̉���
        sensor.AddObservation(volume_list[3]);
        //���Ίp�x
        //sensor.AddObservation((goalangle - cubeangle) / 180f);
        //RayCast�̋���
        sensor.AddObservation(raydistance);
        //RayCast�̋����i�E�΂߁j
        sensor.AddObservation(rightraydistance);
        //RayCast�̋����i���΂߁j
        sensor.AddObservation(leftraydistance);
        */

        /*
        Debug.Log("Left"+leftraydistance);
        Debug.Log("Center"+raydistance);
        Debug.Log("Right"+rightraydistance);
        Debug.DrawRay(rayPosition, ray.direction * 3.0f, Color.red);
        Debug.DrawRay(rayPosition, rightray.direction * 3.0f, Color.red);
        Debug.DrawRay(rayPosition, leftray.direction * 3.0f, Color.red);
        */
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {        
        //�s��
        int action = actionBuffers.DiscreteActions[0];
        foreach (var handle in cm.syncHandles)
        {
            if (action == 1) handle.Move(0, -40, 100, false); //��
            if (action == 2) handle.Move(0, 40, 100, false); //�E
            if (action == 3) handle.Move(40, 0, 100, false); //�O
            if (action == 4) handle.Move(-40, 0, 100, false); //���
        }

        distance = Vector3.Distance(this.cube.transform.position, this.targetPole.position);

        //���B
        if (distance < 0.03f)
        {
            if(connectType == ConnectType.Real)
            {
                c.PlayPresetSound(10, 100);
            }
            AddReward(1.0f);          
            distance = 0.2f;
            EndEpisode();
        }

        //����������I��
        Vector3 CubePos = this.cube.transform.position;
        if (CubePos.x < -matW * 0.9f / 2f || matW * 0.9f / 2f < CubePos.x
            || CubePos.z < -matH * 0.9f / 2f || matH * 0.9f / 2f < CubePos.z)
        {
            distance = 0.2f;
            EndEpisode();            
        }
        
        //�߂Â������V
        if (distance < previousDistance- 0.01f)
        {
            previousDistance = distance;
            AddReward(0.03f);
        }
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

    //���@�̓���
    public void FixedUpdate()
    {
        if (this.connectType == ConnectType.Real)
        {
            foreach(var c in cm.cubes)
            {
                this.cube.transform.position = this.mat.MatCoord2UnityCoord(c.x, c.y);
                this.cube.transform.rotation = Quaternion.Euler(0, this.mat.MatDeg2UnityDeg(c.angle), 0);
            } 
        }
    }

    
}