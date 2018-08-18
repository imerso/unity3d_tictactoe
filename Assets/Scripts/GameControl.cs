using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


// A move stores the player and the cell index
struct TMove
{
	public byte player;
	public int cellIdx;
	public Button button;
	public TMove(byte player, int cellIdx, Button button)
	{
		this.player = player;
		this.cellIdx = cellIdx;
		this.button = button;
	}
};


//
// The entire game is controlled by this class.
//
public class GameControl : MonoBehaviour
{
	public int boardDimension = 180;		// this is not the board size in terms of cells, but physical dimension
	public GameObject[] menuObjects;		// all the menu objects (texts and buttons) that will be animated in and out
	public ParticleSystem[] menuParticles;	// the menu particles that stop when game starts
	public GameObject boardObject;			// the board root itself
	public GameObject gridCanvas;			// game buttons will be created dynamically over this canvas
	public GameObject cellPrefab;			// the prefab used for the grid buttons
	public Sprite[] sprites;				// player sprites plus empty sprite 
	public Text logText;
    public Text titleText;
    public GameObject replayButton;
    public AudioClip[] sfx;                 // list of sfx: 0=menu click, 1=game click, 2=win sfx, 3=draw sfx, 4=error sound


	// size of the current board
	int boardSize;

	// current player move
	byte currentPlayer = 0;

	// list of moves
	List<TMove> moves = new List<TMove>();

	// each entry on this array will have:
	// 0 = player 1 occupied
	// 1 = player 2 occupied
	// 2 = empty
	byte[] cellsStates;
    GameObject[] cellsObjs;

	// we also have a special array
	// which counts presence on rows, cols and diagonals
	// it starts with all zeroed,
	// when player 1 moves, it adds 1 to one of these cells,
	// when player 2 moves, it subtracts 1
	// if any of these cells reach board size, then one player won.
	int[] presenceArray;

	// after a winner is found, game stops.
	bool gameEnded;
    bool isPlaying;
    bool isQuitting;
    AudioSource audioSource;


    // Init
    private void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
    }


    // Check for esc / back
    void Update()
	{
		// exit on escape
		if (Input.GetKeyDown(KeyCode.Escape))
		{
            QuitClicked();
		}
	}


	// Board selected, transition and start game
	public void BoardClicked(int size)
	{
        // Initialize
        GenBoard(size);
        ReplayClicked();
        titleText.text = "TicTacToe " + size + "x" + size;

        // animate menu items out
        for (int i = 0; i < menuObjects.Length; i++)
		{
			Animator anim = menuObjects[i].GetComponent<Animator>();
			anim.SetFloat("Direction", -1);
			anim.Play("Anim", 0, 1);
		}

		// animate the board in
		Animator boardAnim = boardObject.GetComponent<Animator>();
		boardAnim.SetFloat("Direction", 1);
		boardAnim.Play("Anim", 0, 0);
	}


    // Stop or resume particle systems
    void SetParticles(bool enabled)
    {
        for (int i = 0; i < menuParticles.Length; i++)
        {
            if (enabled) menuParticles[i].Play();
            else menuParticles[i].Stop();
        }
    }


    // Quit game selected, transition back to menu
    public void QuitClicked()
	{
        if (isPlaying)
        {
            // animate menu items in
            for (int i = 0; i < menuObjects.Length; i++)
            {
                Animator anim = menuObjects[i].GetComponent<Animator>();
                anim.SetFloat("Direction", 1);
                anim.Play("Anim", 0, 0);
            }

            // animate the board out
            Animator boardAnim = boardObject.GetComponent<Animator>();
            boardAnim.SetFloat("Direction", -1);
            boardAnim.Play("Anim", 0, 1);

            // restart particle systems
            SetParticles(true);

            isPlaying = false;
            Camera.main.GetComponent<Camera_Menu>().finalPos.z = -500;

            audioSource.PlayOneShot(sfx[0]);
        }
        else if (!isQuitting)
        {
            isQuitting = true;
            StartCoroutine(QuitAnim());
        }
    }


    // Animate out before quitting
    IEnumerator QuitAnim()
    {
        yield return null;

        // animate menu items out
        for (int i = 0; i < menuObjects.Length; i++)
        {
            Animator anim = menuObjects[i].GetComponent<Animator>();
            anim.SetFloat("Direction", -1);
            anim.Play("Anim", 0, 1);
        }

        SetParticles(false);

        // fade out audio
        while (audioSource.volume > 0)
        {
            audioSource.volume -= 0.01f;
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(2.0f);
        Application.Quit();
    }


    // Restart game
    public void ReplayClicked()
    {
        // reset game
        audioSource.PlayOneShot(sfx[0]);
        moves.Clear();
        replayButton.SetActive(false);
        gameEnded = false;
        isPlaying = true;
        currentPlayer = 0;
        logText.text = "Next: player " + (currentPlayer + 1);
        //Camera.main.GetComponent<Camera_Menu>().finalPos.z = -240;

        // stop particles
        SetParticles(false);

        // clear board
        for (int i = 0; i < cellsStates.Length; i++)
        {
            cellsStates[i] = 2;
            cellsObjs[i].GetComponent<Button>().image.sprite = sprites[2];
        }

        for (int i = 0; i < presenceArray.Length; i++)
            presenceArray[i] = 0;
    }


    // Board generator
    void GenBoard(int size)
	{
        boardSize = size;

        // cell control states
        // 0 = player 1 occupied
        // 1 = player 2 occupied
        // 2 = empty
        cellsStates = new byte[size*size];
        cellsObjs = new GameObject[size*size];
		for (int i = 0; i < cellsStates.Length; i++)
			cellsStates[i] = 2;

		// size for rows + size for cols + 2 diagonals
		presenceArray = new int[size+size+2];
		for (int i = 0; i < presenceArray.Length; i++)
			presenceArray[i] = 0;

		// clear moves list
		moves.Clear();

		// clear any cells
		int count = gridCanvas.transform.childCount;
		for (int i = count - 1; i >= 0; i--)
		{
			Transform child = gridCanvas.transform.GetChild(i);
			Destroy(child.gameObject);
		}

		// re-create cells for board size

		// wh = size of a cell
		float wh = boardDimension / size;

		// initial py
		float py = ((float)boardDimension / 2) - (wh/2) - 16;

		// cell id
		int id = 0;

		// create cells
		for (int y = 0; y < size; y++)
		{
			float px = -(((float)boardDimension / 2) - (wh/2));

			for (int x = 0; x < size; x++)
			{
				// create button
				GameObject cell = cellsObjs[id] = GameObject.Instantiate(cellPrefab, new Vector3(px, 2052, py), gridCanvas.transform.rotation, gridCanvas.transform);
				cell.name = (id++).ToString();
				Button button = cell.GetComponent<Button>();
				button.onClick.AddListener(delegate { CellClicked(button); });

                // size button
                button.image.rectTransform.sizeDelta = new Vector2(wh, wh);

				// next cell px pos
				px += wh;
			}

			// next cell py pos
			py -= wh;
		}
	}


	// Player clicked on board cell
	public void CellClicked(Button button)
	{
        // no more movements after finding a winner
        if (gameEnded)
        {
            audioSource.PlayOneShot(sfx[4]);
            return;
        }

        // button name is its own index inside board cells
        int cellIdx = int.Parse(button.name);

        // if the cell is already occupied,
        // ignore the click.
        if (cellsStates[cellIdx] < 2)
        {
            audioSource.PlayOneShot(sfx[4]);
            return;
        }

        // play move sound
        audioSource.PlayOneShot(sfx[1]);

        // occupy cell with player index
        cellsStates[cellIdx] = currentPlayer;
		moves.Add(new TMove(currentPlayer, cellIdx, button));
		button.image.sprite = sprites[currentPlayer];

		// update presence scores
		UpdatePresence(cellIdx, (currentPlayer == 0 ? 1 : -1));

		// check and flags if there is a winner
		CheckWinner();

		if (!gameEnded)
		{
			// switch player
			currentPlayer ^= 1;
			logText.text = "Next: player " + (currentPlayer+1);
		}
	}


	// Undo move
	public void UndoClicked()
	{
		if (moves.Count == 0)
			return;

        audioSource.PlayOneShot(sfx[0]);

        // restore the move
        TMove cell = moves[moves.Count - 1];

		// empties the related cell
		cellsStates[cell.cellIdx] = 2;

		// empties the related button
		cell.button.image.sprite = sprites[2];

		// current player is the player of that move
		currentPlayer = cell.player;

		// undo presence computations,
		// by adding the inverse of the correct value
		UpdatePresence(cell.cellIdx, (currentPlayer == 0 ? -1 : 1));

		// remove the move from history
		moves.RemoveAt(moves.Count - 1);

		// certainly there is no winner yet
		gameEnded = false;
		logText.text = "Next: player " + (currentPlayer+1);
        replayButton.SetActive(false);
        SetParticles(false);
    }


    void UpdatePresence(int cellIdx, int add)
	{
		int row = cellIdx / boardSize;
		int col = cellIdx - (row * boardSize);

		// update row presence
		presenceArray[row] += add;

		// update col presence
		presenceArray[boardSize + col] += add;

		// update diagonal 1 presence
		if (row == col) presenceArray[2 * boardSize] += add;

		// update diagonal 2 presence
		if (boardSize-1 - col == row) presenceArray[2 * boardSize + 1] += add;
	}


	// Check if there is a winner
	bool CheckWinner()
	{
		gameEnded = false;

		for (int i = 0; i < presenceArray.Length; i++)
		{
			if (presenceArray[i] == boardSize)
			{
                // player 1 won
                gameEnded = true;
				logText.text = "Player 1 won!";
			}
			else if (presenceArray[i] == -boardSize)
			{
                // player 2 won
                gameEnded = true;
				logText.text = "Player 2 won!";
            }
        }

        if (gameEnded)
        {
            audioSource.PlayOneShot(sfx[2]);
            SetParticles(true);
        }

        if (!gameEnded && moves.Count == boardSize * boardSize)
		{
            // game draw
            audioSource.PlayOneShot(sfx[3]);
            logText.text = "Draw!";
			gameEnded = true;
		}

        if (gameEnded)
        {
            replayButton.SetActive(true);
        }

        return gameEnded;
	}
}
