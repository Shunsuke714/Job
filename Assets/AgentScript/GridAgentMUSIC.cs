using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using toio;
using System;
using System.Linq;
using System.IO;
using System.Text;
using toio.Simulator;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra.Complex32;

using StearingVector = MathNet.Numerics.LinearAlgebra.Vector<MathNet.Numerics.Complex32>;


public class GridAgentMUSIC : Agent
{
    public ConnectType connectType;
    CubeManager cm;
    GameObject cube1;
    GameObject cube2;
    Stage stage;
    Transform targetPole;
    float[,] grid = new float[9, 9];
    static float[] ex = new float[81];
    float sum; // グリッドの合計値
    static float startposz = 0.28f;//マスの上端の値
    static float startposx = -0.28f;//マスの左端の値
    static float width = 0.56f / 9f;//マスの一辺の長さ

    [SerializeField]
    public float beta = 0.1f;
    public float init = 1f;
    public float plus = 1f;
    int count = 0;
    int numStep = 0;

    [SerializeField] AudioSource _source;
    [SerializeField] GameObject _sourceobj;
    [SerializeField] float radius;


    double startDspTime;
    double buffer = 5 / 60d;

    bool recOutput;
    static public List<float> RecordData;
    const double c = 340;
    float len;

    //パーティクルフィルタ
    public class ParticleFilter
    {
        public float[,] particles = new float[30, 2];

        public ParticleFilter(float x, float z)
        {
            for(int i = 0; i < particles.GetLength(0); i++)
            {
                float[] R = BoxMuller();
                particles[i, 0] = x + R[0];
                particles[i, 1] = z + R[1];
            }
        }

        //正規分布に従い、パーティクルを移動させる
        public void Step()
        {
            for (int i = 0; i < particles.GetLength(0); i++)
            {
                float [] Z = BoxMuller();
                for (int j = 0; j < particles.GetLength(1); j++)
                {
                    particles[i, j] += Z[j];
                }
            }
        }

        //尤度を計算
        private float [] Likelihood()
        {
            float[] likelihood = new float[81];
            float likelihoodsum = 0;
            for (int i = 0; i < particles.GetLength(0); i++)
            {
                //パーティクルがグリッドの何番目にいるか求める
                int numx = (int)((particles[i, 0] - startposx) / width);
                int numz = (int)((startposz - particles[i, 1]) / width);
                numx = Mathf.Min(8, numx);
                numx = Mathf.Max(0, numx);
                numz = Mathf.Min(8, numz);
                numz = Mathf.Max(0, numz);
                likelihood[numz * 9 + numx] += ex[numz * 9 + numx];
                likelihoodsum += ex[numz * 9 + numx];
            }
            for(int i = 0; i < likelihood.Length; i++)
            {
                likelihood[i] /= likelihoodsum;
            }
            return likelihood;
        }

        //尤度から再びサンプリングする
        public void Resampling(bool last = false)
        {
            float[] likelihood = Likelihood();
            float[] cumsum = new float[81];
            cumsum[0] = likelihood[0];
            for(int i = 1; i < likelihood.Length; i++)
            {
                cumsum[i] = cumsum[i - 1] + likelihood[i];
            }
            for(int i = 0; i < particles.GetLength(0); i++)
            {
                float rnd = UnityEngine.Random.value;
                int r = 81;
                int l = 0;
                while (r - l > 1)
                {
                    int mid = (r + l) / 2;
                    if (rnd < cumsum[mid])
                    {
                        r = mid;
                    }
                    else
                    {
                        l = mid;
                    }
                }
                int z = r / 9;
                int x = r % 9;
                if (!last)
                {
                    particles[i, 0] = UnityEngine.Random.Range(startposx + x * width, startposx + (x + 1) * width);
                    particles[i, 1] = UnityEngine.Random.Range(startposz - z * width, startposz - (z + 1) * width);
                }
                else
                {
                    particles[i, 0] = startposz + x * width + width / 2;
                    particles[i, 1] = startposz + x * width + width / 2;
                }
            }
        }

        //パーティクルの分散を求めて、報酬を与える
        public float Reward()
        {
            float reward = 0;
            float[] squaresum = new float[2];
            float[] sum = new float[2];
            for(int i = 0; i < particles.GetLength(0); i++)
            {
                for (int j = 0; j < particles.GetLength(1); j++)
                {
                    squaresum[j] += particles[i, j] * particles[i, j];
                    sum[j] += particles[i, j];
                }
            }
            for(int i = 0; i < 2; i++)
            {
                float avg = sum[i] / particles.GetLength(0);
                reward += squaresum[i] / particles.GetLength(0) - avg * avg;
            }
            return reward;
        }
        //正規分布に従う乱数を生成。
        private float [] BoxMuller()
        {
            float sigma = width / 2;
            float x = UnityEngine.Random.value;
            float y = UnityEngine.Random.value;
            float z1 = sigma * Mathf.Sqrt(-2.0f * Mathf.Log(x)) * Mathf.Cos(2.0f * Mathf.PI * y);
            float z2 = sigma * Mathf.Sqrt(-2.0f * Mathf.Log(x)) * Mathf.Sin(2.0f * Mathf.PI * y);
            float[] Z = new float[2];
            Z[0] = z1;
            Z[1] = z2;
            return Z;
        }
        //パーティクルが何番目にいるか答える（デバッグ用）
        public void Pos()
        {
            for (int i = 0; i < particles.GetLength(0); i++)
            {
                int numx = (int)((particles[i, 0] - startposx) / width);
                int numz = (int)((startposz - particles[i, 1]) / width);
                Debug.Log((numz, numx));
            }
        }
    }
    //マイクロフォンアレイのクラス
    public class MicArray
    {
        public StearingVector[][] stearingVector;
        public int angle1;
        public int angle2;
        int advancePosition = 256;
        int windowSize = 512;
        int numStep = 10;
        int outputRate;
        int numSource = 1;
        int numChannel = 4;

        public MicArray()
        {
            stearingVector = LoadSrearingVec(257);
            outputRate = AudioSettings.outputSampleRate;
            angle1 = -1;
            angle2 = -1;
        }

        private StearingVector[][] LoadSrearingVec(int numFFTPoint)
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
        float[][] LoadWaveData(List<float> RecordData)
        {
            float[][] wavData;
            int size = advancePosition * (numStep - 1) + windowSize;
            wavData = new float[numChannel][];
            for (int ch = 0; ch < numChannel; ch++)
            {
                wavData[ch] = new float[size];
                for (int j = 0; j < size; j++)
                {
                    wavData[ch][j] = RecordData[23000 + outputRate * (2 + ch) + 3 * j];
                }
            }
            return wavData;
        }
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
        public int MUSIC()
        {
            float[][] wavData = LoadWaveData(RecordData);
            int numChannel = wavData.Length;
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
            int numDirection = stearingVector.Length;//-pi ～　pi  
            float[] musicPowerList = new float[numDirection];//各方向を表すインデックス番目にMUSIC Powerが計算される

            for (int dir = 0; dir < numDirection; dir++)
            {
                float musicPower = 0;
                for (int freq = 0; freq < windowSize / 2 + 1; freq++)
                {
                    var evd = corrArray[freq].Evd(MathNet.Numerics.LinearAlgebra.Symmetricity.Symmetric);
                    var A = evd.EigenValues;
                    StearingVector aVec = stearingVector[dir][freq];
                    double denom = 0;

                    for (int src = 0; src < numChannel - numSource; src++)
                    {
                        StearingVector e = evd.EigenVectors.Column(src);
                        denom += aVec.Conjugate().DotProduct(e).Norm();
                    }
                    musicPower += (float)(aVec.L2Norm() / denom);
                }
                musicPowerList[dir] = musicPower;
            }
            int maxidx = Array.IndexOf(musicPowerList, musicPowerList.Max());
            int maxDir = 135 - (maxidx * 20 - 180);
            return maxDir;
        }
    }

    ParticleFilter particleFilter;
    MicArray micArray;

    async public override void Initialize()
    {
        this.stage = GameObject.FindObjectOfType<Stage>();
        this.cube1 = GameObject.Find("Cube1");
        this.cube2 = GameObject.Find("Cube2");
        this.targetPole = this.stage.transform.Find("TargetPole");
        this.cm = new CubeManager(connectType);
        micArray = new MicArray();
        RecordData = new List<float>();
        await this.cm.MultiConnect(2);
        len = radius * Mathf.Sqrt(2);
    }

    public override void OnEpisodeBegin()
    {
        //キューブの位置のリセット
        if(this.connectType != ConnectType.Real)
        {
            while (true)
            {
                float posx1 = UnityEngine.Random.Range(-0.5f, 0.5f);
                float posx2 = UnityEngine.Random.Range(-0.5f, 0.5f);
                if((Mathf.Abs(posx1-posx2) > 0.01f)){
                    if (posx1 > posx2)
                    {
                        float tmp;
                        tmp = posx2;
                        posx2 = posx1;
                        posx1 = tmp;
                    }
                    this.cube1.transform.position = new Vector3(posx1, 0, -0.5f);
                    this.cube2.transform.position = new Vector3(posx2, 0, -0.5f);
                    break;
                }
            }
            this.cube1.transform.rotation = Quaternion.Euler(0, 90f, 0);
            this.cube2.transform.rotation = Quaternion.Euler(0, -90f, 0);
        }

        //ターゲットポールの配置
        float x = UnityEngine.Random.Range(-0.28f, 0.28f);
        float z = UnityEngine.Random.Range(-0.28f, 0.28f);
        this.targetPole.position = new Vector3(x, 0.001f, z);
        this.targetPole.gameObject.SetActive(true);

        particleFilter = new ParticleFilter(x, z);
        numStep = 0;
        //グリッドの初期化
        for(int i=0;i<grid.GetLength(0);i++)
        {
            for(int j = 0; j < grid.GetLength(1); j++)
            {
                grid[i, j] = init;
            }
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {


        //直線のパラメータを取得
        float posx1 = this.cube1.transform.position.x;
        float posz1 = this.cube1.transform.position.z;
        float a1 = Mathf.Tan(micArray.angle1);
        float posx2 = this.cube2.transform.position.x;
        float posz2 = this.cube2.transform.position.z;
        float a2 = Mathf.Tan(micArray.angle2);


        //入力
        sensor.AddObservation((micArray.angle1) / (Mathf.PI));
        sensor.AddObservation(posx1 / 1.44f);
        sensor.AddObservation((micArray.angle2) / (Mathf.PI));
        sensor.AddObservation(posx2 / 1.44f);

        Debug.Log((micArray.angle1, micArray.angle2));
        micArray.angle1 = -1;
        micArray.angle2 = -1;
        //グリッドを埋めていく
        calcgrid(posx1, posz1, a1);
        calcgrid(posx2, posz2, a2);

        //グリッドの値を計算
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                ex[9 * i + j] = Mathf.Exp(beta * grid[i, j]) / sum;
            }
        }
        //パーティクルのリサンプリング
        particleFilter.Step();
        particleFilter.Resampling();
    }


    //行動
    public override void OnActionReceived(ActionBuffers actions)
    {
        int action1 = actions.DiscreteActions[0];
        int action2 = actions.DiscreteActions[1];
        int[] action_list = new int[2];
        action_list[0] = action1;
        action_list[1] = action2;
        for (int i = 0; i < cm.syncHandles.Count(); i++)
        {
            if (action_list[i] == 1) cm.syncHandles[i].Move(40, 0, 100, false);
            if (action_list[i] == 2) cm.syncHandles[i].Move(-40, 0, 100, false);
        }

    }
    private void FixedUpdate()
    {
        double NextRingTime()
        {
            var interval = 1d;
            var elapsedDspTime = AudioSettings.dspTime - startDspTime;
            var num = System.Math.Floor(elapsedDspTime / interval);

            float dis = Vector3.Distance(_sourceobj.transform.position, this.transform.position);
            double delay = dis / c;

            return startDspTime + (num + 1d) * interval + delay;
        }
        if(micArray.angle1==-1 && micArray.angle2 == -1 && recOutput == false)
        {
            startDspTime = AudioSettings.dspTime;
            recOutput = true;
        }
        if (micArray.angle1 == -1 && micArray.angle2 == -1 && recOutput == true)
        {
            var elapsedDspTime = AudioSettings.dspTime - startDspTime;
            var nxtRng = NextRingTime();
            if (nxtRng < AudioSettings.dspTime + buffer)
            {
                _source.PlayScheduled(nxtRng);
            }
            MoveListener(this.cube1.transform.position, elapsedDspTime, count);
            if (elapsedDspTime > 6.5d)
            {
                micArray.angle1 = micArray.MUSIC();
                count = 0;
                recOutput = false;
                RecordData.Clear();
            }
        }
        if(micArray.angle1!=-1 && micArray.angle2==-1 && recOutput == false)
        {
            startDspTime = AudioSettings.dspTime;
            recOutput = true;
        }
    
        if (micArray.angle1 != -1 && micArray.angle2 == -1 && recOutput == true)
        {
            var elapsedDspTime = AudioSettings.dspTime - startDspTime;
            var nxtRng = NextRingTime();
            if (nxtRng<AudioSettings.dspTime + buffer)
            {
                _source.PlayScheduled(nxtRng);
            }
            MoveListener(this.cube2.transform.position, elapsedDspTime, count);
            if (elapsedDspTime > 6.5d)
            {
                micArray.angle2 = micArray.MUSIC();
                count = 0;
                recOutput = false;
                RecordData.Clear();
            }
        }
        if(micArray.angle1!=-1 && micArray.angle2 != -1)
        {
            RequestDecision();
            
            numStep++;
        }
        
        if (numStep == 200)
        {
            TerminateEpisode();
        }

    }

    //AudioListenerを動かす
    public void MoveListener(Vector3 cubePosition, double elapsedDspTime, int count)
    {
        if (count == 0 && elapsedDspTime > 1.8d)
        {
            this.transform.position = new Vector3(cubePosition.x- len/2, cubePosition.y, cubePosition.z+len/2);
            count++;
        }
        if (count == 1 && elapsedDspTime > 2.8d)
        {
            this.transform.position = new Vector3(cubePosition.x+len/2, cubePosition.y, cubePosition.z+len/2);
            count++;
        }
        if (count == 2 && elapsedDspTime > 3.8d)
        {
            this.transform.position = new Vector3(cubePosition.x+len/2, cubePosition.y, cubePosition.z-len/2);
            count++;
        }
        if (count == 3 && elapsedDspTime > 4.8d)
        {
            this.transform.position = new Vector3(cubePosition.x-len/2, cubePosition.y, cubePosition.z-len/2);
            count++;
        }
        
    }

    void TerminateEpisode()
    {
        particleFilter.Resampling(true);
        float reward = particleFilter.Reward();
        AddReward(-reward);
        EndEpisode();
    }
    //直線の式(x->z)
    public float Linearx(float posx, float posz, float a, float target)
    {
        return a * (target - posz) + posz;
    }

    //直線の式(z->x)
    public float Linearz(float posx, float posz, float a, float target)
    {
        return posx + (target - posz) / a;
    }

    //グリッドの値を計算する
    void calcgrid(float posx, float posz, float a)
    {
        sum = 0;
        for(int i = 0; i < grid.GetLength(0); i++)
        {
            float up = startposz - width * i;
            float low = startposz - width * (i + 1);
            for(int j = 0; j < grid.GetLength(1); j++)
            {
                float left = startposx + width * j;
                float right = startposx + width * (j + 1);
                if((Linearx(posx, posz, a, left) >= low && Linearx(posx, posz, a, left)<=up)
                    || (Linearx(posx, posz, a, right) >= low && Linearx(posx, posz, a, right) <= up)
                    || (Linearz(posx, posz, a, low) >= left && Linearz(posx, posz, a, low) <= right)
                    || (Linearz(posx, posz, a, up) >= left) && Linearz(posx, posz, a, low) <= right)
                {
                    grid[i, j] += plus;
                }
                sum += Mathf.Exp(beta * grid[i, j]);

            }
        }
    }

    //音のデータをとる
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
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        float v = Input.GetAxis("Vertical");
        float h = Input.GetAxis("Horizontal");
        var discreseActionsOut = actionsOut.DiscreteActions;
        discreseActionsOut[0] = 0;
        discreseActionsOut[1] = 0;
        if (v > 0.5f) discreseActionsOut[0] = 1;
        if (v < -0.5f) discreseActionsOut[0] = 2;
        if (h > 0.5f) discreseActionsOut[1] = 1;
        if (h < -0.5f) discreseActionsOut[1] = 2;
    }

}
