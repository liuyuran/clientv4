using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using game.scripts.server;
using game.scripts.utils;
using Google.FlatBuffers;

namespace game.scripts.manager;

public class MultiPlayerManager {
    public static MultiPlayerManager instance { get; private set; } = new();
    private readonly ConcurrentDictionary<string, ServerMeta> _servers = new();
    private readonly ConcurrentDictionary<string, ServerMeta> _extraServers = new();

    private MultiPlayerManager() {
        Task.Run(async () => {
            var clientUdp = new UdpClient(ServerStartupConfig.instance.serverPort);
            while (true) {
                var result = await clientUdp.ReceiveAsync();
                try {
                    var serverMeta = generated.server.ServerMeta.GetRootAsServerMeta(new ByteBuffer(result.Buffer));
                    var ip = result.RemoteEndPoint.Address.ToString();
                    if (_servers.ContainsKey(ip)) {
                        var old = _servers[ip];
                        old.LastReceiveTime = PlatformUtil.GetTimestamp();
                        _servers[ip] = old;
                    } else {
                        _servers[ip] = new ServerMeta {
                            Name = serverMeta.Name,
                            Description = serverMeta.Desc,
                            Ip = ip,
                            Port = ServerStartupConfig.instance.serverPort,
                            LastReceiveTime = PlatformUtil.GetTimestamp()
                        };
                    }
                } catch (Exception) {
                    //
                }                
            }
            // ReSharper disable once FunctionNeverReturns
        });
    }
    
    public void SaveServerConnection(string ip, int port, string name, string description) {
        if (_extraServers.ContainsKey(ip)) {
            var old = _extraServers[ip];
            old.Name = name;
            old.Description = description;
            old.Ip = ip;
            old.Port = port;
            _extraServers[ip] = old;
        } else {
            _extraServers[ip] = new ServerMeta {
                Name = name,
                Description = description,
                Ip = ip,
                Port = port,
                LastReceiveTime = PlatformUtil.GetTimestamp()
            };
        }
    }
    
    public void RemoveServerConnection(string ip) {
        if (_extraServers.ContainsKey(ip)) {
            _extraServers.TryRemove(ip, out _);
        }
    }
    
    public List<ServerMeta> GetAllServers() {
        var extraResult = _extraServers.Values.ToList();
        var searchResult = _servers.Values.Where(item => PlatformUtil.GetTimestamp() - item.LastReceiveTime < 10000).ToList();
        return searchResult
            .Concat(extraResult)
            .ToList();
    }
    
    public struct ServerMeta {
        public string Name;
        public string Description;
        public string Ip;
        public int Port;
        public ulong LastReceiveTime;
    }
}