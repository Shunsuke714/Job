using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class BallAgent : Agent
{
    // Start is called before the first frame update
    Rigidbody rBody;
    float countTime;
    //float distance;
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
    }

    public GameObject Target1;
    public GameObject Target2;
    public GameObject Target3;
    //public Transform Wall1;
    //public Transform Wall2;
    //public Transform Wall3;

    int cnt;

    public override void OnEpisodeBegin()
    {
        // If the Agent fell, zero its momentum
        countTime = 0;
        cnt = 0;
        //this.rBody.angularVelocity = Vector3.zero;
        //this.rBody.velocity = Vector3.zero;
        //this.transform.localPosition = new Vector3(0, 0.5f, 0f);

        Target1.SetActive(true);
        Target2.SetActive(true);
        Target3.SetActive(true);
        //distance = 20f;
        // Move the target to a new spot
        Target1.transform.localPosition = new Vector3(Random.Range(-9f, 9f),
                                           0.5f,
                                           Random.Range(-9f, 9f));
        Target2.transform.localPosition = new Vector3(Random.Range(-9f, 9f),
                                           0.5f,
                                           Random.Range(-9f, 9f));
        Target3.transform.localPosition = new Vector3(Random.Range(-9f, 9f),
                                           0.5f,
                                           Random.Range(-9f, 9f));
        //Wall1.localPosition = new Vector3(Random.Range(-2.5f, 2.5f), 0.5f, -10f);
        //Wall2.localPosition = new Vector3(Random.Range(-2.5f, 2.5f), 0.5f, 0f);
        //Wall3.localPosition = new Vector3(Random.Range(-2.5f, 2.5f), 0.5f, 10f);

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation(this.transform.localPosition.x/10f);
        sensor.AddObservation(this.transform.localPosition.z/10f);
        sensor.AddObservation(Target1.activeSelf);
        sensor.AddObservation(Target2.activeSelf);
        sensor.AddObservation(Target3.activeSelf);
        /*
        float distancetoTarget1 = Vector3.Distance(this.transform.localPosition, Target1.transform.localPosition);
        float distancetoTarget2 = Vector3.Distance(this.transform.localPosition, Target2.transform.localPosition);
        float distancetoTarget3 = Vector3.Distance(this.transform.localPosition, Target3.transform.localPosition);
        sensor.AddObservation(distancetoTarget1);
        sensor.AddObservation(distancetoTarget2);
        sensor.AddObservation(distancetoTarget3);
        */

        sensor.AddObservation(this.transform.localEulerAngles.y/360f);
        // Agent velocity
        sensor.AddObservation(rBody.velocity.x/10f);
        sensor.AddObservation(rBody.velocity.z/10f);
    }

    public float forceMultiplier = 10;
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        
        Vector3 dirToGo = Vector3.zero;
        Vector3 rotateDir = Vector3.zero;
        int action = actionBuffers.DiscreteActions[0];

        if (action == 1) dirToGo = transform.forward;
        if (action == 2) dirToGo = transform.forward * -1.0f;
        if (action == 3) rotateDir = transform.up;
        if (action == 4) rotateDir = transform.up * -1.0f;
        transform.Rotate(rotateDir, Time.deltaTime * 200f);
        this.rBody.AddForce(dirToGo * 0.4f, ForceMode.VelocityChange);

        float distancetoTarget1 = Vector3.Distance(this.transform.localPosition, Target1.transform.localPosition);
        float distancetoTarget2 = Vector3.Distance(this.transform.localPosition, Target2.transform.localPosition);
        float distancetoTarget3 = Vector3.Distance(this.transform.localPosition, Target3.transform.localPosition);


        // Reached target
        if (Target1.activeSelf &&  distancetoTarget1 < 1.42f)
        {
            AddReward(0.1f);
            Target1.SetActive(false);
            cnt++;
        }
        if (Target2.activeSelf && distancetoTarget2< 1.42f)
        {
            AddReward(0.1f);
            Target2.SetActive(false);
            cnt++;
        }
        if (Target3.activeSelf && distancetoTarget3 < 1.42f)
        {
            AddReward(0.1f);
            Target3.SetActive(false);
            cnt++;
        }

        AddReward(-1f/MaxStep);
        if (cnt == 3)
        {
            AddReward(1f);
            Debug.Log(GetCumulativeReward());
            EndEpisode();
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
    public void FixedUpdate()
    {
        countTime += Time.deltaTime;
        if (countTime > 50f)
        {
            SetReward(0);
            EndEpisode();
        }
    }
}
