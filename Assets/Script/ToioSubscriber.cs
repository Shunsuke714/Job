using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
public class ToioSubscriber : MonoBehaviour
{
    ROSConnection ros;
    int angle;
    public void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        //�T�u�X�N���C�o�[�̓o�^
        ros.Subscribe<Int16Msg>("voice", OnSubscribe);
    }

    // �T�u�X�N���C�u�����Ƃ��ɌĂ΂�A�󂯎�����p�x�A��]����B
    public void OnSubscribe(Int16Msg msg)
    {
        
    }
}
