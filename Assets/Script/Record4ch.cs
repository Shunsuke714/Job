using System;
using System.Collections;
using UnityEngine;
using System.IO;

public class Record4ch : MonoBehaviour
{
    private int outputRate = 44100;
    private string fileName = "output.wav";
    private int headerSize = 44;
   
    private int Channels = 4;
    private bool recOutput;
    private FileStream fileStream;
    
    void Start()
    {
        Debug.Log(AudioSettings.driverCapabilities);
        StartWriting(fileName);
        recOutput = true;
    }

    // Update is called once per frame
    void Update()
    {
        
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


            //Audiodataは-1～1のfloat型だから、Int16に変換するときに、2^15-1を、かけて整数にする。
            int rescaleFactor = 32767; //to convert float to short

            
            int index = 0;
            for (int i = 0; i < data.Length; i += channels)
            {
                float frontL = data[i];
                float frontR = data[i + 1];
                float backL = data[i + 2];
                float backR = data[i + 3];

                for (int j = 0; j < Channels; j++)
                {
                    float writeData = 0.0f;
                    switch (j)
                    {
                        case 0:
                            writeData = frontL;
                            break;
                        case 1:
                            writeData = frontR;
                            break;
                        case 2:
                            writeData = backL;
                            break;
                        case 3:
                            writeData = backR;
                            break;
                    }
                    byte[] bytes = new Byte[2];
                    shortData[index] = (short)(writeData * rescaleFactor);
                    bytes = BitConverter.GetBytes(shortData[index]);
                    bytes.CopyTo(bytesData, index * 2);
                    index++;
                }
            }

            if (!recOutput)
            {
                return;
            }
            fileStream.Write(bytesData, 0, bytesData.Length);
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
