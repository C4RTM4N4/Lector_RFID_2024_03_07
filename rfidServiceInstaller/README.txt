#####################################################################################################
#    NEM SOLUTIONS  2014                                                                            #
#    rfid Service Installer                                                                         #
#                                                                                                   #
#####################################################################################################

The service will start upon the start of the windows machine (no login necessary). Once active it will monitor the configured folder for the creation of .csv files. For each new .csv file it will generate a new file with the same name plus _tag.json on the same folder. This file will contain the tag read since the last file generated in json format.  Logging of errors and information is redirected to the windows Event Viewer. 
 INSTALLATION INSTRUCTIONS
 To install the rfid service, follow the next instructions:
- Open a command line windows (cmd) and cd into the directory containing the setup.bat installation script.
- Execute the setup.bat script and follow the on screen instructions. 
The script will ask for the director where the rfid binary and configuration file will be located. After that the files will be copied to that location and the service will be registered.  
By default this service is installed with the LOCAL SERVICE account. This account must be added to the accounts able to modify and control de folder where the csv files will be created. Another way to grant access to the folder is to change the service account; this can be changed from the windows service controller (services.msc). If the default account is change it must be to an account that has also network access capabilities. 
SERVICE CONFIGURATION
The configuration options of the service are stored in the file �rfidService_cfg.xml� located in the installation directory.  This file is a xml file with the following parameters. 

<?xml version="1.0" encoding="utf-8"?>
<!-- RFID reader Service configuration file V1.0-->
<configuration>
  <!-- IP address of the rfid reader -->
  <IPaddress>0.0.0.0</IPaddress>
  <!-- RFID connection port -->
  <port>2189</port>
  <!-- RFID commands Response Timeout (ms) -->
  <responseTimeout>100000</responseTimeout>
  <!-- Folder to check for new csv files -->
  <folder>C:\Users</folder>
  <!-- Delete files after upload  true/false-->
  <delete>true</delete>
  <!-- url of the Rest web service http:// -->
  <url>http://</url>
  <!-- user name-->
  <user>user</user>
  <!-- password-->
  <password>pass</password>
  <statusUrl>http://</statusUrl>
  <!-- time between status checks minutes-->
  <heartbeat>1</heartbeat>
</configuration>

IPaddress: IP address of the RFID reader in x.x.x.x format
Port: the port where the RFID reader will be listening to requests.
responseTimeout: Delay between the command send a receive a response from the RFID reader.
Folder: The folder on the local machine where the service will be watching for the creation of .csv files. 
delete: true/false if the measurement files need to be deleted after a succesful upload to the server. If false the measurements will be stored in a folder named meassured inside the main folder.
url: Url of the server where the measurements will be sent. 
user: User name to access the remote server.
password: Password to access the remote server encryted with the ARC4 encription algorith.
statusUrl: url where to send the status REST service
heartbeat: period between status calls

SERVICE UNINSTALL
To uninstall the service, open a command line windows (cmd) on the directory containing the setup.bat script. Type setup.bat u , and the uninstallation process will begin. 


rfidServiceInstaller
    README.txt
    setup.bat
    /utils
        ARC4encript.exe
    /bin
        InstallUtil.exe
        Intermec.DataCollection.RFID.BasicBRI.dll
        rfidService.exe
        rfidService_cfg.xml
        




