using Genesis.Shared.Services;
using MagicOnion;
using MagicOnion.Server;
using UnityEngine;

namespace Genesis.Server.Services;

public class MyFirstService : ServiceBase<IMyFirstService>, IMyFirstService
{
    /// <inheritdoc/>
    public async UnaryResult<int> SumAsync(int x, int y)
    {
        Console.WriteLine($"Received {x}, {y}");
        await Task.CompletedTask;
        return x + y;
    }

    /// <inheritdoc/>
    public UnaryResult<Vector3> MoveForwardAsync(Vector3 position)
    {
        Console.WriteLine($"Received position: {position}");
        var newPosition = position + Vector3.forward;
        return UnaryResult.FromResult(newPosition);
    }
}