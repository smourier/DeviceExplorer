using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace DeviceExplorer.Model
{
    public static class Bluetooth
    {
        public static IReadOnlyDictionary<ushort, string> CompanyNames => _companyNames.Value;
        private static readonly Lazy<IReadOnlyDictionary<ushort, string>> _companyNames = new(() => GetKVDictionary("company_identifiers"));

        public static IReadOnlyDictionary<ushort, string> DataTypes => _dataTypes.Value;
        private static readonly Lazy<IReadOnlyDictionary<ushort, string>> _dataTypes = new(() => GetKVDictionary("ad_types"));

        private class KV
        {
            public ushort Value { get; set; }
            public string Name { get; set; }
        }

        private static IReadOnlyDictionary<ushort, string> GetKVDictionary(string fileName)
        {
            var dic = new Dictionary<ushort, string>();
            var asm = Assembly.GetEntryAssembly();
            using var stream = asm.GetManifestResourceStream(asm.GetName().Name + ".Resources.Bluetooth." + fileName + ".json");
            foreach (var kv in JsonSerializer.Deserialize<KV[]>(stream))
            {
                dic[kv.Value] = kv.Name;
            }
            return dic;
        }

        public static IReadOnlyDictionary<ushort, BluetoothService> ServicesByUuid => _services.Value;
        private static readonly Lazy<IReadOnlyDictionary<ushort, BluetoothService>> _services = new(GetServicesByUuid);

        public static IReadOnlyDictionary<Guid, BluetoothService> ServicesByGuid => _servicesByGuid.Value;
        private static readonly Lazy<IReadOnlyDictionary<Guid, BluetoothService>> _servicesByGuid = new(() => ServicesByUuid.Values.ToDictionary(k => k.Guid));

        private static IReadOnlyDictionary<ushort, BluetoothService> GetServicesByUuid()
        {
            var dic = new Dictionary<ushort, BluetoothService>();
            var asm = Assembly.GetEntryAssembly();
            using var stream = asm.GetManifestResourceStream(asm.GetName().Name + ".Resources.Bluetooth.service_uuids.json");
            foreach (var service in JsonSerializer.Deserialize<BluetoothService[]>(stream))
            {
                dic[service.Uuid] = service;
            }
            return dic;
        }

        public static string GetMacAddress(ulong address) => string.Join(":", BitConverter.GetBytes(address).Take(6).Reverse().Select(b => b.ToString("X2")));

        public static BluetoothService GetService(ushort uuid)
        {
            _services.Value.TryGetValue(uuid, out var service);
            if (service == null)
            {
                service = new BluetoothService
                {
                    Uuid = uuid,
                    Name = uuid + " (0x" + uuid.ToString("X4") + ")"
                };
            }
            return service;
        }

        public static BluetoothService GetService(Guid guid)
        {
            _servicesByGuid.Value.TryGetValue(guid, out var service);
            if (service == null)
            {
                service = new BluetoothService
                {
                    Guid = guid,
                    Name = guid.ToString()
                };
            }
            return service;
        }

        public static string GetCompanyName(ushort id)
        {
            if (_companyNames.Value.TryGetValue(id, out var companyName))
                return "'" + companyName + "' (" + id + "/0x" + id.ToString("X4") + ")";

            return id + " (0x" + id.ToString("X4") + ")";
        }

        public static string GetDataType(ushort id)
        {
            if (_dataTypes.Value.TryGetValue(id, out var dataType))
                return "'" + dataType + "' (" + id + "/0x" + id.ToString("X4") + ")";

            return id + " (0x" + id.ToString("X4") + ")";
        }
    }
}
