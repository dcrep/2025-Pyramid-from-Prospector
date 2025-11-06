using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;   // We’ll need this line later in the chapter

[RequireComponent(typeof(Deck))]                                              // a
[RequireComponent(typeof(JsonParseLayout))]
public class Prospector : MonoBehaviour
{
    private static Prospector S; // A private Singleton for Prospector

    [Header("Dynamic")]
    public List<CardProspector> stockPile;

    public List<CardProspector> wastePile;
    public List<CardProspector> pyramid;

    public List<CardProspector> foundationPile;
    public CardProspector target;

    private Transform layoutAnchor;

    private Deck deck;
    private JsonLayout jsonLayout;

    CardProspector selectedCard = null;

    // A Dictionary to pair pyramid layout IDs and actual Cards
    private Dictionary<int, CardProspector> pyramidIdToCardDict;                 // a


    void Start()
    {
        // Set the private Singleton. We’ll use this later.
        if (S != null) Debug.LogError("Attempted to set S more than once!");  // b
        S = this;

        jsonLayout = GetComponent<JsonParseLayout>().layout;

        deck = GetComponent<Deck>();
        // These two lines replace the Start() call we commented out in Deck
        deck.InitDeck();
        Deck.Shuffle(ref deck.cards);

        stockPile = ConvertCardsToCardProspectors(deck.cards);

        LayoutPyramid();

        MoveToTarget(Draw());
        UpdateDrawPile();
    }

    /// <summary>
    /// Converts each Card in a List(Card) into a List(CardProspector) so that it
    ///  can be used in the Prospector game.
    /// </summary>
    /// <param name="listCard">A List(Card) to be converted</param>
    /// <returns>A List(CardProspector) of the converted cards</returns>
    List<CardProspector> ConvertCardsToCardProspectors(List<Card> listCard)
    {
        List<CardProspector> listCP = new List<CardProspector>();
        CardProspector cp;
        foreach (Card card in listCard)
        {
            cp = card as CardProspector;                                      // c
            listCP.Add(cp);
        }
        return (listCP);
    }

    /// <summary>
    /// Pulls a single card from the beginning of the stockPile and returns it
    /// Note: There is no protection against trying to draw from an empty pile!
    /// </summary>
    /// <returns>The top card of stockPile</returns>
    CardProspector Draw()
    {
        CardProspector cp = stockPile[0]; // Pull the 0th CardProspector
        stockPile.RemoveAt(0);            // Then remove it from stockPile
        return (cp);                      // And return it
    }

    /// <summary>
    /// Positions the initial tableau of cards, a.k.a. the "pyramid"
    /// </summary>
    void LayoutPyramid()
    {
        // Create an empty GameObject to serve as an anchor for the tableau   // a
        if (layoutAnchor == null)
        {
            // Create an empty GameObject named _LayoutAnchor in the Hierarchy
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;             // Grab its Transform
        }

        CardProspector cp;

        // Generate the Dictionary to match pyramid layout ID to CardProspector
        pyramidIdToCardDict = new Dictionary<int, CardProspector>();             // b


        // Iterate through the JsonLayoutSlots pulled from the JSON_Layout
        foreach (JsonLayoutSlot slot in jsonLayout.slots)
        {
            cp = Draw(); // Pull a card from the top (beginning) of the draw Pile
            cp.faceUp = slot.faceUp;    // Set its faceUp to the value in SlotDef
                                        // Make the CardProspector a child of layoutAnchor
            cp.transform.SetParent(layoutAnchor);

            // Convert the last char of the layer string to an int (e.g. "Row 0")
            int z = int.Parse(slot.layer[slot.layer.Length - 1].ToString());  // c

            // Set the localPosition of the card based on the slot information
            cp.SetLocalPos(new Vector3(
            jsonLayout.multiplier.x * slot.x,
            jsonLayout.multiplier.y * slot.y,
            -z));                                                       // d

            cp.layoutID = slot.id;
            cp.layoutSlot = slot;
            // CardProspectors in the pyramid have the state CardState.pyramid
            cp.state = eCardState.pyramid;

            // Set the sorting layer of all SpriteRenderers on the Card
            cp.SetSpriteSortingLayer(slot.layer);

            pyramid.Add(cp); // Add this CardProspector to the List<pyramid>

            // Add this CardProspector to the pyramidIdToCardDict Dictionary
            pyramidIdToCardDict.Add(slot.id, cp);                                // c

        }
    }

    /// <summary>
    /// Moves the current target card to the wastePile
    /// </summary>
    /// <param name="cp">The CardProspector to be moved</param>
    void MoveToWaste(CardProspector cp)
    {
        // Set the state of the card to waste
        cp.state = eCardState.waste;

        // add to wastePile if not already there
        if (target != cp) wastePile.Add(cp);  // Add it to the wastePile List<>
        
        cp.transform.SetParent(layoutAnchor); // Update its transform parent

        // Position it on the wastePile
        cp.SetLocalPos(new Vector3(
        jsonLayout.multiplier.x * jsonLayout.wastePile.x,
        jsonLayout.multiplier.y * jsonLayout.wastePile.y,
        0));

        cp.faceUp = true;

        // Place it on top of the pile for depth sorting
        cp.SetSpriteSortingLayer(jsonLayout.wastePile.layer);               // a
        cp.SetSortingOrder(-200 + (wastePile.Count * 3));                  // b
    }

    void MoveToFoundation(CardProspector cp)
    {
        // Set the state of the card to foundation
        cp.state = eCardState.foundation;
        foundationPile.Add(cp);  // Add it to the foundationPile List<>
        cp.transform.SetParent(layoutAnchor, false); // Update its transform parent

        // Position it on the foundationPile
        cp.SetLocalPos(new Vector3(
        jsonLayout.multiplier.x * jsonLayout.foundationPile.x,
        jsonLayout.multiplier.y * jsonLayout.foundationPile.y,
        0));

        Debug.Log("Moving to foundation: " + cp.name + "coords: " +
            (jsonLayout.multiplier.x * jsonLayout.foundationPile.x) + ", " +
            (jsonLayout.multiplier.y * jsonLayout.foundationPile.y));

        cp.faceUp = true;

        // Place it on top of the pile for depth sorting
        cp.SetSpriteSortingLayer(jsonLayout.foundationPile.layer);               // a
        cp.SetSortingOrder(-200 + (foundationPile.Count * 3));                  // b
    }

    void MoveTwoToFoundation(CardProspector cp1, CardProspector cp2)
    {
        /*if (S.selectedCard == cp1 || S.selectedCard == cp2)
        {
            S.selectedCard.circleHighlightRenderer.enabled = false;
            S.selectedCard = null;
        }*/
        // always deselect
        if (S.selectedCard != null)
        {
            S.selectedCard.circleHighlightRenderer.enabled = false;
            S.selectedCard = null;
        }
        //bool targetMoved = false;
        if (S.target == cp1)
        {
            S.pyramid.Remove(cp2);
            MoveToFoundation(cp2);
            // Must be last in order
            MoveTargetToFoundation();
            //targetMoved = true;
        }
        else if (S.target == cp2)
        {
            S.pyramid.Remove(cp1);
            MoveToFoundation(cp1);
            // Must be last in order
            MoveTargetToFoundation();
            //targetMoved = true;
        }
        else
        {
            S.pyramid.Remove(cp1);
            MoveToFoundation(cp1);
            S.pyramid.Remove(cp2);
            MoveToFoundation(cp2);
        }
    }

    public static void MoveTargetToFoundation()
    {
        if (S.target == null) return;
        S.wastePile.Remove(S.target);
        S.MoveToFoundation(S.target);

        if (S.wastePile.Count == 0)
        {
            S.target = null;
            if (S.stockPile.Count > 0)
            {
                S.MoveToTarget(S.Draw());  // Draw a new target card
                S.UpdateDrawPile();          // Restack the stockPile
            }
        }
        else
        {
            S.target = S.wastePile[^1];
            S.target.state = eCardState.target;
        }
    }

    /// <summary>
    /// Make cp the new target card
    /// </summary>
    /// <param name="cp">The CardProspector to be moved</param>
    void MoveToTarget(CardProspector cp)
    {
        // If there is currently a target card, move it to wastePile
        // !! already there - adjusted routine to detect for this
        if (target != null) MoveToWaste(target);

        // Use MoveToWaste to move the target card to the correct location
        MoveToWaste(cp);                                                    // c

        // Then set a few additional things to make cp the new target
        target = cp; // cp is the new target
        cp.state = eCardState.target;

        // Set the depth sorting so that cp is on top of the wastePile
        cp.SetSpriteSortingLayer("Target");                                 // c
        cp.SetSortingOrder(0);
    }

    static public void MoveWasteToStock()
    {
        CardProspector cp;
        S.target = null;
        if (S.selectedCard != null)
        {
            S.selectedCard.circleHighlightRenderer.enabled = false;
            S.selectedCard = null;
        }
        while (S.wastePile.Count > 0)
        {
            cp = S.wastePile[^1];
            S.wastePile.RemoveAt(S.wastePile.Count - 1);
            S.stockPile.Insert(0, cp);
        }
        S.MoveToTarget(S.Draw());  // Draw a new target card
        S.UpdateDrawPile();          // Restack the stockPile
    }

    /// <summary>
    /// Arranges all the cards of the stockPile to show how many are left
    /// </summary>
    void UpdateDrawPile()
    {
        CardProspector cp;
        // Go through all the cards of the stockPile
        for (int i = 0; i < stockPile.Count; i++)
        {
            cp = stockPile[i];
            cp.transform.SetParent(layoutAnchor);

            // Position it correctly with the layout.stockPile.stagger
            Vector3 cpPos = new Vector3();
            cpPos.x = jsonLayout.multiplier.x * jsonLayout.stockPile.x;
            // Add the staggering for the stockPile
            cpPos.x += jsonLayout.stockPile.xStagger * i;
            cpPos.y = jsonLayout.multiplier.y * jsonLayout.stockPile.y;
            cpPos.z = 0.1f * i;
            cp.SetLocalPos(cpPos);

            cp.faceUp = false; // DrawPile Cards are all face-down
            cp.state = eCardState.drawpile;
            // Set depth sorting
            cp.SetSpriteSortingLayer(jsonLayout.stockPile.layer);
            cp.SetSortingOrder(-10 * i);
        }
    }

    /// <summary>
    /// This turns cards in the Mine face-up and face-down
    /// </summary>
    public void SetMineFaceUps()
    {                                            // d
        CardProspector coverCP;
        foreach (CardProspector cp in pyramid)
        {
            bool faceUp = true; // Assume the card will be face-up

            // Iterate through the covering cards by pyramid layout ID
            foreach (int coverID in cp.layoutSlot.hiddenBy)
            {
                coverCP = pyramidIdToCardDict[coverID];
                // If the covering card is null or still in the pyramid...
                if (coverCP == null || coverCP.state == eCardState.pyramid)
                {
                    faceUp = false; // then this card is face-down
                }
            }
            cp.faceUp = faceUp; // Set the value on the card
        }
    }



    /// <summary>
    /// Handler for any time a card in the game is clicked
    /// </summary>
    /// <param name="cp">The CardProspector that was clicked</param>
    static public void CARD_CLICKED(CardProspector cp)
    {
        // The reaction is determined by the state of the clicked card
        switch (cp.state)
        {
            case eCardState.target:
                if (S.target.rank == 13)
                {
                    MoveTargetToFoundation();
                }
                else if (S.stockPile.Count == 0 && S.wastePile.Count > 1)
                {
                    MoveWasteToStock();
                    return;
                }
                break;
            case eCardState.drawpile:
                if (S.selectedCard != null)
                {
                    S.selectedCard.circleHighlightRenderer.enabled = false;
                    S.selectedCard = null;
                }
                // Draw a new card, update draw pile
                S.MoveToTarget(S.Draw());  // Draw a new target card
                S.UpdateDrawPile();          // Restack the stockPile
                break;
            case eCardState.pyramid:
                // Clicking a card in the pyramid will check if it’s a valid play
                bool validMatch = false;  // Initially assume that it’s invalid 

                // If the card is face-down, it’s not valid
                //if (!cp.faceUp) validMatch = false;

                // If it’s not an adjacent rank, it’s not valid
                //if (!cp.AdjacentTo(S.target)) validMatch = false;            // b

                // king (13 rank) selected? - remove instantly
                if (cp.rank == 13)
                {
                    // previous selection exists? - deselect it
                    if (S.selectedCard != null)
                    {
                        S.selectedCard.circleHighlightRenderer.enabled = false;
                        S.selectedCard = null;
                    }
                    // Remove from pyramid and move to foundation
                    S.pyramid.Remove(cp);
                    S.MoveToFoundation(cp);
                    //validMatch = true;    // unnecessary - returning immediately
                    return;
                }
                // Is this 1st pyramid selection?
                else if (S.selectedCard == null)
                {
                    // Selected card + target = 13?
                    if (cp.rank + S.target.rank == 13)
                    {
                        //validMatch = true;
                        S.MoveTwoToFoundation(cp, S.target);
                        return;
                    }
                    else    // just select otherwise
                    {
                        S.selectedCard = cp;
                        cp.circleHighlightRenderer.enabled = true;
                        //validMatch = false;   // already false - could just return
                    }
                }
                else    // previous pyramid selection exists
                {
                    // Newly clicked card + previous selected = 13
                    if (S.selectedCard.rank + cp.rank == 13)
                    {
                        //validMatch = true;
                        S.MoveTwoToFoundation(cp, S.selectedCard);
                        return;
                        //S.selectedCard.circleHighlightRenderer.enabled = false;
                        //S.selectedCard = null;
                    }
                    // Newly clicked card + target = 13
                    else if (cp.rank + S.target.rank == 13)
                    {
                        //validMatch = true;
                        S.MoveTwoToFoundation(cp, S.target);
                        return;
                        //S.selectedCard.circleHighlightRenderer.enabled = false;
                        // Invalidate previous selection and don't move it below
                        //S.selectedCard = null;
                    }
                    else    // No total 13 combinations - just change selection
                    {
                        S.selectedCard.circleHighlightRenderer.enabled = false;
                        S.selectedCard = cp;
                        cp.circleHighlightRenderer.enabled = true;
                        //validMatch = false;   // already false - could just return
                    }
                }
                // King/13 case already handled, other combination possible
                if (validMatch)
                {
                    Debug.LogError("Should not get here!");
                    // Selected card + target/cp = 13
                    if (S.selectedCard != null)
                    {
                        /*S.pyramid.Remove(cp);   // Remove it from the tableau List
                        S.MoveToFoundation(cp);
                        S.pyramid.Remove(S.selectedCard);   // Remove it from the tableau List
                        S.MoveToWaste(S.selectedCard);
                        //S.MoveToTarget(S.selectedCard);  // Make it the target card
                        S.selectedCard = null;*/
                        S.MoveTwoToFoundation(cp, S.selectedCard);
                    }
                    else    // clicked card + target = 13
                    {
                        Debug.Log("Valid match with target");
                        /*S.pyramid.Remove(cp);   // Remove it from the tableau List
                        S.MoveToWaste(cp);
                        if (S.stockPile.Count > 0)
                        {
                            S.MoveToTarget(S.Draw());  // Draw a new target card
                            S.UpdateDrawPile();          // Restack the stockPile 
                        }*/
                        S.MoveTwoToFoundation(cp, S.target);
                    }
                                           
                    //S.MoveToTarget(cp);  // Make it the target card

                    //S.SetMineFaceUps();  // Be sure to add this line!!
                }
                break;
        }
    }

}