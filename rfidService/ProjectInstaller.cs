using System;
using System.Collections;
using System.Configuration.Install;
using System.ServiceProcess;
using System.ComponentModel;


namespace rfidService
{
    [RunInstallerAttribute(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceInstaller serviceInstaller;
        private ServiceProcessInstaller processInstaller;
        
        public ProjectInstaller()
        {

            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            // Service will run under system account
            //# Service Account Information
            processInstaller.Account = ServiceAccount.LocalService;

            // Service will have Start Type of Manual
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            serviceInstaller.ServiceName = "rfid Service";
            serviceInstaller.Description = "Watch a folder for added csv files and generates rfid data from a IF2 RFID reader";
            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
        }
    }
}
