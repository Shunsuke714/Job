using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using toio;

public class voiceSubscriber : MonoBehaviour
{
    ROSConnection ros;
    public ConnectType connectType;
    CubeManager cm;
    
    async void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        cm = new CubeManager(connectType);

        //�T�u�X�N���C�o�[�̓o�^
        ros.Subscribe<Int16Msg>("voice", OnSubscribe);

        await cm.SingleConnect();
        
    }

    // �T�u�X�N���C�u�����Ƃ��ɌĂ΂�A�󂯎�����p�x�A��]����B
    void OnSubscribe(Int16Msg msg)
    {
        Debug.Log(msg.data);
        foreach(var handle in cm.syncHandles)
        {
            if (msg.data > 180)
            {
                handle.RotateByDeg(-(360 - msg.data), 50).Exec();
            }
            else
            {
                handle.RotateByDeg(msg.data, 50).Exec();
            }
            
        }
    }
}
