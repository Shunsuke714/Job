using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using toio.Simulator;

public class ActivateTargetPole : MonoBehaviour
{
    Transform targetPole;
    Stage stage;
    public float x;
    public float z;
    // Start is called before the first frame update
    void Start()
    {
        this.stage = GameObject.FindObjectOfType<Stage>();
        this.targetPole = this.stage.transform.Find("TargetPole");
        //íÜêSÇ…îzíu
        this.targetPole.position = new Vector3(x, 0.001f, z);
        this.targetPole.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
