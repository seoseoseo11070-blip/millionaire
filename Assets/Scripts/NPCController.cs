using UnityEngine;
using System.Collections.Generic;

public class NPCController : MonoBehaviour
{
    [Header("連動させる大元のゲーム管理スクリプト")]
    [SerializeField] private GameManager gameManager;

    public void ThinkAndPlay(int cpuIndex, List<Card> cpuHand)
    {
        if (cpuHand == null || cpuHand.Count == 0)
        {
            Debug.Log($"プレイヤー {cpuIndex + 1} はすでに上がっています");
            gameManager.NextTurn();
            return;
        }
        Card playCard = cpuHand[0];

        List<Card> cardsToPlay = new List<Card> { playCard };

        gameManager.PlayCpuCards(cpuIndex, cardsToPlay);
    }
}

