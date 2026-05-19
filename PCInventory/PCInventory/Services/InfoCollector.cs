using Microsoft.Win32;
using PCInventory.Models;
using System;
using System.Collections.Generic;
using System.Management;

namespace PCInventory.Services
{
    public class InfoCollector
    {
        public PassportData CollectFullInfo(string computerName = null, string username = null, string password = null, string domain = null)
        {
            if (string.IsNullOrEmpty(computerName) || computerName == "localhost" || computerName == "127.0.0.1")
                computerName = Environment.MachineName;

            var passport = new PassportData
            {
                UniqueId = GenerateUniqueId(),
                CollectedAt = DateTime.Now,
                Hardware = new Dictionary<string, string>(),
                Software = new Dictionary<string, string>(),
                Drivers = new Dictionary<string, string>(),
                Peripherals = new Dictionary<string, string>()
            };

            // Собираем все разделы
            GetHardwareInfo(passport.Hardware);
            GetSoftwareInfo(passport.Software);
            GetDriverInfo(passport.Drivers);
            GetPeripheralInfo(passport.Peripherals);

            return passport;
        }

        private string GenerateUniqueId()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                foreach (var mo in searcher.Get())
                {
                    return $"CPU-{mo["ProcessorId"]}";
                }
            }
            catch { }
            return $"PC-{DateTime.Now:yyyyMMddHHmmss}";
        }

        private void GetHardwareInfo(Dictionary<string, string> hardware)
        {
            try
            {
                // Процессор
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                foreach (var mo in searcher.Get())
                {
                    hardware["Процессор"] = $"{mo["Name"]}";
                    hardware["Ядра процессора"] = $"{mo["NumberOfCores"]}";
                    hardware["Потоки процессора"] = $"{mo["NumberOfLogicalProcessors"]}";
                    hardware["Частота процессора"] = $"{mo["MaxClockSpeed"]} MHz";
                    hardware["Сокет процессора"] = $"{mo["SocketDesignation"]}";
                    break;
                }

                // Оперативная память
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
                long totalMemory = 0;
                int memoryModules = 0;
                foreach (var mo in searcher.Get())
                {
                    totalMemory += Convert.ToInt64(mo["Capacity"]);
                    memoryModules++;
                    hardware[$"Модуль памяти {memoryModules}"] =
                        $"{Convert.ToInt64(mo["Capacity"]) / (1024 * 1024 * 1024)} GB, {mo["Speed"]} MHz, {mo["Manufacturer"]}";
                }
                hardware["Общий объем памяти"] = $"{totalMemory / (1024 * 1024 * 1024)} GB";
                hardware["Количество модулей памяти"] = $"{memoryModules}";

                // Материнская плата
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
                foreach (var mo in searcher.Get())
                {
                    hardware["Материнская плата"] = $"{mo["Manufacturer"]} {mo["Product"]}";
                    hardware["Версия платы"] = $"{mo["Version"]}";
                    break;
                }

                // BIOS
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
                foreach (var mo in searcher.Get())
                {
                    hardware["BIOS"] = $"{mo["Manufacturer"]} {mo["SMBIOSBIOSVersion"]}";
                    hardware["Версия BIOS"] = $"{mo["Version"]}";
                    hardware["Серийный номер ПК"] = $"{mo["SerialNumber"]}";
                    break;
                }

                // Жесткие диски
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                int diskCount = 0;
                foreach (var mo in searcher.Get())
                {
                    diskCount++;
                    var size = Convert.ToInt64(mo["Size"]) / (1024 * 1024 * 1024);
                    hardware[$"Диск {diskCount}"] =
                        $"{mo["Model"]}, Серийный номер: {mo["SerialNumber"]?.ToString()?.Trim()}, Размер: {size} GB";
                }

                // Логические диски
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType=3");
                foreach (var mo in searcher.Get())
                {
                    long size = Convert.ToInt64(mo["Size"]) / (1024 * 1024 * 1024);
                    long free = Convert.ToInt64(mo["FreeSpace"]) / (1024 * 1024 * 1024);
                    hardware[$"Логический диск {mo["DeviceID"]}"] = $"Размер: {size} GB, Свободно: {free} GB, ФС: {mo["FileSystem"]}, Метка: {mo["VolumeName"]}";
                }

                // Видеокарты
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                int gpuCount = 0;
                foreach (var mo in searcher.Get())
                {
                    if (mo["Name"] != null && !mo["Name"].ToString().Contains("Microsoft"))
                    {
                        gpuCount++;
                        var memory = mo["AdapterRAM"] != null ? Convert.ToInt64(mo["AdapterRAM"]) / (1024 * 1024) : 0;
                        hardware[$"Видеокарта {gpuCount}"] =
                            $"{mo["Name"]}, Память: {memory} MB, Драйвер: {mo["DriverVersion"]}";
                    }
                }

                // Сетевые адаптеры
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE NetEnabled=True");
                int netCount = 0;
                foreach (var mo in searcher.Get())
                {
                    netCount++;
                    hardware[$"Сетевой адаптер {netCount}"] =
                        $"{mo["Name"]}, MAC: {mo["MACAddress"]}";
                }

                // Звуковые устройства
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice");
                int soundCount = 0;
                foreach (var mo in searcher.Get())
                {
                    soundCount++;
                    hardware[$"Звуковое устройство {soundCount}"] = $"{mo["Name"]}";
                }

                // Производитель и модель ПК
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                foreach (var mo in searcher.Get())
                {
                    hardware["Производитель ПК"] = $"{mo["Manufacturer"]}";
                    hardware["Модель ПК"] = $"{mo["Model"]}";
                    break;
                }
            }
            catch (Exception ex)
            {
                hardware["Ошибка сбора"] = ex.Message;
            }
        }

        private void GetSoftwareInfo(Dictionary<string, string> software)
        {
            try
            {
                // Операционная система
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                foreach (var mo in searcher.Get())
                {
                    software["Операционная система"] = $"{mo["Caption"]}";
                    software["Версия ОС"] = $"{mo["Version"]}";
                    software["Архитектура ОС"] = $"{mo["OSArchitecture"]}";
                    software["Сборка ОС"] = $"{mo["BuildNumber"]}";
                    software["Производитель ОС"] = $"{mo["Manufacturer"]}";
                    break;
                }

                // Установленные программы
                GetInstalledPrograms(software);
            }
            catch (Exception ex)
            {
                software["Ошибка сбора"] = ex.Message;
            }
        }

        private void GetInstalledPrograms(Dictionary<string, string> software)
        {
            try
            {
                string[] registryPaths = {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

                var allPrograms = new List<string>();

                foreach (string registryPath in registryPaths)
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
                    {
                        if (key != null)
                        {
                            foreach (string subKeyName in key.GetSubKeyNames())
                            {
                                using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                                {
                                    string displayName = subKey?.GetValue("DisplayName") as string;
                                    if (!string.IsNullOrEmpty(displayName))
                                    {
                                        string version = subKey?.GetValue("DisplayVersion") as string;
                                        string publisher = subKey?.GetValue("Publisher") as string;

                                        string programInfo = displayName;
                                        if (!string.IsNullOrEmpty(version))
                                            programInfo += $", Версия: {version}";
                                        if (!string.IsNullOrEmpty(publisher))
                                            programInfo += $", Производитель: {publisher}";

                                        allPrograms.Add(programInfo);
                                    }
                                }
                            }
                        }
                    }
                }

                // Добавляем все программы
                for (int i = 0; i < allPrograms.Count; i++)
                {
                    software[$"Программа {i + 1}"] = allPrograms[i];
                }
            }
            catch (Exception ex)
            {
                software["Ошибка сбора программ"] = ex.Message;
            }
        }

        private void GetDriverInfo(Dictionary<string, string> drivers)
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPSignedDriver WHERE DeviceClass IS NOT NULL");
                int driverCount = 0;
                foreach (var mo in searcher.Get())
                {
                    driverCount++;
                    var deviceName = mo["DeviceName"]?.ToString();
                    if (!string.IsNullOrEmpty(deviceName))
                    {
                        var version = mo["DriverVersion"]?.ToString();
                        var driverDate = mo["DriverDate"]?.ToString();

                        // Преобразуем дату из формата CIM_DATETIME в нормальный вид
                        string formattedDate = "Неизвестно";
                        if (!string.IsNullOrEmpty(driverDate) && driverDate.Length >= 14)
                        {
                            try
                            {
                                // Формат CIM_DATETIME: yyyymmddHHMMSS.mmmmmmsUUU
                                int year = int.Parse(driverDate.Substring(0, 4));
                                int month = int.Parse(driverDate.Substring(4, 2));
                                int day = int.Parse(driverDate.Substring(6, 2));
                                formattedDate = $"{day:00}.{month:00}.{year}";
                            }
                            catch
                            {
                                // Если не удалось распарсить, берем первые 8 символов (год, месяц, день)
                                if (driverDate.Length >= 8)
                                    formattedDate = $"{driverDate.Substring(6, 2)}.{driverDate.Substring(4, 2)}.{driverDate.Substring(0, 4)}";
                                else
                                    formattedDate = driverDate;
                            }
                        }

                        drivers[$"Драйвер {driverCount}"] = $"{deviceName}, Версия: {version}, Дата: {formattedDate}";
                    }
                }
            }
            catch (Exception ex)
            {
                drivers["Ошибка сбора"] = ex.Message;
            }
        }

        private void GetPeripheralInfo(Dictionary<string, string> peripherals)
        {
            try
            {
                // USB устройства
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_USBHub");
                int usbCount = 0;
                foreach (var mo in searcher.Get())
                {
                    usbCount++;
                    peripherals[$"USB устройство {usbCount}"] = $"{mo["Name"]}";
                }

                // Принтеры
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer");
                int printerCount = 0;
                foreach (var mo in searcher.Get())
                {
                    printerCount++;
                    peripherals[$"Принтер {printerCount}"] = $"{mo["Name"]}";
                }

                // Мониторы
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DesktopMonitor");
                int monitorCount = 0;
                foreach (var mo in searcher.Get())
                {
                    monitorCount++;
                    peripherals[$"Монитор {monitorCount}"] = $"{mo["Name"]}";
                }
            }
            catch (Exception ex)
            {
                peripherals["Ошибка сбора"] = ex.Message;
            }
        }
    }
}