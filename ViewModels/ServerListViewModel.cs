using CsvHelper;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VPN_Gate_Servers_WPF.Base;
using VPN_Gate_Servers_WPF.Models;

namespace VPN_Gate_Servers_WPF.ViewModels
{
    public class ServerListViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<ServerModel> Servers { get; set; } = null!;

        private string _serverLogin = "vpn";
        private string _serverPassword = "vpn";

        //combobox
        private TextBlock _selectedTypeOfServer = null!;
        public TextBlock SelectedTypeOfServer
        {
            get { return _selectedTypeOfServer; }
            set
            {
                _selectedTypeOfServer = value;
            }
        }

        //datagrid
        private ServerModel _selectedServer = null!;
        public ServerModel SelectedServer
        {
            get { return _selectedServer; }
            set
            {
                _selectedServer = value;
            }
        }

        private string _connectionStatus = "Disconnected";

        public string ConnectionStatus
        {
            get { return _connectionStatus; }
            set
            {
                _connectionStatus = value;
            }
        }

        private string _connectionStatusServerName = "";

        public string ConnectionStatusServerName
        {
            get { return _connectionStatusServerName; }
            set
            {
                _connectionStatusServerName = value;
            }
        }

        public RelayCommand ConnectCommand { get; set; }
        public RelayCommand DisconnectCommand { get; set; }

        public RelayCommand AddListOfServerCommand { get; set; }

        public ServerListViewModel()
        {

            //-------------------- add list of server to datagrid
            AddListOfServerCommand = new RelayCommand(async o =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "CSV Files (*.csv)|*.csv";
                openFileDialog.FilterIndex = 2;

                if (openFileDialog.ShowDialog() == true)
                {
                    // using (var reader = new StreamReader(@"C:\Users\Admin\Desktop\data1.csv"))
                    using (var reader = new StreamReader(@$"{System.IO.Path.GetFullPath(openFileDialog.FileName)}"))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        var record = csv.GetRecords<ServerModel>();
                        Servers = new ObservableCollection<ServerModel>(record);
                    }
                    MessageBox.Show($"You got a list of servers ", "Server list", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            });



            //------------------connect to selected VPN server-----------------------------------------------------------------------------------------------------------------
            ConnectCommand = new RelayCommand(async o =>
            {
                if (Servers == null)
                {
                    MessageBox.Show("If you didn't add list of servers, please do it. Click button 'Add List of Servers from CSV file' ", "Server list", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedTypeOfServer == null)
                {
                    MessageBox.Show("Please choose one of the server types .", "Server type", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
               
                if (SelectedServer == null)
                {
                    MessageBox.Show("Please choose one of the VPN servers.", "Selected server ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                //if (SelectedTypeOfServer.Text == "SSTP")
                //    MessageBox.Show($"Success! You connected MS-SSTP  server.", "Connection Server", MessageBoxButton.OK, MessageBoxImage.Information);
                //if (SelectedTypeOfServer.Text == "L2TP")
                //    MessageBox.Show($"Success! You connected L2TP server.", "Connection Server", MessageBoxButton.OK, MessageBoxImage.Information);

                if (ConnectionStatus == "Connected")
                    MessageBox.Show("You should disconnect current connection and then connect to another VPN server.", "Connection Server", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                {
                    if (SelectedTypeOfServer != null && SelectedServer != null)
                    {

                        ServerBuilder(SelectedTypeOfServer.Text);

                        await Task.Run(() =>
                                {
                                    ConnectionStatus = "Connecting...";
                                    var process = new Process();
                                    process.StartInfo.FileName = "cmd.exe";
                                    process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

                                    Application.Current.Dispatcher.Invoke(
                           new ThreadStart(
                               delegate
                               {
                                   process.StartInfo.ArgumentList.Add(@$"/c rasdial MyServer_{SelectedServer.CountryLong}_{SelectedServer.HostName}_{SelectedTypeOfServer.Text} {_serverLogin} {_serverPassword} /phonebook:./VPN/{SelectedServer.HostName}_{SelectedTypeOfServer.Text}.pbk");
                                   process.Start();
                                   process.WaitForExit();
                               })
                           );

                                    switch (process.ExitCode)
                                    {
                                        case 0:
                                            MessageBox.Show($"Success! You connected to {SelectedServer.HostName} server.", "Connection Server", MessageBoxButton.OK, MessageBoxImage.Information);
                                            ConnectionStatus = "Connected";
                                            ConnectionStatusServerName = $"to {SelectedServer.HostName} server";
                                            break;
                                        case 691:
                                            MessageBox.Show("Wrong credentials!");
                                            ConnectionStatus = "Wrong credentials!";
                                            break;
                                        default:
                                            MessageBox.Show($"Error connecting to {SelectedServer.HostName} {SelectedServer.IP}: {process.ExitCode}.", "Process Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                            ConnectionStatus = "Error Connection";
                                            break;
                                    }
                                });

                    }
                }
            });

            //--------------------- disconnect from selected VPN server
            DisconnectCommand = new RelayCommand(o =>
                {
                    if (ConnectionStatus == "Connected")
                    {
                        Task.Run(() =>
                        {
                            var process = new Process();
                            process.StartInfo.FileName = "cmd.exe";
                            //  rasdial / d - disconnect vpn server
                            process.StartInfo.ArgumentList.Add(@"/c rasdial/d");
                            process.Start();
                            process.WaitForExit();
                            MessageBox.Show("Connection is disconnected! You can select any VPN server to connection.", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
                            ConnectionStatus = "Disconnected";
                            ConnectionStatusServerName = "";
                        });
                    }
                    else
                        MessageBox.Show("Connection is not created.Choose type of VPN server, select VPN Server from List and click Connect to VPN Server.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
        }

        // create file .pbk with connection configuration
        private void ServerBuilder(string VPNType)
        {
            var address = SelectedServer.HostName;
            var FolderPath = $"{Directory.GetCurrentDirectory()}/VPN";
            var PbkPath = $"{FolderPath}/{address}_{VPNType}.pbk";

            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
                MessageBox.Show("File with connection's configuration is created!", "Create File VPN configuration", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            if (File.Exists(PbkPath))
            {
                MessageBox.Show("File with connection's configuration already exists!", "Create File VPN configuration", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[MyServer_{SelectedServer.CountryLong}_{SelectedServer.HostName}_{VPNType}]");
            sb.AppendLine("MEDIA=rastapi");
            sb.AppendLine("Port=VPN2-0");
            sb.AppendLine($"Device=WAN Miniport ({VPNType})");
            sb.AppendLine("DEVICE=vpn");
            sb.AppendLine($"PhoneNumber={address}.opengw.net");
            File.WriteAllText(PbkPath, sb.ToString());
        }
    }
}
