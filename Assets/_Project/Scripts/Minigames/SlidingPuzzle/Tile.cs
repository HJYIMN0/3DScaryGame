using UnityEngine;
using UnityEngine.UI;             
using UnityEngine.EventSystems;   

public class Tile : MonoBehaviour, IPointerClickHandler
{
    public int row;
    public int column;

    [HideInInspector] public int homeRow;
    [HideInInspector] public int homeColumn;


    public bool isEmpty;
    [HideInInspector] public SlidingPuzzleManager manager;
    [HideInInspector] public Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager != null)
            manager.OnTileClicked(this);
    }
}