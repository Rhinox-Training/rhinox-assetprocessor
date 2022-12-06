using System;
using System.Collections;
using System.Text;
using Rhinox.Perceptor;
using UnityEngine;
using UnityEngine.Networking;

namespace Rhinox.AssetProcessor.Editor
{
    public static class NetworkHelper
    {
        public static IEnumerator PUTJson<T>(string url, T jsonData, Action<T> onSuccess = null, Action<T> onFailure = null)
        {
            using (UnityWebRequest www = UnityWebRequest.Put(url, JsonUtility.ToJson(jsonData)))
            {
                www.SetContentTypeJson();
                yield return www.SendWebRequest();

                HandleUploadCompleted(www, jsonData, onSuccess, onFailure);
            }
        }
        
        public static IEnumerator POSTJson<T>(string url, T jsonData, Action<T> onSuccess = null, Action<T> onFailure = null)
        {
            // NOTE: Need to use PUT here since POST would encode the json as url-encoded through the constructor
            using (UnityWebRequest www = UnityWebRequest.Put(url, JsonUtility.ToJson(jsonData)))
            {
                www.SetContentTypeJson();
                www.method = "POST"; // NOTE: this reencodes the Put as a POST, keeping Json data as json
                yield return www.SendWebRequest();

                HandleUploadCompleted(www, jsonData, onSuccess, onFailure);
            }
        }

        private static void SetContentTypeJson(this UnityWebRequest www)
        {
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Accept", "application/json");
        }

        private static void HandleUploadCompleted<T>(UnityWebRequest www, T body, Action<T> onSuccess = null, Action<T> onFailure = null)
        {
            if (www.isHttpError || www.isNetworkError)
            {
                PLog.Error($"{www.method.ToUpperInvariant()} request failed on {www.url}, ERROR: {www.error}");
                onFailure?.Invoke(body);
            }
            else
            {
                PLog.Info($"{www.method.ToUpperInvariant()} request\n{DecodeUpload(www.uploadHandler)}\ncompleted for '{www.url}' with response HTTP/{www.responseCode}: '{www.downloadHandler.text}'");
                onSuccess?.Invoke(body);
            }
        }

        private static string DecodeUpload(UploadHandler handler)
        {
            return UnityWebRequest.UnEscapeURL(UTF8Encoding.UTF8.GetString(handler.data));
        }
    }
}