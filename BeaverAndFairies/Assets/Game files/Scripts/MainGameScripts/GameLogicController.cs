﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class GameLogicController : MonoBehaviour {

	public GameObject[] blockTemplates;
	public GameObject[] blockBordersTemplates;
	public GameObject blockPadTemplate;
	public GameObject blockExample;
	public Text scoreLabel;

	public float _blockHeight;
	public int _score;
	public Queue<GameObject> _currentBlocks;
	public SwipeDirectionController _swipeDirectionController;

	public bool stopGame;

	public GameBalanceController gameBalanceController;

	float _blocksSpeed = -1.00f;
	public float blocksSpeed { get { return _blocksSpeed; } }
	int _spawnTime;
	public int spawnTime { get { return _spawnTime; } }
	public int blockTasksCount;
	public int blockType;
	public bool randomTasksCount;

	int _currentSpawnTime;
	float _loseHeight;
	bool _lose;

	PlayerInputController _playerInputController;
	MoveBlocksController _moveBlocksController;

	public GameGlobalSettings gameSettings;
	public FadingScript fadingController;
	public EndGameController endGameController;
	public GameObject pauseGamePopUp;


	void Start () {
		BlockTasksController blockTasksController = blockExample.GetComponent<BlockTasksController>();
		Renderer renderer = blockTasksController.blockRect.GetComponent<Renderer>();
		_blockHeight = renderer.bounds.size.y;
		_loseHeight = (_blockHeight * gameSettings.boardHeight);
		_currentSpawnTime = 0;
		_currentBlocks = new Queue<GameObject>();
		_score = 0;
		_playerInputController = new PlayerInputController();
		_playerInputController.gameLogicController = this;
		_swipeDirectionController = new SwipeDirectionController();
		_moveBlocksController = new MoveBlocksController();
		_moveBlocksController.gameLogicController = this;
		gameBalanceController.setNewBalance();
	}
		
	void Update () {

		if(_lose == false && stopGame == false)
		{
			spawnNewBlock();
			_swipeDirectionController.getTouchDirection();
			_playerInputController.getUserInput();
			_moveBlocksController.moveDownBlocks();
			checkGameResult();
		}

		scoreLabel.text = _score.ToString();
	}

	void spawnNewBlock()
	{
		_currentSpawnTime++;

		if(_currentSpawnTime >= spawnTime)
		{
			_currentSpawnTime = 0;

			GameObject block = Instantiate(blockPadTemplate, transform.position, Quaternion.identity) as GameObject;
			block.transform.SetParent(transform.parent, false);
			block.transform.position = transform.position;

			int borderIndex = Random.Range(0, blockBordersTemplates.Length);
			Vector3 startBorderPosition = new Vector3(0,blockBordersTemplates[borderIndex].transform.localPosition.y,0);
			GameObject blockBorder = Instantiate(blockBordersTemplates[borderIndex], startBorderPosition, Quaternion.identity) as GameObject;
			blockBorder.transform.SetParent(block.transform, false);

			BlockTasksController tasksController = block.GetComponent<BlockTasksController>();
			Destroy(tasksController.blockRect);
			tasksController.blockRect = blockBorder;

			BlockTasksController blockTasksController = block.GetComponent<BlockTasksController>();

			int taskCount = blockTasksCount;
			if(randomTasksCount)
			{
				taskCount = Random.Range(1, blockTasksCount + 1);
			}
				
			float tasksLength = 0.0f;
			for(int blockTaskIndex = 0; blockTaskIndex < taskCount; blockTaskIndex++)
			{
				int blockIndex = blockType;
				if(blockType == 0)
				{
					blockIndex = Random.Range(0, blockTemplates.Length);
				}

				Vector3 newBlockTaskPosition = new Vector3(0,blockTemplates[blockIndex].transform.localPosition.y,0);
				GameObject blockTaskTemplate = blockTemplates[blockIndex];
				GameObject blockTask = Instantiate(blockTemplates[blockIndex], newBlockTaskPosition, blockTaskTemplate.transform.rotation) as GameObject;
				blockTask.transform.SetParent(block.transform, false);
				blockTasksController.blockTasks.Add(blockTask);

				Renderer renderer = blockTask.GetComponent<Renderer>();
				tasksLength += (renderer.bounds.size.x + gameSettings.distanceBetweeneTasks);
			}

			tasksLength -= gameSettings.distanceBetweeneTasks;
			float startX = - tasksLength / 2.0f;

			foreach(GameObject task in blockTasksController.blockTasks)
			{
				Renderer renderer = task.GetComponent<Renderer>();
				float width = renderer.bounds.size.x;
				task.transform.localPosition = new Vector3(startX + (width / 2) ,task.transform.localPosition.y,0);
				startX += (width + gameSettings.distanceBetweeneTasks);
			}

			_currentBlocks.Enqueue(block);
		}
	}

	public void showErrorSwipeAnimation()
	{
		GameObject firstBlock = _currentBlocks.Peek();

		BlockTasksController blockTasksController = firstBlock.GetComponent<BlockTasksController>();
		GameObject firstTask = blockTasksController.blockTasks[0];

		Vector3 startScale = firstTask.transform.localScale;
		Vector3 newScale = new Vector3 (startScale.x * 1.5f, startScale.y * 1.5f, 0);
		Sequence scaleSequence = DOTween.Sequence();
		scaleSequence.Append(firstTask.transform.DOScale(newScale, gameSettings.scaleAnimationDuration));
		scaleSequence.Append(firstTask.transform.DOScale(startScale, gameSettings.scaleAnimationDuration));
	}

	public void removeFirstBlock()
	{
		GameObject firstBlock = _currentBlocks.Dequeue();
		Destroy(firstBlock);
		startMoveUpBlocks();
	}

	public void startMoveUpBlocks()
	{
		foreach (GameObject block in _currentBlocks) 
		{
			BlockTasksController blockTypeComponent = block.GetComponent<BlockTasksController>();
			if (blockTypeComponent.placed == true) 
			{
				blockTypeComponent.placed = false;
			}
		}
	}

	void checkGameResult()
	{
		foreach (GameObject block in _currentBlocks) 
		{
			BlockTasksController blockTypeComponent = block.GetComponent<BlockTasksController>();
			if (blockTypeComponent.placed == true && block.transform.localPosition.y > (_loseHeight * _blockHeight)) 
			{
				_lose = true;
				stopGame = true;
				endGameController.endMainGame();
				break;
			}
		}
	}

	public void setBlocksSpeed(float aSpeed)
	{
		_blocksSpeed = aSpeed; 
		if(_spawnTime > 0)
		{
			float minSpeed = _blockHeight / _spawnTime;
			if (aSpeed <= (minSpeed + 0.001f )) {
				_blocksSpeed = minSpeed + 0.01f;
			}
		}
	}

	public void setBlocksSpawnTime(int aSpawnTime)
	{
		_currentSpawnTime = 0;
		_spawnTime = aSpawnTime; 
		if(_blocksSpeed > 0.0)
		{
			int minSpawnTime = (int) (_blockHeight / _blocksSpeed);
			if (aSpawnTime < minSpawnTime) {
				_spawnTime = minSpawnTime + 1;
			}
		}
	}

	public void pauseGame()
	{
		stopGame = true;
	}

	public void resumeGame()
	{
		GamePlayerDataController playerData = ServicesLocator.getServiceForKey(typeof(GamePlayerDataController).Name) as GamePlayerDataController;
		if(playerData.completedTutorial == true && pauseGamePopUp.activeSelf == false)
		{
			stopGame = false;
		}
	}

	public void backToSelectLevel()
	{
		saveCollectedPlayerScore();
		fadingController.startFade(gameSettings.mainMenuScreenName, false);
	}

	public void saveCollectedPlayerScore()
	{
		GamePlayerDataController playerData = ServicesLocator.getServiceForKey(typeof(GamePlayerDataController).Name) as GamePlayerDataController;
		int scoreCount = 0;

		if(playerData.selectedLevelIndex == 0)
		{
			scoreCount = _score;
		}

		if(playerData.selectedLevelIndex == 1)
		{
			scoreCount = _score * 2;
		}

		if(playerData.selectedLevelIndex == 2)
		{
			scoreCount = _score * 3;
		}

		playerData.playerScore += scoreCount;
		playerData.savePlayerData();
	}

}
