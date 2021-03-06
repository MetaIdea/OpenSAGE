﻿using System.Collections.Generic;
using System.Numerics;
using OpenSage.Data.Map;
using OpenSage.Logic.Object;
using OpenSage.Mathematics;
using OpenSage.Terrain;

namespace OpenSage.Navigation
{
    public class Navigation
    {
        readonly Graph _graph;
        readonly HeightMap _heightMap;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public Navigation(BlendTileData tileData, HeightMap heightMap)
        {
            var width = tileData.Impassability.GetLength(0);
            var height = tileData.Impassability.GetLength(1);
            _graph = new Graph(width, height);

            for (int x = 0; x < _graph.Width; x++)
            {
                for (int y = 0; y < _graph.Height; y++)
                {
                    var impassable = tileData.Impassability[x, y];
                    _graph.GetNode(x, y).Passability = impassable ? Passability.Impassable : Passability.Passable;
                }
            }

            _heightMap = heightMap;
        }

        private Vector2 GetNodePosition(Node node)
        {
            var xyz =_heightMap.GetPosition(node.X, node.Y);
            return new Vector2(xyz.X, xyz.Y);
        }

        private Node GetClosestNode(Vector3 pos)
        {
            var coords = _heightMap.GetTilePosition(pos);

            if (coords == null)
            {
                return null;
            }

            var (x, y) = coords.Value;
            return _graph.GetNode(x, y);
        }

        public IEnumerable<Vector3> CalculatePath(Vector3 start, Vector3 end)
        {
            var startNode = GetClosestNode(start);
            var endNode = GetClosestNode(end);

            if (startNode == null || endNode == null || !endNode.IsPassable)
            {
                Logger.Info("Aborting pathfinding because start and/or end are null or impassable.");
                yield break;
            }

            var route = _graph.Search(startNode, endNode);

            if (route == null)
            {
                Logger.Warn($"Graph search failed to find a path between {start} and {end}.");
                yield break;
            }

            PathOptimizer.RemoveRedundantNodes(route);
            PathOptimizer.SmoothPath(route, _graph);

            foreach (var node in route)
            {
                var pos = GetNodePosition(node);
                yield return new Vector3(pos.X, pos.Y, _heightMap.GetHeight(pos.X, pos.Y));
            }
        }

        public void UpdateAreaPassability(GameObject gameObject, bool passable)
        {
            if (gameObject.Collider == null) return;

            var aabb = gameObject.Collider.GetAxisAlignedBoundingBox();

            var bottomLeftNode = GetClosestNode(aabb.Min);
            var topRightNode = GetClosestNode(aabb.Max);

            //sometimes map objects are places outside the actual map....
            if (bottomLeftNode == null || topRightNode == null) return;

            var area = gameObject.Collider.GetBoundingArea();

            for (var x = 0; x < topRightNode.X - bottomLeftNode.X; x++)
            {
                for (var y = 0; y < topRightNode.Y - bottomLeftNode.Y; y++)
                {
                    var node = _graph.GetNode(bottomLeftNode.X + x, bottomLeftNode.Y + y);
                    var position = GetNodePosition(node);
                    if (area.Contains(position))
                    {
                        node.Passability = passable ? Passability.Passable : Passability.Impassable;
                    }
                }
            }
        }
    }
}
