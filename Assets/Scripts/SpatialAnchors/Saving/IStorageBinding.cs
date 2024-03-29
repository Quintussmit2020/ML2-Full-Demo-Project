using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

public interface IStorageBinding
{
    public string Id { get; }
    public string StickyText { get; }

    public string ObjectType { get; }

}
