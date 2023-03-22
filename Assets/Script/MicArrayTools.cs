using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra.Complex32;

//ステアリングベクトルを表す型：複素数ベクトルの型を定義
using StearingVector = MathNet.Numerics.LinearAlgebra.Vector<MathNet.Numerics.Complex32>;


public class MicArrayTools : MonoBehaviour
{
    // wavリストのstartPositionからwindowSize分取り出してフーリエ変換を実行
    public Complex32[] computeFFT(float[] wav, int startPosition, int windowSize)
    {
        Complex32[] complexData = new Complex32[windowSize];
        //複素数型に変換
        //Debug.Log(startPosition);
        for (int i = 0; i < windowSize; i++)
        {
            complexData[i] = new Complex32(wav[startPosition+i], 0f);   //
        }

        //run FFT
        Fourier.Forward(complexData, FourierOptions.Matlab); // arbitrary length
        return complexData;
    }

    // 音声波形をcsvにしたものを利用
    // 1行に４チャネル分の値が入っている
    float[][] LoadWaveData()
    {
        TextAsset csvFile; // CSVファイル
        List<string[]> csvData = new List<string[]>(); // CSVの中身を入れるリスト;
        float[][] wavData;
        csvFile = Resources.Load("wave") as TextAsset; // Resouces下のCSV読み込み

        StringReader reader = new StringReader(csvFile.text);

        // csvの読み込み：コンマ で分割しつつ一行ずつ読み込みリストに追加
        while (reader.Peek() != -1) // 最終行まで
        {
            string line = reader.ReadLine();
            csvData.Add(line.Split(','));
        }
        int numChannel = csvData[0].Length;
        int fullLength = csvData.Count;
        wavData = new float[numChannel][];
        for (int ch = 0; ch < numChannel; ch++)
        {
            wavData[ch] = new float[fullLength];
            for (int i = 0; i < fullLength; i++)
            {
                wavData[ch][i] = float.Parse(csvData[i][ch]) / 65536;//16ビットの音声から-1～1のfloatに変換
            }

        }
        
        return wavData;
    }

    //ステアリングベクトルのcsvを読み込む
    //　stearing_vec_18direction_4ch.csvの場合
    //　　一行の数値の数はマイク数 x 2(複素数の実部・虚部) 18 x 4 = 116
    //　　行数＝方向数18 (20度刻み)x 257（フーリエ変換の窓幅/2+1 x 複素数の実部・虚部）=　4626行
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
        for(int i = 0; i< numDirection; i++)
        {
            stearingVecArray[i]= new StearingVector[numFFTPoint];
        }
        
        //代入
        for (int i = 0; i < csvData.Count; i++)
        {
            int dir = i / numFFTPoint; //方向インデックス
            int freq = i % numFFTPoint;  //周波数インデックス
            int L = csvData[i].Length; //=チャンネル数*2
            StearingVector stearingVec = Vector.Build.Dense(L / 2);
            for (int j = 0; j < L/2; j++)
            {
                float real = float.Parse(csvData[i][j * 2]);
                float imag = float.Parse(csvData[i][j * 2+1]);
                stearingVec[j]=new Complex32(real, imag);
            }
            stearingVecArray[dir][freq] = stearingVec;
        }
        return stearingVecArray;
    }

    void Start()
    {
        float[][] wavData = LoadWaveData();
        int numChannel = wavData.Length;
        int fullLength = wavData[0].Length;
        StearingVector[][] stearingVecArray= LoadStearingVec(257);

        /*
        //読み込んだ多チャンネル波形の表示（デバッグ用）
        Color[] pallet = new Color[] { Color.blue, Color.red, Color.green, Color.cyan, Color.magenta, Color.yellow, Color.gray, Color.white};
        for (int ch = 0;ch< numChannel;ch++) {
            //表示用のLineRendererオブジェクトを作成して各種設定
            GameObject g = new GameObject();
            LineRenderer renderer = g.AddComponent<LineRenderer>();
            renderer.startWidth = 0.05f;
            renderer.endWidth = 0.05f;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.startColor = pallet[ch];
            renderer.endColor = pallet[ch];
            // 頂点を設定：表示に時間がかかるので最大1024点
            int L = fullLength;
            if (L > 1024)
            {
                L = 1024;
            }
            // 頂点の位置を設定
            float scaleX = 1 / 50f;
            float scaleY = 100f;
            renderer.positionCount=L;
            for (int i = 0; i < L; i++)
            {
                renderer.SetPosition(i, new Vector3((float)(i - (L / 2.0)) * scaleX, wavData[ch][i] * scaleY, 0f));
            }
        }
        */
        
        

        int advancePosition = 256;
        int windowSize = 512;

        Complex32[][] freqData = new Complex32[numChannel][];
        for (int i = 0; i < numChannel; i++)
        {
            freqData[i] = computeFFT(wavData[i], 0, windowSize);
        }
        
        /*
        //フーリエ変換後スペクトルの表示（デバッグ用）
        {
            //表示用のLineRendererオブジェクトを作成して各種設定
            GameObject g = new GameObject();
            LineRenderer renderer = g.AddComponent<LineRenderer>();
            renderer.startWidth = 0.05f;
            renderer.endWidth = 0.05f;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.startColor = Color.black;
            renderer.endColor = Color.black;
            // 頂点の位置を設定
            float scaleX = 1 / 20f;
            float scaleY = 30f;
            int L = windowSize / 2 + 1;
            renderer.positionCount = L;
            for (int i = 0; i < L; i++)
            {
                float abs = freqData[0][i].Magnitude;
                renderer.SetPosition(i, new Vector3((float)(i - (L / 2.0)) * scaleX, abs * scaleY - 0.1f, 0f));
            }
        }
        */
        
        
        
        int numStep =10;
        var corrArray=new MathNet.Numerics.LinearAlgebra.Matrix<MathNet.Numerics.Complex32>[windowSize / 2 + 1];
        for (int freq = 0; freq < windowSize / 2 + 1; freq++)
        {
            var corr = Matrix.Build.Dense(numChannel, numChannel);
            corrArray[freq] = corr;
        }
        for (int step = 0; step < numStep; step++)
        {
            for (int i = 0; i < numChannel; i++)
            {
                freqData[i] = computeFFT(wavData[i], advancePosition*step, windowSize);
            }

            for (int freq = 0; freq < windowSize / 2 + 1; freq++)
            {
                for (int i = 0; i < numChannel; i++)
                {
                    for (int j = 0; j < numChannel; j++)
                    {
                        corrArray[freq][i, j] += freqData[i][freq] * freqData[j][freq].Conjugate()/ numStep;
                    }
                }

            }

        }
        int numSource = 1;
        int numDirection = stearingVecArray.Length;//-pi ～　pi  
        float[] musicPowerList = new float[numDirection];//各方向を表すインデックス番目にMUSIC Powerが計算される

        for (int dir=0; dir < numDirection; dir++)
        {
            float musicPower = 0;
            for (int freq = 0; freq < windowSize / 2 + 1; freq++)
            {
                var evd = corrArray[freq].Evd(MathNet.Numerics.LinearAlgebra.Symmetricity.Symmetric);
                var A = evd.EigenValues;
                //Debug.Log(string.Join(",", A));
                StearingVector aVec = stearingVecArray[dir][freq];
                double denom = 0;
                for (int src=0;src< numSource; src++)
                {
                    StearingVector e = evd.EigenVectors.Column(numChannel-src-1);
                    denom += aVec.Conjugate().DotProduct(e).Norm();
                }
                musicPower += (float)(aVec.L2Norm() / denom);
            }
            Debug.Log("Direction index:"+dir.ToString() + ", Power:" + musicPower.ToString());
            musicPowerList[dir] = musicPower;
        }
        
        //方向ごとのMUSICパワーの表示（デバッグ用）
        {
            //表示用のLineRendererオブジェクトを作成して各種設定
            GameObject g = new GameObject();
            LineRenderer renderer = g.AddComponent<LineRenderer>();
            renderer.startWidth = 0.05f;
            renderer.endWidth = 0.05f;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.startColor = Color.black;
            renderer.endColor = Color.black;
            // 頂点の位置を設定
            float scaleX = 0.5f;
            float scaleY = 1 / 300f;
            renderer.positionCount = numDirection;
            for (int i = 0; i < numDirection; i++)
            {
                float power = musicPowerList[i];
                renderer.SetPosition(i, new Vector3((float)(i - (numDirection / 2.0)) * scaleX, power * scaleY - 0.1f, 0f));
            }
        }
    }
    void Update()
    {
    }
}
