using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace PCInventory.Models
{
    public class ComputerInfo : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _status;
        private string _lastUpdate;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public string ComputerName { get; set; }
        public string IPAddress { get; set; }
        public object Tag { get; set; }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public string OS { get; set; }
        public string OSVersion { get; set; }
        public string OSArchitecture { get; set; }
        public string CPU { get; set; }
        public string CPUCores { get; set; }
        public string CPUThreads { get; set; }
        public string CPUClockSpeed { get; set; }
        public string RAM { get; set; }
        public string RAMTotalGB { get; set; }
        public string RAMModules { get; set; }
        public string Motherboard { get; set; }
        public string MotherboardVersion { get; set; }
        public string BIOS { get; set; }
        public string BIOSVersion { get; set; }
        public string GPU { get; set; }
        public string DiskInfo { get; set; }
        public string NetworkAdapters { get; set; }
        public string SoundDevices { get; set; }
        public string InstalledPrograms { get; set; }
        public string Antivirus { get; set; }
        public string Browsers { get; set; }
        public string OfficeInstalled { get; set; }
        public string Drivers { get; set; }
        public string DriverCount { get; set; }
        public string USBDevices { get; set; }
        public string Printers { get; set; }
        public string Monitors { get; set; }
        public string MACAddress { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }

        public string LastUpdate
        {
            get => _lastUpdate;
            set
            {
                _lastUpdate = value;
                OnPropertyChanged(nameof(LastUpdate));
            }
        }

        public string UniqueId { get; set; }
        public List<PassportData> History { get; set; } = new List<PassportData>();

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class HardwareInfo
    {
        public string CPU { get; set; }
        public string CPUCores { get; set; }
        public string CPUThreads { get; set; }
        public string CPUClockSpeed { get; set; }
        public string RAM { get; set; }
        public int RAMSizeGB { get; set; }
        public string RAMModules { get; set; }
        public string Motherboard { get; set; }
        public string MotherboardVersion { get; set; }
        public string BIOS { get; set; }
        public string BIOSVersion { get; set; }
        public string GPU { get; set; }
        public List<DiskInfo> Disks { get; set; } = new List<DiskInfo>();
        public string OS { get; set; }
        public string OSVersion { get; set; }
        public string OSArchitecture { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public List<NetworkAdapterInfo> NetworkAdapters { get; set; } = new List<NetworkAdapterInfo>();
        public List<string> SoundDevices { get; set; } = new List<string>();
    }

    public class DiskInfo
    {
        public string DriveLetter { get; set; }
        public string VolumeName { get; set; }
        public long TotalSizeGB { get; set; }
        public long FreeSpaceGB { get; set; }
        public string FileSystem { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }

        public override string ToString()
        {
            return $"{DriveLetter} - {VolumeName} ({TotalSizeGB} GB, свободно {FreeSpaceGB} GB)";
        }
    }

    public class NetworkAdapterInfo
    {
        public string Name { get; set; }
        public string MACAddress { get; set; }
        public string IPAddress { get; set; }
    }

    public class ScanResult
    {
        public string IPAddress { get; set; }
        public string ComputerName { get; set; }
        public bool IsOnline { get; set; }
        public long ResponseTime { get; set; }
        public string MACAddress { get; set; }
    }

    public class PassportData
    {
        public string UniqueId { get; set; }
        public DateTime CollectedAt { get; set; }
        public string ComputerName { get; set; }
        public string IPAddress { get; set; }
        public Dictionary<string, string> Hardware { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Software { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Drivers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Peripherals { get; set; } = new Dictionary<string, string>();

        public List<string> CompareTo(PassportData other)
        {
            var differences = new List<string>();
            if (other == null)
            {
                differences.Add("Нет данных для сравнения");
                return differences;
            }

            CompareDictionaries(Hardware, other.Hardware, "Аппаратная конфигурация", differences);
            CompareDictionaries(Software, other.Software, "Программное обеспечение", differences);
            CompareDictionaries(Drivers, other.Drivers, "Драйверы", differences);
            CompareDictionaries(Peripherals, other.Peripherals, "Периферийные устройства", differences);
            return differences;
        }

        private void CompareDictionaries(Dictionary<string, string> current, Dictionary<string, string> old, string section, List<string> differences)
        {
            foreach (var kv in current)
            {
                if (old.ContainsKey(kv.Key))
                {
                    if (old[kv.Key] != kv.Value)
                    {
                        differences.Add($"[{section}] ИЗМЕНЕНО: {kv.Key}\n  Было: {old[kv.Key]}\n  Стало: {kv.Value}");
                    }
                }
                else
                {
                    differences.Add($"[{section}] ДОБАВЛЕНО: {kv.Key} = {kv.Value}");
                }
            }

            foreach (var kv in old)
            {
                if (!current.ContainsKey(kv.Key))
                {
                    differences.Add($"[{section}] УДАЛЕНО: {kv.Key} = {kv.Value}");
                }
            }
        }
    }

    public class AppSettings
    {
        public int NetworkTimeout { get; set; } = 100;
        public bool ResolveNames { get; set; } = true;
        public bool UseWMI { get; set; } = true;
        public string Domain { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        public string ExportPath { get; set; } = "";
        public bool ExportHTML { get; set; } = true;
        public bool ExportPDF { get; set; } = true;
        public bool ExportJSON { get; set; } = true;
        public bool ExportExcel { get; set; } = false;

        public int UpdateInterval { get; set; } = 60;
        public bool AutoRefresh { get; set; } = false;

        public void Save(string filePath)
        {
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(filePath, json);
            }
            catch { }
        }

        public static AppSettings Load(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(filePath);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                catch { }
            }
            return new AppSettings();
        }
    }
}