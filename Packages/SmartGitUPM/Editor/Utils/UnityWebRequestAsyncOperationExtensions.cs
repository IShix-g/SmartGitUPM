
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace SmartGitUPM.Editor
{
    internal static class UnityWebRequestAsyncOperationExtensions
    {
        public static TaskAwaiter<UnityWebRequest> GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
        {
            var source = new TaskCompletionSource<UnityWebRequest>();
            asyncOp.completed += operation =>
            {
                var webRequest = ( (UnityWebRequestAsyncOperation) operation ).webRequest;
                if (webRequest.result
                    is UnityWebRequest.Result.ConnectionError
                    or UnityWebRequest.Result.ProtocolError)
                {
                    source.SetException(new HttpRequestException(webRequest.error));
                }
                else
                {
                    source.SetResult(webRequest);
                }
            };
            return source.Task.GetAwaiter();
        }
    }
}