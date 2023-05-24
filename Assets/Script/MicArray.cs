using System.Collections;
using System;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra.Complex32;

using StearingVector = MathNet.Numerics.LinearAlgebra.Vector<MathNet.Numerics.Complex32>;


public class MicArray : MonoBehaviour
{
    [SerializeField] AudioSource _source;
    [SerializeField] GameObject _sourceobj;
    [SerializeField] float radius;
    [SerializeField] float height;
    [SerializeField] float degree;

    double startDspTime;
    double buffer = 5 / 60d;

    private int outputRate;
    bool recOutput;
    public List<float> RecordData = new List<float>();
    int count = 0;
    const double c = 340;
    float len;
    int advancePosition = 256;
    int windowSize = 512;
    int numStep = 10;

    public Complex32[] computeFFT(float[] wav, int startPosition, int windowSize)
    {
        Complex32[] complexData = new Complex32[windowSize];
        //複素数型に変換
        //Debug.Log(startPosition);
        for (int i = 0; i < windowSize; i++)
        {
            complexData[i] = new Complex32(wav[startPosition + i], 0f);   //
        }

        //run FFT
        Fourier.Forward(complexData, FourierOptions.Matlab); // arbitrary length
        return complexData;
    }

    StearingVector[][] LoadStearingVec(int numFFTPoint) //numFFTPoint= windowSize/2+1 = 257
    {
        TextAsset csvFile; // CSVファイル
        List<string[]> csvData = new List<string[]>(); // CSVの中身を入れるリスト
        csvFile = Resources.Load("stearing_vec_18direction_4ch") as TextAsset; // Resouces下のCSV読み込み

        StringReader reader = new StringReader(csvFile.text);

        // csvの読み込み：コンマ で分割しつつ一行ずつ読み込みリストに追加
        while (reader.Peek() != -1) // 最終行まで
        {
            string line = reader.ReadLine();
            csvData.Add(line.Split(','));
        }

        //領域確保
        int numDirection = csvData.Count / numFFTPoint;
        var stearingVecArray = new StearingVector[numDirection][];
        for (int i = 0; i < numDirection; i++)
        {
            stearingVecArray[i] = new StearingVector[numFFTPoint];
        }

        //代入
        for (int i = 0; i < csvData.Count; i++)
        {
            int dir = i / numFFTPoint; //方向インデックス
            int freq = i % numFFTPoint;  //周波数インデックス
            int L = csvData[i].Length; //=チャンネル数*2
            StearingVector stearingVec = Vector.Build.Dense(L / 2);
            for (int j = 0; j < L / 2; j++)
            {
                float real = float.Parse(csvData[i][j * 2]);
                float imag = float.Parse(csvData[i][j * 2 + 1]);
                stearingVec[j] = new Complex32(real, imag);
            }
            stearingVecArray[dir][freq] = stearingVec;
        }
        return stearingVecArray;
    }

    float[][] LoadWaveData()
    {
        
        float[][] wavData;
        int numChannel = 4;
        int size = advancePosition * (numStep - 1) + windowSize;
        wavData = new float[numChannel][];
        for(int ch = 0; ch < numChannel; ch++)
        {
            wavData[ch] = new float[size];
            for(int j = 0; j < size; j++)
            {
                wavData[ch][j] = RecordData[23000+outputRate*(2+ch)+3*j];
            }
        }
        return wavData;
    }

    // Start is called before the first frame update
    void Start()
    {
        recOutput = true;
        startDspTime = AudioSettings.dspTime;
        outputRate = AudioSettings.outputSampleRate;
        Debug.Log(AudioSettings.driverCapabilities);
        Debug.Log(AudioSettings.speakerMode);
        Debug.Log(outputRate);
        //Debug.Log(startDspTime);
        len = radius * Mathf.Sqrt(2);
        float radian = degree / 360 * 2 * Mathf.PI;
        float x = Mathf.Cos(radian) / Mathf.Sqrt(2) + Mathf.Sin(radian) / Mathf.Sqrt(2);
        float z = Mathf.Cos(radian) / Mathf.Sqrt(2) - Mathf.Sin(radian) / Mathf.Sqrt(2);
        _sourceobj.transform.position = new Vector3(x, height, z);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var elapsedDspTime = AudioSettings.dspTime - startDspTime;
        var nxtRng = NextRingTime();
        if (nxtRng < AudioSettings.dspTime + buffer)
        {
            _source.PlayScheduled(nxtRng);
            //Debug.Log(nxtRng);
        }
        //次に音を鳴らす時間を計算する
        double NextRingTime()
        {
            var interval = 1d;
            var elapsedDspTime = AudioSettings.dspTime - startDspTime;
            var num = System.Math.Floor(elapsedDspTime / interval);

            float dis = Vector3.Distance(_sourceobj.transform.position, this.transform.position);
            double delay = dis / c;

            return startDspTime + (num + 1d) * interval + delay;
        }

        //Listenerの移動
        if (count == 0 && elapsedDspTime > 1.8d)
        {
            this.transform.position = new Vector3(len / 2, height, len / 2);
            count++;
        }
        if (count == 1 && elapsedDspTime > 2.8d)
        {
            this.transform.position = new Vector3(len / 2, height, -len / 2);
            count++;
        }
        if (count == 2 && elapsedDspTime > 3.8d)
        {
            this.transform.position = new Vector3(-len / 2, height, -len / 2);
            count++;
        }
        if (count == 3 && elapsedDspTime > 4.8d)
        {
            this.transform.position = new Vector3(-len / 2, height, len / 2);
            count++;
        }
        if (elapsedDspTime > 6.5d)
        {
            recOutput = false;
            MUSIC();
            //RecordData.Clear();
            //WriteCsv();

        }


    }


    void MUSIC()
    {
        float[][] wavData = LoadWaveData();
        int numChannel = wavData.Length;
        int fullLength = wavData[0].Length;
        StearingVector[][] stearingVecArray = LoadStearingVec(257);
        Complex32[][] freqData = new Complex32[numChannel][];
        var corrArray = new MathNet.Numerics.LinearAlgebra.Matrix<MathNet.Numerics.Complex32>[windowSize / 2 + 1];
        for (int freq = 0; freq < windowSize / 2 + 1; freq++)
        {
            var corr = Matrix.Build.Dense(numChannel, numChannel);
            corrArray[freq] = corr;
        }
        for (int step = 0; step < numStep; step++)
        {
            for (int i = 0; i < numChannel; i++)
            {
                freqData[i] = computeFFT(wavData[i], advancePosition * step, windowSize);
            }

            for (int freq = 0; freq < windowSize / 2 + 1; freq++)
            {
                for (int i = 0; i < numChannel; i++)
                {
                    for (int j = 0; j < numChannel; j++)
                    {
                        corrArray[freq][i, j] += freqData[i][freq] * freqData[j][freq].Conjugate() / numStep;
                    }
                }

            }

        }
        int numSource = 1;
        int numDirection = stearingVecArray.Length;//-pi ～　pi  
        float[] musicPowerList = new float[numDirection];//各方向を表すインデックス番目にMUSIC Powerが計算される

        for (int dir = 0; dir < numDirection; dir++)
        {
            float musicPower = 0;
            for (int freq = 0; freq < windowSize / 2 + 1; freq++)
            {
                var evd = corrArray[freq].Evd(MathNet.Numerics.LinearAlgebra.Symmetricity.Symmetric);
                var A = evd.EigenValues;
                StearingVector aVec = stearingVecArray[dir][freq];
                double denom = 0;

                for (int src = 0; src < numChannel - numSource; src++)
                {
                    StearingVector e = evd.EigenVectors.Column(src);
                    denom += aVec.Conjugate().DotProduct(e).Norm();
                }
                musicPower += (float)(aVec.L2Norm() / denom);
            }
            //Debug.Log("Direction:" + (dir*20-180).ToString() + ", Power:" + musicPower.ToString());
            musicPowerList[dir] = musicPower;
        }
        int maxDir = Array.IndexOf(musicPowerList, musicPowerList.Max());
        Debug.Log("Max Direction:" + (maxDir * 20 - 180).ToString());
    }


    void WriteCsv()
    {
        StreamWriter file = new StreamWriter(@"C:\Users\shunsuke\UnityProjects\My Project\Data\Audio_data\output_data.csv", false, Encoding.GetEncoding("Shift_JIS"));
        for (int i = 0; i < RecordData.Count; i++)
        {
            string line = RecordData[i].ToString();
            line += ",";
            file.WriteLine(line);
        }
        file.Close();
        //UnityEditor.EditorApplication.isPlaying = false;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {

        if (recOutput)
        {

            //Audiodataは-1～1のfloat型だから、Int16に変換するときに、2^15-1を、かけて整数にする。

            for (int i = 0; i < data.Length; i++)
            {
                RecordData.Add(data[i]);
            }

            if (!recOutput)
            {
                return;
            }
        }
    }

}
