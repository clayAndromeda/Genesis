using System;
using Cysharp.Threading.Tasks;
using Genesis.Shared.Services;
using MagicOnion;
using MagicOnion.Client;
using UnityEngine;

public class SampleComponent : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        try
        {
            var channel = GrpcChannelx.ForAddress("http://localhost:5277");
            var client = MagicOnionClient.Create<IMyFirstService>(channel);
            var result = await client.MoveForwardAsync(new Vector3(1, 2, 3));
            Debug.Log(result);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            throw;
        }
    }
}
