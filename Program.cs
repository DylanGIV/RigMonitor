using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ManagedCuda.Nvml;
using Newtonsoft.Json;

namespace RigMonitor
{
    struct Device
    {
        public nvmlDevice NvmlDevice;
        public string DeviceId;
        public string DeviceName;
    }
    struct LogInModel
    {
        public string Username;
        public string Password;
    }
    struct PostRigModel
    {
        public string RigId;
        public string RigName;
        public string RigDescription;
    }
    struct PostDevices
    {
        public List<PostDeviceModel> Devices;
    }
    struct PostDeviceModel
    {
        public string DeviceName;
        public string DeviceDescription;
        public string DeviceId;
        public string RigId;
    }
    struct PostDevicesStatsModel
    {
        public DateTime Timestamp;
        public List<DeviceStats> DevicesStats;
    }
    struct DeviceStats
    {
        public string DeviceId;
        public uint Temperature;
        public float PowerUsage;
        public uint FanSpeed;
        public uint MemoryClockSpeed;
        public uint CoreClockSpeed;
        public uint DeviceUsage;
    }
    class JWT
    {
        public string Token { get; set; }
    }
    class RigResponse
    {
        public string RigId { get; set; }
        public string RigName { get; set; }
        public string RigDescription { get; set; }
        public string UserId { get; set; }
    }

    class Program
    {
        static readonly HttpClient client = new HttpClient();
        public static string GetMACAddress()
        {
            // This code will pull the MAC address of the network interface that has the most bytes sent/received.
            // This is done so we always select the MAC address that has the main internet connection.

            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

            if (nics == null || nics.Length < 1)
            {
                Console.WriteLine("  No network interfaces found.");
                return null;
            }

            Dictionary<string, long> macAddresses = new Dictionary<string, long>();
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                    macAddresses[nic.GetPhysicalAddress().ToString()] = nic.GetIPStatistics().BytesSent + nic.GetIPStatistics().BytesReceived;
            }

            long maxValue = 0;
            string mac = "";

            foreach (KeyValuePair<string, long> pair in macAddresses)
            {
                if (pair.Value > maxValue)
                {
                    mac = pair.Key;
                    maxValue = pair.Value;
                }
            }
            return mac;
        }
        static async Task LogIn(string username, string password)
        {
            var logInModel = new LogInModel
            {
                Username = username,
                Password = password
            };

            var json = JsonConvert.SerializeObject(logInModel);
            var logInContent = new StringContent(json, UnicodeEncoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync("http://rig-monitor-api.herokuapp.com/api/Authenticate/login", logInContent);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                var deserialized = JsonConvert.DeserializeObject<JWT>(responseBody);
                var jwt = deserialized.Token;

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);

                throw;
            }
        }
        static async Task<RigResponse> PostRig(string rigId, string rigName, string rigDescription)
        {
            var rigModel = new PostRigModel
            {
                RigId = rigId,
                RigName = rigName,
                RigDescription = rigDescription
            };

            var json = JsonConvert.SerializeObject(rigModel);
            var rigContent = new StringContent(json, UnicodeEncoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync("http://rig-monitor-api.herokuapp.com/Rig", rigContent);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                var deserialized = JsonConvert.DeserializeObject<RigResponse>(responseBody);
                return deserialized;

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);

                throw;
            }
        }
        static async Task<PostDevices> PostDevices(Device[] postDevices, string rigId)
        {
            var devices = new List<PostDeviceModel>();
            foreach (var device in postDevices)
            {
                var tempPostDevice = new PostDeviceModel
                {
                    DeviceName = device.DeviceName,
                    DeviceDescription = "",
                    DeviceId = device.DeviceId,
                    RigId = rigId
                };

                devices.Add(tempPostDevice);
            }
            var listOfDevices = new PostDevices
            {
                Devices = devices
            };

            var json = JsonConvert.SerializeObject(listOfDevices);
            var devicesContent = new StringContent(json, UnicodeEncoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync("http://rig-monitor-api.herokuapp.com/Device", devicesContent);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                var deserialized = JsonConvert.DeserializeObject<PostDevices>(responseBody);
                return deserialized;

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);

                throw;
            }
        }
        static async Task PostDevicesStats (List<DeviceStats> postDevicesStats)
        {
            var devicesStats = new PostDevicesStatsModel
            {
                Timestamp = DateTime.UtcNow,
                DevicesStats = postDevicesStats
            };

            var json = JsonConvert.SerializeObject(devicesStats);
            var devicesStatsContent = new StringContent(json, UnicodeEncoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync("http://rig-monitor-api.herokuapp.com/DeviceStats", devicesStatsContent);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);

                throw;
            }
        }
        static async Task<RigResponse> GetRig(string rigId)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync($"http://rig-monitor-api.herokuapp.com/Rig/rigId?rigId={rigId}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                var deserialized = JsonConvert.DeserializeObject<RigResponse>(responseBody);
                return deserialized;

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);

                return null;
            }
        }
        static async Task Main()
        {
            string username = "TestRig";
            string password = "!1String";
            uint deviceCount = 0;

            await LogIn(username, password);

            //string email = "TestRig@email.com";
            string rigName = "Rig001";
            string rigDescription = "Main Machine";

            var rigId = GetMACAddress();

            var currentRig = await GetRig(rigId);

            if (currentRig == null)
            {
                var rigResponse = await PostRig(rigId, rigName, rigDescription);
                Console.WriteLine(rigResponse.RigId);
            }

            // Initialize NVML
            NvmlNativeMethods.nvmlInit();

            // Retrieve the amount of NVIDIA Devices
            NvmlNativeMethods.nvmlDeviceGetCount(ref deviceCount);

            // Create array for devices and resize to the amount of devices.
            var devices = new List<Device>().ToArray();
            Array.Resize(ref devices, (int)deviceCount);

            // Get handle, UUID, and Name of each device
            for (uint i = 0; i < deviceCount; i++)
            {
                NvmlNativeMethods.nvmlDeviceGetHandleByIndex(i, ref devices[i].NvmlDevice);
                NvmlNativeMethods.nvmlDeviceGetUUID(devices[i].NvmlDevice, out devices[i].DeviceId);
                NvmlNativeMethods.nvmlDeviceGetName(devices[i].NvmlDevice, out devices[i].DeviceName);
            }

            // Save devices to the database.
            await PostDevices(devices, rigId);

            while (true)
            {
                // Get stats of each device and store them, these will be overwritten, but saved to database.
                var devicesStats = new List<DeviceStats>();    
                for (uint i = 0; i < deviceCount; i++)  
                {
                    var tempNvmlUtilization = new nvmlUtilization();
                    var tempPowerUsage = new uint();
                    var tempDeviceStats = new DeviceStats
                    {
                        DeviceId = devices[i].DeviceId
                    };

                    NvmlNativeMethods.nvmlDeviceGetTemperature(devices[i].NvmlDevice, nvmlTemperatureSensors.Gpu, ref tempDeviceStats.Temperature);
                    NvmlNativeMethods.nvmlDeviceGetFanSpeed(devices[i].NvmlDevice, ref tempDeviceStats.FanSpeed);

                    NvmlNativeMethods.nvmlDeviceGetPowerUsage(devices[i].NvmlDevice, ref tempPowerUsage); //
                    tempDeviceStats.PowerUsage = (((float)tempPowerUsage) / 1000);  // Convert to Watts

                    NvmlNativeMethods.nvmlDeviceGetClock(devices[i].NvmlDevice, nvmlClockType.Mem, nvmlClockId.Current, ref tempDeviceStats.MemoryClockSpeed);
                    NvmlNativeMethods.nvmlDeviceGetClock(devices[i].NvmlDevice, nvmlClockType.Graphics, nvmlClockId.Current, ref tempDeviceStats.CoreClockSpeed);

                    NvmlNativeMethods.nvmlDeviceGetUtilizationRates(devices[i].NvmlDevice, ref tempNvmlUtilization); // Returns an object but we need just one of its values: gpu usage
                    tempDeviceStats.DeviceUsage = tempNvmlUtilization.gpu;  //

                    devicesStats.Add(tempDeviceStats);
                } 

                await PostDevicesStats(devicesStats);

                // Sleep 10 seconds
                Thread.Sleep(10000);
            }
        }
    }
}
