using TMPro;
using Unity.Netcode;
using UnityEngine.UI;

public class CraftingButton : NetworkBehaviour
{
    public Recipe recipe;
    public Image icon;
    public TMP_Text text;
    public Player player;

    public void CraftItem()
    {
        if (recipe == null) { return; }

        if (checkIfPresent())
        {
            player.currentRecipe = recipe;
            player.spawnServerRpc();

            for (int i = 0; i < player.Items.Count; i++)
            {
                for (int j = 0; j < recipe.materials.Length; j++)
                {
                    if (player.Items[i].icon == recipe.materials[j].item.icon && player.Items[i].networkAmount.Value >= recipe.materials[j].amount)
                    {
                        player.Items[i].ChangeAmountServerRpc(-recipe.materials[j].amount, false);
                    }
                }
            }
        }
    }

    public bool checkIfPresent()
    {
        int count = 0;
        for (int i = 0; i < player.Items.Count; i++)
        {
            for (int j = 0; j < recipe.materials.Length; j++)
            {
                if (player.Items[i].icon == recipe.materials[j].item.icon && player.Items[i].networkAmount.Value >= recipe.materials[j].amount)
                {
                    count++;
                }
            }
        }
        if (count == recipe.materials.Length)
        {
            return true;
        }
        return false;
    }
}
