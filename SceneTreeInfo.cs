using Godot;
using System;
using System.Collections.Generic;
using System.Data;

namespace TurboTartine.ReparentScenePlugin
{
    public partial class SceneTreeInfo
    {
        public class NodeInfo
        {
            public string[] groups;
            public int index;
            public PackedScene instance;
            public string instancePlaceholder;
            public StringName name;
            public NodePath ownerPath;
            public NodePath path;
            public Dictionary<StringName, Variant> porperties;
            public StringName type;
            public bool isInstancePlaceHolder;
        }

        public PackedScene boundScene;
        public string boundScenePath;

        public List<NodeInfo> nodeInfos = new List<NodeInfo>();

        public SceneTreeInfo(string path)
        {
            InitFromPath(path);
        }

        public bool IsValid()
        {
            return nodeInfos.Count > 0;
        }

        private void InitFromPath(string scenePath)
        {
            boundScenePath = scenePath;
            boundScene = ResourceLoader.Load<PackedScene>(scenePath);
            SceneState state = boundScene.GetState();
            nodeInfos = new List<NodeInfo>();
            for (int i = 0; i < state.GetNodeCount(); i++)
            {
                NodeInfo nodeInfo = new NodeInfo();
                nodeInfo.groups = state.GetNodeGroups(i);
                nodeInfo.index = state.GetNodeIndex(i);
                nodeInfo.instance = state.GetNodeInstance(i);
                nodeInfo.instancePlaceholder = state.GetNodeInstancePlaceholder(i);
                nodeInfo.name = state.GetNodeName(i);
                nodeInfo.ownerPath = state.GetNodeOwnerPath(i);
                nodeInfo.path = state.GetNodePath(i);

                nodeInfo.porperties = new Dictionary<StringName, Variant>();
                for (int j = 0; j < state.GetNodePropertyCount(i); j++)
                {
                    nodeInfo.porperties.Add(state.GetNodePropertyName(i, j), state.GetNodePropertyValue(i, j));
                }

                nodeInfo.type = state.GetNodeType(i);
                nodeInfo.isInstancePlaceHolder = state.IsNodeInstancePlaceholder(i);

                nodeInfos.Add(nodeInfo);
            }
        }
        public NodeInfo FindParentNodeInfo(NodeInfo nodeInfo)
        {
            int nameCount = nodeInfo.path.GetNameCount();
            if (nameCount == 1) return null;

            string pathString = "";
            for (int i = 0; i < nameCount - 1; i++) pathString += nodeInfo.path.GetName(i) + "/";
            pathString = pathString.Remove(pathString.Length - 1);

            NodeInfo parentNodeInfo = FindNodeInfoByPath(pathString);
            if (parentNodeInfo == null) parentNodeInfo = FindNodeInfoByPath(nodeInfo.ownerPath);

            return parentNodeInfo;
        }

        public List<NodeInfo> FindAncestorsNodeInfo(NodeInfo nodeInfo)
        {
            List<NodeInfo> ancestors = new List<NodeInfo>();

            int nameCount = nodeInfo.path.GetNameCount();
            if (nameCount == 1) return ancestors;

            string pathString = "";
            for (int i = 0; i < nameCount - 1; i++)
            {
                pathString += nodeInfo.path.GetName(i);
                ancestors.Insert(0, FindNodeInfoByPath(pathString));
                pathString += "/";
            }

            return ancestors;
        }

        public List<NodeInfo> FindDescendantsNodeInfo(NodeInfo nodeInfo)
        {
            List<NodeInfo> descendant = new List<NodeInfo>();

            foreach (NodeInfo candidate in nodeInfos)
            {
                if (IsDescendantNodeInfo(candidate, nodeInfo))
                    descendant.Add(candidate);
            }

            return descendant;
        }

        //TODO optimize
        public bool IsDescendantNodeInfo(NodeInfo candidate, NodeInfo parent)
        {
            List<NodeInfo> ancestors = FindAncestorsNodeInfo(candidate);
            foreach (NodeInfo node in ancestors)
            {
                if (parent == node) return true;
            }

            return false;
        }

        public NodeInfo FindNodeInfoByPath(string stringPath)
        {
            foreach (NodeInfo nodeInfo in nodeInfos)
            {
                if (nodeInfo.path.GetConcatenatedNames() == stringPath) return nodeInfo;
            }

            return null;
        }
    }
}
