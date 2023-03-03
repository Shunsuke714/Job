using System;
using System.IO;
using System.Text;
using UnityEngine;

[RequireComponent(typeof(AudioListener))]
public class Record : MonoBehaviour
{
    private int m_outputRate = 44100;
    private bool m_isRecording = false;
    private FileStream m_stream;
    readonly private string m_fileName = "output.wav";
    readonly private int m_headerSize = 44;
    float[] wave = new float[1024];

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.R)) return;
        if (m_isRecording == false)
        {
            Debug.Log("rec started", this);
            m_isRecording = true;
            startWriting(m_fileName);
        }
        else
        {
            m_isRecording = false;
            writeHeader();
            Debug.Log("rec stop", this);
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!m_isRecording) return;
        convertAndWrite(data);
    }

    /// <summary>
    /// ストリームを0埋めして初期化
    /// </summary>
    /// <param name="name">ファイルの名前</param>
    private void startWriting(string name)
    {
        m_stream = new FileStream(name, FileMode.Create);
        var emptyByte = new byte();
        for (int i = 0; i < m_headerSize; i++)
        {
            m_stream.WriteByte(emptyByte);
        }
    }

    /// <summary>
    /// 変換してストリームに書き込む
    /// </summary>
    /// <param name="dataSource">書き込むデータ</param>
    private void convertAndWrite(float[] dataSource)
    {
        Int16[] intData = new Int16[dataSource.Length];
        var bytesData = new byte[dataSource.Length * 2];
        int rescaleFactor = 32767;
        for (int i = 0; i < dataSource.Length; i++)
        {
            intData[i] = (short)(dataSource[i] * rescaleFactor);
            var byteArr = new byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }
        m_stream.Write(bytesData, 0, bytesData.Length);
    }

    /// <summary>
    /// ヘッダを書く
    /// 詳しくはwavのフォーマットを確認
    /// http://www.graffiti.jp/pc/p030506a.htm
    /// </summary>
    private void writeHeader()
    {
        m_stream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = Encoding.UTF8.GetBytes("RIFF");
        m_stream.Write(riff, 0, 4);
        Byte[] chunkSize = BitConverter.GetBytes(m_stream.Length - 8);
        m_stream.Write(chunkSize, 0, 4);
        Byte[] wave = Encoding.UTF8.GetBytes("WAVE");
        m_stream.Write(wave, 0, 4);
        Byte[] fmt = Encoding.UTF8.GetBytes("fmt ");
        m_stream.Write(fmt, 0, 4);
        Byte[] subChunk1 = BitConverter.GetBytes(16);
        m_stream.Write(subChunk1, 0, 4);

        UInt16 one = 1;
        UInt16 two = 2;
        Byte[] audioFormat = BitConverter.GetBytes(one);
        m_stream.Write(audioFormat, 0, 2);
        Byte[] numChannels = BitConverter.GetBytes(two);
        m_stream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(m_outputRate);
        m_stream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(m_outputRate * 4);
        // sampleRate * bytesPerSample*number of channels, here 44100*2*2
        m_stream.Write(byteRate, 0, 4);

        UInt16 four = 4;
        Byte[] blockAlign = BitConverter.GetBytes(four);
        m_stream.Write(blockAlign, 0, 2);

        UInt16 sixteen = 16;
        Byte[] bitPerSample = BitConverter.GetBytes(sixteen);
        m_stream.Write(bitPerSample, 0, 2);

        Byte[] dataString = Encoding.UTF8.GetBytes("data");
        m_stream.Write(dataString, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(m_stream.Length - m_headerSize);
        m_stream.Write(subChunk2, 0, 4);

        m_stream.Close();
    }
}