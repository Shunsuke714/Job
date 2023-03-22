using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra.Complex32;

//�X�e�A�����O�x�N�g����\���^�F���f���x�N�g���̌^���`
using StearingVector = MathNet.Numerics.LinearAlgebra.Vector<MathNet.Numerics.Complex32>;


public class MicArrayTools : MonoBehaviour
{
    // wav���X�g��startPosition����windowSize�����o���ăt�[���G�ϊ������s
    public Complex32[] computeFFT(float[] wav, int startPosition, int windowSize)
    {
        Complex32[] complexData = new Complex32[windowSize];
        //���f���^�ɕϊ�
        //Debug.Log(startPosition);
        for (int i = 0; i < windowSize; i++)
        {
            complexData[i] = new Complex32(wav[startPosition+i], 0f);   //
        }

        //run FFT
        Fourier.Forward(complexData, FourierOptions.Matlab); // arbitrary length
        return complexData;
    }

    // �����g�`��csv�ɂ������̂𗘗p
    // 1�s�ɂS�`���l�����̒l�������Ă���
    float[][] LoadWaveData()
    {
        TextAsset csvFile; // CSV�t�@�C��
        List<string[]> csvData = new List<string[]>(); // CSV�̒��g�����郊�X�g;
        float[][] wavData;
        csvFile = Resources.Load("wave") as TextAsset; // Resouces����CSV�ǂݍ���

        StringReader reader = new StringReader(csvFile.text);

        // csv�̓ǂݍ��݁F�R���} �ŕ�������s���ǂݍ��݃��X�g�ɒǉ�
        while (reader.Peek() != -1) // �ŏI�s�܂�
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
                wavData[ch][i] = float.Parse(csvData[i][ch]) / 65536;//16�r�b�g�̉�������-1�`1��float�ɕϊ�
            }

        }
        
        return wavData;
    }

    //�X�e�A�����O�x�N�g����csv��ǂݍ���
    //�@stearing_vec_18direction_4ch.csv�̏ꍇ
    //�@�@��s�̐��l�̐��̓}�C�N�� x 2(���f���̎����E����) 18 x 4 = 116
    //�@�@�s����������18 (20�x����)x 257�i�t�[���G�ϊ��̑���/2+1 x ���f���̎����E�����j=�@4626�s
    StearingVector[][] LoadStearingVec(int numFFTPoint) //numFFTPoint= windowSize/2+1 = 257
    {
        TextAsset csvFile; // CSV�t�@�C��
        List<string[]> csvData = new List<string[]>(); // CSV�̒��g�����郊�X�g
        csvFile = Resources.Load("stearing_vec_18direction_4ch") as TextAsset; // Resouces����CSV�ǂݍ���

        StringReader reader = new StringReader(csvFile.text);

        // csv�̓ǂݍ��݁F�R���} �ŕ�������s���ǂݍ��݃��X�g�ɒǉ�
        while (reader.Peek() != -1) // �ŏI�s�܂�
        {
            string line = reader.ReadLine();
            csvData.Add(line.Split(','));
        }
        
        //�̈�m��
        int numDirection = csvData.Count / numFFTPoint;
        var stearingVecArray = new StearingVector[numDirection][];
        for(int i = 0; i< numDirection; i++)
        {
            stearingVecArray[i]= new StearingVector[numFFTPoint];
        }
        
        //���
        for (int i = 0; i < csvData.Count; i++)
        {
            int dir = i / numFFTPoint; //�����C���f�b�N�X
            int freq = i % numFFTPoint;  //���g���C���f�b�N�X
            int L = csvData[i].Length; //=�`�����l����*2
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
        //�ǂݍ��񂾑��`�����l���g�`�̕\���i�f�o�b�O�p�j
        Color[] pallet = new Color[] { Color.blue, Color.red, Color.green, Color.cyan, Color.magenta, Color.yellow, Color.gray, Color.white};
        for (int ch = 0;ch< numChannel;ch++) {
            //�\���p��LineRenderer�I�u�W�F�N�g���쐬���Ċe��ݒ�
            GameObject g = new GameObject();
            LineRenderer renderer = g.AddComponent<LineRenderer>();
            renderer.startWidth = 0.05f;
            renderer.endWidth = 0.05f;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.startColor = pallet[ch];
            renderer.endColor = pallet[ch];
            // ���_��ݒ�F�\���Ɏ��Ԃ�������̂ōő�1024�_
            int L = fullLength;
            if (L > 1024)
            {
                L = 1024;
            }
            // ���_�̈ʒu��ݒ�
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
        //�t�[���G�ϊ���X�y�N�g���̕\���i�f�o�b�O�p�j
        {
            //�\���p��LineRenderer�I�u�W�F�N�g���쐬���Ċe��ݒ�
            GameObject g = new GameObject();
            LineRenderer renderer = g.AddComponent<LineRenderer>();
            renderer.startWidth = 0.05f;
            renderer.endWidth = 0.05f;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.startColor = Color.black;
            renderer.endColor = Color.black;
            // ���_�̈ʒu��ݒ�
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
        int numDirection = stearingVecArray.Length;//-pi �`�@pi  
        float[] musicPowerList = new float[numDirection];//�e������\���C���f�b�N�X�Ԗڂ�MUSIC Power���v�Z�����

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
        
        //�������Ƃ�MUSIC�p���[�̕\���i�f�o�b�O�p�j
        {
            //�\���p��LineRenderer�I�u�W�F�N�g���쐬���Ċe��ݒ�
            GameObject g = new GameObject();
            LineRenderer renderer = g.AddComponent<LineRenderer>();
            renderer.startWidth = 0.05f;
            renderer.endWidth = 0.05f;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.startColor = Color.black;
            renderer.endColor = Color.black;
            // ���_�̈ʒu��ݒ�
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
