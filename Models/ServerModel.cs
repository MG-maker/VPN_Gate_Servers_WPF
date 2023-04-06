using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPN_Gate_Servers_WPF.Models
{
   public class ServerModel
    {
        public string? HostName { get; set; }
        public string? IP { get; set; }
        public string? Score { get; set; }
        public string? Ping { get; set; }
        public string? Speed { get; set; }
        public string? CountryLong { get; set; }
        public string? CountryShort { get; set; }
        public string? NumVpnSessions { get; set; }
        public string? Uptime { get; set; }
        public string? TotalUsers { get; set; }
        public string? TotalTraffic { get; set; }
        public string? LogType { get; set; }
        public string? Operator { get; set; }
        public string? Message { get; set; }
        public string? OpenVPN_ConfigData_Base64 { get; set; }
    }
}
