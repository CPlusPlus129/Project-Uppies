using DialogueModule;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

class DialogueAssetManager : IDialogueAssetManager
{
    protected Dictionary<string, UnityEngine.Object> currentUsingAssetDict = new Dictionary<string, UnityEngine.Object>();
    public IReadOnlyDictionary<string, UnityEngine.Object> CurrentUsingAssetDict => currentUsingAssetDict;
    IAssetLoader assetLoader;
    public DialogueAssetManager(IAssetLoader assetLoader)
    {
        this.assetLoader = assetLoader;
    }

    public IEnumerator<T> LoadCoroutine<T>(string fileName) where T : UnityEngine.Object
    {
        var handle = assetLoader.LoadAsyncByHandle<T>(fileName);
        while (!handle.IsDone)
        {
            yield return null;
        }
        if (handle.Status != AsyncOperationStatus.Succeeded)
            Debug.LogError($"dam Failed to Load {fileName}");

        currentUsingAssetDict[fileName] = handle.Result;
    }

    public void Release(string fileName)
    {
        currentUsingAssetDict.Remove(fileName);
    }

}