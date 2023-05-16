using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Biblioteka;

namespace ZarządzanieUsługą
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Aktywność usługi
        private bool isRunning = false;
        private const string nazwaUslugi = "SezamCRYPT";

        //Zarządzanie usługą
        private ServiceController serviceController;
        private ServiceControllerStatus serviceControllerStatus;

        //Timer do sprawdzania stanu usługi i logów dziennika
        private DispatcherTimer dispatcherTimer;
        private DispatcherTimer dispatcherTimerLog;

        //Nazwa i ścieżka katalogu pracy
        private string srcDirName = string.Empty;
        private string srcDir = string.Empty;

        //Lista wpisów do dziennika
        private ObservableCollection<LogEntry> logEntries;

        //Dziennik zdarzeń
        private EventLog eventLog;

        //Nazwa dziennika
        private string logName = "";

        public MainWindow()
        {
            InitializeComponent();

            //Lista z logami z dziennika
            logEntries = new ObservableCollection<LogEntry>();
            logListView.ItemsSource = logEntries;

            //Sortowanie wpisów od najwcześniejszych do najpóźniejszych
            logListView.Items.SortDescriptions.Add(new SortDescription("Date", ListSortDirection.Descending));

            //Odczytanie nazw katalogów do pracy oraz nazwy dziennika i źródła
            NameValueCollection nameValueCollection = ConfigurationManager.AppSettings;
            srcDirName = nameValueCollection["SourceDirectory"];
            logName = nameValueCollection["LogName"];

            //Wygenerowanie ścieżki do katalogów pracy
            srcDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), srcDirName);

            //Wyświetlenie ścieżki użytkownikowi
            sourcePathLabel.Content = srcDir;


            try 
            {
                //Usługa
                serviceController = new ServiceController("SezamCRYPT");
                serviceControllerStatus = serviceController.Status;

                //Ustalenie aktywności początkowej
                if(serviceControllerStatus == ServiceControllerStatus.Running)
                {
                    isRunning = true;
                    changeButtonEnable();
                }
                if(serviceControllerStatus == ServiceControllerStatus.Stopped)
                {
                    isRunning = false;
                    changeButtonEnable();
                }

                //DispatcherTimer i DispatcherTimerLog
                dispatcherTimer = new DispatcherTimer();
                dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 2, 0);
                dispatcherTimer.Tick += DispatcherTimerTick;
                dispatcherTimer.Start();

                dispatcherTimerLog = new DispatcherTimer();
                dispatcherTimerLog.Interval = new TimeSpan(0, 0, 0, 1, 0);
                dispatcherTimerLog.Tick += DispatcherTimerLogTick;
            }
            catch(Exception ex) 
            {
                MessageBox.Show($"{ex.Message}", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                this.Close();
            }

            //Dziennik
            eventLog = new EventLog(logName);
        }

        private void DispatcherTimerLogTick(object sender, EventArgs e)
        {
            try
            {
                //Aktualizacja wpisów z dziennika
                logEntries.Clear();
                foreach (EventLogEntry entry in eventLog.Entries)
                {
                    logEntries.Add(new LogEntry(entry.Message, entry.TimeWritten));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private void DispatcherTimerTick(object sender, EventArgs e)
        {
            try
            {
                //Odświeżenie stanu uruchomienia usługi - w przypadku zmiany stanu -> zmiana dostępności przycisków sterowania
                serviceController.Refresh();
                if (serviceController.Status != serviceControllerStatus)
                {
                    serviceControllerStatus = serviceController.Status;
                    isRunning = serviceControllerStatus == ServiceControllerStatus.Running;
                    changeButtonEnable();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"{ex.Message}", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        async private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            isRunning = true;

            //Uruchomienie usługi
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                //Uruchomienie usługi z nazwą użytkownika - wymagane do pracy!!!
                serviceController.Start(new string[] { srcDir, desktopPath });
                await Task.Run(() => serviceController.WaitForStatus(ServiceControllerStatus.Running));
                changeButtonEnable();

                //Uruchomienie timera logów
                dispatcherTimerLog.Start();
            }
            catch(Exception ex)
            {
                MessageBox.Show($"{ex.Message}", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        async private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            isRunning = false;

            //Zatrzymanie pracy usługi
            try
            {
                serviceController.Stop();
                await Task.Run(() => serviceController.WaitForStatus(ServiceControllerStatus.Stopped));
                changeButtonEnable();

                //Zatrzymanie timera logów
                dispatcherTimerLog.Stop();
            }
            catch(Exception ex)
            {
                MessageBox.Show($"{ex.Message}", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private void changeButtonEnable()
        {
            startButton.IsEnabled = !isRunning;
            stopButton.IsEnabled = isRunning;
        }
    }

    public class LogEntry
    {
        public string Description { set; get; }
        public DateTime Date { set; get; }
        public LogEntry(string description, DateTime date)
        {
            Description = description;
            Date = date;
        }
    }
}