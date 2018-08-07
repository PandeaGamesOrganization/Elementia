﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;
using Polenter.Serialization;


public class WorldDataAccessService : Service
{
    private class WorldDataAccessRequest:ServiceRequest<WorldDataAccess>
    {
        private WorldPersistanceService _worldPersistanceService;
        private DataConfig _dataConfig;
        
        public WorldDataAccessRequest(WorldDataAccessService worldDataAccessService, WorldPersistanceService worldPersistanceService, DataConfig dataConfig) : base(worldDataAccessService)
        {
            _dataConfig = dataConfig;
            _worldPersistanceService = worldPersistanceService;
        }
        
        protected override IEnumerator MakeRequestCoroutine(Action<WorldDataAccess> onComplete, Action onError)
        {
            WorldIndex index = null;
            bool hasError = false;
            
            _worldPersistanceService.Load((worldIndex) => { index = worldIndex; }, () => { hasError = true; });

            while (index == null && !hasError)
            {
                yield return null;
            }

            if (hasError)
            {
                onError();
            }
            else
            {
                onComplete(new WorldDataAccess(index, _dataConfig));
            }
        }
    }
    
    private WorldPersistanceService _worldPersistanceService;
    private WorldSimulationState _worldSimulationState;
    private WorldDataAccessRequest _worldDataAccessRequest;

    [SerializeField] private DataConfig _dataConfig;

    public override void StartService(ServiceManager serviceManager)
    {
        base.StartService(serviceManager);
        _worldPersistanceService = serviceManager.GetService<WorldPersistanceService>();
        _worldDataAccessRequest = new WorldDataAccessRequest(this, _worldPersistanceService, _dataConfig);
    }

    public void RequestAccess(Action<WorldDataAccess> onComplete, Action onError)
    {
        _worldDataAccessRequest.AddRequest(onComplete, onError);
    }
}

public struct TokenRequest
{
    private int _left;
    private int _right;
    private int _bottom;
    private int _top;

    public int left { get { return _left; } }
    public int right { get { return _right; } }
    public int bottom { get { return _bottom; } }
    public int top { get { return _top; } }
    public int width { get { return _right - _left; } }
    public int height { get { return _bottom - top; } }

    public TokenRequest(int left, int right, int bottom, int top)
    {
        _left = left;
        _right = right;
        _bottom = bottom;
        _top = top;
    }
}

public class WorldDataAccess
{
    private Dictionary<string, LoadedArea> _cache;
    private Dictionary<string, SaveAreaJob> _saveCache;
    private WorldIndex _worldIndex;
    private DataConfig _dataConfig;
    private List<LoadAreaJob> _loadJobPool;
    
    public WorldDataAccess(WorldIndex worldIndex, DataConfig dataConfig)
    {
        _cache = new Dictionary<string, LoadedArea>();
        _saveCache = new Dictionary<string, SaveAreaJob>();
        _loadJobPool = new List<LoadAreaJob>();
        _worldIndex = worldIndex;
        _dataConfig = dataConfig;
    }

    public void ReturnToken(WorldDataToken token)
    {
        return;
        /*
        if (_tokens.Contains(token))
        {
            _tokens.Remove(token);

            HashSet<string> _removeCachedAreas = new HashSet<string>();

            foreach (KeyValuePair<AreaIndex, string> keyValuePair in token.Filepaths)
            {
                _removeCachedAreas.Add(keyValuePair.Value);
            }

            foreach (WorldDataToken cachedToken in _tokens)
            {
                foreach (KeyValuePair<AreaIndex, string> keyValuePair in cachedToken.Filepaths)
                {
                    if (_removeCachedAreas.Contains(keyValuePair.Value))
                    {
                        _removeCachedAreas.Remove(keyValuePair.Value);
                    }
                }
            }

            string[] removeResult = new string[_removeCachedAreas.Count];
            _removeCachedAreas.CopyTo(removeResult);

            foreach (string removeCache in removeResult)
            {
                _areaCache[removeCache].Destroy();
                _areaCache.Remove(removeCache);
            }
        }*/
    }

    public void SaveAndReturnToken(WorldDataToken token, Action onComplete)
    {
        /*SaveToken(token, (savedToken) => {
            ReturnToken(savedToken);
            onComplete();
        });*/
    }

    public void SaveToken(WorldDataToken token)
    {
       /* List<SaveAreaJob.SaveAreaRequest> saveRequests = new List<SaveAreaJob.SaveAreaRequest>();
        foreach (KeyValuePair<AreaIndex, string> tokenFile in token.Filepaths)
        {
            if(!savingAreas.Contains(tokenFile.Value))
            {
                savingAreas.Add(tokenFile.Value);
                saveRequests.Add(new SaveAreaJob.SaveAreaRequest(tokenFile.Key, tokenFile.Value));
            }
        }

        SaveAreaJob job = new SaveAreaJob(saveRequests, token.WorldIndex);
        job.Start();*/
    }

    public WorldDataToken GetToken(TokenRequest request, string persistentDataPath)
    {
        int leftArea = request.left / _worldIndex.AreaDimensions;
        int rightArea = (request.right - 1) / _worldIndex.AreaDimensions;
        int topArea = request.top / _worldIndex.AreaDimensions;
        int bottomArea = (request.bottom - 1) / _worldIndex.AreaDimensions;

        int horizontalAreaCount = (rightArea - leftArea) + 1;
        int verticalAreaCount = (bottomArea - topArea) + 1;

        LoadedArea[,] jobsResult = new LoadedArea[horizontalAreaCount, verticalAreaCount];
        Dictionary<AreaIndex, string> filepaths = new Dictionary<AreaIndex, string>();

        List<LoadedArea> loadedAreas = new List<LoadedArea>();
        List<LoadedArea> newLoadedAreaRequests = new List<LoadedArea>();

        for (int i = 0; i < horizontalAreaCount; i++)
        {
            for (int j = 0; j < verticalAreaCount; j++)
            {
                int areaX = i + leftArea;
                int areaY = j + topArea;
                string areaKey = String.Join("_", new string[]{areaX.ToString(), areaY.ToString()});
                LoadedArea loadedArea = null;
                _cache.TryGetValue(areaKey, out loadedArea);

                if (loadedArea == null)
                {
                    LoadAreaJob.AreaRequest loadRequest = new LoadAreaJob.AreaRequest(areaX, areaY);
                    loadedArea = new LoadedArea(loadRequest);
                    newLoadedAreaRequests.Add(loadedArea);
                    string key = loadRequest.GetAreaKey();
                    _cache.Add(key, loadedArea);
                }
                
                loadedAreas.Add(loadedArea);
            }
        }
        
        LoadAreaJob loadAreaJob = null;
        
        if (_loadJobPool.Count == 0)
        {
            loadAreaJob = new LoadAreaJob(_worldIndex, _dataConfig, persistentDataPath);
            loadAreaJob.Start();
        }
        else
        {
            loadAreaJob = _loadJobPool[0];
            _loadJobPool.RemoveAt(0);
        }
        
        loadAreaJob.SetJob(newLoadedAreaRequests);
        
        //wait until all areas loaded
        while (loadedAreas.Find((loadedArea) => loadedArea.Result == null) != null)
        {
            Thread.Sleep(10);
        }
 
        _loadJobPool.Add(loadAreaJob);
        
        loadedAreas.ForEach((loadedArea) =>
        {
            jobsResult[loadedArea.Request.areaX - leftArea, loadedArea.Request.areaY - topArea] = loadedArea;
        });
        
        WorldDataToken token = new WorldDataToken(request, _worldIndex, jobsResult);
        return token;
    }
}


public class LoadedArea
{
    private LoadAreaJob.AreaRequest _request;
    private LoadAreaJob.AreaRequestResult _result;

    public LoadAreaJob.AreaRequest Request
    {
        get { return _request; }
    }
    
    public LoadAreaJob.AreaRequestResult Result
    {
        get { return _result; }
    }
    
    public LoadedArea(LoadAreaJob.AreaRequest request)
    {
        _request = request;
    }

    public void SetResult(LoadAreaJob.AreaRequestResult result)
    {
        _result = result;
    }
}