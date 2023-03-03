using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GetAudioDataMono : MonoBehaviour
{
    public bool drawLine;
    public readonly float volume;
    private float[] waveData_ = new float[1024];
    public int ymag = 100;
    public float xmag = 0.2f;
    public int amp;
    private void Update()
    {
        AudioListener.GetOutputData(waveData_, 0);
        float volume = waveData_.Select(x => x * x).Sum() / waveData_.Length;

        Debug.Log("Volume" + volume * amp);
    }

}
