using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.EcosEmu
{
    /// <summary>
    /// Simple JSON based implementation of ILocoRepository.
    /// </summary>
    public sealed class JsonLocoRepository : ILocoRepository
    {
        private readonly string _filePath;
        private readonly List<LocoInfo> _locos = new();
        private readonly object _syncRoot = new();

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public JsonLocoRepository(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }

        public async Task LoadAsync(CancellationToken ct = default)
        {
            if (!File.Exists(_filePath))
            {
                Console.WriteLine($"[LOCO] No JSON file found at '{_filePath}', starting with empty loco list.");

                lock (_syncRoot)
                {
                    _locos.Clear();
                }

                // Create minimal DB so the central has a valid loco list
                await SaveAsync(ct);
                return;
            }

            try
            {
                await using var stream = File.OpenRead(_filePath);

                if (stream.Length == 0)
                {
                    Console.WriteLine($"[LOCO] JSON file '{_filePath}' is empty, starting with empty loco list.");

                    lock (_syncRoot)
                    {
                        _locos.Clear();
                    }

                    await SaveAsync(ct);   // <-- FIX: maak een geldig, leeg JSON-bestand
                    return;
                }

                var loaded = await JsonSerializer.DeserializeAsync<List<LocoInfo>>(stream, SerializerOptions, ct)
                              ?? new List<LocoInfo>();

                lock (_syncRoot)
                {
                    _locos.Clear();
                    _locos.AddRange(loaded);
                }

                Console.WriteLine($"[LOCO] Loaded {loaded.Count} locomotives from '{_filePath}'.");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[LOCO] Invalid JSON in '{_filePath}': {ex.Message}");
                Console.WriteLine("[LOCO] Ignoring file and starting with empty loco list.");

                lock (_syncRoot)
                {
                    _locos.Clear();
                }

                // Move corrupt file aside
                File.Move(_filePath, _filePath + ".bak", overwrite: true);

                // And create fresh empty DB
                await SaveAsync(ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOCO] Error reading '{_filePath}': {ex.Message}");
                Console.WriteLine("[LOCO] Starting with empty loco list.");

                lock (_syncRoot)
                {
                    _locos.Clear();
                }

                await SaveAsync(ct);   // <-- important
            }
        }

        //public async Task LoadAsync(CancellationToken ct = default)
        //{
        //    if (!File.Exists(_filePath))
        //    {
        //        Console.WriteLine($"[LOCO] No JSON file found at '{_filePath}', starting with empty loco list.");
        //        return;
        //    }

        //    try
        //    {
        //        await using var stream = File.OpenRead(_filePath);

        //        if (stream.Length == 0)
        //        {
        //            // Empty file – treat as "no locos" instead of throwing.
        //            Console.WriteLine($"[LOCO] JSON file '{_filePath}' is empty, starting with empty loco list.");
        //            return;
        //        }

        //        var loaded = await JsonSerializer.DeserializeAsync<List<LocoInfo>>(stream, SerializerOptions, ct)
        //                      ?? new List<LocoInfo>();

        //        lock (_syncRoot)
        //        {
        //            _locos.Clear();
        //            _locos.AddRange(loaded);
        //        }

        //        Console.WriteLine($"[LOCO] Loaded {loaded.Count} locomotives from '{_filePath}'.");
        //    }
        //    catch (JsonException ex)
        //    {
        //        Console.WriteLine($"[LOCO] Invalid JSON in '{_filePath}': {ex.Message}");
        //        Console.WriteLine("[LOCO] Ignoring file and starting with empty loco list.");

        //        lock (_syncRoot)
        //        {
        //            _locos.Clear();
        //        }
        //        // Optioneel: je kunt hier het corrupte bestand hernoemen naar .bak
        //        File.Move(_filePath, _filePath + ".bak", overwrite: true);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"[LOCO] Error reading '{_filePath}': {ex.Message}");
        //        Console.WriteLine("[LOCO] Starting with empty loco list.");

        //        lock (_syncRoot)
        //        {
        //            _locos.Clear();
        //        }
        //    }
        //}

        public async Task SaveAsync(CancellationToken ct = default)
        {
            List<LocoInfo> snapshot;

            lock (_syncRoot)
            {
                snapshot = _locos.ToList();
            }

            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, snapshot, SerializerOptions, ct);

            Console.WriteLine($"[LOCO] Saved {snapshot.Count} locomotives to '{_filePath}'.");
        }

        public IReadOnlyCollection<LocoInfo> GetAll()
        {
            lock (_syncRoot)
            {
                return _locos.ToList();
            }
        }

        public LocoInfo? GetByEcosId(int ecosId)
        {
            lock (_syncRoot)
            {
                return _locos.FirstOrDefault(l => l.EcosId == ecosId);
            }
        }

        public LocoInfo? GetByAddress(int address, int? addressExt = null)
        {
            lock (_syncRoot)
            {
                return _locos.FirstOrDefault(l =>
                    l.Address == address &&
                    (addressExt == null || l.AddressExt == addressExt));
            }
        }

        public void AddOrUpdate(LocoInfo loco)
        {
            if (loco == null) throw new ArgumentNullException(nameof(loco));

            lock (_syncRoot)
            {
                var index = _locos.FindIndex(l => l.EcosId == loco.EcosId);
                if (index >= 0)
                {
                    _locos[index] = loco;
                }
                else
                {
                    _locos.Add(loco);
                }
            }
        }

        public void RemoveByEcosId(int ecosId)
        {
            lock (_syncRoot)
            {
                _locos.RemoveAll(l => l.EcosId == ecosId);
            }
        }
    }
}
