﻿using UnityEngine;
using System.Collections.Generic;
using System;

public class WorldDataToken
{
    private struct PixelInformation
    {
        public int areaX;
        public int areaY;
        public int areaPixelX;
        public int areaPixelY;

        public override string ToString()
        {
            return string.Format("[areaX:{0}, areaY:{1}, areaPixelX:{2}, areaPixelY:{3}]", areaX, areaY, areaPixelX, areaPixelY);
        }
    }

    private AreaIndex[,] _areas;
    private Dictionary<AreaIndex, string> _filepaths;
    private WorldIndex _index;
    private TokenRequest _request;

    public TokenRequest Request { get { return _request; } }
    public WorldIndex WorldIndex { get { return _index; } }
    public Dictionary<AreaIndex, string> Filepaths { get { return _filepaths; } }

    public AreaIndex[,] Areas
    {
        get
        {
            return _areas;
        }
    }

    public WorldDataToken(TokenRequest request, WorldIndex index, AreaIndex[,] areas, Dictionary<AreaIndex, string> filepaths)
    {
        _request = request;
        _index = index;
        _areas = areas;
        _filepaths = filepaths;
    }

    private PixelInformation GetPixelInformation(int x, int y)
    {
        PixelInformation info = default(PixelInformation);

        int areaDim = _index.AreaDimensions;
        int trueAreaX = (int)((_request.left + x) / areaDim);
        int requestedAreaX = (int)(_request.left / areaDim);
        int trueAreaY = (int)((_request.top + y) / areaDim);
        int requestedAreaY = (int)(_request.top / areaDim);

        info.areaX = trueAreaX - requestedAreaX;
        info.areaY = trueAreaY - requestedAreaY;
        info.areaPixelX = (_request.left + x) % areaDim;
        info.areaPixelY = (_request.top + y) % areaDim;

        return info;
    }

    private bool AreCoordinatesInvalid(int x, int y)
    {
        return x < 0 || x > _request.width || y < 0 || y > _request.height;
    }

    private bool IsPixelInformationInvalid(PixelInformation info)
    {
        return info.areaX >= _areas.Length || info.areaY >= _areas.GetLongLength(1);
    }

    public ushort GetUshort(int x, int y, UshortDataID id)
    {
        if (AreCoordinatesInvalid(x, y))
        {
            return 1;
        }

        PixelInformation info = GetPixelInformation(x, y);

        if (IsPixelInformationInvalid(info))
        {
            Debug.LogWarning("Thigns went BAD " + info);
        }

        AreaIndex area = _areas[info.areaX, info.areaY];

        switch (id)
        {
            case UshortDataID.HeightLayerData:
                return area.AlphaDataLayer.HeightLayerData.data[info.areaPixelX, info.areaPixelY];
        }

        return 0;
    }

    public void SetUshort(int x, int y, ushort value, UshortDataID id)
    {
        PixelInformation info = GetPixelInformation(x, y);
        AreaIndex area = _areas[info.areaX, info.areaY];

        switch (id)
        {
            case UshortDataID.HeightLayerData:
                area.AlphaDataLayer.HeightLayerData.data[info.areaPixelX, info.areaPixelY] = value;
                break;
        }
    }

    public int GetInt(int x, int y, IntDataID id)
    {
        if (AreCoordinatesInvalid(x, y))
        {
            return 1;
        }

        PixelInformation info = GetPixelInformation(x, y);

        if (IsPixelInformationInvalid(info))
        {
            Debug.LogWarning("THigns went BAD "+ info);
        }

        AreaIndex area = null;

        try
        {
            area = _areas[info.areaX, info.areaY];
        }
        catch (Exception e)
        {
            Debug.LogWarning("THigns went BAD " + e);
            info = GetPixelInformation(x, y);
        }

        switch(id)
        {
            case IntDataID.NoiseLayerData:
                return area.AlphaDataLayer.NoiseLayerData.data[info.areaPixelX, info.areaPixelY];
        }

        return 0;
    }

    public void SetInt(int x, int y, int value, IntDataID id)
    {
        PixelInformation info = GetPixelInformation(x, y);
        AreaIndex area = _areas[info.areaX, info.areaY];

        switch (id)
        {
            case IntDataID.NoiseLayerData:
                area.AlphaDataLayer.NoiseLayerData.data[info.areaPixelX, info.areaPixelY] = value;
                break;
        }
    }

    public byte GetByte(int x, int y, ByteDataLyerID id)
    {
        if (AreCoordinatesInvalid(x, y))
        {
            return 1;
        }

        PixelInformation info = GetPixelInformation(x, y);

        if (IsPixelInformationInvalid(info))
        {
            Debug.LogWarning("THigns went BAD " + info);
        }

        AreaIndex area = _areas[info.areaX, info.areaY];

        switch (id)
        {
            case ByteDataLyerID.WaterLayerData:
                return area.AlphaDataLayer.WaterLayerData.data[info.areaPixelX, info.areaPixelY];
        }

        return 0;
    }

    public void SetByte(int x, int y, byte value, ByteDataLyerID id)
    {
        PixelInformation info = GetPixelInformation(x, y);
        AreaIndex area = _areas[info.areaX, info.areaY];

        switch (id)
        {
            case ByteDataLyerID.WaterLayerData:
                area.AlphaDataLayer.WaterLayerData.data[info.areaPixelX, info.areaPixelY] = value;
                break;
        }
    }
}