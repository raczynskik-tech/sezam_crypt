using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Biblioteka;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Usługa
{
    public partial class Usluga : ServiceBase
    {
        //Pola zawierające odpowiednie nazwy - przed pracą zostaną wypełnione zawartością z pliku konfiguracyjnego
        public const string serviceName = "SezamCRYPT";
        public const string displayedServiceName = "Usługa SezamCRYPT";
        public const string serviceDescription = "Usługa szyfrująca zwykłe pliki takie, jak pliki tekstowe .txt";
        private string logName = "";
        private string sourceName = "";

        //Dziennik
        private EventLog eventLog;

        //Timer
        private Timer timer;
        private TimeSpan timespan;

        //Ścieżka do katalogu pracy
        private string rootPath;
        private string desktopPath;

        public Usluga()
        {
            InitializeComponent();

            //Nazwanie usługi
            this.ServiceName = serviceName;

            //Odczytanie danych z pliku konfiguracyjnego
            NameValueCollection nameValueCollection = ConfigurationManager.AppSettings;
            logName = nameValueCollection["LogName"];
            sourceName = nameValueCollection["SourceName"];

            //Przygotowanie do pracy dziennika
            if (!EventLog.SourceExists(sourceName))
            {
                EventLog.CreateEventSource(sourceName, logName);
            }
            eventLog = new EventLog(logName, ".", sourceName);
        }

        protected override void OnStart(string[] args)
        {
            //Zapisanie do dziennika informacji o uruchomieniu usługi
            eventLog.WriteEntry($"Usługa {serviceName} została uruchomiona");

            //Pobranie ścieżki do katalogu pracy i do pulpitu
            rootPath = args[0];
            desktopPath = args[1];

            //Co 15 sekund sprawdza listę plików i przechodzi do działania
            timespan = new TimeSpan(0, 0, 15);
            timer = new Timer(o => { ActionToDo(); }, null, timespan, timespan);
        }

        protected override void OnStop()
        {
            //Zapisanie do dziennika informacji o zatrzymaniu usługi
            eventLog.WriteEntry($"Usługa {serviceName} została zatrzymana");
        }

        private void ActionToDo()
        {
            //Lista na pliki z katalogu pracy
            List<FileInfo> files = new List<FileInfo>();
            try
            {
                //Zapis wiadomości do dziennika
                eventLog.WriteEntry($"Skanowanie zawartości katalogu: {rootPath}");

                //Pobranie listy plików z katalogu
                files = EncryptDecrypt.getFilesFromDir(rootPath);
                foreach (var file in files)
                {
                    //Szyfrowanie pliku
                    if (file.FullName.EndsWith(".txt"))
                    {
                        //Przygotowanie zmiennych do pracy
                        string path = EncryptDecrypt.encryptFile(file);
                        string newNameFile = EncryptDecrypt.preparePathToEncrypt(file.Name);

                        //Zapis wiadomości do dziennika
                        eventLog.WriteEntry($"Plik do zaszyfrowania: {file.Name}");
                        
                        //Szyfrowanie + przeniesienie pliku na pulpit użytkownika
                        EncryptDecrypt.deleteFile(file.FullName);
                        EncryptDecrypt.moveFile(path, Path.Combine(desktopPath, newNameFile));

                        //Zapis wiadomości do dziennika
                        eventLog.WriteEntry($"Zakończono szyfrowanie pliku: {file.Name}");
                    }

                    //Rozszyfrowanie pliku
                    if (file.FullName.EndsWith(".txtsc"))
                    {
                        //Przygotowanie zmiennych do pracy
                        string path = EncryptDecrypt.decryptFile(file);
                        string newNameFile = EncryptDecrypt.preparePathToDecrypt(file.Name);

                        //Zapis wiadomości do dziennika
                        eventLog.WriteEntry($"Plik do rozszyfrowania: {file.Name}");

                        //Deszyfrowanie + przeniesienie pliku na pulpit użytkownika
                        EncryptDecrypt.deleteFile(file.FullName);
                        EncryptDecrypt.moveFile(path, Path.Combine(desktopPath, newNameFile));

                        //Zapis wiadomości do dziennika
                        eventLog.WriteEntry($"Zakończono rozszyfrowanie pliku: {file.Name}");
                    }
                }
            }
            catch (NoFilesInDirException) { } //Jeśli brak plików w katalogu nic się nie dzieje
            catch(Exception exception)
            {
                throw exception;
            }
        }
    }
}
