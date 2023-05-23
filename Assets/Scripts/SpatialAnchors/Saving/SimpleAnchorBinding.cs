using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

[Serializable]
public class SimpleAnchorBinding : IStorageBinding
{
    /// <summary>
    /// Storage field used for locally persisting TransformBindings across device boot ups.
    /// </summary>
    public static BindingsLocalStorage<SimpleAnchorBinding> Storage = new BindingsLocalStorage<SimpleAnchorBinding>("transformbindings.json");

    public string Id
    {
        get { return this.id; }
    }

    public string StickyText
    {
        get { return this.stickyText; }
    }

    public string ObjectType
    {
        get { return this.objectType; }
    }


    public MLAnchors.Anchor Anchor
    {
        get { return this.anchor; }
    }

    public string JsonData;

    [SerializeField, HideInInspector]
    private string id;

    [SerializeField, HideInInspector]
    private string stickyText;

    [SerializeField, HideInInspector]
    private MLAnchors.Anchor anchor;

    [SerializeField, HideInInspector]
    private string objectType;

    public bool Bind(MLAnchors.Anchor anchor,string stickyTextToSave, string jsonData, string objectTypeToSave)
    {
        this.JsonData = jsonData;
        id = anchor.Id;
        this.anchor = anchor;
        this.stickyText = stickyTextToSave;
        this.objectType = objectTypeToSave;
        
        Storage.SaveBinding(this);

        return true;
    }

    public bool UnBind()
    {
        return Storage.RemoveBinding(this);
    }

}
