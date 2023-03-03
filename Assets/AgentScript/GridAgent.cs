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

public class GridAgent : Agent
{
    //設定
    public ConnectType connectType;
    CubeManager cm;
    GameObject cube;
    Stage stage;
    Transform targetPole;
    Cube c;
    float[,] grid = new float [9, 9];
    float [] ex = new float [81];
    float countTime;
    public float timeLimit = 15f;//制限時間
    float sum; // グリッドの合計値
    float angle; //Toioとターゲットポールの角度
    float startposz = 0.28f;//マスの上端の値
    float startposx = -0.28f;//マスの左端の値
    float width = 0.56f/9f;//マスの一辺の長さ
    public bool csv;


    async public override void Initialize()
    {
        this.stage = GameObject.FindObjectOfType<Stage>();
        this.cube = GameObject.Find("Cube");
        this.targetPole = this.stage.transform.Find("TargetPole");
        this.cm = new CubeManager(connectType);
        c = await this.cm.SingleConnect();


    }

    //エピソード開始時
    public override void OnEpisodeBegin()
    {
        //キューブの位置のリセット
        if (this.connectType != ConnectType.Real)
        {
            this.cube.transform.rotation = Quaternion.Euler(0, 90f, 0);
            this.cube.transform.position = new Vector3(0, 0, -0.5f);
        }

        //ターゲットポールの配置
        float x = Random.Range(-0.28f, 0.28f);
        float z = Random.Range(-0.28f, 0.28f);


        this.targetPole.position = new Vector3(x, 0.001f, z);
        this.targetPole.gameObject.SetActive(true);

        //グリッドの初期化
        for(int i = 0; i < grid.GetLength(0); i++)
        {
            for(int j = 0; j < grid.GetLength(1); j++)
            {
                grid[i, j] = 0;
            }
        }
        
        sum = 0;
        
        //時間計測開始
        countTime = 0;

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

    //状態の観測
    public override void CollectObservations(VectorSensor sensor)
    {
        //角度を取得
        Vector3 diff = targetPole.position - this.cube.transform.position;
        angle = Mathf.Atan2(diff.z, diff.x);
        
        //乱数 [-5度, 5度]の一様分布から取得。
        float rand = Random.Range(-Mathf.PI/36, Mathf.PI/36);
        
        //直線のパラメータを取得
        float posx = this.cube.transform.position.x;
        float posz = this.cube.transform.position.z;
        float a = Mathf.Tan(angle+rand);


        //入力
        sensor.AddObservation((angle+rand)/(Mathf.PI));
        sensor.AddObservation(posx/1.44f);

        //グリッドを埋めていく
        sum = 0;
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            float up = startposz - width * i;//マスの上端の座標
            float low = startposz - width * (i + 1);//マスの下端の座標
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                float left = startposx + width * j;//マスの左端の座標
                float right = startposx + width * (j + 1);//マスの右端の座標
                if ((Linearx(posx, posz, a, left)>=low && Linearx(posx, posz, a, left)<=up ) 
                    || (Linearx(posx, posz, a, right) >= low && Linearx(posx, posz, a, right) <= up)
                    || (Linearz(posx, posz, a, low) >= left && Linearz(posx, posz, a, low) <= right)
                    || (Linearz(posx, posz, a, up) >= left) && Linearz(posx, posz, a, low) <= right)
                {
                    grid[i, j]+= 0.1f;

                    //Debug.Log((i, j));
                }
                sum += Mathf.Exp(grid[i, j]);
            }
        }
        //グリッドの値を計算
        for(int i = 0; i < grid.GetLength(0); i++)
        {
            for(int j = 0; j < grid.GetLength(1); j++)
            {
                ex[9 * i + j] = Mathf.Exp(grid[i, j]) / sum;
            }
        }
        
        sensor.AddObservation(ex);
    }

    //行動
    public override void OnActionReceived(ActionBuffers actions)
    {
        int action = actions.DiscreteActions[0];
        foreach(var handle in cm.syncHandles)
        {
            if (action == 1) handle.Move(40, 0, 100, false); //前
            if (action == 2) handle.Move(-40, 0, 100, false); //後ろ
        }

    }

    

    //エピソード終了時
    public void Finished()
    {
        sum = 0;
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                sum += Mathf.Exp(grid[i, j]);
            }
        }
        
        float reward = 0;
        for(int i = 0; i < grid.GetLength(0); i++)
        {
            for(int j = 0; j < grid.GetLength(1); j++)
            {
                
                float p = Mathf.Exp(grid[i, j]) / sum;
                reward += -p * Mathf.Log(p, 2);
            }
        }
        
        reward = (Mathf.Log(81, 2)) - reward;
        Debug.Log(reward);
        AddReward(reward);

        if (csv)
        {
            WriteCsv();
        }


        EndEpisode();
    }

    void WriteCsv()
    {
        // expで計算した値
        StreamWriter file = new StreamWriter(@"C:\Users\shunsuke\UnityProjects\Toio\Data\Grid_data\grid.csv", false, Encoding.GetEncoding("Shift_JIS"));
        var col = "";
        for (int i = 0; i < grid.GetLength(0);i++){
            col += i.ToString();
            if (i != grid.GetLength(0) - 1)
            {
                col+=",";
            }
        }
        file.WriteLine(col);
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            var line = "";
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                line += ((Mathf.Exp(grid[i, j]))/sum).ToString("0.000000");
                if (j != 8)
                {
                    line += ",";
                }
            }
            file.WriteLine(line);
        }
        file.Close();

        // グリッドの値そのまま
        StreamWriter fileorigin = new StreamWriter(@"C:\Users\shunsuke\UnityProjects\Toio\Data\Grid_data\gridorigin.csv", false, Encoding.GetEncoding("Shift_JIS"));
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

    void FixedUpdate()
    {
        countTime += Time.deltaTime;
        if (countTime > timeLimit)
        {
            Finished();
        }
        
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        float v = Input.GetAxis("Vertical");
        var discreseActionsOut = actionsOut.DiscreteActions;
        discreseActionsOut[0] = 0;
        if (v > 0.5f) discreseActionsOut[0] = 1;
        if (v < -0.5f) discreseActionsOut[0] = 2;
    }
}
