using System.Collections.Generic;
using UnityEngine;

public class LightgunInteractable : InteractableBase
{
    public List<Renderer> renderers;
    public GameObject toHideGameObject;
    [ColorUsage(true, true)] public Color emissionBaseColor = new Color(1f, 1f, 0.3f);
    public float intensity = 0.2f;
    public float speed = 1f;

    private List<Material> _matList = new List<Material>();
    private float _time;

    public override void Interact()
    {
        PlayerStatSystem.Instance.CanUseWeapon.Value = true;
        toHideGameObject.SetActive(false);
    }

    protected override void Awake()
    {
        base.Awake();
        toHideGameObject ??= gameObject;
        foreach (Renderer r in renderers)
        {
            if (r != null)
            {
                var mat = r.material;
                mat.EnableKeyword("_EMISSION");
                _matList.Add(mat);
            }
        }
    }

    void Update()
    {
        _time += Time.deltaTime * speed;
        float t = Mathf.Sin(_time) * 0.5f + 0.5f;  // 0~1

        Color finalColor = emissionBaseColor * (t * intensity);

        foreach (var mat in _matList)
        {
            mat.SetVector("_EmissionColor", finalColor);
        }
    }
}
