using MagicOnion;
using UnityEngine;

namespace Genesis.Shared.Services
{
    public interface IMyFirstService : IService<IMyFirstService>
    {
        /// <summary> 合計値を計算する</summary>
        UnaryResult<int> SumAsync(int x, int y);

        /// <summary> 前進する </summary>
        UnaryResult<Vector3> MoveForwardAsync(Vector3 position);
    }
}

