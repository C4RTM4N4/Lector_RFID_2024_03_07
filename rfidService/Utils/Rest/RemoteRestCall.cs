using System;

using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace com.nem.aurawheel.Utils.Rest
{
    class RemoteRestCall
    {
        public static HttpWebResponse ExecuteRestGET(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "text/html; charset=utf-8";
            SetHeadersGET(request);
            if (Settings.HasProxy) request.Proxy = SetProxy();
            return (HttpWebResponse)request.GetResponse();
        }

        //Para testear conectividad
        public static HttpWebResponse ExecuteRestGET(string url, bool hasProxy, string proxyUrl, string username, string pass, bool isSecure)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "text/html; charset=utf-8";
            if(isSecure) SetHeadersGET(request);
            if (hasProxy)
            {
                // Set the client-side credentials using the Credentials property.
                ICredentials credentials = new NetworkCredential(username, pass);
                // Set the proxy server to proxyserver, set the port to 80, and specify to bypass
                // the proxy server for local addresses.
                IWebProxy proxyObject = new WebProxy(proxyUrl);
                proxyObject.Credentials = credentials;
                request.Proxy = proxyObject;

            }
            return (HttpWebResponse)request.GetResponse();
        }

        public static HttpWebResponse ExecuteRestPOST(string url, object body)
        {
            return ExecuteRestPOST(url, JSONSerializer.JsonEncode(body));
        }

        public static HttpWebResponse ExecuteRestPOST(string url, string requestBody)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
            request.Method = "POST";
            request.ContentType = "application/json";
            SetHeadersPUT(request, requestBody);
            SetRequestBody(request, requestBody);
            if (Settings.HasProxy) request.Proxy = SetProxy();
            return (HttpWebResponse)request.GetResponse();
        }

        public static HttpWebResponse ExecuteRestPUT(string url, object body)
        {
            return ExecuteRestPUT(url, JSONSerializer.JsonEncode(body));
        }

        public static HttpWebResponse ExecuteRestPUT(string url, string requestBody)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
            request.Method = "PUT";
            request.ContentType = "application/json";
            SetHeadersPUT(request, requestBody);
            SetRequestBody(request, requestBody);
            if (Settings.HasProxy) request.Proxy = SetProxy();
            return (HttpWebResponse)request.GetResponse();
        }

        private static void SetHeadersGET(HttpWebRequest request)
        {
            long dateTime = NemDateUtils.UniversalTimeMillis(DateTime.Now.ToUniversalTime());
            request.Headers.Set("x-nem-datetime", dateTime.ToString());
            string query = request.RequestUri.AbsoluteUri;
            //if (query.Length > 0)
            //    query = query.Remove(0, 1);
            string messageToEncrypt = MessageToEncrypt(dateTime, query, Settings.Pass);
            string authentication = String.Format("NEM {0}:{1}", DeviceID.GetDeviceID(), Encrypter.EncryptSHA1Message(messageToEncrypt));
            request.Headers.Set("Authorization", authentication);
        }

        private static void SetHeadersPUT(HttpWebRequest request, string requestBody)
        {
            long dateTime = NemDateUtils.UniversalTimeMillis(DateTime.Now.ToUniversalTime());
            request.Headers.Set("x-nem-datetime", dateTime.ToString());
            string messageToEncrypt = MessageToEncrypt(dateTime, request.RequestUri.AbsoluteUri + requestBody, Settings.Pass);
            string authentication = String.Format("NEM {0}:{1}", DeviceID.GetDeviceID(), Encrypter.EncryptSHA1Message(messageToEncrypt));
            request.Headers.Set("Authorization", authentication);
        }

        private static string MessageToEncrypt(long dateTime, string requestBody, string pass)
        {
            StringBuilder textToEncryting = new StringBuilder();
            textToEncryting.Append(dateTime.ToString());
            if (requestBody != null)
                textToEncryting.Append(requestBody);
            textToEncryting.Append(pass);
            return textToEncryting.ToString();
        }

        private static void SetRequestBody(HttpWebRequest request, string requestBody)
        {
            if (requestBody != null && requestBody.Length > 0)
            {
                byte[] formData = UTF8Encoding.UTF8.GetBytes(requestBody);
                request.ContentLength = formData.Length;
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(formData, 0, formData.Length);
                }
            }
        }

        private static IWebProxy SetProxy()
        {
            // Set the client-side credentials using the Credentials property.
            ICredentials credentials = new NetworkCredential(Settings.ProxyUser, Settings.ProxyPassword);
            // Set the proxy server to proxyserver, set the port to 80, and specify to bypass
            // the proxy server for local addresses.
            IWebProxy proxyObject = new WebProxy(Settings.ProxyUrl);
            proxyObject.Credentials = credentials;
            return proxyObject;
        }

    }
}

