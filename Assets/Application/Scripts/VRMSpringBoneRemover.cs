using UnityEngine;
using VRM;

public class VRMSpringBoneRemover : MonoBehaviour
{
    void Start()
    {
        var components = GetComponentsInChildren<VRMSpringBone>();
        foreach(var component in components)
        {
            GameObject.Destroy(component);
        }
    }
}
