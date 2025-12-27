using Genesis.Shared.Services;
using MagicOnion;
using MagicOnion.Server;

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
}