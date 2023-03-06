using System.Collections;
using System;
using UnityEngine;
using System.IO;

public class Recordmonolinear : MonoBehaviour
{

    [SerializeField] AudioSource _source;
    // Start is called before the first frame update

    double startDspTime;
    double buffer = 5 / 60d;

    private int outputRate;
    private string fileName = "Data/Audio_data/linear.wav";
    private int headerSize = 44;

    private int Channels = 1;
    private bool recOutput;
    private FileStream fileStream;
    int count = 0;

    void Start()
    {
        StartWriting(fileName);
        recOutput = true;
        startDspTime = AudioSettings.dspTime;
        outputRate = AudioSettings.outputSampleRate;
        Debug.Log(AudioSettings.driverCapabilities);
        Debug.Log(AudioSettings.speakerMode);
        Debug.Log(outputRate);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var elapsedDspTime = AudioSettings.dspTime - startDspTime;
        var nxtRng = NextRingTime();

        if (nxtRng < AudioSettings.dspTime + buffer)
        {
            _source.PlayScheduled(nxtRng);
            Debug.Log(nxtRng);
        }

        double NextRingTime()
        {
            var interval = 10d;
            var elapsedDspTime = AudioSettings.dspTime - startDspTime;
            var num = System.Math.Floor(elapsedDspTime / interval);

            return startDspTime + (num + 1d) * interval;
        }

        Move(elapsedDspTime);

        
    }   

    private void Move(double elapsedDspTime)
    {
        if (elapsedDspTime > 18d + count * 10)
        {
            this.transform.position = new Vector3(0, 0.02f, 0.18f-count*0.02f);
            count++;
        }
    }


    private void StartWriting(string name)
    {
        fileStream = new FileStream(name, FileMode.Create);
        byte emptyByte = new byte();

        for (int i = 0; i < headerSize; i++)
        { //preparing the header
            fileStream.WriteByte(emptyByte);
        }
    }

    void OnDestroy()
    {
        recOutput = false;
        WriteHeader();
    }

    void OnAudioFilterRead(float[] data, int channels)
    {

        if (recOutput)
        {
            short[] shortData = new short[data.Length];
            //converting in 2 steps : float[] to short[], //then short[] to Byte[]

            byte[] bytesData = new Byte[data.Length * 2];
            //bytesData array is twice the size of
            //dataSource array because a float converted in short is 2 bytes.


            //Audiodata‚Í-1`1‚ÌfloatŒ^‚¾‚©‚çAInt16‚É•ÏŠ·‚·‚é‚Æ‚«‚ÉA2^15-1‚ðA‚©‚¯‚Ä®”‚É‚·‚éB
            int rescaleFactor = 32767; //to convert float to short

            for (int i = 0; i < data.Length; i++)
            {
                shortData[i] = (short)(data[i] * rescaleFactor);
                var byteArr = new byte[2];
                byteArr = BitConverter.GetBytes(shortData[i]);
                byteArr.CopyTo(bytesData, i * 2);

            }

            if (!recOutput)
            {
                return;
            }
            fileStream.Write(bytesData, 0, bytesData.Length);
        }
    }

    private void WriteHeader()
    {

        fileStream.Seek(0, SeekOrigin.Begin);

        byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        byte[] audioFormat = BitConverter.GetBytes(1); //Uncompressed PCM
        fileStream.Write(audioFormat, 0, 2);

        byte[] numChannels = BitConverter.GetBytes(Channels);
        fileStream.Write(numChannels, 0, 2);

        byte[] sampleRate = BitConverter.GetBytes(outputRate);
        fileStream.Write(sampleRate, 0, 4);

        byte[] byteRate = BitConverter.GetBytes(outputRate * 2 * Channels);
        // sampleRate * bytesPerSample*number of channels, here 44100 * 2 * 4
        fileStream.Write(byteRate, 0, 4);

        byte[] blockAlign = BitConverter.GetBytes(8);
        // 16bit * number of channels, here 16bit * 4 = 64bit = 8byte
        fileStream.Write(blockAlign, 0, 2);

        byte[] bitsPerSample = BitConverter.GetBytes(16);
        fileStream.Write(bitsPerSample, 0, 2);

        byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(dataString, 0, 4);

        byte[] subChunk2 = BitConverter.GetBytes(fileStream.Length - headerSize);
        fileStream.Write(subChunk2, 0, 4);

        fileStream.Close();
    }

}
