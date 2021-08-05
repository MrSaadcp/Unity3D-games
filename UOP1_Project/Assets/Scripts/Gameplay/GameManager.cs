using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

	[SerializeField]
	private QuestManagerSO _questManager = default;

	[SerializeField]
	private GameStateSO _gameState = default;

	[SerializeField]
	private VoidEventChannelSO _addRockCandyRecipeEvent = default;
	[SerializeField]
	private VoidEventChannelSO _cerisesMemoryEvent = default;
	[SerializeField]
	private VoidEventChannelSO _decideOnDishesEvent = default;

	[SerializeField]
	private ItemSO _rockCandyRecipe = default;
	[SerializeField]
	private ItemSO _sweetDoughRecipe = default;
	[SerializeField]
	private ItemSO[] _finalRecipes = default;

	[SerializeField]
	private InventorySO _inventory = default;

	private void Start()
	{
		StartGame();

	}
	private void OnEnable()
	{
		_addRockCandyRecipeEvent.OnEventRaised += AddRockCandyRecipe;
		_cerisesMemoryEvent.OnEventRaised += AddSweetDoughRecipe;
		_decideOnDishesEvent.OnEventRaised += AddFinalRecipes;
	}
	private void OnDisable()
	{
		_addRockCandyRecipeEvent.OnEventRaised -= AddRockCandyRecipe;
		_cerisesMemoryEvent.OnEventRaised -= AddSweetDoughRecipe;
		_decideOnDishesEvent.OnEventRaised -= AddFinalRecipes;

	}
	void AddRockCandyRecipe()
	{
		Debug.Log("Add rock candy Recipe ");
		_inventory.Add(_rockCandyRecipe);

	}
	void AddSweetDoughRecipe()
	{
		Debug.Log("Add sweet dough Recipe ");
		_inventory.Add(_sweetDoughRecipe);

	}
	void AddFinalRecipes()
	{
		foreach (ItemSO item in _finalRecipes)
		{
			_inventory.Add(item);
		}

	}
	// Start is called before the first frame update
	void StartGame()
	{
		_gameState.UpdateGameState(GameState.Gameplay);
		_questManager.StartGame();
	}
	public void PauseGame()
	{
	}
	public void UnpauseGame()
	{
		_gameState.ResetToPreviousGameState();
	}

}
