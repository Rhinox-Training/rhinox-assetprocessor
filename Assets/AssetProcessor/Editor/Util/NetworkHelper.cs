using System;
using System.Collections;
using System.Text;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using Rhinox.Utilities;
using UnityEngine;
using UnityEngine.Networking;
using Utility = Rhinox.Lightspeed.Utility;

namespace Rhinox.AssetProcessor.Editor
{
    public static class NetworkHelper
    {
        public static IEnumerator Get<T>(string url, Action<T> onSuccess = null, Action onFailure = null)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SetContentTypeJson();
                yield return www.SendWebRequest();

                if (www.IsRequestValid(out string error))
                {
                    LogCompleted(www);
                    var data = www.ParseJsonResult<T>(true);
                    onSuccess?.Invoke(data);
                }
                else
                {
                    LogError(www, error);
                    onFailure?.Invoke();
                }
            }
        }
        
        public static IEnumerator PUTJson<T>(string url, T jsonData, Action<T> onSuccess = null, Action<string> onFailure = null)
        {
            using (UnityWebRequest www = UnityWebRequest.Put(url, Utility.ToJson(jsonData, true)))
            {
                www.SetContentTypeJson();
                yield return www.SendWebRequest();

                if (www.IsRequestValid(out string error))
                {
                    onSuccess
                }

                HandleUploadCompleted(www, jsonData, onSuccess, onFailure);
            }
        }
        
        public static IEnumerator POSTJson<T>(string url, T jsonData, Action<T> onSuccess = null, Action<T> onFailure = null)
        {
            // NOTE: Need to use PUT here since POST would encode the json as url-encoded through the constructor
            using (UnityWebRequest www = UnityWebRequest.Put(url, Utility.ToJson(jsonData, true)))
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
            if (www.IsRequestValid(out string error))
            {
                LogCompleted(www);
                onSuccess?.Invoke(body);
            }
            else
            {
                LogError(www, error);
                onFailure?.Invoke(body);
            }
        }

        private static void LogCompleted(UnityWebRequest www)
        {
            PLog.Info($"{www.method.ToUpperInvariant()} request\n{DecodeUpload(www.uploadHandler)}\ncompleted for '{www.url}' with response HTTP/{www.responseCode}: '{www.downloadHandler.text}'");
        }
        
        private static void LogError(UnityWebRequest www, string error = null)
        {
            if (error == null)
                error = www.error;
            PLog.Error($"{www.method.ToUpperInvariant()} request failed on {www.url}, ERROR: {error}\nbody-request:{DecodeUpload(www.uploadHandler)}\nbody-response:{DecodeDownload(www.downloadHandler)}");
        }

        private static string DecodeDownload(DownloadHandler handler)
        {
            if (handler == null || handler.data == null)
                return "<empty>";
            return UnityWebRequest.UnEscapeURL(UTF8Encoding.UTF8.GetString(handler.data));
        }

        private static string DecodeUpload(UploadHandler handler)
        {
            if (handler == null)
                return "<empty>";
            return UnityWebRequest.UnEscapeURL(UTF8Encoding.UTF8.GetString(handler.data));
        }
    }
}