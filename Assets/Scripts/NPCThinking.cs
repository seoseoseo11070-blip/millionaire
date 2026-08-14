using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NPCThinkingEffect : MonoBehaviour
{
    [System.Serializable]
    public class NpcThinkSlot
    {
        [Tooltip("Image")]
        public Image image;

        [Tooltip("画像")]
        public Sprite[] frames = new Sprite[3];
    }

    [Header("NPC")]
    [SerializeField] private NpcThinkSlot[] npcSlots = new NpcThinkSlot[3];

    [Header("切り替え,秒")]
    [SerializeField] private float frameInterval = 0.4f;

    private Coroutine animCoroutine;
    private int activeSlotIndex = -1;

    void Awake()
    {
        HideAll();
    }

    public void SetThinkingPlayer(int playerIndex)
    {
        Debug.Log($"思考演出: playerIndex={playerIndex}");

        StopAnim();
        HideAll();

        if (playerIndex <= 0 || playerIndex > npcSlots.Length)
            return;

        int slotIndex = playerIndex - 1;
        NpcThinkSlot slot = npcSlots[slotIndex];

        if (slot == null)
        {
            Debug.LogError($"npcSlots[{slotIndex}] が null");
            return;
        }
        if (slot.image == null)
        {
            Debug.LogError($"npcSlots[{slotIndex}].image が null");
            return;
        }
        if (slot.frames == null || slot.frames.Length == 0)
        {
            Debug.LogError($"npcSlots[{slotIndex}].frames が空");
            return;
        }

        slot.image.gameObject.SetActive(true);
        slot.image.enabled = true;
        slot.image.sprite = slot.frames[0];
        animCoroutine = StartCoroutine(AnimateFrames(slot));
    }
    public void HideAll()
    {
        StopAnim();
        activeSlotIndex = -1;

        if (npcSlots == null) return;
        foreach (var slot in npcSlots)
        {
            if (slot != null && slot.image != null)
                slot.image.gameObject.SetActive(false);
        }
    }

    private IEnumerator AnimateFrames(NpcThinkSlot slot)
    {
        int frame = 0;
        while (true)
        {
            if (slot.frames != null && slot.frames.Length > 0)
            {
                slot.image.sprite = slot.frames[frame % slot.frames.Length];
                frame++;
            }
            yield return new WaitForSeconds(frameInterval);
        }
    }

    private void StopAnim()
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }
    }

}