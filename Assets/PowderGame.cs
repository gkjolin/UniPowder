﻿using System;
using System.CodeDom;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Transforms2D;
using UnityEngine;

public static class PowderGame
{
    public static int height = 480;
    public static int width = 640;
    public static int frame = 0;
    public static int powderCount = 0;
    public static Color worldBoundariesColor = Color.white;
    public static EntityArchetype powderArchetype;

    public static int brushSize = 0;
    public static int currentPowder = PowderTypes.Sand;
    public static Camera mainCamera;

    public static Rect pixelWorldRect;
    public static Rect unitWorldRect;
    public static Vector3 unitWorldPos = new Vector3(350, 230, 0);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        var x = (Screen.width - width) / 2;
        var y = (Screen.height - height) / 2;
        pixelWorldRect = new Rect(x, y, width, height);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitializeWithScene()
    {
        var mgr = World.Active.GetOrCreateManager<EntityManager>();
        powderArchetype = mgr.CreateArchetype(typeof(Powder), typeof(Position2D), typeof(TransformMatrix), typeof(Heading2D));
        PowderTypes.Init(mgr);

        var proto = GameObject.Find("Main Camera");
        if (proto == null)
        {
            throw new Exception("Cannot find Main Camera");
        }
        mainCamera =  proto.GetComponent<Camera>();
        var bottomLeftCorner = new Vector3(0, 0, 0);
        var bottomLeftCornerUnit = mainCamera.ScreenToWorldPoint(bottomLeftCorner);
        var newCameraPosition = bottomLeftCorner - bottomLeftCornerUnit;
        newCameraPosition.y = 1;
        mainCamera.transform.position = newCameraPosition;

        unitWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(pixelWorldRect.x, pixelWorldRect.y));
        var topRightCornerWorld = mainCamera.ScreenToWorldPoint(new Vector3(pixelWorldRect.x + pixelWorldRect.width, pixelWorldRect.y + pixelWorldRect.height));
        unitWorldRect = new Rect(
            new Vector2(unitWorldPos.x, unitWorldPos.z), 
            new Vector2(topRightCornerWorld.x - unitWorldPos.x, topRightCornerWorld.z - unitWorldPos.z));

        SetupWorld(mgr);
    }

    public static void SetupWorld(EntityManager mgr)
    {
        // Put a solid ground:
        /*
        for (var x = 0; x < width; ++x)
        {
            for (var y = 0; y < 5; ++y)
            {
                Spawn(mgr, x, y, PowderTypes.Stone);
            }
        }
        */

        /*
        for (var x = 0; x < 10; x+=2)
        {
            for (var y = 0; y < 10; y+=2)
            {
                Spawn(mgr, x, y, PowderTypes.Stone);
            }
        }
        */

        /*
        Spawn(mgr, 0, 0, PowderTypes.Stone);
        Spawn(mgr, 0, 1, PowderTypes.Sand);
        */

        for (var y = 0; y < height; y++)
        {
            Spawn(mgr, 25, y, PowderTypes.Stone);
        }

        for (var x = 0; x < width; x++)
        {
            Spawn(mgr, x, 25, PowderTypes.Sand);
        }
    }

    public static int CoordKey(Vector2Int coord)
    {
        return coord.x * width + coord.y;
    }

    public static int CoordKey(int x, int y)
    {
        return x * width + y;
    }

    public static bool IsInWorld(Vector2 pos)
    {
        return pixelWorldRect.Contains(pos);
    }

    public static void Reset()
    {
        var mgr = World.Active.GetOrCreateManager<EntityManager>();
        var allEntities = mgr.GetAllEntities();
        powderCount = 0;
        mgr.DestroyEntity(allEntities);
        allEntities.Dispose();
        SetupWorld(mgr);
    }

    public static void PrintInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Camera rect: " + mainCamera.rect);
        sb.AppendLine("pixelRect: " + mainCamera.pixelRect);
        sb.AppendLine("pixelHeight: " + mainCamera.pixelHeight);
        sb.AppendLine("pixelWidth: " + mainCamera.pixelWidth);
        sb.AppendLine("scaledPixelWidth: " + mainCamera.scaledPixelWidth);
        sb.AppendLine("scaledPixelHeight: " + mainCamera.scaledPixelHeight);

        var bottomLeftCorner = new Vector3(0, 0, 0);
        var topRightCorner = new Vector3(mainCamera.pixelWidth, mainCamera.pixelHeight, 0);
        var bottomLeftCornerWorld = mainCamera.ScreenToWorldPoint(bottomLeftCorner);
        var topRightCornerWorld = mainCamera.ScreenToWorldPoint(topRightCorner);
        sb.AppendLine("bottomLeftCornerWorld: " + bottomLeftCornerWorld);
        sb.AppendLine("topRightCornerWorld: " + topRightCornerWorld);

        var worldWidth = topRightCornerWorld.x - bottomLeftCornerWorld.x;
        var worldHeight = topRightCornerWorld.z - bottomLeftCornerWorld.z;
        sb.AppendLine("WorldSize: " + worldWidth + ", " + worldHeight);

        sb.AppendLine("AspectRatio: " + mainCamera.aspect + " OrthoSize: " + mainCamera.orthographicSize);

        Debug.Log(sb.ToString());
    }

    public static Vector2Int ScreenToCoord(Vector3 pos)
    {
        return new Vector2Int((int)(pos.x - pixelWorldRect.x), (int)(pos.y - pixelWorldRect.y));
    }

    public static float2 CoordToWorld(Vector2Int coord)
    {
        return CoordToWorld(coord.x, coord.y);
    }

    public static float2 CoordToWorld(int x, int y)
    {
        var screenPos = new Vector3(x + pixelWorldRect.x, y + pixelWorldRect.y, 0);
        var unitPos = mainCamera.ScreenToWorldPoint(screenPos);
        return new float2(unitPos.x, unitPos.z);
    }

    public static Entity Spawn(EntityManager mgr, int x, int y, int type)
    {
        var e = mgr.CreateEntity(powderArchetype);
        mgr.SetComponentData(e, PowderTypes.values[type].creator(new Vector2Int(x, y)));
        mgr.SetComponentData(e, new Position2D { Value = CoordToWorld(x, y) });
        mgr.SetComponentData(e, new Heading2D { Value = new float2(0.0f, 1.0f) });
        mgr.AddSharedComponentData(e, PowderTypes.values[type].renderer);
        powderCount++;
        return e;
    }

    public static void Spawn(EntityCommandBuffer cmdBuffer, int x, int y, int type)
    {
        cmdBuffer.CreateEntity(PowderGame.powderArchetype);
        cmdBuffer.SetComponent(PowderTypes.values[type].creator(new Vector2Int(x, y)));
        cmdBuffer.SetComponent(new Position2D { Value = CoordToWorld(x, y) });
        cmdBuffer.SetComponent(new Heading2D { Value = new float2(0.0f, 1.0f) });
        cmdBuffer.AddSharedComponent(PowderTypes.values[type].renderer);
        powderCount++;
    }

    public static bool Chance(float prob)
    {
        return UnityEngine.Random.Range(0f, 1.0f) <= prob;
    }
}
