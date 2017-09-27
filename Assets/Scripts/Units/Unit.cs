﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{

	public int x, y, movement = 2, range = 1;

	public int attack = 2, defense = 1, hp = 10;

	bool isSelected = false;

	public bool hasAttacked = false;

	public Vector2 gridPosition
	{
		get
		{
			return new Vector2(x, y);
		}
	}

	void Start ()
	{
		transform.position = GridManager.gridMan.tiles[x, y].position;
	}
	
	void Update ()
	{
		
	}

	protected void FindTraversableTiles()
	{
		///<summary>
		/// Primitive pathfinding implementation.
		/// Code will look left, right, up, down, diagonal for tiles it can move to.
		/// </summary>
		
		for (int i = -1; i < 2; i++)
		{
			for (int j = -1; j < 2; j++)
			{
				// we use nested for loops bounded from -1 to 1, which allows us to look at all adjacent tiles to our current position.
				// To do this, we call x + i, x + j. For instance, [4 + -1, 4 + -1] will check the tile [3, 3]. The loop is nested because
				// the next iteration will do [4 + -1, 4 + 0], which checks tile [3, 4].

				if (i == 0 && j == 0)
					continue; // this continues loop if our [x + i, y + j] is equal to our current position. We don't want to check our pos.

				// The below if statement performs bounds checking to make sure our next tile is within the bounds of our 2D tile array.
				// tiles.GetLength(0) grabs the 0th dimension of the tiles array and returns it's length.
				// For instance, if tiles is size [3][4]. tiles.GetLength(0) will return 3. GetLength(1) will return 4

				if (x + i > GridManager.gridMan.tiles.GetLength(0) - 1 || x + i < 0 || y + j > GridManager.gridMan.tiles.GetLength(1) - 1 || y + j < 0)
					continue;

				Tile currentTile = GridManager.gridMan.tiles[x + i, y + j]; // Set our current tile. We don't start with our unit's pos.

				float totalCost = 0; // initialize our totalCost variable. This will keep a running tally of the movement cost in a direction.

				while (totalCost <= movement)
				{
					// This loop will run as long as total cost does not exceed our unit's movement.

					// the below equation adds the distance from our currentTile to our previous tile and adds the movementModifier of the current tile. The weird division black magic is necessary due to the way the GridLayoutManager is implemented. In most circumstances, you would just run Vector2.Distance between one position and another position.
					totalCost += (Vector2.Distance(currentTile.gridPosition, GridManager.gridMan.tiles[currentTile.x - i, currentTile.y - j].gridPosition) / GridManager.gridMan.grid.cellSize.x) + currentTile.movementModifier;

					if (totalCost <= movement && currentTile.unit == null)
						// If our cost has not exceeded movement and no unit is on the current tile, then we can move to this tile.
						currentTile.SetTraversable(); 
					else if (totalCost > movement)
						// If our totalCost has exceeded our movement, then we can't access the currentTile. Break loop and start next search
						break;

					// bounds checking again
					if (currentTile.x + i > GridManager.gridMan.tiles.GetLength(0) - 1 || currentTile.x + i < 0 || currentTile.y + j > GridManager.gridMan.tiles.GetLength(1) - 1 || currentTile.y + j < 0)
						break;

					currentTile = GridManager.gridMan.tiles[currentTile.x + i, currentTile.y + j]; // assign next tile
				}
			}
		}
	}
	
	private void OnMouseDown()
	{
		///<summary>
		/// This function is called whenever a unit is clicked on with the mouse. We need this function to have several if statements
		/// 
		/// 1. If Statement 1 will set our selected unit and find traversible tiles for movement.
		///		- If this unit is not selected AND has not attacked AND the Unit Manager's selectedUnit is null, THEN set this unit toselected, set the Unit Manager's selectedUnit to this, and FindTraversibleTiles()
		///	
		/// 2. else if statement 1 will commence the AttackRoutine() function.
		///		- If this unit is not selected AND Unit MAnager's selectedUnit is NOT this, AND Unit Manager's selectedUnit is NOT null, THEN call AttackRoutine()
		///	
		/// 3. else if statement 2 will cancel unit selection if we click on the selected unit again.
		///		- If this unit is selected, THEN set this unit to not selected, set Unit Manager's selectedUnit to null
		/// </summary>
		if (!isSelected && !hasAttacked && UnitManager.unitManager.selectedUnit == null)
		{
			// if no unit is selected at all, select this unit for movement/attacking.
			isSelected = true;
			UnitManager.unitManager.selectedUnit = this;
			FindTraversableTiles();
			//Move();
		}
		else if(!isSelected && UnitManager.unitManager.selectedUnit != this && UnitManager.unitManager.selectedUnit != null /* && UnitManager.selectedUnit.player != this.player*/)
		{
			// This means a different unit is selected and they are attempting to attack this unit
			AttackRoutine(UnitManager.unitManager.selectedUnit);
		}
		else if (isSelected)
		{
			// if this unit is selected and we click on it again, cancel selection
			isSelected = false;
			UnitManager.unitManager.selectedUnit = null;
		}
	}

	public void IncreaseStats(int attackMod, int defenseMod)
	{
		///<summary>
		/// This function will be called on by the Tile that the unit enters
		/// Tile will pass it's attack and defense mods to this function and the unit will update their stats accordingly.
		/// </summary>
		
		attack += attackMod;
		defense += defenseMod;
	}

	public void DecreaseStats(int attackMod, int defenseMod)
	{
		///<summary>
		/// This function will be called on by the Tile that the unit leaves
		/// Tile will pass it's attack and defense mods to this function and the unit will update their stats accordingly.
		/// </summary>
		
		attack -= attackMod;
		defense -= defenseMod;
	}

	void HasAttacked()
	{
		///<summary>
		/// This function will do three things:
		/// 1. Set isSelected to false
		/// 2. set hasAttacked to true
		/// 3. set the Unit Manager's selectedUnit to null
		/// 
		/// This function will be called at the end of the attack routine. It will be called on the attacking unit.
		/// E.G:    attackingUnit.HasAttacked();
		/// </summary>
		isSelected = false;
		hasAttacked = true;
		UnitManager.unitManager.selectedUnit = null;
	}

	void AttackRoutine(Unit attackingUnit)
	{
		// check if attacker is in range of this unit
		if (!attackingUnit.CheckAttackRange(this))
			return; // exit the function if not in range

		// If we make it here, then we're in range. Execute unit attack against this unit
		attackingUnit.Attack(this); // attack unit and make it take damage

		// now the unit gets to return fire if they are in range
		if (!this.CheckAttackRange(attackingUnit))
			return;

		// if we make it here, then defender is in range. Execute a counter attack against attacking unit
		this.Attack(attackingUnit);

		// if we make it all the way here, then we can set the selectedUnit's turn to be over
		attackingUnit.HasAttacked();
	}

	bool CheckAttackRange(Unit defendingUnit)
	{
		/// <summary>
		/// Check if enemy unit is in range to attack. Returns true if distance is within range. False otherwise.
		/// Terrain modifiers for movement are not considered. This is a standard distance calculation with no weights.
		/// </summary>
		
		if (Vector2.Distance(gridPosition, defendingUnit.gridPosition) > this.range)
			return false;
		else
			return true;
	}

	void Attack(Unit defendingUnit)
	{
		/// <summary>
		/// call TakeDamage on defendingUnit. Pass the attacking unit's attack value as a parameter
		/// We don't currently need this function. We could just call TakeDamage directly in the AttackRoutine function
		/// However, if we want to create some kind of Attack state, which can dictate animations, we will want a separate function like this
		/// </summary> 
		
		defendingUnit.TakeDamage(this.attack, this.hp);
	}

	void TakeDamage(int attackDamage, int attackerHP)
	{
		///<summary>
		/// Perform damage calculations against the unit that calls this function.
		/// 
		/// This function should implement our equations we came up with
		/// 
		/// NOTE: attackDamage variable includes terrain modifiers since Tile class is handling updating those stats. Yo udo not have to worry about handling those modifiers in this function
		/// 
		/// ANOTHER NOTE: Casting gets tricky in this function. We only want to remove the decimal place when we go to subtract totalDamageDone from hp. the totalDamageDone variable needs to have decimal precision. However, keep in mind that all our variables we're working with are integers. 
		/// 
		/// My hint would be that you can create float values in C# by putting an 'f' at the end of a number. 1f, 3f, 2.5f, etc. Remember, if you have integers performing any math with a floating number, the result will be cast to a float.
		/// 
		/// Additionally, you can cast values in C# by putting (type) in front of the variable you want to cast. For instance
		/// 
		/// float x = (float) 3; // this will turn x into 3.0
		/// 
		/// 1.	totalDamageDone = equation we came up with.
		/// 2.	If statement 1: if totalDamageDone less than 0.5, THEN totalDamageDone = 0
		/// 3.	if statement 2: if totalDamageDone greater or equal to 0.5 AND above result less than 1, THEN totalDamageDone = 1
		/// 4.	subtract totalDamageDone from defending unit's hp
		/// 5.	For testing, I recommend putting a Debug.Log(transform.name + " Took " + finalDamage + " Damage") statement at end of function.
		/// 
		/// </summary>

		float totalDamage = attackDamage * ((attackerHP / 10f) * (attackDamage / this.defense));

		if (totalDamage < 0.5f)
			totalDamage = 0;
		else if (totalDamage >= 0.5f && totalDamage < 1)
			totalDamage = 1f;

		hp -= (int)totalDamage;
		Debug.Log(transform.name + " Took " + (int)totalDamage + " Damage");
	}


}
