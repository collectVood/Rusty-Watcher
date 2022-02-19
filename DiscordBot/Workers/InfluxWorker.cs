using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using InfluxDB.Collector;
using RustyWatcher.Configurations;
using RustyWatcher.Models.Rcon;
using Serilog;

namespace RustyWatcher.Workers;

public static class InfluxWorker
{
    #region Fields

    private static InfluxDbConfiguration _configuration => Configuration.Instance.InfluxDbConfiguration;

    private static readonly Dictionary<string, object> _metricsFPSData = new();
    private static readonly Dictionary<string, object> _metricsPlayersData = new();
    private static readonly Dictionary<string, object> _metricsPlayersJoiningData = new();
    private static readonly Dictionary<string, object> _metricsPlayersQueuedData = new();
    private static readonly Dictionary<string, object> _metricsEntitiesData = new();
    private static readonly Dictionary<string, object> _metricsCollectionsData = new();
    private static readonly Dictionary<string, object> _metricsNetInData = new();
    private static readonly Dictionary<string, object> _metricsNetOutData = new();
    
    private static Timer _metricsTimer;
    
    #endregion

    #region Init

    public static void Start()
    {
        if (!_configuration.Use)
        {
            Log.Information(
                "{0} is currently disabled. This is an awesome feature great to combine with {1}, consider setting it up :)",
                GetTag(), "https://grafana.com/");
            return;
        }
        
        Metrics.Collector = new CollectorConfiguration()
                .Tag.With("host", Environment.GetEnvironmentVariable("COMPUTERNAME"))
                .Batch.AtInterval(TimeSpan.FromSeconds(2))
                .WriteTo.InfluxDB(_configuration.Address + ":" + _configuration.Port, _configuration.Database, _configuration.Username, _configuration.Password)
                .CreateCollector();
        
        Log.Information("{0} Connected!", GetTag());

        var delay = TimeSpan.FromSeconds(Configuration.Instance.UpdateDelay);
        
        _metricsTimer = new Timer(_ =>
        {
            Metrics.Write("server_fps", _metricsFPSData);
            Metrics.Write("server_playercount", _metricsPlayersData);
            Metrics.Write("server_joiningcount", _metricsPlayersJoiningData);
            Metrics.Write("server_queuedcount", _metricsPlayersQueuedData);
            Metrics.Write("server_entities", _metricsEntitiesData);
            Metrics.Write("server_collections", _metricsCollectionsData);
            Metrics.Write("server_netin", _metricsNetInData);
            Metrics.Write("server_netout", _metricsNetOutData);

            Log.Debug("{0} Metrics write done", GetTag());
        }, null, delay, delay);
    }

    #endregion

    #region Methods

    public static void AddData(string name, ResponseServerInfo serverInfo)
    {        
        if (!_configuration.Use)
            return;
        
        _metricsFPSData[name] = serverInfo.Framerate;
        _metricsCollectionsData[name] = serverInfo.Collections;
        
        _metricsPlayersData[name] = serverInfo.Players;
        _metricsPlayersJoiningData[name] = serverInfo.Joining;
        _metricsPlayersQueuedData[name] = serverInfo.Queued;
        
        _metricsEntitiesData[name] = serverInfo.EntityCount;
        _metricsNetInData[name] = serverInfo.NetworkIn;
        _metricsNetOutData[name] = serverInfo.NetworkOut;
    }

    #endregion
    
    #region Helpers

    private static string GetTag()
    {
        const string influx = "[INFLUX]";
        return influx;
    }

    #endregion
}