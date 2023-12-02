using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SimulateTurns 
{
    // Start is called before the first frame update


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}


public class Spaceship_Headless{


    public Dictionary<RoomType, List<Room>> rooms;
    public int speed;
    public Dictionary<RoomType, List<Room>> GetRooms()
    {
        if (this is PlayerSpaceship){return GameManager.Instance.playerRooms;}
        return GameManager.Instance.enemyRooms;
    }
    public List<Room> GetRoomList()
    {
        return GetRooms().Values.SelectMany(x => x).ToList();
    }


}


public class GameState
{
    public Dictionary<RoomType, List<Room>> playerRooms = new Dictionary<RoomType, List<Room>>();
    public Hand enemyHand;
    public Hand playerHand;

    public Dictionary<RoomType, List<Room>> enemyRooms = new Dictionary<RoomType, List<Room>>();

    public List<CardAction> playerTurnActions = new List<CardAction>();
    public List<CardAction> enemyTurnActions = new List<CardAction>();

    public Spaceship_Headless playerShip;
    public Spaceship_Headless enemyShip;
    public int playerToMove;
    public GameState(){
        playerToMove = 1;
    }


    public GameState Clone(){
        return null;
    }


       public void ResolveActions()
    {


        // Decide Who goes first
        System.Random random = new System.Random();
        
        bool playerFirst = (playerShip.speed > enemyShip.speed) ? true :
                                (enemyShip.speed > playerShip.speed) ? false :
                                   (random.Next(2) == 0) ? true : false;
        if (playerFirst){UnityEngine.Debug.Log("Player First");}
        else{UnityEngine.Debug.Log("Enemy First");}

        // Activate Each Action
        if (playerFirst)
        {
            PlayOutActions(playerTurnActions, playerRooms);
            PlayOutActions(enemyTurnActions, enemyRooms);
        }
        else
        {
            PlayOutActions(enemyTurnActions, enemyRooms);
            PlayOutActions(playerTurnActions, playerRooms);
        }
    }



    public void PlayOutActions(List<CardAction> actions, Dictionary<RoomType, List<Room>> rooms)
    {
        
        List<System.Action> weaponCalls = new List<System.Action>();
        
        // Trigger any effects that are still affecting the affected
        List<Room> allRooms = rooms.Values.SelectMany(x => x).ToList();
        foreach (Room room in allRooms)
        {
            // Have to be careful here as effects will remove themselves from the rooms 
            // To Do there is a big where if a effect remove another effect shit will get wild
                
            if (room.effectsApplied.Count > 0)
            {
                List<CombatEffect> effectsCopy = room.effectsApplied.Select(obj => obj).ToList();
                
                foreach(CombatEffect effect in effectsCopy)
                {
                    if (room.effectsApplied.Contains(effect))
                    {
                        effect.Activate();
                    }
                }
            }
        }
        

        // Activate actions, which will apply and trigger some more effects
        foreach (CardAction action in actions)
        {

            if (!action.IsReady())
            {
        
                continue;
            }
        
            
            if (action.effects.Where(obj => obj is DamageEffect).ToList().Count > 0)
            {
        
            }
            action.Activate();
        }
        
        
        
        
        
    }

    public void DoMove(Move move){

    }

    public List<Move> GetMoves(){
        return null;
    }

    public int GetResult(int player){
        return 0;
    }

    public int GetNextPlayer(int player){
        return (player+1)%2;
    }

    public int maxComboSize = 1000;

    public void GenerateCardCombinations(List<CardAction> currentCombination, int index, Dictionary<int, List<CardAction>> allCardCombinations, float APLeft, List<Card> cardPool)
    {
        if(allCardCombinations.Count>maxComboSize){
            return;
        }

        if (index == cardPool.Count)
        {
            int currentComboHash = GenerateCardCombinationHash(currentCombination);
            if (allCardCombinations.ContainsKey(currentComboHash))
            {
                return;
            }
            allCardCombinations[currentComboHash] = currentCombination;
            return;
        }
        CardAction currentAction = cardPool[index].cardAction;

        // Use the current action
        if (currentAction.CanBeUsed(APLeft))
        {
            float newAP = APLeft - currentAction.cost;
            int nextCard = currentAction.cooldown > 0 ? 1 : 0; // If the card isn't infinite move onto the next action
            if (currentAction.needsTarget)
            {
                foreach(Room room in enemyShip.GetRoomList())
                {
                    CardAction cardActionWithTarget = currentAction.Clone();
                    cardActionWithTarget.affectedRoom = room;
                    List<CardAction> comboWithActionTarget = new List<CardAction>( currentCombination.Concat(new List<CardAction>{cardActionWithTarget}) );
                    
                    GenerateCardCombinations(comboWithActionTarget, index + nextCard, allCardCombinations, newAP, cardPool);
                }
                foreach(Room room in playerShip.GetRoomList())
                {
                    CardAction cardActionWithTarget = currentAction.Clone();
                    cardActionWithTarget.affectedRoom = room;
                    List<CardAction> comboWithActionTarget = new List<CardAction>( currentCombination.Concat(new List<CardAction>{cardActionWithTarget}) );
                    
                    GenerateCardCombinations(comboWithActionTarget, index + nextCard, allCardCombinations, newAP, cardPool);
                }
            }
            else
            {
                List<CardAction> comboWithAction = new List<CardAction>( currentCombination.Concat(new List<CardAction>{currentAction}) );
                GenerateCardCombinations(comboWithAction, index + nextCard, allCardCombinations, newAP, cardPool);
            }
        }
        // Don't use the current action
        GenerateCardCombinations(currentCombination, index + 1, allCardCombinations, APLeft, cardPool);

    }

    public int GenerateCardCombinationHash(List<CardAction> cardActions)
    {
        // Convert the list of CardAction objects into a set of strings
        HashSet<string> cardActionSet = new HashSet<string>(
            cardActions.Select(ca => $"{ca.GetType().ToString()}:{ca.sourceRoom.roomType.ToString()}:{(ca.affectedRoom == null ? 1 : ca.affectedRoom.roomType.ToString())}")
        );

        // Sort the elements in the HashSet
        var sortedCardActionSet = new SortedSet<string>(cardActionSet);

        int hash = 17;
        foreach (var item in sortedCardActionSet)
        {
            hash = hash * 31 + item.GetHashCode();
        }
        return hash;
    }


}

public class Move{

}

public class Node {


public Move move;
public Node parentNode;

public List<Node> childNodes;
public int wins;
public int visits;
public int avails;

public int playerJustMoved;


public Node(Move move=null, Node parent = null, int playerMoved=-1 ){
    this.move = move;
    this.parentNode = parent;
    this.wins=0;
    this.visits=1;
    this.avails =1;
    this.playerJustMoved = playerMoved;
}

public List<Move> GetUntriedMoves(List<Move> legalMoves){

    List<Move> triedMoves = new List<Move>();
    foreach(Node child in childNodes){
        triedMoves.Add(child.move);
    }
    List<Move> untriedMoves = new List<Move>();

    foreach(Move move in legalMoves){
        if(!triedMoves.Contains(move)){
            untriedMoves.Add(move);
        }
    }
    return untriedMoves;
}

public Node UCBSelectChild(List<Move> legalMoves, float exploration=0.7f){
        // Use the UCB1 formula to select a child node, filtered by the given list of legal moves.
        //             exploration is a constant balancing between exploitation and exploration, with default value 0.7 (approximately sqrt(2) / 2)

    List<Node> legalChildren = new List<Node>();
    foreach(Node child in childNodes){
        if(legalMoves.Contains(child.move)){
            legalChildren.Add(child);
        }
    }
    float s = 0;
    Node bestChild = legalChildren[0];
    foreach(Node legalChild in legalChildren){
        float t_s = (float)legalChild.wins / (float)legalChild.visits + exploration * Mathf.Sqrt(Mathf.Log(legalChild.avails) / (float)legalChild.visits);
        if(t_s>s){
            s=t_s;
            bestChild = legalChild;
        }
        legalChild.avails++;
    }
    

    return bestChild;
}

public Node AddChild(Move m, int playerMoved){
    
    Node n = new Node(m,this,playerMoved);
    this.childNodes.Add(n);
    return n;
}

public void Update(GameState terminalState){
    this.visits++;
    if(this.playerJustMoved!=-1){
        this.wins+=terminalState.GetResult(this.playerJustMoved);
    }
    
}



}

public class ISMCTS{
    public static Move Search(GameState rootstate, int itermax){
        Node rootnode = new Node();
        
        for(int i =0;i<itermax;i++){
            Node node = rootnode;
            GameState state = rootstate.Clone();
            List<Move> moves = state.GetMoves();
            // Select
            while(moves.Count>0 && node.GetUntriedMoves(moves).Count==0){
                node = node.UCBSelectChild(moves);
                state.DoMove(node.move);
                moves= state.GetMoves();
            }
            List<Move> untriedMoves = node.GetUntriedMoves(moves);
            // Expand
            if(untriedMoves.Count>0){
                Move m = untriedMoves[ Random.Range(0, untriedMoves.Count)];
                int player = state.playerToMove;
                state.DoMove(m);
                node = node.AddChild(m,player);
            }

            // Simulate 
            while(state.GetMoves().Count>0){
                moves = state.GetMoves();
                state.DoMove(moves[ Random.Range(0, moves.Count)]);
            }
            
            // Backpropagate
            while(node!=null){
                node.Update(state);
                node = node.parentNode;
            }

        }
        Node best = rootnode.childNodes[0];
        foreach(Node child in rootnode.childNodes){
            if((float)best.wins/(float)best.visits<(float)child.wins/(float)child.visits){
                best = child;
            }
        }
        return best.move;

    } 
}
