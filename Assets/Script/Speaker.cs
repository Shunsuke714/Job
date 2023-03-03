using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using toio.Simulator;

public class Speaker : MonoBehaviour
{
    [SerializeField] AudioSource _source;

    double startDspTime;
    double buffer = 2 / 60d;

    // Start is called before the first frame update
    void Start()
    {
        startDspTime = AudioSettings.dspTime;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var nxtRng = NextRingTime();
        if(nxtRng < AudioSettings.dspTime + buffer)
        {
            _source.PlayScheduled(nxtRng);
            Debug.Log(nxtRng);
        }
    }

    double NextRingTime()
    {
        var interval = 10d;
        var elapsedDspTime = AudioSettings.dspTime - startDspTime;
        var num = System.Math.Floor(elapsedDspTime / interval);

        return startDspTime + (num + 1d) * interval;
    }
}

