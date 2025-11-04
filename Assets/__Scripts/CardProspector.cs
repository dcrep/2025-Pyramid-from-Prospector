using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This enum defines the variable type eCardState with four named values.      // a
public enum eCardState { drawpile, mine, target, discard }

public class CardProspector : Card
{ // Make CardProspector extend Card        // b
    [Header("Dynamic: CardProspector")]
    public eCardState state = eCardState.drawpile;                   // c
                                                                     // The hiddenBy list stores which other cards will keep this one face down
    public List<CardProspector> hiddenBy = new List<CardProspector>();
    // The layoutID matches this card to the tableau JSON if it’s a tableau card
    public int layoutID;
    // The JsonLayoutSlot class stores information pulled in from JSON_Layout
    public JsonLayoutSlot layoutSlot;

    /// <summary>
    /// Informs the Prospector class that this card has been clicked.
    /// </summary>
    override public void OnMouseUpAsButton()
    {
        //var sortLayer = GetComponent<Renderer>().sortingLayerName;
        Debug.Log("Card clicked: " + name + " on layer " + layerAsInt);

        if (layerAsInt >= 0)
        {
            var collider = GetComponent<BoxCollider2D>();
            // Get overlapping colliders (note size - 1 because box collider extends slightly further than the visible card)
            Collider2D[] overlapResults = Physics2D.OverlapBoxAll((Vector2)collider.bounds.center,
                                                                (Vector2)collider.bounds.size - Vector2.one * 1f,
                                                                collider.transform.eulerAngles.z);
            List<Collider2D> overlappedColliders = new(overlapResults);
            foreach (var col in overlappedColliders)
            {
                if (col != collider)
                {
                    // If another collider is found that is not this card's collider, do not register the click.
                    Debug.Log("Collider overlap detected with " + col.name + " layer #: " + col.GetComponent<Card>().layerAsInt);
                    if (layerAsInt < col.GetComponent<Card>().layerAsInt)
                    {
                        Debug.Log("Click ignored due to overlapping collider with higher layer.");
                        return; // Ignore the click if this card is underneath another
                    }
                }
            }
        }

        // Uncomment the next line to call the base class version of this method
        // base.OnMouseUpAsButton();                                          // a
        // Call the CardClicked method on the Prospector Singleton
        circleHighlightRenderer.enabled = true;
        Prospector.CARD_CLICKED(this);
        base.OnMouseUpAsButton();// b
    }

}
