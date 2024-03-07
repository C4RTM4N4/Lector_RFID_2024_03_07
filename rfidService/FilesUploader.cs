using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Threading;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Net.Http.Headers;


namespace com.nem.aurawheel.Utils
{
    class FilesUploader
    {
        public async Task<HttpResponseMessage> UploadFiles(List<String> fileList, String serverUrl, String user, String Password, bool zip, double timeout) 
        {
            String x_nem_datetime;
            String authorization;
            byte[] payload = new byte[] { };
            MultipartFormDataContent multiPartContent = new MultipartFormDataContent();

            foreach (String file in fileList)
            {
                FileInfo fi = new FileInfo(file);
                string fileName = fi.Name;
                byte[] fileContents = File.ReadAllBytes(fi.FullName);
                ByteArrayContent byteArrayContent;
                if (zip)
                {
                    byte[] fileContentsGz = Compress(fileContents);
                    payload = CombineBarray(payload, fileContentsGz);
                    byteArrayContent = new ByteArrayContent(fileContentsGz);
                }
                else
                {
                    payload = CombineBarray(payload, fileContents);
                    byteArrayContent = new ByteArrayContent(fileContents);
                }
                              
                byteArrayContent.Headers.Add("Content-Type", "application/octet-stream");
                byteArrayContent.Headers.Add("Content-Transfer-Encoding", "binary");
                multiPartContent.Add(byteArrayContent, "name", fileName);
            }

            Uri url = new Uri(serverUrl);
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            requestMessage.Headers.ExpectContinue = false;

            x_nem_datetime = NemDateUtils.UniversalTimeMillis(DateTime.Now).ToString();

            byte[] bAuthorization = CombineBarray(Encoding.UTF8.GetBytes(x_nem_datetime + url.ToString() + Password), payload);
            authorization = "NEM " + user + ":" + Encrypter.EncryptSHA1Message(bAuthorization);
          
            requestMessage.Headers.Add("x-nem-datetime", x_nem_datetime);
            requestMessage.Headers.Add("authorization", authorization);
            requestMessage.Content = multiPartContent;

            HttpClient httpClient = new HttpClient();
            HttpStatusCode statusCode = HttpStatusCode.BadRequest;
            httpClient.Timeout = TimeSpan.FromSeconds(timeout);
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            try
            {
                //Console.WriteLine(requestMessage);
                httpResponse = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, CancellationToken.None);
                statusCode = httpResponse.StatusCode;
                HttpContent responseContent = httpResponse.Content;
                 
                //Console.WriteLine(statusCode);

            }
            catch (Exception ex)
            {
                throw ex;
            }
            
            ///remove
            return httpResponse;
        }
        //Compresion en formato gzip
        private static byte[] Compress(byte[] raw)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, false))
                {
                    gzip.Write(raw, 0, raw.Length);
                }
                return memory.ToArray();
            }
        }
        //Combina dos arrays de bytes en uno solo
        private static byte[] CombineBarray(byte[] a, byte[] b)
        {
            byte[] c = new byte[a.Length + b.Length];
            System.Buffer.BlockCopy(a, 0, c, 0, a.Length);
            System.Buffer.BlockCopy(b, 0, c, a.Length, b.Length);
            return c;
        }
    }
}
