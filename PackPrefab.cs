using UnityEngine;
using System.IO;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

public class PackPrefab {

    public class PrefabTree
    {
        public TreeNode Root;        
        public static void PrintTree(TreeNode node)
        {            
            Debug.Log("Tree Node:" + node.Path);
			if(node.Children == null)return;
            foreach (var item in node.Children)
            {
                PrintTree(item);
            }
        }
    }

    public class TreeNode
    {
        public string Path;
        public TreeNode Parent;
        public List<TreeNode> Children;
        public TreeNode Find(string path)
        {
            foreach (var item in Children)
            {
                if (path == item.Path) return item;
            }
            return null;
        }
        public TreeNode Find(TreeNode node)
        {
            foreach (var item in Children)
            {
                if (node.Path == item.Path) return item;
            }
            return null;
        }
		public int IndexOf(TreeNode node)
		{
			for(int i = 0; i < Children.Count; ++i)
			{
				if(Children[i].Path == node.Path)return i;
			}
			return -1;
		}
        public void RemoveRedundant(List<TreeNode> preserve)
        {
            int i = 0;
            for (; i < Children.Count; )
            {
                int j = 0;
                for(; j < preserve.Count; ++j)
                {
                    if (Children[i].Path == preserve[j].Path)
                    {
                        break;                        
                    }
                }
                if (j >= preserve.Count)
                {
                    Debug.LogWarning("Remove:" + Children[i].Path);
                    Children.RemoveAt(i);
                }
                else
                    ++i;
            }            
        }
    }

    public static PrefabTree GenerateTree(string prefabPath)
    {
        PrefabTree tree = new PrefabTree();
        tree.Root = new TreeNode();
        tree.Root.Parent = null;
        tree.Root.Path = prefabPath;
        tree.Root.Children = new List<TreeNode>();

        //find tree element
        string[] depends = AssetDatabase.GetDependencies(new string[] { prefabPath });
        Debug.Log("depends:" + depends.Length);
        List<TreeNode> depListNode = new List<TreeNode>();
        foreach (var item in depends)
        {
            if (prefabPath == item) continue;
            TreeNode tmp = new TreeNode() { Path = item, Parent = tree.Root, Children = new List<TreeNode>() };
            //depListNode.Add();
            string[] depList = AssetDatabase.GetDependencies(new string[] { item });
            foreach (var it in depList)
            {
                if (it == tmp.Path) continue;
                TreeNode t = new TreeNode() { Path = it, Parent = tmp, Children = null};
                tmp.Children.Add(t);
            }
            depListNode.Add(tmp);
            tree.Root.Children.Add(tmp);
        }
        TreeNode parent = tree.Root;
        List<TreeNode> queue = new List<TreeNode>();
        queue.Add(parent);
        while (true)
        {
            int i = 0;
            List<TreeNode> tmp = new List<TreeNode>();
            for (; i < depListNode.Count; ++i)
            {
                if (!IsDependByAny(depListNode[i], depListNode))
                {
                    tmp.Add(depListNode[i]);
                    Debug.LogWarning( parent.Path + "=>Add node:" + depListNode[i].Path);
                }
            }
			List<TreeNode> tmp_1 = new List<TreeNode>();
			for(int j = 0; j < queue.Count; ++j)
			{
				queue[j].RemoveRedundant(tmp);
				if(queue[j].Children.Count == 0)continue;
				foreach (var item in tmp)
	            {
	                tmp_1.Add(item);
	                depListNode.Remove(item);
	                int index = queue[j].IndexOf(item);
	                if (index != -1)
	                {
	                    queue[j].Children[index] = item;
	                }                
	            }
			}
			if(tmp_1.Count > 0)
			{
				queue.Clear();
				queue.AddRange(tmp_1);
			}
			else
			{
				break;
			}            
        }        
        return tree;
    }

    private static bool IsDependByAny(TreeNode para1, List<TreeNode> treeNodeLst)
    {
        foreach (var item in treeNodeLst)
        {
            if (item.Path == para1.Path) continue;
            foreach (var it in item.Children)
            {
                if (it.Path == para1.Path) return true;
            }
        }
        return false;
    }

    private static bool IsDependByAny(string path, List<TreeNode> listTreeNode)
    {
        List<string> subDepends = new List<string>();
        foreach (var item in listTreeNode)
        {
            if (path == item.Path) continue;
            subDepends.Add(item.Path);            
        }
        string[] depends = AssetDatabase.GetDependencies(subDepends.ToArray());
        foreach (var item in depends)
        {
            if (path == item) return true;
        }
        return false;
    }

    [MenuItem("Qiyu/Dyz/Prefab/BuildTree")]
    public static void BuildTree()
    {
        PrefabTree tree = GenerateTree("Assets/Prefabs/SpritePrefab.prefab");
        //PrefabTree.PrintTree(tree.Root);
		packTree(tree);
    }
	
	private static void packTree(PrefabTree tree)
	{
		string Dir = "Prefabs/";
		BuildAssetBundleOptions opts = 0;
		opts |= BuildAssetBundleOptions.CompleteAssets;
        opts |= BuildAssetBundleOptions.UncompressedAssetBundle;
        opts |= BuildAssetBundleOptions.DeterministicAssetBundle;
        opts |= BuildAssetBundleOptions.CollectDependencies;
		
		packNode(tree.Root, Dir, opts);
	}
	
	private static void packNode(TreeNode node, string dir, BuildAssetBundleOptions opts)
	{
		if(node.Path.EndsWith(".cs"))return;
		BuildPipeline.PushAssetDependencies();
		string fnMain = node.Path.Substring(node.Path.LastIndexOf('/') + 1);
		fnMain = fnMain.Replace(".prefab", ".unity3d");		
		foreach(var item in node.Children)
		{
			if(item.Path.EndsWith(".cs"))continue;
			if(item.Path == node.Path)continue;
			packNode(item, dir, opts);			
			Debug.LogError("pushItem:" + item.Path);
			string fn = item.Path.Substring(item.Path.LastIndexOf('/') + 1);
			fn = fn.Replace(".prefab", ".unity3d");
			BuildPipeline.BuildAssetBundle(AssetDatabase.LoadMainAssetAtPath(item.Path), null, dir + fn, opts);
			/*foreach(var it in item.Children)
			{				
				if(it.Path.EndsWith(".cs"))continue;
				string fn_1 = it.Path.Substring(it.Path.LastIndexOf('/') + 1);
				fn_1 = fn_1.Replace(".prefab", ".unity3d");
				Debug.LogError("pushItem:" + it.Path);
				BuildPipeline.BuildAssetBundle(AssetDatabase.LoadMainAssetAtPath(it.Path), null, dir + fn_1, opts);
				/*foreach(var i in it.Children)
				{
					if(i.Path.EndsWith(".cs"))continue;
					string fn_2 = i.Path.Substring(i.Path.LastIndexOf('/') + 1);
					fn_2 = fn_2.Replace(".prefab", ".unity3d");
					Debug.LogError("pushItem:" + i.Path);
					BuildPipeline.BuildAssetBundle(AssetDatabase.LoadMainAssetAtPath(i.Path), null, dir + fn_2, opts);
				}
			}*/
		}
		BuildPipeline.PushAssetDependencies();
		Debug.LogError("pushNode:" + node.Path);
	    BuildPipeline.BuildAssetBundleExplicitAssetNames(new Object[]{
			AssetDatabase.LoadMainAssetAtPath(node.Path)}, new string[]{"MainAsset"}, dir + fnMain, opts);
		BuildPipeline.PopAssetDependencies();
		BuildPipeline.PopAssetDependencies();
	}
	
	[MenuItem("Qiyu/Dyz/Prefab/Pack")]
	public static void PackSelPrefab()
	{
		string Dir = "Prefabs/";
		Object obj = Selection.activeObject;
		string path_tmp = AssetDatabase.GetAssetPath(obj);
		string[] dependPaths = AssetDatabase.GetDependencies(new string[]{path_tmp});
		List<string> fileDependsPath = new List<string>();
		BuildAssetBundleOptions opts = 0;
		opts |= BuildAssetBundleOptions.CompleteAssets;
        opts |= BuildAssetBundleOptions.UncompressedAssetBundle;
        opts |= BuildAssetBundleOptions.DeterministicAssetBundle;
        opts |= BuildAssetBundleOptions.CollectDependencies;
		foreach(var item in dependPaths)
		{
			if(item.Contains("SpritePrefab") || item.Contains(".cs"))continue;
			fileDependsPath.Add(item);
		}
        //BuildPipeline.PushAssetDependencies();				
	    BuildPipeline.PushAssetDependencies();
		foreach(var item in fileDependsPath)
		{			
			string fn = item.Substring(item.LastIndexOf('/') + 1);
			BuildPipeline.BuildAssetBundle(AssetDatabase.LoadMainAssetAtPath(item), null, Dir + fn, opts,BuildTarget.StandaloneWindows);			
		}        				
		BuildPipeline.PushAssetDependencies();
		//Object[] selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
        BuildPipeline.BuildAssetBundle(obj, new Object[]{obj},Dir + "SpritePrefab", opts, BuildTarget.StandaloneWindows);
		BuildPipeline.PopAssetDependencies();
		BuildPipeline.PopAssetDependencies();
		/*BuildAssetBundleOptions opts = 0;
		opts |= BuildAssetBundleOptions.CompleteAssets;
        opts |= BuildAssetBundleOptions.UncompressedAssetBundle;
        opts |= BuildAssetBundleOptions.DeterministicAssetBundle;
		opts |= BuildAssetBundleOptions.CollectDependencies;
		Object[] selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets); 
		if(!System.IO.Directory.Exists(Dir))
		{
			System.IO.Directory.CreateDirectory(Dir);
		}
		string path = Dir + obj.name;
		BuildPipeline.BuildAssetBundleExplicitAssetNames(new Object[]{obj}, new string[]{"MainAsset"}, path, opts);*/
		Debug.Log("pack complete:");
	}

}
