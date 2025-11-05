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
        wastePile.Add(cp);  // Add it to the wastePile List<>
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

    /// <summary>
    /// Make cp the new target card
    /// </summary>
    /// <param name="cp">The CardProspector to be moved</param>
    void MoveToTarget(CardProspector cp)
    {
        // If there is currently a target card, move it to wastePile
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
                    S.MoveToWaste(S.target);
                    if (S.stockPile.Count > 0)
                    {
                        S.MoveToTarget(S.Draw());  // Draw a new target card
                        S.UpdateDrawPile();          // Restack the stockPile
                    }
                }
                break;
            case eCardState.drawpile:
                if (S.selectedCard != null)
                {
                    S.selectedCard.circleHighlightRenderer.enabled = false;
                    S.selectedCard = null;
                }
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

                if (cp.rank == 13)
                {
                    validMatch = true;
                    if (S.selectedCard != null)
                    {
                        S.selectedCard.circleHighlightRenderer.enabled = false;
                        S.selectedCard = null;
                    }
                    S.pyramid.Remove(cp);   // Remove it from the tableau List
                    S.MoveToWaste(cp);
                    return;
                }
                else if (S.selectedCard == null)
                {
                    if (cp.rank + S.target.rank == 13)
                    {
                        validMatch = true;
                    }
                    else
                    {
                        S.selectedCard = cp;
                        cp.circleHighlightRenderer.enabled = true;
                    }
                }
                else
                {
                    if (S.selectedCard.rank + cp.rank == 13)
                    {
                        validMatch = true;
                        S.selectedCard.circleHighlightRenderer.enabled = false;
                        //S.selectedCard = null;
                    }
                    // Does the newly clicked card total up to 13 with the target card?
                    else if (cp.rank + S.target.rank == 13)
                    {
                        validMatch = true;
                        S.selectedCard.circleHighlightRenderer.enabled = false;
                        // Invalidate previous selection and don't move it below
                        S.selectedCard = null;
                    }
                    else
                    {
                        S.selectedCard.circleHighlightRenderer.enabled = false;
                        S.selectedCard = cp;
                        cp.circleHighlightRenderer.enabled = true;
                    }
                }

                if (validMatch)
                {
                    if (S.selectedCard != null)
                    {
                        S.pyramid.Remove(cp);   // Remove it from the tableau List
                        S.MoveToWaste(cp);
                        S.pyramid.Remove(S.selectedCard);   // Remove it from the tableau List
                        S.MoveToWaste(S.selectedCard);
                        //S.MoveToTarget(S.selectedCard);  // Make it the target card
                        S.selectedCard = null;
                    }
                    else    // no selected card
                    {
                        Debug.Log("Valid match with target");
                        S.pyramid.Remove(cp);   // Remove it from the tableau List
                        S.MoveToWaste(cp);
                        if (S.stockPile.Count > 0)
                        {
                            S.MoveToTarget(S.Draw());  // Draw a new target card
                            S.UpdateDrawPile();          // Restack the stockPile 
                        }
                    }
                                           
                    //S.MoveToTarget(cp);  // Make it the target card

                    //S.SetMineFaceUps();  // Be sure to add this line!!
                }
                break;
        }
    }

}