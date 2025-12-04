using UnityEngine;
using UnityEngine.VFX;
using Unity.Entities;

public class BeamVFXUpdater : MonoBehaviour
{
    public VisualEffect vfx;
    public Entity entity;
    private EntityManager entityManager;

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    void Update()
    {

        Debug.Log("Updating Beam VFX");

        var endpoints = entityManager.GetComponentData<BeamComponent>(entity);

        vfx.SetVector3("Start", endpoints.Start);
        vfx.SetVector3("End", endpoints.End);
        vfx.SendEvent("OnPlay");
    }
}
