using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AddItemSlot : MonoBehaviour
{
    public TMP_Text text;
    public Image icon;

    public void Destroy()
    {
        Destroy(gameObject);
    }
}
