﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldRendererLoader : MonoBehaviour {

    [SerializeField]
    private ServiceManager _serviceManager;

    [SerializeField]
    private Transform _target;

    [SerializeField]
    private int _radius;

    [SerializeField]
    private int _scale = 1;

    [SerializeField]
    private Material _material;

    private GameObject _renderingPlane;

    private WorldDataAccessService _worldDataAccessService;
    private int RedrawCountdown = 0;

    private void Start()
    {
        _worldDataAccessService = _serviceManager.GetService<WorldDataAccessService>();
        DrawGround();
    }

    // Update is called once per frame
    void Update ()
    {
		if(--RedrawCountdown == 0)
        {
            DrawGround();
        }
	}

    private void DrawGround()
    {
        Vector3 targetPosition = _target.position;
        _worldDataAccessService.GetToken(new TokenRequest((int)targetPosition.x - _radius, (int)targetPosition.x + _radius, (int)targetPosition.z + _radius, (int)targetPosition.z - _radius), OnGetTokenComplete, () => { });
    }

    private void OnGetTokenComplete(WorldDataToken token)
    {
        int testValue = token.GetInt(50, 50, IntDataID.NoiseLayerData);
        StartCoroutine(RenderGround(token));
    }

    private IEnumerator RenderGround(WorldDataToken token)
    {
        MeshCollider collider = null;

        if(_renderingPlane == null)
        {
            _renderingPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            collider = _renderingPlane.AddComponent<MeshCollider>();
            Rigidbody rb = _renderingPlane.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }
        else
        {
            collider = _renderingPlane.GetComponent<MeshCollider>();
        }

        GameObject plane = _renderingPlane;
        plane.GetComponent<Renderer>().material = _material;

        MeshFilter meshFilter = plane.GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.sharedMesh;
        collider.sharedMesh = mesh;

        int verticiesLength = (token.Request.width + 1) * (token.Request.height + 1);

        Vector3[] vertices = new Vector3[verticiesLength];
        Color[] colors = new Color[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];
        // Vector2[] triangles = new Vector2[(int)(dimensions.Area * 2)];

        //for every point, there is 2 triangles, equaling 6 total vertices
        int[] triangles = new int[(int)((token.Request.width * token.Request.height) * 6)];

        float totalWidth = token.Request.width * _scale;
        float totalHeight = token.Request.height * _scale;

        //Create Vertices
        for (int x = 0; x < token.Request.width + 1; x++)
        {
            for (int y = 0; y < token.Request.height + 1; y++)
            {
                int position = (x * (token.Request.width + 1)) + y;
                vertices[position] = new Vector3(token.Request.left + x * _scale, token.GetUshort(x, y, UshortDataID.HeightLayerData) * 0.5f, token.Request.top + y * _scale);
                colors[position] = new Color(0.5f, 0.5f, 0.5f);
                uvs[position] = new Vector2((vertices[position].x - token.Request.left) / totalWidth, (vertices[position].z - token.Request.top) / totalHeight);
               // Debug.Log(uvs[position]);
            }

            yield return 0;
        }

        List<Vector3> vectorTriangles = new List<Vector3>();

        //Create Triangles
        for (int x = 0; x < token.Request.width; x++)
        {
            for (int y = 0; y < token.Request.height; y++)
            {
                //we are making 2 triangles per loop. so offset goes up by 6 each time
                int triangleOffset = (x * token.Request.height + y) * 6;
                int verticeX = token.Request.width + 1;
                int verticeY = token.Request.height + 1;



                //triangle 1
                triangles[triangleOffset] = x * verticeY + y;
                triangles[1 + triangleOffset] = x * verticeY + y + 1;
                triangles[2 + triangleOffset] = x * verticeY + y + verticeY;

                vectorTriangles.Add(new Vector3(triangles[triangleOffset], triangles[1 + triangleOffset], triangles[2 + triangleOffset]));

                //triangle 2
                triangles[3 + triangleOffset] = x * verticeY + y + verticeY;
                triangles[4 + triangleOffset] = x * verticeY + y + 1;
                triangles[5 + triangleOffset] = x * verticeY + y + verticeY + 1;

                vectorTriangles.Add(new Vector3(triangles[3 + triangleOffset], triangles[4 + triangleOffset], triangles[5 + triangleOffset]));
            }
        }

        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors;

        RedrawCountdown = 100;
        plane.SetActive(true);
        _worldDataAccessService.ReturnToken(token);
    }
}