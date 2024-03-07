using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Security.Permissions;
using System.IO;
using System.Xml;
using System.ServiceProcess;
using System.Diagnostics;
using System.IO.Compression;
using com.nem.aurawheel.Utils;
using Chypher;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;

namespace rfidService
{

    public struct fleetAxles
    {
        public String fleetPrefix;
        public int axleNumber;

    };

    struct cfgStruct
    {
        public String IPaddress;
        public String port;
        public int responseTimetout;
        public String folder;
        public bool delete;
        public String user;
        public String url;
        public String password;
        public int heartbeat;
        public String statusUrl;
        public String prefix;
        public String measuredPath;
        public int rfidTimeRangeDelay;
        public List<fleetAxles> fleetCfg;
    }

public class rfidService: ServiceBase
    {
        const string VERSION = "v3.2";
        /// Archivo xml que contiene la configuración del programa
        const string CONFIGURATION_FILE = @"./rfidService_cfg.xml";
        const int RFID_COMM_CHECK_INTERVAL = 10000;
        const int COMM_RETRY = 540000;
        const int UPLOADER_INTERVAL = 2000;
        const int DATA_LINE = 3;
        const int DEPARTURE_TIME_INDEX = 21;
        const int ARRIVAL_TIME_INDEX = 20;
        const int AXLE_COUNT_INDEX = 32;
        const double UPLOAD_TIMEOUT = 900;
        const string PATH_TO_UPLOAD_FILES = @"\toupload";
        const string PATH_TO_MEASURED_FILES = @"\measured";
        const string PATH_TO_UPLOAD_ERROR = @"\uploaderror";
        const string PATH_TO_AXLE_NUMBER_ERROR = @"\axleNumberError";
        const string PATH_TO_RFID_ERROR = @"\rfidError";
        const string PATH_TO_DISCARD_TAGS = @"\discardedTags";
        const string KEY = "nem0056";
        const int RFID_RECONECT =20000;
        const int TAG_TIME_OFFSET_MS = 1000; 

        //
        private static rfidController rfid = new rfidController();
        private static cfgStruct configuration = new cfgStruct();
        private static System.Timers.Timer timer = new System.Timers.Timer();
        private static System.Timers.Timer uptimer = new System.Timers.Timer();
        private static System.Timers.Timer heartbeatTimer = new System.Timers.Timer();
        private static FileSystemWatcher watcher = new FileSystemWatcher();
        private static EventLog EvtLog;

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void Main()
        {
            ServiceBase.Run(new rfidService());  
        }
        public rfidService()
        {
            CanPauseAndContinue = false;
            ServiceName = "rfid Service";
            EvtLog = new System.Diagnostics.EventLog("Application");
            EvtLog.Source = this.ServiceName;         
        }
        protected override void OnStart(string[] args)
        {             
            try
            {
                EvtLog.WriteEntry("rfid Service Started SW version " + VERSION, EventLogEntryType.Information);
                               
                //Cargar configuracion
                LoadConfiguration();
                timer.Interval = RFID_COMM_CHECK_INTERVAL;
                timer.Elapsed += new ElapsedEventHandler(OnTimer);
                uptimer.Interval = UPLOADER_INTERVAL;
                uptimer.Elapsed += new ElapsedEventHandler(Upload);
                heartbeatTimer.Interval = configuration.heartbeat;
                heartbeatTimer.Elapsed += new ElapsedEventHandler(OnHeartbeat);
                // Create a new FileSystemWatcher and set its properties.          
                watcher.Path = configuration.folder;
                /* Watch for changes in LastAccess and LastWrite times, and
                   the renaming of files or directories. */
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                // Only watch text files.
                watcher.Filter = "*.csv";
                // Add event handlers.
                watcher.Created += new FileSystemEventHandler(OnNewMeassurement);
                // Begin watching.
                watcher.EnableRaisingEvents = true;
                // Enable Timer
                timer.Start();
                uptimer.Start();
                heartbeatTimer.Start();             

            }
            catch (Exception ex)
            {
                EvtLog.WriteEntry("Error: " + ex.Message, EventLogEntryType.Error);
            }

                     
        }
        protected override void OnStop()
        {
            EventLog.WriteEntry("rfid Service Stopped ", EventLogEntryType.Information);
            timer.Enabled = false;
            watcher.EnableRaisingEvents = false;
            try
            {
                rfid.closeConnection();
            }
            catch (Exception ex) 
            {
                EvtLog.WriteEntry("Error: " + ex.Message, EventLogEntryType.Error);
            }
        }
        protected static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {   
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
        public static void LoadConfiguration()
        {
            XmlDocument configXml = new XmlDocument();
            try 
            {
                string path;
                path = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase) + CONFIGURATION_FILE;
                configXml.Load(path);
                configuration.IPaddress = configXml.GetElementsByTagName("IPaddress")[0].InnerText;
                configuration.port = configXml.GetElementsByTagName("port")[0].InnerText;
                configuration.responseTimetout = Convert.ToInt32(configXml.GetElementsByTagName("responseTimeout")[0].InnerText); 
                configuration.folder = configXml.GetElementsByTagName("folder")[0].InnerText;
                configuration.delete = Convert.ToBoolean(configXml.GetElementsByTagName("delete")[0].InnerText);
                configuration.user = configXml.GetElementsByTagName("user")[0].InnerText;
                configuration.url = configXml.GetElementsByTagName("url")[0].InnerText;
                configuration.measuredPath = configXml.GetElementsByTagName("measuredPath")[0].InnerText;
                configuration.statusUrl = configXml.GetElementsByTagName("statusUrl")[0].InnerText;
                configuration.heartbeat = Convert.ToInt32(configXml.GetElementsByTagName("heartbeat")[0].InnerText) * 60000;
                configuration.rfidTimeRangeDelay = Convert.ToInt32(configXml.GetElementsByTagName("rfidTimeRangeDelay")[0].InnerText);
                string codedpass = configXml.GetElementsByTagName("password")[0].InnerText;
                ARC4Cypher cypher = new ARC4Cypher();
                configuration.password = cypher.decode(codedpass, KEY);
                
                configuration.prefix = configXml.GetElementsByTagName("tagprefix")[0].InnerText;

                XmlNodeList fleetsConfiguration = configXml.GetElementsByTagName("fleetAxles");

                configuration.fleetCfg = new List<fleetAxles>();
                foreach (XmlNode fleet in fleetsConfiguration)
                {
                    fleetAxles fleetCfg = new fleetAxles();
                    fleetCfg.fleetPrefix = fleet.ChildNodes.Item(0).InnerText;
                    fleetCfg.axleNumber = Convert.ToInt32(fleet.ChildNodes.Item(1).InnerText);
                    configuration.fleetCfg.Add(fleetCfg);
                    EvtLog.WriteEntry("Fleet " + fleetCfg.fleetPrefix + "Axles : " + fleetCfg.axleNumber, EventLogEntryType.Information);
                }
                EvtLog.WriteEntry("Configuration loaded successfully", EventLogEntryType.Information);

            }
            catch(Exception ex)
            {
                EvtLog.WriteEntry("Error loading Configuration:" + ex.Message, EventLogEntryType.Error); 
            }

        }


  
        private static void OnNewMeassurement(object source, FileSystemEventArgs e)
        {
            EvtLog.WriteEntry("New csv File Created: " + e.Name, EventLogEntryType.Information);
            string TrainDepartureTimeUTCstr = "";
            string TrainArrivalTimeUTCstr = "";
            string readAxlesNumber = "";
            bool upload = false;
            bool tagError = false;

            int index = e.FullPath.LastIndexOf('.');
            String tagFileFullPath = (index == -1 ? e.FullPath : e.FullPath.Substring(0, index)) + "_tags.json";
            index = e.Name.LastIndexOf('.');
            String tagFileName = (index == -1 ? e.Name + "_tags.json" : e.Name.Substring(0, index)) + "_tags.json";
            String discardedTagsFileName = (index == -1 ? e.Name + "_tags.json" : e.Name.Substring(0, index)) + "_discarded.csv";
            String discardedTagsFilePath = configuration.folder + PATH_TO_DISCARD_TAGS + @"\" + discardedTagsFileName;

            FileInfo meassurementFile = new FileInfo(e.FullPath);

            try
            {
                int waittime = 0;
                while(IsFileLocked(meassurementFile) & (waittime <= 60000) ){
                    Thread.Sleep(5000);
                    waittime += 5000;
                }
                using (StreamReader sr = new StreamReader(e.FullPath)){
                    for (int i = 1; i < DATA_LINE; i++)
                        sr.ReadLine();
                    String[] configLine = sr.ReadLine().Split(',');
                    TrainDepartureTimeUTCstr = configLine[DEPARTURE_TIME_INDEX];
                    TrainArrivalTimeUTCstr = configLine[ARRIVAL_TIME_INDEX];
                    readAxlesNumber = configLine[AXLE_COUNT_INDEX];
                }
                DateTime TrainDepartureTime;
                DateTime TrainArrivalTime;

                if (TrainDepartureTimeUTCstr == ""){
                    TrainDepartureTime = DateTime.Now;
                    EvtLog.WriteEntry("Could not retreieve Departure Time using: " + TrainDepartureTime.ToString(), EventLogEntryType.Information);
                }
                else
                {
                    try
                    {
                        TrainDepartureTime = DateTime.ParseExact(TrainDepartureTimeUTCstr, "yyyyMMdd_HHmmss",
                                           System.Globalization.CultureInfo.InvariantCulture).AddSeconds(configuration.rfidTimeRangeDelay);
                    }
                    catch (Exception ex)
                    {
                        TrainDepartureTime = DateTime.Now;
                        EvtLog.WriteEntry("Could not retreieve Departure time due to:\n" + ex.Message + "\nUsing :\n " + TrainDepartureTime.ToString(), EventLogEntryType.Information);                       
                    }
                    
                    EvtLog.WriteEntry("Departure Time: " + TrainDepartureTime.ToString(), EventLogEntryType.Information);
                }

               if (TrainArrivalTimeUTCstr == "")
                {
                    TrainArrivalTime = DateTime.Now.AddMinutes(5) ;
                    EvtLog.WriteEntry("Could not retreieve Arrival Time using: " + TrainArrivalTime.ToString(), EventLogEntryType.Information);
                }
                else
                {
                    try
                    {
                        TrainArrivalTime = DateTime.ParseExact(TrainArrivalTimeUTCstr, "yyyyMMdd_HHmmss",
                                           System.Globalization.CultureInfo.InvariantCulture).AddSeconds(-configuration.rfidTimeRangeDelay);
                    }
                    catch (Exception ex)
                    {
                        TrainArrivalTime = DateTime.Now.AddMinutes(5);
                        EvtLog.WriteEntry("Could not retreieve Arrival time due to:\n" + ex.Message + "\nUsing :\n " + TrainArrivalTime.ToString(), EventLogEntryType.Information);
                    }

                    EvtLog.WriteEntry("Arrival Time: " + TrainArrivalTime.ToString(), EventLogEntryType.Information);
                }

                tag[] resultAll = null;
                tag[] result = null;

                if (!File.Exists(tagFileFullPath))
                {
                    rfid.stopReading();
                    resultAll = rfid.returnTags(NemDateUtils.DateTimetoMillis1970(TrainDepartureTime) + TAG_TIME_OFFSET_MS);  //rfid.readTags;
                    rfid.startReading();
                    int n_tags = resultAll.Length;
                    EvtLog.WriteEntry("Tags Read : " + n_tags, EventLogEntryType.Information);
                    UInt64 minTagTime = NemDateUtils.DateTimetoMillis1970(TrainArrivalTime) - TAG_TIME_OFFSET_MS;
                    List<tag> tagList = new List<tag>();
                    if (!Directory.Exists(configuration.folder + PATH_TO_DISCARD_TAGS))
                    {
                        Directory.CreateDirectory(configuration.folder + PATH_TO_DISCARD_TAGS);
                    }

                    foreach (tag resultEntry in resultAll)
                    {
                        UInt64 tagTimestamp = (UInt64)NemDateUtils.UniversalTimeMillis(resultEntry.timeStamp);
                        if (tagTimestamp >= minTagTime)
                        {
                            tagList.Add(resultEntry);
                            EvtLog.WriteEntry("Tags Read : " + resultEntry.data, EventLogEntryType.Information);
                        }
                        else
                        {
                            EvtLog.WriteEntry("Tags Discarded : " + resultEntry.data, EventLogEntryType.Information);                          
                            using (var tw = new StreamWriter(discardedTagsFilePath, true))
                            {                               
                                tw.WriteLine("UTC:" + resultEntry.timeStamp + ";" + tagTimestamp + ";" + resultEntry.data);
                            }
                        }

                    }

                    result = tagList.ToArray();
                    if (result == null || result.Length == 0)
                    {
                        EvtLog.WriteEntry("Could not read Tag data ", EventLogEntryType.Error);
                        tagError = true;
                    }
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(tagFileFullPath, true))
                    {

                        file.Write("{\"g_st\":0,\"g_ts\":" + NemDateUtils.UniversalTimeMillis(DateTime.Now).ToString() + ",\"p_un\": [");
                        UInt64 unixTimestamp;
                        String timeStamp;
                        String tagCode;

                        foreach (tag i in result)
                        {
                            unixTimestamp = (UInt64)NemDateUtils.UniversalTimeMillis(i.timeStamp);
                            timeStamp = "{ \"u_ts\": " + "\"" + unixTimestamp + "\",";
                            tagCode = " \"u_tc\": " + "\"" + configuration.prefix + i.data + "\"}";
                            if (n_tags != 1) tagCode = tagCode + ",";
                            file.Write(timeStamp + tagCode);
                            n_tags--;
                        }
                        file.Write("]}");
                    }
                }
                else
                {
                    EvtLog.WriteEntry("Read from file \n" + tagFileFullPath, EventLogEntryType.Information);
                    List<tag> readTags = new List<tag>();
                    try
                    {
                        using (System.IO.StreamReader file = new System.IO.StreamReader(tagFileFullPath, true))
                        {

                            String tagInfoJson = file.ReadToEnd();
                            EvtLog.WriteEntry(tagInfoJson, EventLogEntryType.Information);

                            dynamic tagInfo = JsonConvert.DeserializeObject(tagInfoJson);
                            string json = JsonConvert.SerializeObject(tagInfo, Newtonsoft.Json.Formatting.Indented);
                            EvtLog.WriteEntry(json, EventLogEntryType.Information);
                            
                            foreach (dynamic tagJson in tagInfo.p_un)
                            {
                                EvtLog.WriteEntry("tagValues", EventLogEntryType.Information);
                                tag tag = new tag();
                                tag.data = tagJson.u_tc;
                                EvtLog.WriteEntry(tag.data, EventLogEntryType.Information);
                                //string dateMilis = tagJson.u_ts;
                                //EvtLog.WriteEntry(tagJson.u_ts, EventLogEntryType.Information);
                                //tag.timeStamp = (new DateTime(1970, 1, 1)).AddMilliseconds(long.Parse(dateMilis));                              
                                readTags.Add(tag);
                            }
                        }
                        result = readTags.ToArray();
                        if(result == null || result.Length == 0)
                        {
                            tagError = true;
                        }
                    }catch(Exception ex)
                    {
                        EvtLog.WriteEntry("" + ex.Message, EventLogEntryType.Error);
                        tagError = true;
                    }
                }
                if (isAxlesNumberCorrect(readAxlesNumber, result))
                {
                    EvtLog.WriteEntry("Axle Count correct ", EventLogEntryType.Information);
                    upload = true;
                }
                else
                {
                    EvtLog.WriteEntry("Axle Count incorrect ", EventLogEntryType.Error);
                }
            }
            catch (Exception ex)
            {
                EvtLog.WriteEntry("Error retrieving tag data " + ex.Message, EventLogEntryType.Error);
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(tagFileFullPath, true))
                {
                    String time = NemDateUtils.UniversalTimeMillis(DateTime.Now).ToString();
                    file.Write("{\"g_st\":0,\"g_ts\":" + time  + ",\"p_un\": []}");
                }
                tagError = true;
            }
            finally 
            {
                EvtLog.WriteEntry("Moving files : " + "\n" + e.Name + "\n" + tagFileName, EventLogEntryType.Information);
                try
                {
                    //FileInfo csvFileInfo = new FileInfo(e.FullPath);
                    //Move files to upload folder
                    if (!Directory.Exists(configuration.folder + PATH_TO_UPLOAD_FILES))
                    {
                        Directory.CreateDirectory(configuration.folder + PATH_TO_UPLOAD_FILES);
                    }
                    if (!Directory.Exists(configuration.folder + PATH_TO_AXLE_NUMBER_ERROR))
                    {
                        Directory.CreateDirectory(configuration.folder + PATH_TO_AXLE_NUMBER_ERROR);
                    }
                    if (!Directory.Exists(configuration.folder + PATH_TO_RFID_ERROR))
                    {
                        Directory.CreateDirectory(configuration.folder + PATH_TO_RFID_ERROR);
                    }

                    if (upload)
                    {
                        if (File.Exists(configuration.folder + PATH_TO_UPLOAD_FILES + @"\" + e.Name)) File.Delete(configuration.folder + PATH_TO_UPLOAD_FILES + @"\" + e.Name);
                        if (File.Exists(configuration.folder + PATH_TO_UPLOAD_FILES + @"\" + tagFileName)) File.Delete(configuration.folder + PATH_TO_UPLOAD_FILES + @"\" + tagFileName);
                        File.Move(e.FullPath, configuration.folder + PATH_TO_UPLOAD_FILES + @"\" + e.Name);
                        File.Move(tagFileFullPath, configuration.folder + PATH_TO_UPLOAD_FILES + @"\" + tagFileName);
                    }
                    else if (tagError)
                    {
                        if (File.Exists(configuration.folder + PATH_TO_RFID_ERROR + @"\" + e.Name)) File.Delete(configuration.folder + PATH_TO_RFID_ERROR + @"\" + e.Name);
                        if (File.Exists(configuration.folder + PATH_TO_RFID_ERROR + @"\" + tagFileName)) File.Delete(configuration.folder + PATH_TO_RFID_ERROR + @"\" + tagFileName);
                        File.Move(e.FullPath, configuration.folder + PATH_TO_RFID_ERROR + @"\" + e.Name);
                        File.Move(tagFileFullPath, configuration.folder + PATH_TO_RFID_ERROR + @"\" + tagFileName);
                    }
                    else
                    {
                        if (File.Exists(configuration.folder + PATH_TO_AXLE_NUMBER_ERROR + @"\" + e.Name)) File.Delete(configuration.folder + PATH_TO_AXLE_NUMBER_ERROR + @"\" + e.Name);
                        if (File.Exists(configuration.folder + PATH_TO_AXLE_NUMBER_ERROR + @"\" + tagFileName)) File.Delete(configuration.folder + PATH_TO_AXLE_NUMBER_ERROR + @"\" + tagFileName);
                        File.Move(e.FullPath, configuration.folder + PATH_TO_AXLE_NUMBER_ERROR + @"\" + e.Name);
                        File.Move(tagFileFullPath, configuration.folder + PATH_TO_AXLE_NUMBER_ERROR + @"\" + tagFileName);
                    }
                }
                catch (Exception ex)
                {
                    EvtLog.WriteEntry("Error Moving File" + ex.Message, EventLogEntryType.Error);
                }               
            }
        }

        private static bool isAxlesNumberCorrect(string readAxlesNumber, tag[] tagInfo)
        {
            int axleNumber ;
            if (Int32.TryParse(readAxlesNumber, out axleNumber) == false)
            {
                axleNumber = -1;
            }
            int tagAxleNumber = axleNumberFromReadTags(tagInfo);
            EvtLog.WriteEntry("Axles read " + axleNumber + " expected :" + tagAxleNumber, EventLogEntryType.Information);
            return tagAxleNumber == axleNumber;
        }

        private static int axleNumberFromReadTags(tag[] tagInfo)
        {
            List<tag> unitList = new List<tag>();
            int axleNumber = 0;

            if (tagInfo != null)
            {
                foreach (tag readTag in tagInfo)
                {
                    if (unitList.Count <= 0)
                    {
                        unitList.Add(readTag);
                    }
                    else
                    {
                        bool tagAlreadyIn = false;
                        foreach (tag unit in unitList)
                        {
                            if (unit.data.Substring(8).Equals(readTag.data.Substring(8)) && unit.data.Substring(0, 4).Equals(readTag.data.Substring(0, 4)))
                            {
                                tagAlreadyIn = true;
                            }
                        }
                        if (!tagAlreadyIn)
                        {
                            unitList.Add(readTag);
                        }
                    }
                }
                foreach (tag unit in unitList)
                {
                    string unitfleet = unit.data.Substring(0, 4);
                    foreach (fleetAxles fleetCfg in configuration.fleetCfg)
                    {
                        if (fleetCfg.fleetPrefix.Equals(unitfleet))
                        {
                            axleNumber += fleetCfg.axleNumber;
                        }
                    }
                }
            }
            return axleNumber;

        }

        protected static void OnTimer(Object source, ElapsedEventArgs e)
        {

            if (rfid.isConnected == true)
            {
                try
                {
                    rfid.checkConnection();
                }
                catch (Exception ex)
                {
                    rfid.closeConnection();
                    EvtLog.WriteEntry("RFID Error: Connection lost " + ex.Message, EventLogEntryType.Error);
                }

            }
            else
            {
                try
                {
                    timer.Stop();
                    EvtLog.WriteEntry("Trying to Connect", EventLogEntryType.Information);
                    rfid.openConnection(configuration.IPaddress, configuration.port);
                    EvtLog.WriteEntry("Connected to : " + configuration.IPaddress + ":" + configuration.port, EventLogEntryType.Information);
                    rfid.startReading();
                }
                catch (Exception ex)
                {
                    EvtLog.WriteEntry("RFID Error: " + ex.Message, EventLogEntryType.Error);
                    Thread.Sleep(RFID_RECONECT);
                }
                finally
                {
                    timer.Start();
                }
            }           
        }

        protected static void Upload(Object source, ElapsedEventArgs e) 
        {
            uptimer.Stop();
            
            try
            {
                if (Directory.Exists(configuration.folder + PATH_TO_UPLOAD_FILES))
                {
                    string[] filesToUpload = Directory.GetFiles(configuration.folder + PATH_TO_UPLOAD_FILES + @"\", "*.csv");
                    String zipFilePath;

                    if (filesToUpload.Length > 0)
                    {
                        foreach (String file in filesToUpload)
                        {
                            int index = file.LastIndexOf('.'); ;
                            String tagFileFullPath = (index == -1 ? file : file.Substring(0, index)) + "_tags.json";
                            EvtLog.WriteEntry("Uploading File : " + "\n" + file + "\n" + tagFileFullPath, EventLogEntryType.Information);
                            //EvtLog.WriteEntry("Uploading File : " + tagFileFullPath, EventLogEntryType.Information);
                            if (File.Exists(tagFileFullPath))
                            {
                                List<String> files = new List<String>();
                                files.Add(file);
                                files.Add(tagFileFullPath);                              
                                FilesUploader uploader = new FilesUploader();
                                Task<HttpResponseMessage> response = uploader.UploadFiles(files, configuration.url, configuration.user, configuration.password, true, UPLOAD_TIMEOUT);
                                HttpResponseMessage result = response.Result;
                                HttpContent responseContent = result.Content;

                                //Console.WriteLine(statusCode);
                                String stringContents = "";
                                if (responseContent != null)
                                {
                                    Task<String> stringContentsTask = responseContent.ReadAsStringAsync();
                                    stringContents = stringContentsTask.Result;
                                }
                               
                                switch (result.StatusCode) { 
                                    case System.Net.HttpStatusCode.OK:
                                    case System.Net.HttpStatusCode.ServiceUnavailable: //Paliar el  "Network Error (tcp_error)  A communication error occurred: "Operation timed out" "

                                        if (!configuration.delete)
                                        {
                                            if (!Directory.Exists(configuration.measuredPath))
                                            {
                                                Directory.CreateDirectory(configuration.measuredPath);
                                            }
                                            zipFilePath = Path.GetDirectoryName(file) + @"\" + Path.GetFileNameWithoutExtension(file) + ".zip";
                                            using (ZipArchive zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                                            {
                                                zip.CreateEntryFromFile(file, Path.GetFileName(file));
                                                zip.CreateEntryFromFile(tagFileFullPath, Path.GetFileName(tagFileFullPath));
                                            }
                                            if (File.Exists(configuration.measuredPath + @"\" + Path.GetFileName(zipFilePath)))
                                            {
                                                File.Delete(configuration.measuredPath + @"\" + Path.GetFileName(zipFilePath));
                                            }
                                            File.Move(zipFilePath, configuration.measuredPath + @"\" + Path.GetFileName(zipFilePath));
                                        }
                                        foreach (String uploadedFile in files)
                                        {
                                            File.Delete(uploadedFile);
                                        }
                                        if (stringContents.Contains("tcp_error"))
                                        {
                                            EvtLog.WriteEntry("Files Uploaded but a TCP_ERROR occurred", EventLogEntryType.Information);
                                        }
                                        else {
                                            EvtLog.WriteEntry("Files Uploaded ", EventLogEntryType.Information);
                                        }
                                        
                                        break;
                                    case System.Net.HttpStatusCode.BadGateway:

                                        EvtLog.WriteEntry(@"502 error \n", EventLogEntryType.Information);
                                        EvtLog.WriteEntry(stringContents, EventLogEntryType.Information);

                                        if (!Directory.Exists(configuration.folder + @"\Error502"))
                                        {
                                            Directory.CreateDirectory(configuration.folder + @"\Error502");
                                        }
                                        zipFilePath = Path.GetDirectoryName(file) + @"\" + Path.GetFileNameWithoutExtension(file) + ".zip";
                                        using (ZipArchive zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                                        {
                                            zip.CreateEntryFromFile(file, Path.GetFileName(file));
                                            zip.CreateEntryFromFile(tagFileFullPath, Path.GetFileName(tagFileFullPath));
                                        }
                                        if (File.Exists(configuration.folder + @"\Error502" + @"\" + Path.GetFileName(zipFilePath))) File.Delete(configuration.folder + @"\Error502" + @"\" + Path.GetFileName(zipFilePath));
                                        File.Move(zipFilePath, configuration.folder + @"\Error502" + @"\" + Path.GetFileName(zipFilePath));

                                        EvtLog.WriteEntry("Files move to Error502 Folder ", EventLogEntryType.Information);

                                        foreach (String uploadedFile in files)
                                        {
                                            File.Delete(uploadedFile);
                                        }
                                        
                                        break;

                                    case System.Net.HttpStatusCode.NotFound:
                                    case System.Net.HttpStatusCode.GatewayTimeout:
                                    //case System.Net.HttpStatusCode.InternalServerError:
                                    case System.Net.HttpStatusCode.RequestTimeout:
                                   
                                    
                                        EvtLog.WriteEntry("Error Uploading Files: " + result.StatusCode + '\n' + stringContents, EventLogEntryType.Error);
                                        Thread.Sleep(COMM_RETRY);
                                        
                                        break;
                                    default:

                                        EvtLog.WriteEntry("Error Uploading Files: " + "\n" + file + "\n" + tagFileFullPath + "\n" + result.StatusCode + '\n' + stringContents, EventLogEntryType.Error);
                                        EvtLog.WriteEntry("Moving Files to  " + PATH_TO_UPLOAD_ERROR, EventLogEntryType.Error);
                                        try
                                        {
                                            if (!Directory.Exists(configuration.folder + PATH_TO_UPLOAD_ERROR))
                                            {
                                                Directory.CreateDirectory(configuration.folder + PATH_TO_UPLOAD_ERROR);
                                            }
                                            zipFilePath = Path.GetDirectoryName(file) + @"\" + Path.GetFileNameWithoutExtension(file) + ".zip";
                                            using (ZipArchive zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                                            {
                                                zip.CreateEntryFromFile(file, Path.GetFileName(file));
                                                zip.CreateEntryFromFile(tagFileFullPath, Path.GetFileName(tagFileFullPath));
                                            }
                                            if (File.Exists(configuration.folder + PATH_TO_UPLOAD_ERROR + @"\" + Path.GetFileName(zipFilePath))) File.Delete(configuration.folder + PATH_TO_UPLOAD_ERROR + @"\" + Path.GetFileName(zipFilePath));
                                            File.Move(zipFilePath, configuration.folder + PATH_TO_UPLOAD_ERROR + @"\" + Path.GetFileName(zipFilePath));
                                        
                                        }
                                        catch (Exception ex)
                                        {
                                            EvtLog.WriteEntry("Error Moving Files: " + ex.Message, EventLogEntryType.Error);
                                            
                                        }
                                        finally
                                        {
                                            foreach (String uploadedFile in files)
                                            {
                                                File.Delete(uploadedFile);
                                            }
                                        }

                                        break;
                                }

                            }
                        }
                    }
                }
            }
            catch (AggregateException exception)
            {
                foreach (Exception ex in exception.InnerExceptions)
                    EvtLog.WriteEntry("Error Uploading Files: " + ex.Message +'\n' + ex.Source, EventLogEntryType.Error);
                Thread.Sleep(COMM_RETRY);
            }
            catch (TaskCanceledException ex)
            {
                EvtLog.WriteEntry("Error Uploading Files: Timeout/Canceled \n" + ex.Message, EventLogEntryType.Error);
                Thread.Sleep(COMM_RETRY);
            }
            catch (Exception ex)
            {

                EvtLog.WriteEntry("Error Uploading Files: General Error\n" + ex.Message, EventLogEntryType.Error);
                Thread.Sleep(COMM_RETRY);
            }
            finally 
            {
                uptimer.Start();
            }
        }
        protected static void OnHeartbeat(Object source, ElapsedEventArgs e)
        {
            heartbeatTimer.Stop();
            String x_nem_datetime = NemDateUtils.UniversalTimeMillis(DateTime.Now).ToString();
            Uri url = new Uri(configuration.statusUrl + "/" + x_nem_datetime.ToString());
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, url);
            requestMessage.Headers.ExpectContinue = false;
                        
            String payload = "{\"g_st\":0,\"p_ts\":" + x_nem_datetime.ToString()  + ",\"p_st\":1}";


            byte[] bAuthorization = Encoding.UTF8.GetBytes(x_nem_datetime.ToString() + url.ToString() + payload + configuration.password);
            String authorization = "NEM " + configuration.user + ":" + Encrypter.EncryptSHA1Message(bAuthorization);

            requestMessage.Headers.Add("x-nem-datetime", x_nem_datetime);
            requestMessage.Headers.Add("authorization", authorization);
            requestMessage.Content = new StringContent(payload, Encoding.UTF8,"application/json");

            HttpClient httpClient = new HttpClient();
            HttpStatusCode statusCode = HttpStatusCode.BadRequest;
            try
            {
                EvtLog.WriteEntry("HeartBeat Send ", EventLogEntryType.Information);
                Task<HttpResponseMessage> httpRequest = httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, CancellationToken.None);
                HttpResponseMessage httpResponse = httpRequest.Result;
                statusCode = httpResponse.StatusCode;
                HttpContent responseContent = httpResponse.Content;



                if (responseContent != null && statusCode != HttpStatusCode.OK)
                {
                    Task<String> stringContentsTask = responseContent.ReadAsStringAsync();
                    String stringContents = stringContentsTask.Result;
                    EvtLog.WriteEntry("HeartBeat response: " + statusCode + "\n" + stringContents, EventLogEntryType.Warning);
                }
                else {
                    EvtLog.WriteEntry("HeartBeat response: " + statusCode, EventLogEntryType.Information);
                }
                
            }
            catch (AggregateException exception)
            {
                foreach (Exception ex in exception.InnerExceptions)
                    EvtLog.WriteEntry("Error sending HeartBeat: " + ex.Message + '\n' + ex.Source, EventLogEntryType.Warning);
            }
            catch (TaskCanceledException ex)
            {
                EvtLog.WriteEntry("Error sending HeartBeat: Timeout/Canceled \n" + ex.Message, EventLogEntryType.Warning);
            }
            catch (Exception ex)
            {

                EvtLog.WriteEntry("Error sending HeartBeat: General Error\n" + ex.Message, EventLogEntryType.Warning);
            }
            finally {
                heartbeatTimer.Start();
            }

        }
       
    }
}
