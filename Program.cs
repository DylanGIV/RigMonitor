using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
        public uint Temperature;
        public uint PowerUsage;
        public uint FanSpeed;
        public uint MemoryClockSpeed;
        public uint CoreClockSpeed;
        public nvmlUtilization DeviceUsage;
    }

    struct LogInModel
    {
        public string Username;
        public string Password;
    }
    struct PostRigModel
    {
        public string RigName;
        public string RigDescription;
    }

    class JWT
    {
        public string Token { get; set; }
    }
    
    class RigResponse
    {
        public long RigId { get; set; }
        public string RigName { get; set; }
        public string RigDescription { get; set; }
        public string UserId { get; set; }
    }

    class Program
    {


        static readonly HttpClient client = new HttpClient();
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
                HttpResponseMessage response = await client.PostAsync("http://localhost:59921/api/Authenticate/login", logInContent);
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
        static async Task<RigResponse> PostRig(string rigName, string rigDescription)
        {
            var rigModel = new PostRigModel
            {
                RigName = rigName,
                RigDescription = rigDescription
            };

            var json = JsonConvert.SerializeObject(rigModel);
            var rigContent = new StringContent(json, UnicodeEncoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync("http://localhost:59921/Rig", rigContent);
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
        static async Task<RigResponse> GetRig(long rigId)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync($"http://localhost:59921/Rig/rigId?rigId={rigId}");
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
        static async Task Main()
        {
            long rigId = 10;
            string username = "TestRig";
            string password = "!1String";
            uint deviceCount = 0;

            await LogIn(username, password);

            //string email = "TestRig@email.com";
            //string rigName = "Rig001";
            //string rigDescription = "Main Machine";
            //var rigResponse = await PostRig(rigName, rigDescription);

            var currentRig = await GetRig(rigId);
            Console.WriteLine(currentRig.RigName);


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

            // Get stats of each device and store them, these will be overwritten.
            for (uint i = 0; i < deviceCount; i++)
            {
                NvmlNativeMethods.nvmlDeviceGetTemperature(devices[i].NvmlDevice, nvmlTemperatureSensors.Gpu, ref devices[i].Temperature);
                NvmlNativeMethods.nvmlDeviceGetFanSpeed(devices[i].NvmlDevice, ref devices[i].FanSpeed);
                NvmlNativeMethods.nvmlDeviceGetPowerUsage(devices[i].NvmlDevice, ref devices[i].PowerUsage);
                NvmlNativeMethods.nvmlDeviceGetClock(devices[i].NvmlDevice, nvmlClockType.Mem, nvmlClockId.Current, ref devices[i].MemoryClockSpeed);
                NvmlNativeMethods.nvmlDeviceGetClock(devices[i].NvmlDevice, nvmlClockType.Graphics, nvmlClockId.Current, ref devices[i].CoreClockSpeed);
                NvmlNativeMethods.nvmlDeviceGetUtilizationRates(devices[i].NvmlDevice, ref devices[i].DeviceUsage);
            }

            Console.WriteLine(deviceCount);
        }
    }
}
