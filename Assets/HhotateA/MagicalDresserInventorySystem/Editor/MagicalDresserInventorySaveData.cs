﻿/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using HhotateA.AvatarModifyTools.Core;
using UnityEditor;
using UnityEngine.Serialization;
using System.Reflection;

namespace HhotateA.AvatarModifyTools.MagicalDresserInventorySystem
{
    public class MagicalDresserInventorySaveData : ScriptableObject
    {
        [FormerlySerializedAs("name")] public string saveName = "MagicalDresserInventorySaveData";
        public string avatarName;
        public int avatarGUID;
        public Texture2D icon = null;
        public List<MenuElement> menuElements = new List<MenuElement>();
        public LayerSettings[] layerSettingses;
        public AvatarModifyData assets;

        public MagicalDresserInventorySaveData()
        {
            layerSettingses = Enumerable.Range(0,Enum.GetValues(typeof(LayerGroup)).Length).
                Select(l=>new LayerSettings()).ToArray();
        }

        public void SaveGUID(GameObject root)
        {
            avatarGUID = root.GetInstanceID();
            avatarName = root.name;
            root = root.transform.parent?.gameObject;
            while (root)
            {
                avatarName = root.name + "/" + avatarName;
                root = root.transform.parent?.gameObject;
            }
        }

        public GameObject GetRoot()
        {
            var root = GameObject.Find(avatarName);
            if (root)
            {
                if (root.GetInstanceID() == avatarGUID)
                {
                    return root;
                }
            }
            return null;
        }

        public void ApplyRoot(GameObject root)
        {
            foreach (var menuElement in menuElements)
            {
                foreach (var item in menuElement.activeItems)
                {
                    item.GetRelativeGameobject(root.transform);
                }
                // menuElement.activeItems = menuElement.activeItems.Where(e => e.obj != null).ToList();
                
                foreach (var item in menuElement.inactiveItems)
                {
                    item.GetRelativeGameobject(root.transform);
                }
                // menuElement.inactiveItems = menuElement.inactiveItems.Where(e => e.obj != null).ToList();
            }
        }
        public void ApplyPath(GameObject root)
        {
            foreach (var menuElement in menuElements)
            {
                foreach (var item in menuElement.SafeActiveItems())
                {
                    item.GetRelativePath(root);
                }
                menuElement.activeItems = menuElement.activeItems.Where(e => !String.IsNullOrWhiteSpace(e.path)).ToList();
                foreach (var item in menuElement.SafeInactiveItems())
                {
                    item.GetRelativePath(root);
                }
                menuElement.inactiveItems = menuElement.inactiveItems.Where(e => !String.IsNullOrWhiteSpace(e.path)).ToList();
            }
        }
    }

    [Serializable]
    public class LayerSettings
    {
        public bool isSaved = true;
        public bool isRandom = false;
        //public DefaultValue defaultValue;
        public string defaultElementGUID = "";


        public MenuElement GetDefaultElement(List<MenuElement> datas)
        {
            return datas.FirstOrDefault(d => d.guid == defaultElementGUID);
        }
        
        public void SetDefaultElement(MenuElement element)
        {
            if (element != null)
            {
                defaultElementGUID = element.guid;
            }
            else
            {
                defaultElementGUID = "";
            }
        }
    }
    
    [Serializable]
    public class MenuElement
    {
        public string name;
        public Texture2D icon;
        public List<ItemElement> activeItems = new List<ItemElement>();

        public List<ItemElement> SafeActiveItems()
        {
            return activeItems.Where(e => e.obj != null).ToList();
        }
        public List<ItemElement> inactiveItems = new List<ItemElement>();
        public List<ItemElement> SafeInactiveItems()
        {
            return inactiveItems.Where(e => e.obj != null).ToList();
        }
        public bool isToggle = true;
        public LayerGroup layer = LayerGroup.Layer_A;
        public bool isSaved = true;
        public bool isDefault = false;
        public bool isRandom = false;
        public string param = "";
        public int value = 0;
        public string guid;
        
        public List<SyncElement> activeSyncElements = new List<SyncElement>();
        public List<SyncElement> inactiveSyncElements = new List<SyncElement>();

        public bool extendOverrides = false;
        public bool isOverrideActivateTransition = false;
        public ItemElement overrideActivateTransition = new ItemElement();
        public bool isOverrideInactivateTransition = false;
        public ItemElement overrideInactivateTransition = new ItemElement();

        public MenuElement()
        {
            guid = Guid.NewGuid().ToString();
        }

        public string DefaultIcon()
        {
            return isToggle ? EnvironmentGUIDs.itemIcon :
                layer == LayerGroup.Layer_A ? EnvironmentGUIDs.dressAIcon :
                layer == LayerGroup.Layer_B ? EnvironmentGUIDs.dressBIcon :
                layer == LayerGroup.Layer_C ? EnvironmentGUIDs.dressCIcon :
                EnvironmentGUIDs.itemboxIcon;
        }

        public void SetToDefaultIcon()
        {
            icon = AssetUtility.LoadAssetAtGuid<Texture2D>(DefaultIcon());
        }

        public bool IsDefaultIcon()
        {
            return AssetDatabase.GetAssetPath(icon) == AssetDatabase.GUIDToAssetPath(DefaultIcon());
        }

        public void SetLayer(bool t)
        {
            if (IsDefaultIcon())
            {
                isToggle = t;
                SetToDefaultIcon();
            }
            else
            {
                isToggle = t;
            }
        }
        public void SetLayer(LayerGroup l)
        {
            if (IsDefaultIcon())
            {
                layer = l;
                SetToDefaultIcon();
            }
            else
            {
                layer = l;
            }
        }
    }

    [Serializable]
    public class SyncElement
    {
        public string guid;
        public float delay;
        public bool syncOn;
        public bool syncOff;

        public SyncElement(string id)
        {
            guid = id;
            delay = -1f;
            syncOn = false;
            syncOff = false;
        }
    }
    
    [Serializable]
    public class ItemElement
    {
        public string path;
        public GameObject obj;
        public bool active = true;
        public FeedType type = FeedType.None;
        public float delay = 0f;
        public float duration = 1f;

        //public Shader animationShader;
        public Material animationMaterial;
        public string animationParam = "_AnimationTime";
        public float animationParamOff = 0f;
        public float animationParamOn = 1f;

        public bool extendOption = false;
        public List<RendererOption> rendOptions = new List<RendererOption>();

        public ItemElement()
        {
        }

        public ItemElement(GameObject o,GameObject root = null,bool defaultActive = true)
        {
            obj = o;
            active = defaultActive;

            rendOptions = o?.GetComponentsInChildren<Renderer>().Select(r => new RendererOption(r, o)).ToList();
            
            if(root) GetRelativePath(root);
        }
        
        public ItemElement Clone(bool invert = false)
        {
            return new ItemElement(obj)
            {
                active = invert ? !active : active,
                path = path,
                type = type,
                delay = delay,
                duration = duration,
                animationMaterial = animationMaterial,
                animationParam = animationParam,
                rendOptions = rendOptions.Select(r=>r.Clone(obj,invert)).ToList(),
            };
        }
        
        public void GetRelativePath(GameObject root)
        {
            if (!obj) return;
            if (obj == root)
            {
                path = "";
            }
            else
            {
                path = obj.gameObject.name;
                Transform parent = obj.transform.parent;
                while (parent != null)
                {
                    if(parent.gameObject == root) break;
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }
            }
        }
        
        public void GetRelativeGameobject(Transform root)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                obj = null;
                return;
            }
            root = root.Find(path);

            obj = root.gameObject;
            
            /*foreach (var rendOption in rendOptions)
            {
                rendOption.GetRelativeGameobject(obj.transform);
            }*/
            var currentRendOptions = rendOptions;
            var newRendOptions = obj.GetComponentsInChildren<Renderer>().Select(r => new RendererOption(r, obj)).ToList();
            for (int i = 0; i < newRendOptions.Count; i++)
            {
                var currentRendOption = currentRendOptions.FirstOrDefault(r => r.path == newRendOptions[i].path);
                if (currentRendOption!=null)
                {
                    // 設定飛んでそうだったら，破棄
                    currentRendOption.GetRelativeGameobject(obj.transform);
                    if (currentRendOption.rend == newRendOptions[i].rend &&
                        currentRendOption.changeMaterialsOptions.Count ==
                        newRendOptions[i].changeMaterialsOptions.Count &&
                        currentRendOption.changeBlendShapeOptions.Count ==
                        newRendOptions[i].changeBlendShapeOptions.Count)
                    {
                        newRendOptions[i] = currentRendOption;
                    }
                }
            }
            rendOptions = newRendOptions;
        }
    }

    [Serializable]
    public class RendererOption
    {
        public string path;
        public Renderer rend;
        public bool extendMaterialOption = false;
        public bool extendBlendShapeOption = false;
        public List<MaterialOption> changeMaterialsOptions = new List<MaterialOption>();
        public List<BlendShapeOption> changeBlendShapeOptions = new List<BlendShapeOption>();
        [FormerlySerializedAs("changeMaterialsOption")] [SerializeField] private List<Material> compatibility_changeMaterialsOption = null;
        [FormerlySerializedAs("changeBlendShapeOption")] [SerializeField] private List<float> compatibility_changeBlendShapeOption = null;

        public RendererOption(Renderer r, GameObject root)
        {
            rend = r;
            GetRelativePath(root);
            if (r)
            {
                changeMaterialsOptions = r.sharedMaterials.Select(m => new MaterialOption(m)).ToList();
                if (r is SkinnedMeshRenderer)
                {
                    changeBlendShapeOptions = Enumerable.Range(0, r.GetMesh().blendShapeCount).Select(i =>
                        new BlendShapeOption((r as SkinnedMeshRenderer).GetBlendShapeWeight(i))).ToList();
                }
            }
        }
        public RendererOption Clone(GameObject root,bool invert = false)
        {
            var clone = new RendererOption(rend, root);
            clone.changeMaterialsOptions = changeMaterialsOptions.Select(e => e.Clone()).ToList();
            clone.changeBlendShapeOptions = changeBlendShapeOptions.Select(e=> e.Clone()).ToList();
            if (invert)
            {
                for (int i = 0; i < changeMaterialsOptions.Count; i++)
                {
                    clone.changeMaterialsOptions[i].material = rend.sharedMaterials[i];
                }

                if (rend is SkinnedMeshRenderer)
                {
                    for (int i = 0; i < changeBlendShapeOptions.Count; i++)
                    {
                        clone.changeBlendShapeOptions[i].weight = (rend as SkinnedMeshRenderer).GetBlendShapeWeight(i);
                    }
                }
            }

            return clone;
        }
        
        public void GetRelativePath(GameObject root)
        {
            if (!rend) return;
            if (rend.gameObject == root)
            {
                path = "";
            }
            else
            {
                path = rend.gameObject.name;
                Transform parent = rend.transform.parent;
                while (parent != null)
                {
                    if(parent.gameObject == root) break;
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }
            }
        }
        
        public void GetRelativeGameobject(Transform root)
        {
            if (!String.IsNullOrWhiteSpace(path))
            {
                root = root.Find(path);
            }

            rend = root.gameObject.GetComponent<Renderer>();
            ReplaceMaterialOption();
        }

        public void ReplaceMaterialOption()
        {
            if (compatibility_changeMaterialsOption!=null)
            {
                if (compatibility_changeMaterialsOption.Count > 0)
                {
                    changeMaterialsOptions = compatibility_changeMaterialsOption
                        .Select(m => new MaterialOption(m)).ToList();
                    compatibility_changeMaterialsOption = new List<Material>();
                }
            }

            if (compatibility_changeBlendShapeOption != null)
            {
                if (compatibility_changeBlendShapeOption.Count > 0)
                {
                    changeBlendShapeOptions = compatibility_changeBlendShapeOption
                        .Select(m => new BlendShapeOption(m)).ToList();
                    compatibility_changeBlendShapeOption = new List<float>();
                }
            }
        }
    }

    [Serializable]
    public class MaterialOption
    {
        public bool change = false;
        public Material material;
        public float delay = -1f;
        // public float duration = 1f;

        public MaterialOption(Material mat)
        {
            material = mat;
        }

        public MaterialOption Clone()
        {
            var clone = new MaterialOption(material);
            clone.change = change;
            clone.delay = delay;
            // clone.duration = duration;
            return clone;
        }
    }

    [Serializable]
    public class BlendShapeOption
    {
        public bool change = false;
        public float weight;
        public float delay = -1f;
        public float duration = 1f;

        public BlendShapeOption(float w)
        {
            weight = w;
        }
        
        public BlendShapeOption Clone()
        {
            var clone = new BlendShapeOption(weight);
            clone.change = change;
            clone.delay = delay;
            clone.duration = duration;
            return clone;
        }
    }

    public static class AssetLink
    {
        /*public static Shader GetShaderByType(this FeedType type)
        {
            if (type == FeedType.Feed)
            {
                return AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentGUIDs.feedShader);
            }
            if (type == FeedType.Crystallize)
            {
                return AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentGUIDs.crystallizeShader);
            }
            if (type == FeedType.Dissolve)
            {
                return AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentGUIDs.disolveShader);
            }
            if (type == FeedType.Draw)
            {
                return AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentGUIDs.drawShader);
            }
            if (type == FeedType.Explosion)
            {
                return AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentGUIDs.explosionShader);
            }
            if (type == FeedType.Geom)
            {
                return AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentGUIDs.geomShader);
            }
            if (type == FeedType.Mosaic)
            {
                return AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentGUIDs.mosaicShader);
            }
            if (type == FeedType.Polygon)
            {
                return AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentGUIDs.polygonShader);
            }
            if (type == FeedType.Bounce)
            {
                return AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentGUIDs.scaleShader);
            }

            return null;
        }*/
        
        public static Material GetMaterialByType(this FeedType type)
        {
            if (type == FeedType.Fade)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.fadeMaterial);
            }
            if (type == FeedType.Crystallize)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.crystallizeMaterial);
            }
            if (type == FeedType.Dissolve)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.disolveMaterial);
            }
            if (type == FeedType.Draw)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.drawMaterial);
            }
            if (type == FeedType.Explosion)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.explosionMaterial);
            }
            if (type == FeedType.Geom)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.geomMaterial);
            }
            if (type == FeedType.Mosaic)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.mosaicMaterial);
            }
            if (type == FeedType.Polygon)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.polygonMaterial);
            }
            if (type == FeedType.Bounce)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.scaleMaterial);
            }
            if (type == FeedType.Leaf)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.leafMaterial);
            }
            if (type == FeedType.Bloom)
            {
                return AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.bloomMaterial);
            }

            return null;
        }

        public static FeedType GetTypeByMaterial(this Material mat)
        {
            var guid = AssetUtility.GetAssetGuid(mat);
            if (guid == EnvironmentGUIDs.fadeMaterial)
            {
                return FeedType.Fade;
            }
            if (guid == EnvironmentGUIDs.crystallizeMaterial)
            {
                return FeedType.Crystallize;
            }
            if (guid == EnvironmentGUIDs.disolveMaterial)
            {
                return FeedType.Dissolve;
            }
            if (guid == EnvironmentGUIDs.drawMaterial)
            {
                return FeedType.Draw;
            }
            if (guid == EnvironmentGUIDs.explosionMaterial)
            {
                return FeedType.Explosion;
            }
            if (guid == EnvironmentGUIDs.geomMaterial)
            {
                return FeedType.Geom;
            }
            if (guid == EnvironmentGUIDs.mosaicMaterial)
            {
                return FeedType.Mosaic;
            }
            if (guid == EnvironmentGUIDs.polygonMaterial)
            {
                return FeedType.Polygon;
            }
            if (guid == EnvironmentGUIDs.scaleMaterial)
            {
                return FeedType.Bounce;
            }
            if (guid == EnvironmentGUIDs.leafMaterial)
            {
                return FeedType.Leaf;
            }
            if (guid == EnvironmentGUIDs.bloomMaterial)
            {
                return FeedType.Bloom;
            }

            return FeedType.None;
        }
            
    }
    
    public enum FeedType
    {
        None,
        Scale,
        Shader,
        Fade,
        Crystallize,
        Dissolve,
        Draw,
        Explosion,
        Geom,
        Mosaic,
        Polygon,
        Bounce,
        Leaf,
        Bloom,
    }

    public enum LayerGroup : int
    {
        Layer_A,
        Layer_B,
        Layer_C,
        Layer_D,
        Layer_E,
        Layer_F,
        Layer_G,
        Layer_H,
        Layer_I,
        Layer_J,
    }

    public enum ToggleGroup
    {
        IsToggle,
    }
}