using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Usługa
{
    [RunInstaller(true)]
    public partial class Instalator : Installer
    {
        public Instalator()
        {
            ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;

            ServiceInstaller serviceInstaller = new ServiceInstaller();
            serviceInstaller.ServiceName = Usluga.serviceName;
            serviceInstaller.StartType = ServiceStartMode.Manual;
            serviceInstaller.DisplayName = Usluga.displayedServiceName;
            serviceInstaller.Description = Usluga.serviceDescription;

            this.Installers.Add(serviceProcessInstaller);
            this.Installers.Add(serviceInstaller);
        }
    }
}
