using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GetAudioData : MonoBehaviour
{
    public bool drawLine;
    public readonly float volume;
    private float[] waveData_ = new float[1024];
    float[] spectrum = new float[1024];
    public int ymag = 100;
    public float xmag = 0.2f;
    public int amp;
    private void Update()
    {

        /*for (int i = 1; i < waveData_.Length - 1; i++)
        {
            Debug.DrawLine(new Vector3(xmag * (i - 1 - 512), ymag * waveData_[i], 100), new Vector3(xmag * (i - 512), ymag * waveData_[i + 1], 100), Color.red);
        }
        for (int i = 1; i < waveData_.Length - 1; i++)
        {
            Debug.DrawLine(new Vector3(xmag * (i - 1 - 512), ymag * waveData_[i], 100), new Vector3(xmag * (i - 512), ymag * waveData_[i + 1], 100), Color.green);
        }
        for (int i = 1; i < waveData_.Length - 1; i++)
        {
            Debug.DrawLine(new Vector3(xmag * (i - 1 - 512), ymag * waveData_[i], 100), new Vector3(xmag * (i - 512), ymag * waveData_[i + 1], 100), Color.blue);
        }
        for (int i = 1; i < waveData_.Length - 1; i++)
        {
            Debug.DrawLine(new Vector3(xmag * (i - 1 - 512), ymag * waveData_[i], 100), new Vector3(xmag * (i - 512), ymag * waveData_[i + 1], 100), Color.yellow);
        }*/

        AudioListener.GetOutputData(waveData_, 0);
        float frontleftvolume = waveData_.Select(x => x * x).Sum() / waveData_.Length;

        AudioListener.GetOutputData(waveData_, 1);
        float frontrightvolume = waveData_.Select(x => x * x).Sum() / waveData_.Length;
        
        AudioListener.GetOutputData(waveData_, 2);
        float backleftvolume = waveData_.Select(x => x * x).Sum() / waveData_.Length;
        
        AudioListener.GetOutputData(waveData_, 3);
        float backrightvolume = waveData_.Select(x => x * x).Sum() / waveData_.Length;
        
        //AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);
        //float mean_spectrum = spectrum.Select(x => x * x).Sum() / spectrum.Length;

        Debug.Log("FrontLeft"+frontleftvolume * amp);
        Debug.Log("FrontRight"+frontrightvolume * amp);
        Debug.Log("RearLeft" + backleftvolume * amp);
        Debug.Log("RearRight" + backrightvolume * amp);
        //Debug.Log(mean_spectrum);
        if (drawLine)
        {
            for (int i = 1; i < waveData_.Length - 1; i++)
            {
                Debug.DrawLine(new Vector3(xmag * (i - 1 - 512), ymag * waveData_[i], 100), new Vector3(xmag * (i - 512), ymag * waveData_[i + 1], 100), Color.red);
            }

            for (int i = 1; i < waveData_.Length - 1; i++)
            {
                Debug.DrawLine(new Vector3(xmag * (i - 1 - 512), ymag * spectrum[i] + 100, 100), new Vector3(xmag * (i - 512), ymag * spectrum[i + 1] + 100, 100), Color.blue);
            }
        }
        
    }

}
