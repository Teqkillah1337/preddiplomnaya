using System;
using System.Net;
using System.Net.Sockets;

namespace PCInventory.Services
{
    public static class WakeOnLanService
    {
        public static bool SendMagicPacket(string macAddress)
        {
            try
            {
                // Очищаем MAC-адрес от разделителей
                string cleanMac = macAddress.Replace("-", "").Replace(":", "").Replace(" ", "");

                if (cleanMac.Length != 12)
                    return false;

                // Создаем магический пакет
                byte[] magicPacket = new byte[102];

                // Первые 6 байт - 0xFF
                for (int i = 0; i < 6; i++)
                    magicPacket[i] = 0xFF;

                // Затем 16 раз повторяем MAC-адрес
                byte[] macBytes = new byte[6];
                for (int i = 0; i < 6; i++)
                    macBytes[i] = Convert.ToByte(cleanMac.Substring(i * 2, 2), 16);

                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        magicPacket[6 + (i * 6) + j] = macBytes[j];
                    }
                }

                // Отправляем UDP пакет на широковещательный адрес
                using (var client = new UdpClient())
                {
                    client.Send(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Broadcast, 9));
                    client.Send(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Broadcast, 7));
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}