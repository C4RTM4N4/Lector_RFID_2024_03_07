using System;

using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;

namespace com.nem.aurawheel.Utils.Rest
{
    class ResponseManager
    {
        public static string ConvertResponseBodyToString(HttpWebResponse response)
        {
            string result = "";
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                result += reader.ReadToEnd();
            }
            return result;
        }

        // Para el ping
        public static string ConvertResponseBodyToString(WebResponse response)
        {
            string result = "";
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                result += reader.ReadToEnd();
            }
            return result;
        }

        public static object ConvertResponseBodyToJSON(HttpWebResponse response)
        {
            return JSONSerializer.JsonDecode(ConvertResponseBodyToString(response));
        }
    }
}
