using System.Collections;
using UnityEngine;

public class Pokeball : MonoBehaviour
{
    private Rigidbody rb;

    [Header("Parabola_Setting")]
    public float forwardForce = 1200f;
    public float upwardForce = 350f;
    public float sideForce = 0f;

    [Header("Catching_Setting")]
    public string pokemonTag = "Pokemon";
    public int shakeCount = 3;
    public float shakeInterval = 0.65f;

    [Header("Camera_Setting")]
    public Transform targetCamera;      // 可留空，預設自動抓取 Main Camera
    public float zoomDistance = 5f;     // 鏡頭停在距離寶可夢球多遠的位置
    public float cameraHeightOffset = 1.2f; // 鏡頭高度微調，避免貼地
    public float cameraMoveDuration = 0.6f; // 鏡頭往前推進的時間（秒）

    [Header("Effect_Setting")]
    public GameObject hitEffectPrefab;          // 擊中/收入球時的特效
    public GameObject catchSuccessEffectPrefab; // 捕捉成功特效

    private bool isCatching = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }
    }

    void Start()
    {
        Application.targetFrameRate = 60;

        Vector3 throwVector =
            (transform.forward * forwardForce) +
            (Vector3.up * upwardForce) +
            (transform.right * sideForce);

        Shoot(throwVector);
    }

    public void Shoot(Vector3 dir)
    {
        if (rb != null)
        {
            rb.AddForce(dir);
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (rb == null) return;

        if (!isCatching && other.gameObject.CompareTag(pokemonTag))
        {
            isCatching = true;
            rb.isKinematic = true;
            StartCoroutine(CatchRoutine(other.gameObject));
        }
        else if (!isCatching)
        {
            rb.isKinematic = true;
        }
    }

    private IEnumerator CatchRoutine(GameObject targetPokemon)
    {
        // 1. 播放擊中/吸收入球特效並讓寶可夢消失
        SpawnEffect(hitEffectPrefab, targetPokemon.transform.position);
        targetPokemon.SetActive(false);

        // 2. 鏡頭平滑往前推進，對準寶可夢球
        if (targetCamera != null)
        {
            yield return StartCoroutine(MoveCameraToBall());
        }

        // 3. 寶可夢球晃動演出
        Quaternion baseRotation = transform.rotation;
        for (int i = 0; i < shakeCount; i++)
        {
            float timer = 0f;
            while (timer < shakeInterval)
            {
                timer += Time.deltaTime;
                float angle = Mathf.Sin(timer * Mathf.PI * 6f) * 18f;
                transform.rotation = baseRotation * Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            transform.rotation = baseRotation;
            yield return new WaitForSeconds(0.2f);
        }

        // 4. 100% 捕捉成功並播放特效
        SpawnEffect(catchSuccessEffectPrefab, transform.position);
        Debug.Log($"成功捕捉 {targetPokemon.name}！");
        Destroy(targetPokemon);
    }

    private IEnumerator MoveCameraToBall()
    {
        Vector3 startPos = targetCamera.position;
        Quaternion startRot = targetCamera.rotation;

        // 計算鏡頭從目前方向往球靠近後停下的目標位置
        Vector3 directionFromBall = (startPos - transform.position).normalized;
        Vector3 endPos = transform.position + (directionFromBall * zoomDistance) + (Vector3.up * cameraHeightOffset);
        Quaternion endRot = Quaternion.LookRotation(transform.position - endPos);

        float elapsed = 0f;
        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / cameraMoveDuration);
            targetCamera.position = Vector3.Lerp(startPos, endPos, t);
            targetCamera.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        targetCamera.position = endPos;
        targetCamera.rotation = endRot;
    }

    private void SpawnEffect(GameObject effectPrefab, Vector3 position)
    {
        if (effectPrefab == null) return;

        GameObject fx = Instantiate(effectPrefab, position, Quaternion.identity);
        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            float totalDuration = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(fx, totalDuration);
        }
        else
        {
            Destroy(fx, 3f);
        }
    }
}