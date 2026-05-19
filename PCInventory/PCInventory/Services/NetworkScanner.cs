using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using PCInventory.Models;

namespace PCInventory.Services
{
    public class NetworkScanner
    {
        private int _timeout;
        private readonly bool _resolveNames;
        private CancellationTokenSource _cancellationTokenSource;

        public int Timeout
        {
            get => _timeout;
            set => _timeout = value;
        }

        public event Action<ScanResult> ComputerFound;
        public event Action<int, int> ProgressChanged;
        public event Action<string> StatusChanged;

        public NetworkScanner(int timeout = 100, bool resolveNames = true)
        {
            _timeout = timeout;
            _resolveNames = resolveNames;
        }

        public async Task<List<ScanResult>> ScanNetworkAsync(string startIP, string endIP, int timeout, CancellationToken cancellationToken = default)
        {
            _timeout = timeout;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var results = new List<ScanResult>();

            try
            {
                var start = IPAddress.Parse(startIP);
                var end = IPAddress.Parse(endIP);

                var ips = GenerateIPRange(start, end).ToList();
                int total = ips.Count;
                int completed = 0;

                StatusChanged?.Invoke($"Начинаю сканирование {total} адресов...");

                var tasks = ips.Select(ip => Task.Run(async () =>
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        return null;

                    var result = await PingHostAsync(ip);

                    Interlocked.Increment(ref completed);
                    ProgressChanged?.Invoke(completed, total);

                    if (result != null && result.IsOnline)
                    {
                        ComputerFound?.Invoke(result);
                        return result;
                    }

                    return null;
                }, _cancellationTokenSource.Token));

                var completedTasks = await Task.WhenAll(tasks);
                results = completedTasks.Where(r => r != null).ToList();

                StatusChanged?.Invoke($"Сканирование завершено. Найдено {results.Count} устройств.");
                return results;
            }
            catch (OperationCanceledException)
            {
                StatusChanged?.Invoke("Сканирование отменено.");
                return results;
            }
            finally
            {
                _cancellationTokenSource = null;
            }
        }

        public void StopScan()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async Task<ScanResult> PingHostAsync(IPAddress ip)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(ip, _timeout);

                    if (reply.Status == IPStatus.Success)
                    {
                        string computerName = _resolveNames ? await GetComputerNameAsync(ip) : null;
                        string macAddress = await GetMACAddressAsync(ip);

                        return new ScanResult
                        {
                            IPAddress = ip.ToString(),
                            ComputerName = computerName ?? ip.ToString(),
                            IsOnline = true,
                            ResponseTime = reply.RoundtripTime,
                            MACAddress = macAddress
                        };
                    }
                }
            }
            catch (PingException)
            {
                // Хост не отвечает
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при ping {ip}: {ex.Message}");
            }

            return null;
        }

        private async Task<string> GetComputerNameAsync(IPAddress ip)
        {
            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(ip);
                return hostEntry.HostName.Split('.')[0];
            }
            catch
            {
                return null;
            }
        }

        private async Task<string> GetMACAddressAsync(IPAddress ip)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var process = new Process())
                    {
                        process.StartInfo.FileName = "arp";
                        process.StartInfo.Arguments = $"-a {ip}";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.CreateNoWindow = true;
                        process.Start();

                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit(1000);

                        var lines = output.Split('\n');
                        foreach (var line in lines)
                        {
                            if (line.Contains(ip.ToString()))
                            {
                                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 2 && (parts[1].Contains('-') || parts[1].Contains(':')))
                                {
                                    return parts[1];
                                }
                            }
                        }
                    }
                }
                catch { }
                return null;
            });
        }

        private IEnumerable<IPAddress> GenerateIPRange(IPAddress start, IPAddress end)
        {
            uint startInt = IPToUint(start);
            uint endInt = IPToUint(end);

            for (uint i = startInt; i <= endInt; i++)
            {
                yield return UintToIP(i);
            }
        }

        private uint IPToUint(IPAddress ip)
        {
            byte[] bytes = ip.GetAddressBytes();
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        private IPAddress UintToIP(uint ip)
        {
            byte[] bytes = BitConverter.GetBytes(ip);
            Array.Reverse(bytes);
            return new IPAddress(bytes);
        }
    }
}