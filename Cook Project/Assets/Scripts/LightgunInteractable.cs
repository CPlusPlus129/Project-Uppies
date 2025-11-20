using UnityEngine;

public class LightgunInteractable : InteractableBase
{
    public Renderer rend;
    [ColorUsage(true, true)]  public Color emissionBaseColor = new Color(1f, 1f, 0.3f);
    public float intensity = 0.2f;
    public float speed = 1f;

    private Material _mat;
    private float _time;

    public override void Interact()
    {
        PlayerStatSystem.Instance.CanUseWeapon.Value = true;
        gameObject.SetActive(false);
    }

    protected override void Awake()
    {
        base.Awake();
        _mat = rend.material;
        _mat.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        _time += Time.deltaTime * speed;
        float t = Mathf.Sin(_time) * 0.5f + 0.5f;  // 0~1

        Color finalColor = emissionBaseColor * (t * intensity);

        _mat.SetVector("_EmissionColor", finalColor);
    }
}
