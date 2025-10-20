using System.Collections.Generic;

namespace DialogueModule
{
    public interface IDialogueAssetManager
    {
        IReadOnlyDictionary<string, UnityEngine.Object> CurrentUsingAssetDict { get; }
        IEnumerator<T> LoadCoroutine<T>(string fileName) where T : UnityEngine.Object;
        void Release(string fileName);
    }
}