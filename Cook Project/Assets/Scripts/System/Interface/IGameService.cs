using Cysharp.Threading.Tasks;
using System;

public interface IGameService: IDisposable
{
    UniTask Init();
}