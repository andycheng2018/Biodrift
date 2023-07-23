using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Recipe : MonoBehaviour
{
    public Item item;
    public Recipe thisRecipe;
    public Image buttonIcon;
    public TMP_Text buttonText;
    public CraftingButton craftingButton;
    public GameObject materialsContent;
    public GameObject emptyRecipe;
    public Materials[] materials;

    private void Start()
    {
        if (item == null) { return; }
        buttonIcon.sprite = item.icon;
        buttonText.text = item.icon.name;
    }

    public void OpenMaterial()
    {
        if (craftingButton.icon.sprite == buttonIcon.sprite) { return; }

        craftingButton.icon.sprite = buttonIcon.sprite;
        craftingButton.text.text = "Craft " + buttonText.text;
        craftingButton.recipe = thisRecipe;

        for (int i = materialsContent.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(materialsContent.transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < materials.Length; i++)
        {
            var material = Instantiate(emptyRecipe, materialsContent.transform);
            material.GetComponent<Recipe>().buttonIcon.sprite = materials[i].item.icon;
            material.GetComponent<Recipe>().buttonText.text = "x" + materials[i].amount + " " + materials[i].item.icon.name;
            material.GetComponent<Button>().interactable = false;
        }
    }
}

[System.Serializable]
public struct Materials
{
    public Item item;
    public int amount;
}
