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
        //サブスクライバーの登録
        ros.Subscribe<Int16Msg>("voice", OnSubscribe);
    }

    // サブスクライブしたときに呼ばれ、受け取った角度、回転する。
    public void OnSubscribe(Int16Msg msg)
    {
        
    }
}
