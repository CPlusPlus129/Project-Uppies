using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DialogueModule
{
    class AssetManager : IDialogueAssetManager
    {
        protected Dictionary<string, UnityEngine.Object> currentUsingAssetDict = new Dictionary<string, UnityEngine.Object>();
        public IReadOnlyDictionary<string, UnityEngine.Object> CurrentUsingAssetDict => currentUsingAssetDict;

        public T Load<T>(string fileName) where T : UnityEngine.Object
        {
            //resource.load doesn't need extension            
            var searchStr = Path.GetFileNameWithoutExtension(fileName);
            var obj = Resources.Load<T>(searchStr);
            currentUsingAssetDict[fileName] = obj;

            return obj;
        }

        public IEnumerator<T> LoadCoroutine<T>(string fileName) where T : UnityEngine.Object
        {
            var searchStr = Path.GetFileNameWithoutExtension(fileName);
            var request = Resources.LoadAsync<T>(searchStr);
            while (!request.isDone)
            {
                yield return null;
            }
            if (request.asset == null)
                Debug.LogError($"Failed to Load {fileName}");

            T obj = request.asset as T;
            currentUsingAssetDict[fileName] = obj;
        }

        public void Release(string fileName)
        {
            currentUsingAssetDict.Remove(fileName);
        }
    }
}