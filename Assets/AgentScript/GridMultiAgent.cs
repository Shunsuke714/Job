using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using toio;
using System.Linq;
using System.Text;
using System.IO;
using toio.Simulator;

public class GridMultiAgent : Agent
{
    //設定
    public ConnectType connectType;
    CubeManager cm;
    GameObject cube1;
    GameObject cube2;
    Stage stage;
    Transform targetPole;
    Cube c;
    float[,] grid = new float[9, 9];
    static float[] ex = new float[81];
    static float[,] pos = new float[9,9];

    float countTime;
    int episodenum = 0;
    float sum; // グリッドの合計値
    float angle1; //Toioとターゲットポールの角度
    float angle2; //Toioとターゲットポールの角度
    static float startposz = 0.28f;//マスの上端の値
    static float startposx = -0.28f;//マスの左端の値
    static float width = 0.56f / 9f;//マスの一辺の長さ
    int numperepisode;

    public bool fix;
    [SerializeField]
    float timeLimit = 15f;//制限時間
    public bool csv;
    public bool episodecsv;
    public bool printentropy;
    public bool particlecsv;
    public float beta = 0.1f;
    public float init = 1f;
    public float plus = 1f;
    StreamWriter rewardfile;
    StreamWriter entropyfile;
 
    //パーティクルフィルタ
    public class ParticleFilter
    {
        public float[,] particles = new float[30,2];
        public ParticleFilter(float x,float z) 
        {
            for(int i = 0; i < particles.GetLength(0); i++)
            {
                float[] R = BoxMuller();
                particles[i, 0] = x+R[0];
                particles[i, 1] = z+R[1];
            }
        }
        //正規分布に従い、移動
        public void Step()
        {
            for(int i = 0; i < particles.GetLength(0); i++)
            {
                float[] Z = BoxMuller();
                for(int j = 0; j < particles.GetLength(1);j++)
                {
                    particles[i, j] += Z[j];
                }                
            }
        }
        //尤度を計算
        private float [] Likelihood()
        {
            float [] likelihood = new float[81];
            float likelihoodsum = 0;
            for(int i = 0; i < particles.GetLength(0); i++)
            {
                int numx =(int)((particles[i, 0] - startposx) / width);
                int numz = (int)((startposz - particles[i,1]) / width);
                numx = Mathf.Min(8, numx);
                numx = Mathf.Max(0, numx);
                numz = Mathf.Min(8, numz);
                numz = Mathf.Max(0, numz);
                likelihood[numz*9+numx] += ex[numz*9+numx];
                likelihoodsum += ex[numz * 9 + numx];
            }
            for(int i = 0; i < likelihood.Length; i++)
            {
               likelihood[i] /= likelihoodsum;
               
            }
            return likelihood;
        }

        //尤度から再びサンプリングする。
        public void Resampling(bool last=false)
        {
            float[] likelihood = Likelihood();
            float[] cumsum = new float[81];
            cumsum[0] = likelihood[0];
            for(int i = 1; i < likelihood.Length; i++)
            {
                cumsum[i] = cumsum[i-1] + likelihood[i];
            }
            for(int i = 0; i < particles.GetLength(0); i++)
            {
                float rnd = Random.value;
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
                    particles[i, 0] = Random.Range(startposx+x * width, startposx+(x+1)*width);
                    particles[i, 1] = Random.Range(startposz - z * width, startposz - (z + 1) * width);      
                }
                else
                {
                    particles[i, 0] = startposx + x * width+width/2;
                    particles[i, 1] = startposz - z * width-width/2;
                }
            }
        }
        //粒子の分散を求めて、報酬を与える。
        public float Reward()
        {
            float reward = 0;
            float[] squaresum = new float[2];
            float[] sum = new float[2];
            for(int i = 0; i < particles.GetLength(0); i++)
            {
                for(int j = 0; j < particles.GetLength(1); j++)
                {
                    squaresum[j] += particles[i, j] * particles[i, j];
                    sum[j] += particles[i, j];
                }
            }
            for(int i = 0; i < 2; i++)
            {
                float avg = sum[i] / particles.GetLength(0);
                reward += squaresum[i] / particles.GetLength(0) - avg*avg;
            } 
            return reward;
        }
        //正規分布に従う乱数を生成。
        private float[] BoxMuller()
        {
            float sigma = width/2;
            float x = Random.value;
            float y = Random.value;
            float z1 = sigma*Mathf.Sqrt(-2.0f * Mathf.Log(x)) * Mathf.Cos(2.0f * Mathf.PI * y);
            float z2 = sigma*Mathf.Sqrt(-2.0f * Mathf.Log(x)) * Mathf.Sin(2.0f * Mathf.PI * y);
            float[] Z = new float[2];
            Z[0] = z1;
            Z[1] = z2;
            return Z;
        }

        public void Pos()
        {
            for(int i = 0; i < particles.GetLength(0); i++)
            {
                int numx = (int)((particles[i, 0] - startposx) / width);
                int numz = (int)((startposz - particles[i, 1]) / width);
                pos[numz, numx]++;
                Debug.Log((numz, numx));
            }
        }


    }
    ParticleFilter particlefilter;

    async public override void Initialize()
    {
        this.stage = GameObject.FindObjectOfType<Stage>();
        this.cube1 = GameObject.Find("Cube1");
        this.cube2 = GameObject.Find("Cube2");
        this.targetPole = this.stage.transform.Find("TargetPole");
        this.cm = new CubeManager(connectType);
        await this.cm.MultiConnect(2);
        if (episodecsv)
        {
            rewardfile = new StreamWriter(@"C:\Users\shunsuke\UnityProjects\Toio\Data\Reward_data\reward.csv", false, Encoding.GetEncoding("Shift_JIS"));
            var col = "";
            col += "episode";
            col += ",";
            col += "reward";
            rewardfile.WriteLine(col);
        }

    }

    public override void OnEpisodeBegin()
    {
        //キューブの位置のリセット
        if (this.connectType != ConnectType.Real)
        {
            //float posx1 = Random.Range(-0.5f, 0);
            //float posx2 = Random.Range(0, 0.5f);
            while (true)
            {
                float posx1 = Random.Range(-0.5f, 0.5f);
                float posx2 = Random.Range(-0.5f, 0.5f);
                if (Mathf.Abs(posx1 - posx2) > 0.01f)
                {
                    /*if (posx1 > posx2)
                    {
                        float tmp;
                        tmp = posx2;
                        posx2 = posx1;
                        posx1 = tmp;
                    }*/
                    this.cube1.transform.position = new Vector3(posx1, 0, -0.5f);
                    this.cube2.transform.position = new Vector3(posx2, 0, -0.5f);
                    break;
                }
            }
            //float posx1 = -0.28f;
            //float posx2 = 0.28f;
            //float posx1 = 0.5f;
            //float posx2 = 0.55f;
            //this.cube1.transform.position = new Vector3(posx1, 0, -0.5f);
            //this.cube2.transform.position = new Vector3(posx2, 0, -0.5f);
            this.cube1.transform.rotation = Quaternion.Euler(0, 90f, 0);
            this.cube2.transform.rotation = Quaternion.Euler(0, -90f, 0);
        }

        //ターゲットポールの配置
        float x = Random.Range(-0.28f, 0.28f);
        float z = Random.Range(-0.28f, 0.28f);
        if (fix)
        {
            x = -0.1f;
            z = 0.1f;
        }
        this.targetPole.position = new Vector3(x, 0.001f, z);
        this.targetPole.gameObject.SetActive(true);

        particlefilter = new ParticleFilter(x, z);
        
        //グリッドの初期化
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                grid[i, j] = init;
            }
        }
        //時間計測開始
        countTime = 0;
        sum = 0;


        if (episodecsv)
        {
            episodenum++;
        }

        if (printentropy)
        {
            numperepisode = 0;
            entropyfile = new StreamWriter(@"C:\Users\shunsuke\UnityProjects\Toio\Data\Entropy_data\entropy.csv", false, Encoding.GetEncoding("Shift_JIS"));
            var col = "";
            col += "step";
            col += ",";
            col += "reward";
            entropyfile.WriteLine(col);

        }
    }

    
    public override void CollectObservations(VectorSensor sensor)
    {
        //角度を取得
        Vector3 diff1 = targetPole.position - this.cube1.transform.position;
        angle1 = Mathf.Atan2(diff1.z, diff1.x);
        Vector3 diff2 = targetPole.position - this.cube2.transform.position;
        angle2 = Mathf.Atan2(diff2.z, diff2.x);

        //乱数 [-5度, 5度]の一様分布から取得。
        float rand1 = Random.Range(-Mathf.PI / 36, Mathf.PI / 36);
        float rand2 = Random.Range(-Mathf.PI / 36, Mathf.PI / 36);

        //直線のパラメータを取得
        float posx1 = this.cube1.transform.position.x;
        float posz1 = this.cube1.transform.position.z;
        float a1 = Mathf.Tan(angle1 + rand1);
        float posx2 = this.cube2.transform.position.x;
        float posz2 = this.cube2.transform.position.z;
        float a2 = Mathf.Tan(angle2 + rand2);


        //入力
        sensor.AddObservation((angle1 + rand1) / (Mathf.PI));
        sensor.AddObservation(posx1 / 1.44f);
        sensor.AddObservation((angle2 + rand2) / (Mathf.PI));
        sensor.AddObservation(posx2 / 1.44f);

        //Debug.Log((angle1+rand1)/Mathf.PI);
        //Debug.Log((angle2+rand2)/Mathf.PI);


        //グリッドを埋めていく
        //グリッドに直接足すとき→calcgrid
        calcgrid(posx1, posz1, a1);
        calcgrid(posx2, posz2, a2);

        //両方の直線の交点のみに足すとき→calcgridcross
        //calcgridcross(posx1, posz1, a1, posx2, posz2, a2);

        float r = 0;
        //グリッドの値を計算
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                ex[9 * i + j] = Mathf.Exp(beta*grid[i, j]) / sum;
                r += -ex[9 * i + j] * Mathf.Log(ex[9 * i + j], 2);
            }   
        }
        //パーティクルのリサンプリング
        particlefilter.Step();
        particlefilter.Resampling();
        
        r = Mathf.Log(81, 2) - r;

        if (printentropy)
        {
            numperepisode++;
            Debug.Log(r);
            RewardOneEpisode(r, numperepisode);
        }

        //sensor.AddObservation(ex);
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

    void FixedUpdate()
    {
        countTime += Time.deltaTime;
        if (countTime > timeLimit)
        {
            TerminateEpisode();
        }
    }

    //エピソード終了時
    void TerminateEpisode()
    {
        particlefilter.Resampling(true);
        if (particlecsv)
        {
            particlefilter.Pos();
            WriteParticlecsv();
        }
        if (csv)
        {
            WriteCsv();
        }
        float reward = particlefilter.Reward();
        //Debug.Log(reward);
        AddReward(-reward);
        EndEpisode();
    }

    //直線の式（x->z）
    public float Linearx(float posx, float posz, float a, float target)
    {
        return a * (target - posx) + posz;
    }

    //直線の式（z->x）
    public float Linearz(float posx, float posz, float a, float target)
    {
        return posx + (target - posz) / a;
    }

    //直線とグリッドが重なったときに、+する関数
    void calcgrid(float posx, float posz, float a)
    {
        sum = 0;
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            float up = startposz - width * i;//マスの上端の座標
            float low = startposz - width * (i + 1);//マスの下端の座標
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                float left = startposx + width * j;//マスの左端の座標
                float right = startposx + width * (j + 1);//マスの右端の座標
                if ((Linearx(posx, posz, a, left) >= low && Linearx(posx, posz, a, left) <= up)
                    || (Linearx(posx, posz, a, right) >= low && Linearx(posx, posz, a, right) <= up)
                    || (Linearz(posx, posz, a, low) >= left && Linearz(posx, posz, a, low) <= right)
                    || (Linearz(posx, posz, a, up) >= left) && Linearz(posx, posz, a, low) <= right)
                {
                    grid[i, j] += plus;

                    //Debug.Log((i, j));
                }
                
                sum += Mathf.Exp(beta * grid[i, j]);
            }
        }
        
    }


    //二つの直線の交点のみプラスする関数
    void calcgridcross(float posx1, float posz1, float a1, float posx2, float posz2, float a2)
    {
        sum = 0;
        for(int  i= 0; i  < grid.GetLength(0); i++)
        {
            float up = startposz - width * i;//マスの上端の座標
            float low = startposz - width * (i + 1);//マスの下端の座標
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                int cross_judge = 0;
                float left = startposx + width * j;//マスの左端の座標
                float right = startposx + width * (j + 1);//マスの右端の座標
                if ((Linearx(posx1, posz1, a1, left) >= low && Linearx(posx1, posz1, a1, left) <= up)
                    || (Linearx(posx1, posz1, a1, right) >= low && Linearx(posx1, posz1, a1, right) <= up)
                    || (Linearz(posx1, posz1, a1, low) >= left && Linearz(posx1, posz1, a1, low) <= right)
                    || (Linearz(posx1, posz1, a1, up) >= left) && Linearz(posx1, posz1, a1, low) <= right)
                {
                    cross_judge += 1;

                    //Debug.Log("1 " + (i, j));
                }
                if ((Linearx(posx2, posz2, a2, left) >= low && Linearx(posx2, posz2, a2, left) <= up)
                    || (Linearx(posx2, posz2, a2, right) >= low && Linearx(posx2, posz2, a2, right) <= up)
                    || (Linearz(posx2, posz2, a2, low) >= left && Linearz(posx2, posz2, a2, low) <= right)
                    || (Linearz(posx2, posz2, a2, up) >= left) && Linearz(posx2, posz2, a2, low) <= right)
                {
                    cross_judge += 1;
                    //Debug.Log("2 " + (i, j));
                }
                if (cross_judge == 2)
                {
                    //Debug.Log((i, j));
                    grid[i, j] += plus;
                }
                sum += Mathf.Exp(beta * grid[i, j]);
            }
        }
    }

    

    public void Finished()
    {
        sum = 0;
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                sum += Mathf.Exp(beta*grid[i, j]);
            }
        }

        float reward = 0;
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                float p = Mathf.Exp(beta*grid[i, j]) / sum;
                reward += -p * Mathf.Log(p, 2);
            }
        }

        reward = (Mathf.Log(81, 2)) - reward;
        Debug.Log("Reward" + reward);
        AddReward(reward);

        if (csv)
        {
            WriteCsv();
        }

        if (episodecsv)
        {
            EpisodeCsv(episodenum, reward);
        }

        EndEpisode();
    }

    


    

    void EpisodeCsv(int episode, float reward)
    {
        var line = "";
        line += episodenum.ToString("0");
        line += ",";
        line += reward.ToString("0.000000");
        rewardfile.WriteLine(line);
    }

    void RewardOneEpisode(float r, int numperepisode)
    {
        var line = "";
        line += numperepisode.ToString();
        line += ",";
        line += r;
        entropyfile.WriteLine(line);
        

    }
    void OnDestroy()
    {
        if (episodecsv)
        {
            rewardfile.Close();
        }
        if (printentropy)
        {
            entropyfile.Close();
        }
    }
    void WriteParticlecsv()
    {
        StreamWriter file = new StreamWriter(@"C:\Users\shunsuke\UnityProjects\Toio\Data\Particle_data\particle.csv", false, Encoding.GetEncoding("Shift_JIS"));
        var col = "";
        for (int i = 0; i < pos.GetLength(0); i++)
        {
            col += i.ToString();
            if (i != pos.GetLength(0) - 1)
            {
                col += ",";
            }
        }
        file.WriteLine(col);
        for (int i = 0; i < pos.GetLength(0); i++)
        {
            var line = "";
            for (int j = 0; j < pos.GetLength(1); j++)
            {
                line += (pos[i, j]).ToString();
                if (j != 8)
                {
                    line += ",";
                }
            }
            file.WriteLine(line);
        }
        file.Close();
    }
    void WriteCsv()
    {
        // expで計算した値
        StreamWriter file = new StreamWriter(@"C:\Users\shunsuke\UnityProjects\Toio\Data\Grid_data\multigrid.csv", false, Encoding.GetEncoding("Shift_JIS"));
        var col = "";
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            col += i.ToString();
            if (i != grid.GetLength(0) - 1)
            {
                col += ",";
            }
        }
        file.WriteLine(col);
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            var line = "";
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                line += ((Mathf.Exp(beta*grid[i, j])) / sum).ToString("0.000000");
                if (j != 8)
                {
                    line += ",";
                }
            }
            file.WriteLine(line);
        }
        file.Close();

        // グリッドの値そのまま
        StreamWriter fileorigin = new StreamWriter(@"C:\Users\shunsuke\UnityProjects\Toio\Data\Grid_data\multigridorigin.csv", false, Encoding.GetEncoding("Shift_JIS"));
        fileorigin.WriteLine(col);
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            var line = "";
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                line += grid[i, j].ToString("0.0");
                if (j != 8)
                {
                    line += ",";
                }
            }
            fileorigin.WriteLine(line);
        }
        fileorigin.Close();
    }

    

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        /*for(int i = 4; i < 85; i++)
        {
            Debug.Log(GetObservations()[i]);
        }*/
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
